from __future__ import annotations

import json
import os
from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Any, Dict, List, Literal, Optional
from fastapi.staticfiles import StaticFiles

import requests
import yfinance as yf
from cachetools import TTLCache
from dotenv import load_dotenv
from fastapi import FastAPI
from pydantic import BaseModel, Field, ValidationError


from db import get_snapshot, init_db, list_snapshots, save_snapshot
from fastapi.staticfiles import StaticFiles
from fastapi.middleware.cors import CORSMiddleware

# -------------------------------------------------
# Environment
# -------------------------------------------------
load_dotenv()

app = FastAPI(title="Graham Valuation API", version="0.4.0")
# Local dev CORS (optional, but helpful if you later move UI elsewhere)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://127.0.0.1:8000", "http://localhost:8000"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Serve the dashboard
app.mount("/ui", StaticFiles(directory="static", html=True), name="ui")


# -------------------------------------------------
# Startup
# -------------------------------------------------
@app.on_event("startup")
def startup():
    init_db()

# -------------------------------------------------
# Caches
# -------------------------------------------------
QUOTE_CACHE = TTLCache(maxsize=5_000, ttl=int(os.getenv("QUOTE_TTL_SECONDS", "300")))
YIELD_CACHE = TTLCache(maxsize=100, ttl=int(os.getenv("YIELD_TTL_SECONDS", "3600")))

# -------------------------------------------------
# Utilities
# -------------------------------------------------
def _now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()

# -------------------------------------------------
# Ollama helpers
# -------------------------------------------------
def _ollama_url() -> str:
    return os.getenv("OLLAMA_URL", "http://127.0.0.1:11434").rstrip("/")


def _ollama_model(req_model: Optional[str]) -> str:
    # prefer req override; fall back to env; default matches your local tag name without ":latest"
    return req_model or os.getenv("OLLAMA_MODEL", "llama3.1")


def _extract_json_object(text: str) -> str:
    """If model wraps JSON in extra text, extract the outermost {...}."""
    text = (text or "").strip()
    start = text.find("{")
    end = text.rfind("}")
    if start != -1 and end != -1 and end > start:
        return text[start : end + 1]
    return text


def _call_ollama_raw(prompt: str, model: str, temperature: float = 0.2, num_predict: int = 600) -> str:
    url = f"{_ollama_url()}/api/generate"
    payload = {
        "model": model,
        "prompt": prompt,
        "stream": False,
        "options": {"temperature": temperature, "num_predict": num_predict},
    }
    r = requests.post(url, json=payload, timeout=120)
    r.raise_for_status()
    return (r.json().get("response") or "").strip()


def _call_ollama_json(prompt: str, model: str) -> dict:
    """Call Ollama, return parsed JSON dict; retries once with a fixer prompt if needed."""
    full_prompt = (
        prompt
        + "\n\nIMPORTANT: Return ONLY valid JSON. No markdown. No extra text.\n"
        + 'JSON shape: {"verdict":"consider|neutral|avoid|insufficient_data",'
        + '"bullets":["..."],"risks":["..."],"what_to_watch":["..."]}\n'
    )

    text = _call_ollama_raw(full_prompt, model=model, temperature=0.2, num_predict=650)
    candidate = _extract_json_object(text)

    try:
        return json.loads(candidate)
    except json.JSONDecodeError:
        # One repair attempt (very common with local LLMs)
        fix_prompt = (
            "Fix the following so it becomes VALID JSON ONLY.\n"
            "No markdown. No commentary. Output ONLY the corrected JSON.\n\n"
            f"BAD_OUTPUT:\n{text}\n"
        )
        text2 = _call_ollama_raw(fix_prompt, model=model, temperature=0.0, num_predict=500)
        candidate2 = _extract_json_object(text2)
        return json.loads(candidate2)


def _repair_ollama_to_schema(bad_output_text: str, model: str) -> dict:
    """
    Ask Ollama to repair output to match exact schema + list sizes.
    Used when JSON parses but fails validation.
    """
    fix_prompt = (
        "Return ONLY valid JSON with EXACTLY these keys:\n"
        '{"verdict":"consider|neutral|avoid|insufficient_data",'
        '"bullets":[3-6 strings],"risks":[3-6 strings],"what_to_watch":[3-6 strings]}\n\n'
        "Rules:\n"
        "- No markdown, no commentary, only JSON.\n"
        "- Ensure bullets/risks/what_to_watch each has 3 to 6 items.\n"
        "- Do not add any metrics beyond the provided fields.\n\n"
        f"BAD_OUTPUT:\n{bad_output_text}\n"
    )
    text = _call_ollama_raw(fix_prompt, model=model, temperature=0.0, num_predict=650)
    candidate = _extract_json_object(text)
    return json.loads(candidate)

