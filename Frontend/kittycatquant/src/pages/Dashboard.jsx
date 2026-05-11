import { useEffect, useMemo, useRef, useState, useCallback } from "react";
import { useOutletContext } from "react-router-dom";
import LearnHint from "../components/LearnHint";
import "../styles/dashboard.css";
import PriceChart from "../components/PriceChart2";

const API_BASE = process.env.REACT_APP_API_BASE_URL || "";

const money = (n) =>
  typeof n === "number" && Number.isFinite(n)
    ? n.toLocaleString(undefined, { style: "currency", currency: "USD" })
    : "—";

function pctText(n) {
  if (typeof n !== "number" || !Number.isFinite(n)) return "—";
  const sign = n >= 0 ? "+" : "";
  return `${sign}${n.toFixed(2)}%`;
}

function formatTime(d) {
  if (!d) return "loading…";
  return d.toLocaleTimeString([], {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
}

function makeAuthHeaders(token) {
  const h = { "Content-Type": "application/json" };
  if (token) h.Authorization = `Bearer ${token}`;
  return h;
}

async function apiGet(path, token) {
  return fetch(`${API_BASE}${path}`, { headers: makeAuthHeaders(token) });
}

async function fetch2Bars(symbol, token) {
  const res = await apiGet(
    `/api/market/bars?symbol=${encodeURIComponent(symbol)}&tf=1d&limit=2`,
    token
  );

  if (!res.ok) {
    const txt = await res.text();
    throw new Error(`Bars(2) ${symbol} failed (${res.status}): ${txt || "no body"}`);
  }

  const body = await res.json();
  const bars = Array.isArray(body?.data) ? body.data : [];
  const sorted = [...bars].sort((a, b) => new Date(a.t) - new Date(b.t));

  const latest = sorted[sorted.length - 1];
  const prev = sorted.length > 1 ? sorted[sorted.length - 2] : null;

  const price = Number(latest?.c);
  const prevPrice = prev ? Number(prev?.c) : null;

  const change =
    prevPrice != null && Number.isFinite(prevPrice) ? price - prevPrice : 0;

  const changePct =
    prevPrice != null && Number.isFinite(prevPrice) && prevPrice !== 0
      ? (change / prevPrice) * 100
      : 0;

  return {
    symbol: String(symbol).toUpperCase(),
    price: Number.isFinite(price) ? price : null,
    change: Number.isFinite(change) ? change : null,
    changePct: Number.isFinite(changePct) ? changePct : null,
    updatedAt: new Date(),
  };
}

export default function Dashboard() {
  const token = localStorage.getItem("kcq_token");
  const storedUserId = localStorage.getItem("kcq_userId");
  const { learnMode } = useOutletContext();

  const userId = useMemo(() => {
    if (storedUserId) return storedUserId;

    try {
      const user = JSON.parse(localStorage.getItem("kcq_user") || "{}");
      return user.userId || "";
    } catch {
      return "";
    }
  }, [storedUserId]);

  const [tab, setTab] = useState("portfolio");

  const [cash, setCash] = useState(null);
  const [holdings, setHoldings] = useState([]);
  const [holdingValue, setHoldingValue] = useState(0);

  const [watchlists, setWatchlists] = useState([]);
  const [selectedWatchlistId, setSelectedWatchlistId] = useState(null);
  const [watchlistStocks, setWatchlistStocks] = useState([]);

  const [quotes, setQuotes] = useState([]);
  const [chartData, setChartData] = useState([]);
  const [portfolioLoading, setPortfolioLoading] = useState(true);

  const [error, setError] = useState(null);
  const [currentTime, setCurrentTime] = useState(new Date());

  const [enrichedStocks, setEnrichedStocks] = useState([]);
  const [loadingWatchlistStocks, setLoadingWatchlistStocks] = useState(false);
  const [loadingMarketData, setLoadingMarketData] = useState(false);

  const [portfolioId, setPortfolioId] = useState(
    localStorage.getItem("kcq_portfolioId")
  );

  const quotesTimer = useRef(null);
  const chartTimer = useRef(null);
  const portfolioTimer = useRef(null);
  const clockTimer = useRef(null);

  const selectedWatchlist = useMemo(
    () =>
      watchlists.find(
        (w) => String(w.id ?? w.watchlistId) === String(selectedWatchlistId)
      ) || null,
    [watchlists, selectedWatchlistId]
  );

  const selectedWatchlistName =
    selectedWatchlist?.name ??
    selectedWatchlist?.title ??
    "Watchlist";

  const watchlistSymbols = useMemo(() => {
    return (watchlistStocks || [])
      .map((stock) => stock.symbol?.ticker ?? stock.ticker ?? null)
      .filter(Boolean)
      .map((s) => String(s).trim().toUpperCase());
  }, [watchlistStocks]);

  const holdingsSymbols = useMemo(() => {
    return (holdings || [])
      .map((h) => h.ticker)
      .filter(Boolean)
      .map((s) => String(s).trim().toUpperCase());
  }, [holdings]);

  const quoteSymbols = useMemo(() => {
    return [...new Set([...holdingsSymbols, ...watchlistSymbols])];
  }, [holdingsSymbols, watchlistSymbols]);

  const priceMap = useMemo(() => {
    const m = new Map();
    (quotes || []).forEach((q) => m.set(q.symbol, q));
    return m;
  }, [quotes]);

  const holdingsValueLive = useMemo(() => {
    return (holdings || []).reduce((sum, h) => {
      const q = priceMap.get(String(h.ticker).toUpperCase());
      const px = q?.price;
      if (typeof px !== "number") return sum;
      return sum + px * (Number(h.quantity) || 0);
    }, 0);
  }, [holdings, priceMap]);

  const totalValue = useMemo(() => {
    const c = typeof cash === "number" ? cash : 0;
    const hv = quotes.length ? holdingsValueLive : holdingValue;
    return c + (typeof hv === "number" ? hv : 0);
  }, [cash, holdingValue, holdingsValueLive, quotes.length]);

  const fetchWatchlists = useCallback(async () => {
    setError("");

    try {
      if (!userId) throw new Error("No logged-in user found.");

      const res = await apiGet(
        `/api/watchlists?userId=${encodeURIComponent(userId)}`,
        token
      );

      if (!res.ok) {
        const txt = await res.text();
        throw new Error(`Failed to fetch watchlists: ${res.status} - ${txt}`);
      }

      const data = await res.json();
      const lists = Array.isArray(data) ? data : [];
      setWatchlists(lists);

      if (lists.length > 0) {
        setSelectedWatchlistId((prev) => prev ?? (lists[0].id ?? lists[0].watchlistId));
      } else {
        setSelectedWatchlistId(null);
        setWatchlistStocks([]);
        setEnrichedStocks([]);
      }
    } catch (e) {
      console.error(e);
      setError(e.message || "Could not load watchlists.");
      setWatchlists([]);
      setSelectedWatchlistId(null);
      setWatchlistStocks([]);
      setEnrichedStocks([]);
    } finally {
    }
  }, [token, userId]);

  const fetchWatchlistStocks = useCallback(async (watchlistId) => {
    setLoadingWatchlistStocks(true);
    setError("");

    try {
      if (!watchlistId) {
        setWatchlistStocks([]);
        setEnrichedStocks([]);
        return;
      }

      const res = await apiGet(
        `/api/watchlists/${encodeURIComponent(watchlistId)}/stocks`,
        token
      );

      if (!res.ok) {
        const txt = await res.text();
        throw new Error(`Failed to fetch stocks: ${res.status} - ${txt}`);
      }

      const data = await res.json();
      setWatchlistStocks(Array.isArray(data) ? data : []);
    } catch (e) {
      console.error(e);
      setError(e.message || "Could not load watchlist stocks.");
      setWatchlistStocks([]);
      setEnrichedStocks([]);
    } finally {
      setLoadingWatchlistStocks(false);
    }
  }, [token]);

  const enrichStocksWithMarketData = useCallback(async (stocks) => {
    if (!Array.isArray(stocks) || stocks.length === 0) {
      setEnrichedStocks([]);
      return;
    }

    setLoadingMarketData(true);

    try {
      const enriched = await Promise.all(
        stocks.map(async (stock) => {
          const symbolId = stock.symbolId ?? stock.id ?? stock.ticker;
          const symbol = stock.symbol?.ticker ?? stock.ticker ?? "—";
          const name = stock.symbol?.name ?? stock.name ?? "—";

          try {
            const res = await apiGet(
              `/api/market/bars?symbol=${encodeURIComponent(symbol)}&tf=1d&limit=2`,
              token
            );

            if (!res.ok) {
              return {
                symbolId,
                symbol,
                name,
                price: null,
                change: null,
                changePct: null,
                volume: null,
              };
            }

            const body = await res.json();
            const bars = Array.isArray(body?.data) ? body.data : [];

            if (bars.length === 0) {
              return {
                symbolId,
                symbol,
                name,
                price: null,
                change: null,
                changePct: null,
                volume: null,
              };
            }

            const sorted = [...bars].sort(
              (a, b) => new Date(a.t).getTime() - new Date(b.t).getTime()
            );

            const latest = sorted[sorted.length - 1];
            const prev = sorted.length > 1 ? sorted[sorted.length - 2] : null;

            const price = Number(latest.c ?? latest.close ?? latest.Close);
            const prevPrice =
              prev != null
                ? Number(prev.c ?? prev.close ?? prev.Close)
                : null;

            const change =
              prevPrice != null && Number.isFinite(prevPrice)
                ? price - prevPrice
                : 0;

            const changePct =
              prevPrice != null &&
              Number.isFinite(prevPrice) &&
              prevPrice !== 0
                ? (change / prevPrice) * 100
                : 0;

            const volume =
              Number(latest.v ?? latest.volume ?? latest.Volume) || null;

            return {
              symbolId,
              symbol,
              name,
              price,
              change,
              changePct,
              volume,
            };
          } catch {
            return {
              symbolId,
              symbol,
              name,
              price: null,
              change: null,
              changePct: null,
              volume: null,
            };
          }
        })
      );

      setEnrichedStocks(enriched);
    } finally {
      setLoadingMarketData(false);
    }
  }, [token]);

  const refreshPortfolio = useCallback(async () => {
    try {
      setPortfolioLoading(true);
      setError(null);

      if (!userId) {
        setError(
          "Missing userId. Store res.data.userId from /auth/login into localStorage as kcq_userId."
        );
        return;
      }

      const res = await apiGet(`/portfolio/${encodeURIComponent(userId)}`, token);

      if (res.status === 404) {
        setCash(0);
        setHoldingValue(0);
        setHoldings([]);
        setError(
          "No portfolio found for this user yet (404). Backend must create one on register, or add a create-portfolio endpoint."
        );
        return;
      }

      if (!res.ok) {
        const txt = await res.text();
        throw new Error(`Portfolio fetch failed (${res.status}): ${txt || "no body"}`);
      }

      const dto = await res.json();

      if (dto?.portfolioId) {
        localStorage.setItem("kcq_portfolioId", dto.portfolioId);
        setPortfolioId(dto.portfolioId);
      }

      setCash(typeof dto?.cashBalance === "number" ? dto.cashBalance : 0);
      setHoldingValue(typeof dto?.holdingValue === "number" ? dto.holdingValue : 0);

      const hs = Array.isArray(dto?.holdings) ? dto.holdings : [];
      setHoldings(
        hs.map((h) => ({
          ticker: h.ticker,
          quantity: Number(h.quantity) || 0,
          avgBuyPrice: Number(h.avgBuyPrice) || 0,
        }))
      );

    } catch (e) {
      console.error(e);
      setError(e?.message || String(e));
    } finally {
      setPortfolioLoading(false);
    }
  }, [token, userId]);

  const refreshQuotes = useCallback(async () => {
    try {
      setError(null);

      if (!quoteSymbols.length) {
        setQuotes([]);
        return;
      }

      const results = await Promise.allSettled(
        quoteSymbols.map((symbol) => fetch2Bars(symbol, token))
      );

      const ok = results
        .filter((r) => r.status === "fulfilled")
        .map((r) => r.value)
        .filter((q) => q && typeof q.symbol === "string");

      setQuotes(ok);
    } catch (e) {
      console.error(e);
      setError(e?.message || String(e));
    } finally {
    }
  }, [quoteSymbols, token]);

  const refreshChart = useCallback(async () => {
    try {
      setError(null);

      if (!portfolioId) {
        setChartData([]);
        return;
      }

      const res = await apiGet(
        `/api/portfolio/snapshot/${encodeURIComponent(portfolioId)}`,
        token
      );

      if (!res.ok) {
        const txt = await res.text();
        throw new Error(
          `Portfolio snapshot fetch failed (${res.status}): ${txt || "no body"}`
        );
      }

      const body = await res.json();
      const points = Array.isArray(body?.data) ? body.data : [];

      const sorted = [...points].sort(
        (a, b) => new Date(a.date) - new Date(b.date)
      );

      const enriched = sorted.map((p, i) => {
        const snapshotValue = Number(p?.totalValue);
        const totalSnapshotValue = Number.isFinite(snapshotValue) ? snapshotValue : 0;

        const pctFromApi = Number(p?.portfolioChangePercent);
        const portfolioChangePercent = Number.isFinite(pctFromApi) ? pctFromApi : 0;

        let dayChange = 0;

        if (Number.isFinite(snapshotValue) && Number.isFinite(pctFromApi)) {
          const pctDecimal = portfolioChangePercent / 100;
          const estimatedPrevValue =
            pctDecimal !== -1 ? totalSnapshotValue / (1 + pctDecimal) : totalSnapshotValue;
          dayChange = totalSnapshotValue - estimatedPrevValue;
        } else if (i > 0) {
          const prevValue = Number(sorted[i - 1]?.totalValue);
          dayChange = Number.isFinite(prevValue)
            ? totalSnapshotValue - prevValue
            : 0;
        }

        return {
          date: p?.date || "",
          totalValue: totalSnapshotValue,
          dayChange: Number.isFinite(dayChange) ? dayChange : 0,
          dayChangePct: portfolioChangePercent,
          holdings: Array.isArray(p?.holdings) ? p.holdings : [],
          isLive: false,
        };
      });

      const todayStr = new Date().toISOString().slice(0, 10);
      const lastPoint = enriched[enriched.length - 1];
      const lastDate = lastPoint?.date || "";

      if (lastDate !== todayStr) {
        const lastValue = Number.isFinite(lastPoint?.totalValue)
          ? lastPoint.totalValue
          : 0;

        const currentValue = Number.isFinite(totalValue) ? totalValue : lastValue;
        const liveDayChange = currentValue - lastValue;
        const liveDayChangePct =
          lastValue !== 0 ? (liveDayChange / lastValue) * 100 : 0;

        enriched.push({
          date: todayStr,
          totalValue: currentValue,
          dayChange: Number.isFinite(liveDayChange) ? liveDayChange : 0,
          dayChangePct: Number.isFinite(liveDayChangePct) ? liveDayChangePct : 0,
          holdings: [],
          isLive: true,
        });
      }

      setChartData(enriched);
    } catch (e) {
      console.error(e);
      setError(e?.message || String(e));
      setChartData([]);
    } finally {
    }
  }, [portfolioId, token, totalValue]);

  useEffect(() => {
    fetchWatchlists();
    refreshPortfolio();
  }, [fetchWatchlists, refreshPortfolio]);

  useEffect(() => {
    enrichStocksWithMarketData(watchlistStocks);
  }, [watchlistStocks, enrichStocksWithMarketData]);

  useEffect(() => {
    if (selectedWatchlistId) {
      fetchWatchlistStocks(selectedWatchlistId);
    } else {
      setWatchlistStocks([]);
    }
  }, [selectedWatchlistId, fetchWatchlistStocks]);

  useEffect(() => {
    if (quoteSymbols.length) {
      refreshQuotes();
    } else {
      setQuotes([]);
    }
  }, [quoteSymbols, refreshQuotes]);

  useEffect(() => {
    refreshChart();

    quotesTimer.current = setInterval(refreshQuotes, 90_000);
    chartTimer.current = setInterval(refreshChart, 600_000);
    portfolioTimer.current = setInterval(refreshPortfolio, 120_000);
    clockTimer.current = setInterval(() => setCurrentTime(new Date()), 1000);

    return () => {
      if (quotesTimer.current) clearInterval(quotesTimer.current);
      if (chartTimer.current) clearInterval(chartTimer.current);
      if (portfolioTimer.current) clearInterval(portfolioTimer.current);
      if (clockTimer.current) clearInterval(clockTimer.current);
    };
  }, [refreshQuotes, refreshChart, refreshPortfolio]);

  const portfolioRows = useMemo(() => {
    return (holdings || []).map((h) => {
      const q = priceMap.get(String(h.ticker).toUpperCase());
      const price = q?.price;
      const value = typeof price === "number" ? price * h.quantity : null;

      const costBasis = (Number(h.avgBuyPrice) || 0) * (Number(h.quantity) || 0);
      const ret = typeof value === "number" ? value - costBasis : null;
      const retPct =
        typeof value === "number" && costBasis !== 0 ? (ret / costBasis) * 100 : null;

      return {
        symbol: h.ticker,
        shares: h.quantity,
        avgCost: h.avgBuyPrice,
        value,
        price,
        today: q?.change ?? null,
        todayPct: q?.changePct ?? null,
        ret,
        retPct,
      };
    });
  }, [holdings, priceMap]);

  return (
    <div className="main">
      <p className="topNote">
        {error ? (
          <span className="errorText">Data error: {String(error)}</span>
        ) : (
          <>
            Portfolio: {formatTime(currentTime)}
          </>
        )}
      </p>

      <LearnHint
        enabled={learnMode}
        text="This chart shows how the total value of your portfolio has changed over time using daily portfolio snapshots."
        width={300}
      >
        <div className="chartCard">
          <PriceChart data={chartData} />
          <div
            style={{
              padding: "6px 2px 2px",
              color: "rgba(231,238,252,.55)",
              fontSize: 12,
            }}
          >
            Portfolio value history · daily snapshots
          </div>
        </div>
      </LearnHint>

      <div className="kpiRow">
        <LearnHint
          enabled={learnMode}
          text="Total value is the combined value of your cash plus the current market value of all stocks you hold."
        >
          <div className="kpiCard">
            <div className="kpiLabel">Total value</div>
            <div className="kpiValue">{money(totalValue)}</div>
            <div className="kpiSub">Cash + holdings market value</div>
          </div>
        </LearnHint>

        <LearnHint
          enabled={learnMode}
          text="Cash is your available buying power. This is how much money is currently ready to invest."
        >
          <div className="kpiCard">
            <div className="kpiLabel">Cash</div>
            <div className="kpiValue">{money(cash)}</div>
            <div className="kpiSub">Available buying power</div>
          </div>
        </LearnHint>

        <LearnHint
          enabled={learnMode}
          text="Holdings value is the current market value of the stocks you own, based on either live quotes or backend calculations."
        >
          <div className="kpiCard">
            <div className="kpiLabel">Holdings value</div>
            <div className="kpiValue">
              {money(quotes.length ? holdingsValueLive : holdingValue)}
            </div>
            <div className="kpiSub">
              {quotes.length ? "Based on latest quotes" : "Backend computed"}
            </div>
          </div>
        </LearnHint>
      </div>

      <div className="tabs">
        <LearnHint
          enabled={learnMode}
          text="Portfolio shows the stocks you currently own, including shares, value, daily change, and overall return."
          width={280}
        >
          <div
            className={`tab ${tab === "portfolio" ? "active" : ""}`}
            onClick={() => setTab("portfolio")}
          >
            Portfolio
          </div>
        </LearnHint>

        <LearnHint
          enabled={learnMode}
          text="Watchlist shows stocks you are monitoring but may not own yet."
          width={240}
        >
          <div
            className={`tab ${tab === "watchlist" ? "active" : ""}`}
            onClick={() => setTab("watchlist")}
          >
            Watchlist
          </div>
        </LearnHint>
      </div>
           {tab === "watchlist" && watchlists.length > 1 && (
              <div className="miniWatchlistTabs">
                {watchlists.map((w) => {
                  const id = w.id ?? w.watchlistId;
                  const name = w.name ?? w.title ?? `Watchlist ${id}`;
                  const active = String(id) === String(selectedWatchlistId);

                  return (
                    <button
                      key={id}
                      type="button"
                      className={`miniWatchlistTab ${active ? "active" : ""}`}
                      onClick={() => setSelectedWatchlistId(id)}
                    >
                      {name}
                    </button>
                  );
                })}
              </div>
            )}
      <div className="tableWrap">
        {tab === "portfolio" ? (
          <table className="table2">
            <thead>
              <tr align ="center">
                <th>Symbol</th>
                <th>Shares</th>
                <th>Value</th>
                <th>Today</th>
                <th>Return</th>
                <th>Price/Share</th>
              </tr>
            </thead>
            <tbody align ="center">
              {portfolioLoading ? (
                <tr>
                  <td colSpan="6">Loading portfolio…</td>
                </tr>
              ) : portfolioRows.length === 0 ? (
                <tr>
                  <td colSpan="6">No holdings yet.</td>
                </tr>
              ) : (
                portfolioRows.map((r) => (
                  <tr key={r.symbol}>
                    <td><strong>{r.symbol}</strong></td>
                    <td>{r.shares}</td>
                    <td>{money(r.value)}</td>
                    <td className={typeof r.today === "number" && r.today >= 0 ? "pos" : "neg"}>
                      {money(r.today)} ({pctText(r.todayPct)})
                    </td>
                    <td className={typeof r.ret === "number" && r.ret >= 0 ? "pos" : "neg"}>
                      {money(r.ret)} ({pctText(r.retPct)})
                    </td>
                    <td>{money(r.price)}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        ) : (
          <>
 
            <table className="table2 watchlistTable">
              <thead>
                <tr>
                  <th>Symbol</th>
                  <th>Name</th>
                  <th>Price</th>
                  <th>Change</th>
                  <th>% Change</th>
                </tr>
              </thead>
              <tbody>
                {loadingWatchlistStocks || loadingMarketData ? (
                  <tr>
                    <td colSpan="5" className="emptyRow">Loading stocks…</td>
                  </tr>
                ) : enrichedStocks.length === 0 ? (
                  <tr>
                    <td colSpan="5" className="emptyRow">
                      {selectedWatchlistId
                        ? `No stocks in ${selectedWatchlistName}.`
                        : "No watchlist data available."}
                    </td>
                  </tr>
                ) : (
                  enrichedStocks.map((stock) => {
                    const isPos = typeof stock.change === "number" && stock.change >= 0;
                    const isNeg = typeof stock.change === "number" && stock.change < 0;

                    return (
                      <tr key={stock.symbolId}>
                        <td className="symbolCell">
                          <strong>{stock.symbol}</strong>
                        </td>
                        <td>{stock.name}</td>
                        <td className="priceCell">{money(stock.price)}</td>
                        <td className={isPos ? "pos" : isNeg ? "neg" : ""}>
                          {typeof stock.change === "number"
                            ? `${stock.change >= 0 ? "+" : ""}${money(stock.change)}`
                            : "—"}
                        </td>
                        <td className={isPos ? "pos" : isNeg ? "neg" : ""}>
                          {typeof stock.changePct === "number"
                            ? `${stock.changePct >= 0 ? "+" : ""}${stock.changePct.toFixed(2)}%`
                            : "—"}
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </>
        )}
      </div>

      <div className="footerNote">
        Auto-refresh: portfolio every 2 min · quotes every 90s · chart every 10 min.
      </div>
    </div>
  );
}