# -------------------------------------------------
# Graham math
# -------------------------------------------------
def graham_intrinsic_value(
    eps_ttm: float,
    expected_growth_pct: float,
    bond_yield_pct: float,
) -> Optional[float]:
    if eps_ttm <= 0 or bond_yield_pct <= 0:
        return None
    return (eps_ttm * (8.5 + 2 * expected_growth_pct) * 4.4) / bond_yield_pct


def margin_of_safety(price: float, intrinsic_value: float) -> Optional[float]:
    if price <= 0 or intrinsic_value <= 0:
        return None
    return 1.0 - (price / intrinsic_value)

# -------------------------------------------------
# Provenance
# -------------------------------------------------
@dataclass
class FieldProvenance:
    field: str
    source: str
    as_of: str
    notes: Optional[str] = None

# -------------------------------------------------
# Schemas
# -------------------------------------------------
class Quote(BaseModel):
    ticker: str
    price: Optional[float]
    eps_ttm: Optional[float]
    p_to_b: Optional[float]
    market_cap: Optional[float]
    currency: Optional[str]
    as_of: str
    provenance: List[Dict[str, Any]]


class YieldResponse(BaseModel):
    series_id: str
    yield_pct: Optional[float]
    as_of: str
    provenance: List[Dict[str, Any]]


class GrahamRequest(BaseModel):
    tickers: List[str] = Field(
        ...,
        min_length=1,
        json_schema_extra={"example": ["AAPL", "MSFT"]},
    )
    expected_growth_pct: float = Field(5.0, ge=0.0, le=30.0)
    bond_yield_pct: Optional[float] = Field(None, gt=0.0, le=25.0)
    bond_yield_series_id: str = "DGS10"
    investor_type: Literal["defensive", "enterprising"] = "enterprising"
    mos_threshold_enterprising: float = 25.0
    mos_threshold_defensive: float = 40.0


class GrahamResult(BaseModel):
    snapshot_id: Optional[str] = None
    ticker: str
    inputs: Dict[str, Any]
    computed: Dict[str, Any]
    recommendation: str
    quote_provenance: List[Dict[str, Any]]
    computed_provenance: List[Dict[str, Any]]


class ExplainRequest(BaseModel):
    snapshot_id: str
    investor_type: Literal["defensive", "enterprising"] = "enterprising"
    mos_threshold_enterprising: float = 25.0
    mos_threshold_defensive: float = 40.0


class ExplainResponse(BaseModel):
    snapshot_id: str
    ticker: str
    created_at_utc: str
    investor_type: Literal["defensive", "enterprising"]
    key_numbers: Dict[str, Any]
    verdict: str
    bullets: List[str]
    risks: List[str]
    what_to_watch: List[str]
    llm_prompt: str


class LLMExplainRequest(BaseModel):
    snapshot_id: str
    investor_type: Literal["defensive", "enterprising"] = "enterprising"
    model: Optional[str] = None


class LLMExplainOutput(BaseModel):
    verdict: Literal["consider", "neutral", "avoid", "insufficient_data"]
    bullets: List[str] = Field(..., min_length=3, max_length=6)
    risks: List[str] = Field(..., min_length=3, max_length=6)
    what_to_watch: List[str] = Field(..., min_length=3, max_length=6)


class LLMExplainResponse(BaseModel):
    snapshot_id: str
    ticker: str
    created_at_utc: str
    provider: str
    model: str
    output: LLMExplainOutput

class ScreenRequest(BaseModel):
    tickers: List[str] = Field(..., min_length=1)
    expected_growth_pct: float = Field(5.0, ge=0.0, le=30.0)
    bond_yield_pct: Optional[float] = Field(None, gt=0.0, le=25.0)
    bond_yield_series_id: str = "DGS10"
    investor_type: Literal["defensive", "enterprising"] = "enterprising"
    mos_threshold_enterprising: float = 25.0
    mos_threshold_defensive: float = 40.0

    # screening controls
    min_mos_pct: Optional[float] = Field(None, ge=-100.0, le=200.0)
    require_positive_eps: bool = True
    limit: int = Field(20, ge=1, le=200)


class ScreenResponse(BaseModel):
    bond_yield_used: Optional[float]
    sorted_by: str
    results: List[GrahamResult]

# -------------------------------------------------
# Yahoo Finance
# -------------------------------------------------
def get_quote_yahoo(ticker: str) -> Quote:
    t = ticker.upper().strip()
    now = _now_iso()

    if t in QUOTE_CACHE:
        q = QUOTE_CACHE[t]
        return Quote(**{**q.model_dump(), "as_of": now})

    info = yf.Ticker(t).info or {}

    price = info.get("regularMarketPrice") or info.get("currentPrice")
    eps = info.get("trailingEps") or info.get("epsTrailingTwelveMonths")

    quote = Quote(
        ticker=t,
        price=price,
        eps_ttm=eps,
        p_to_b=info.get("priceToBook"),
        market_cap=info.get("marketCap"),
        currency=info.get("currency"),
        as_of=now,
        provenance=[
            FieldProvenance("price", "yahoo_finance", now).__dict__,
            FieldProvenance("eps_ttm", "yahoo_finance", now).__dict__,
        ],
    )

    QUOTE_CACHE[t] = quote
    return quote

# -------------------------------------------------
# FRED
# -------------------------------------------------
def get_fred_yield(series_id: str) -> YieldResponse:
    sid = series_id.upper()
    now = _now_iso()

    cache_key = f"fred:{sid}"
    if cache_key in YIELD_CACHE:
        cached: YieldResponse = YIELD_CACHE[cache_key]
        return YieldResponse(**{**cached.model_dump(), "as_of": now})

    api_key = os.getenv("FRED_API_KEY")
    if not api_key:
        y = YieldResponse(
            series_id=sid,
            yield_pct=None,
            as_of=now,
            provenance=[FieldProvenance("yield_pct", "fred", now, "API key missing").__dict__],
        )
        YIELD_CACHE[cache_key] = y
        return y

    try:
        r = requests.get(
            "https://api.stlouisfed.org/fred/series/observations",
            params={
                "series_id": sid,
                "api_key": api_key,
                "file_type": "json",
                "sort_order": "desc",
                "limit": 1,
            },
            timeout=15,
        )
        r.raise_for_status()
        data = r.json()
        obs = data.get("observations", [])
        val = float(obs[0]["value"]) if obs and obs[0]["value"] not in (".", None) else None

        y = YieldResponse(
            series_id=sid,
            yield_pct=val,
            as_of=now,
            provenance=[FieldProvenance("yield_pct", "fred", now).__dict__],
        )
        YIELD_CACHE[cache_key] = y
        return y

    except Exception as e:
        y = YieldResponse(
            series_id=sid,
            yield_pct=None,
            as_of=now,
            provenance=[FieldProvenance("yield_pct", "fred", now, str(e)).__dict__],
        )
        YIELD_CACHE[cache_key] = y
        return y

# -------------------------------------------------
# Explain builder
# -------------------------------------------------
def build_graham_explanation(snapshot: dict, req: ExplainRequest) -> ExplainResponse:
    ticker = snapshot["ticker"]
    created_at = snapshot["created_at_utc"]

    result = snapshot.get("result") or {}
    inputs = result.get("inputs") or {}
    computed = result.get("computed") or {}

    price = inputs.get("price")
    eps = inputs.get("eps_ttm")
    growth = inputs.get("growth_pct")
    yld = inputs.get("bond_yield_pct")

    iv = computed.get("intrinsic_value_graham")
    mos_pct = computed.get("margin_of_safety_pct")

    threshold = req.mos_threshold_enterprising if req.investor_type == "enterprising" else req.mos_threshold_defensive

    verdict = "insufficient_data"
    if mos_pct is not None:
        if mos_pct < 0:
            verdict = "avoid"
        elif mos_pct >= threshold:
            verdict = "consider"
        else:
            verdict = "neutral"

    bullets: List[str] = []
    risks: List[str] = []
    what_to_watch: List[str] = []

    if price is not None:
        bullets.append(f"Market price at snapshot: {price:.2f}.")
    if iv is not None:
        bullets.append(f"Graham intrinsic value estimate (baseline): {iv:.2f}.")
    if mos_pct is not None:
        bullets.append(f"Margin of safety: {mos_pct:.2f}% (threshold for {req.investor_type}: {threshold:.0f}%).")
    else:
        bullets.append("Margin of safety could not be computed (missing EPS, yield, or price).")

    if mos_pct is not None:
        if mos_pct >= threshold:
            bullets.append("Meets margin-of-safety requirement; candidate for deeper fundamental review.")
        elif mos_pct >= 0:
            bullets.append("Does not meet margin-of-safety requirement; consider watchlist or limit price.")
        else:
            bullets.append("Trading above the intrinsic estimate; Graham-style discipline suggests avoiding at this price.")

    if eps is None or (isinstance(eps, (int, float)) and eps <= 0):
        risks.append("EPS is missing or non-positive; Graham valuation formula cannot be trusted here.")
    else:
        risks.append("EPS can be volatile or affected by accounting; confirm earnings quality from filings.")

    risks.append("Graham formula is a baseline heuristic; validate with balance sheet strength and earnings stability.")
    risks.append("Bond yield choice matters; different yield series changes intrinsic value materially.")

    what_to_watch.append("Re-run valuation after next earnings (EPS changes).")
    what_to_watch.append("Track bond yields (the discount rate input can move intrinsic value).")
    what_to_watch.append("Set a target ‘buy-under’ price based on desired margin of safety.")

    llm_prompt = (
        "You are a value-investing analyst applying Benjamin Graham principles.\n"
        "You MUST base your analysis ONLY on the numeric fields explicitly listed below.\n"
        "You are NOT allowed to invent, infer, assume, or reference any other metrics.\n\n"
        "Snapshot metadata:\n"
        f"- snapshot_id: {snapshot['snapshot_id']}\n"
        f"- ticker: {ticker}\n"
        f"- created_at_utc: {created_at}\n\n"
        "Allowed numeric fields (the ONLY numbers you may reference):\n"
        f"- price: {price}\n"
        f"- eps_ttm: {eps}\n"
        f"- expected_growth_pct: {growth}\n"
        f"- bond_yield_pct: {yld}\n"
        f"- intrinsic_value_graham: {iv}\n"
        f"- margin_of_safety_pct: {mos_pct}\n"
        f"- threshold_pct: {threshold}\n\n"
        "Hard rules (must follow):\n"
        "- Do NOT mention or imply P/E, P/B, valuation multiples, market cap, revenue, products, innovation, or business quality.\n"
        "- Do NOT mention any numbers other than the allowed numeric fields above.\n"
        "- Every bullet MUST explicitly cite at least one field name in parentheses, e.g. (price, intrinsic_value_graham).\n"
        "- If a value is null or missing, state that it cannot be evaluated.\n"
        "- Be concise and analytical, not promotional.\n\n"
        f"Investor type: {req.investor_type}\n"
        f"Margin-of-safety threshold: {threshold}%\n\n"
        "Return STRICT JSON ONLY with this exact shape:\n"
        "{\n"
        '  "verdict": "consider | neutral | avoid | insufficient_data",\n'
        '  "bullets": ["...", "...", "..."],\n'
        '  "risks": ["...", "...", "..."],\n'
        '  "what_to_watch": ["...", "...", "..."]\n'
        "}\n\n"
        "Interpretation guidance:\n"
        '- If margin_of_safety_pct < 0, verdict should usually be "avoid".\n'
        '- If margin_of_safety_pct >= threshold_pct, verdict may be "consider".\n'
        '- Otherwise, verdict should usually be "neutral".\n'
    )

    key_numbers = {
        "price": price,
        "eps_ttm": eps,
        "expected_growth_pct": growth,
        "bond_yield_pct": yld,
        "intrinsic_value_graham": iv,
        "margin_of_safety_pct": mos_pct,
        "threshold_pct": threshold,
    }

    return ExplainResponse(
        snapshot_id=snapshot["snapshot_id"],
        ticker=ticker,
        created_at_utc=created_at,
        investor_type=req.investor_type,
        key_numbers=key_numbers,
        verdict=verdict,
        bullets=bullets,
        risks=risks,
        what_to_watch=what_to_watch,
        llm_prompt=llm_prompt,
    )

# -------------------------------------------------
# Endpoints
# -------------------------------------------------
@app.get("/health")
def health():
    return {"status": "ok", "time": _now_iso()}


@app.get("/v1/quote/{ticker}", response_model=Quote)
def quote(ticker: str):
    return get_quote_yahoo(ticker)


@app.get("/v1/yield", response_model=YieldResponse)
def yield_endpoint(series_id: str = "DGS10"):
    return get_fred_yield(series_id)


@app.post("/v1/graham/valuate", response_model=List[GrahamResult])
def graham_valuate(req: GrahamRequest):
    results: List[GrahamResult] = []

    yield_resp = (
        YieldResponse(series_id="manual", yield_pct=req.bond_yield_pct, as_of=_now_iso(), provenance=[])
        if req.bond_yield_pct
        else get_fred_yield(req.bond_yield_series_id)
    )

    for ticker in req.tickers:
        q = get_quote_yahoo(ticker)

        iv = (
            graham_intrinsic_value(q.eps_ttm, req.expected_growth_pct, yield_resp.yield_pct)
            if q.eps_ttm and yield_resp.yield_pct
            else None
        )
        mos = margin_of_safety(q.price, iv) if iv and q.price else None
        mos_pct = mos * 100 if mos is not None else None

        recommendation = "insufficient_data"
        if mos_pct is not None:
            threshold = (
                req.mos_threshold_enterprising
                if req.investor_type == "enterprising"
                else req.mos_threshold_defensive
            )
            recommendation = "consider" if mos_pct >= threshold else "neutral"
            if mos_pct < 0:
                recommendation = "avoid"

        result = GrahamResult(
            ticker=ticker,
            inputs={
                "price": q.price,
                "eps_ttm": q.eps_ttm,
                "growth_pct": req.expected_growth_pct,
                "bond_yield_pct": yield_resp.yield_pct,
            },
            computed={
                "intrinsic_value_graham": iv,
                "margin_of_safety_pct": mos_pct,
            },
            recommendation=recommendation,
            quote_provenance=q.provenance,
            computed_provenance=yield_resp.provenance,
        )

        snapshot_id = save_snapshot(
            ticker=ticker,
            request_obj=req.model_dump(),
            quote_obj=q.model_dump(),
            result_obj=result.model_dump(),
        )
        result.snapshot_id = snapshot_id
        results.append(result)

    return results


@app.get("/v1/snapshots")
def snapshots(ticker: Optional[str] = None, limit: int = 20):
    return list_snapshots(ticker, limit)


@app.get("/v1/snapshots/{snapshot_id}")
def snapshot(snapshot_id: str):
    s = get_snapshot(snapshot_id)
    return s or {"error": "not_found", "snapshot_id": snapshot_id}


@app.post("/v1/explain", response_model=ExplainResponse)
def explain(req: ExplainRequest):
    s = get_snapshot(req.snapshot_id)
    if not s:
        return ExplainResponse(
            snapshot_id=req.snapshot_id,
            ticker="",
            created_at_utc="",
            investor_type=req.investor_type,
            key_numbers={},
            verdict="not_found",
            bullets=["Snapshot not found."],
            risks=[],
            what_to_watch=[],
            llm_prompt="",
        )
    return build_graham_explanation(s, req)


@app.post("/v1/explain_llm", response_model=LLMExplainResponse)
def explain_llm(req: LLMExplainRequest):
    provider = "ollama"
    cached = get_llm_explanation(req.snapshot_id, req.investor_type, model, provider)
    if cached:
        return LLMExplainResponse(
            snapshot_id=req.snapshot_id,
            ticker=snap["ticker"],
            created_at_utc=snap["created_at_utc"],
            provider=provider,
            model=model,
            output=LLMExplainOutput.model_validate(cached["output"]),
        )

    snap = get_snapshot(req.snapshot_id)
    model = _ollama_model(req.model)

    if not snap:
        # satisfy min_length constraints
        return LLMExplainResponse(
            snapshot_id=req.snapshot_id,
            ticker="",
            created_at_utc="",
            provider="ollama",
            model=model,
            output=LLMExplainOutput(
                verdict="insufficient_data",
                bullets=["Snapshot not found.", "Snapshot not found.", "Snapshot not found."],
                risks=["N/A", "N/A", "N/A"],
                what_to_watch=["N/A", "N/A", "N/A"],
            ),
        )

    explain_req = ExplainRequest(snapshot_id=req.snapshot_id, investor_type=req.investor_type)
    explain_resp = build_graham_explanation(snap, explain_req)

    # First attempt
    try:
        raw = _call_ollama_json(explain_resp.llm_prompt, model)
        output = LLMExplainOutput.model_validate(raw)
    except (ValidationError, json.JSONDecodeError) as e:
        # Repair once (common: wrong list lengths, extra keys, etc.)
        try:
            bad_text = json.dumps(raw, ensure_ascii=False) if "raw" in locals() else str(e)
            repaired = _repair_ollama_to_schema(bad_text, model)
            output = LLMExplainOutput.model_validate(repaired)
        except Exception as e2:
            output = LLMExplainOutput(
                verdict="insufficient_data",
                bullets=[f"LLM output invalid: {type(e).__name__}", "See server logs.", "Retry later."],
                risks=["N/A", "N/A", "N/A"],
                what_to_watch=["N/A", "N/A", "N/A"],
            )
    except Exception as e:
        output = LLMExplainOutput(
            verdict="insufficient_data",
            bullets=[f"LLM call failed: {type(e).__name__}: {e}", "See server logs.", "Retry later."],
            risks=["N/A", "N/A", "N/A"],
            what_to_watch=["N/A", "N/A", "N/A"],
        )

    return LLMExplainResponse(
        snapshot_id=req.snapshot_id,
        ticker=snap["ticker"],
        created_at_utc=snap["created_at_utc"],
        provider="ollama",
        model=model,
        output=output,
    )
@app.post("/v1/graham/screen", response_model=ScreenResponse)
def graham_screen(req: ScreenRequest):
    # decide which bond yield to use
    yield_resp = (
        YieldResponse(series_id="manual", yield_pct=req.bond_yield_pct, as_of=_now_iso(), provenance=[])
        if req.bond_yield_pct
        else get_fred_yield(req.bond_yield_series_id)
    )
    bond_yield = yield_resp.yield_pct

    results: List[GrahamResult] = []

    for ticker in req.tickers:
        q = get_quote_yahoo(ticker)

        # optional filter: positive EPS
        if req.require_positive_eps and (q.eps_ttm is None or q.eps_ttm <= 0):
            continue

        iv = (
            graham_intrinsic_value(q.eps_ttm, req.expected_growth_pct, bond_yield)
            if (q.eps_ttm is not None and bond_yield is not None)
            else None
        )
        mos = margin_of_safety(q.price, iv) if (q.price is not None and iv is not None) else None
        mos_pct = (mos * 100.0) if mos is not None else None

        # optional MoS filter
        if req.min_mos_pct is not None:
            if mos_pct is None or mos_pct < req.min_mos_pct:
                continue

        threshold = (
            req.mos_threshold_enterprising
            if req.investor_type == "enterprising"
            else req.mos_threshold_defensive
        )

        recommendation = "insufficient_data"
        if mos_pct is not None:
            recommendation = "consider" if mos_pct >= threshold else "neutral"
            if mos_pct < 0:
                recommendation = "avoid"

        result = GrahamResult(
            ticker=q.ticker,
            inputs={
                "price": q.price,
                "eps_ttm": q.eps_ttm,
                "growth_pct": req.expected_growth_pct,
                "bond_yield_pct": bond_yield,
            },
            computed={
                "intrinsic_value_graham": iv,
                "margin_of_safety_pct": mos_pct,
            },
            recommendation=recommendation,
            quote_provenance=q.provenance,
            computed_provenance=yield_resp.provenance,
        )

        snapshot_id = save_snapshot(
            ticker=q.ticker,
            request_obj=req.model_dump(),
            quote_obj=q.model_dump(),
            result_obj=result.model_dump(),
        )
        result.snapshot_id = snapshot_id
        results.append(result)

    # sort by margin of safety descending (None last)
    results.sort(
        key=lambda r: (
            r.computed.get("margin_of_safety_pct") is None,
            -(r.computed.get("margin_of_safety_pct") or -1e12),
        )
    )

    results = results[: req.limit]

    return ScreenResponse(
        bond_yield_used=bond_yield,
        sorted_by="margin_of_safety_pct_desc",
        results=results,
    )
