import { useState, useEffect } from "react";
import { useOutletContext } from "react-router-dom";
import LearnHint from "../components/LearnHint";
import "../styles/dashboard.css";

const API_BASE = process.env.REACT_APP_API_BASE_URL || "";

const money = (n) =>
  typeof n === "number"
    ? n.toLocaleString(undefined, { style: "currency", currency: "USD" })
    : "—";

const formatDateLabel = (dateStr, mode = "short") => {
  if (!dateStr) return "";

  const [year, month, day] = dateStr.split("-").map(Number);
  const monthNamesShort = [
    "Jan", "Feb", "Mar", "Apr", "May", "Jun",
    "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
  ];

  if (mode === "short") {
    return `${monthNamesShort[month - 1]} ${day}`;
  }

  return `${month}/${day}/${year}`;
};

// RSI calculation using Wilder's smoothing method (standard 14-period RSI)
function calculateRSI(prices, period = 14) {
  if (!prices || prices.length < period + 1) return null;

  // Compute daily changes
  const changes = [];
  for (let i = 1; i < prices.length; i++) {
    changes.push(prices[i] - prices[i - 1]);
  }

  // Seed the first average gain/loss from the first `period` changes
  let avgGain = 0;
  let avgLoss = 0;
  for (let i = 0; i < period; i++) {
    if (changes[i] > 0) avgGain += changes[i];
    else avgLoss += Math.abs(changes[i]);
  }
  avgGain /= period;
  avgLoss /= period;

  // Apply Wilder's smoothing for remaining changes
  for (let i = period; i < changes.length; i++) {
    const gain = changes[i] > 0 ? changes[i] : 0;
    const loss = changes[i] < 0 ? Math.abs(changes[i]) : 0;
    avgGain = (avgGain * (period - 1) + gain) / period;
    avgLoss = (avgLoss * (period - 1) + loss) / period;
  }

  if (avgLoss === 0) return 100; // All gains, maxed out
  const rs = avgGain / avgLoss;
  return 100 - 100 / (1 + rs);
}

function predictRSI(data) {
  if (!data || data.length < 15) return null;

  const prices = data
    .map((d) => Number(d.c))
    .filter((n) => Number.isFinite(n));

  if (prices.length < 15) return null;

  const rsi = calculateRSI(prices, 14);
  if (rsi === null) return null;

  const rsiRounded = parseFloat(rsi.toFixed(1));

  let signal = "hold";
  let reason = "";
  let color = "rgba(231,238,252,.8)";

  if (rsiRounded < 30) {
    signal = "buy";
    color = "rgba(53,208,127,.95)";
    if (rsiRounded < 20) {
      reason = `RSI is ${rsiRounded}, deeply oversold. The stock has likely been heavily sold off and may be due for a strong rebound.`;
    } else {
      reason = `RSI is ${rsiRounded}, indicating the stock is oversold. Selling pressure appears exhausted, suggesting a potential price recovery.`;
    }
  } else if (rsiRounded > 70) {
    signal = "sell";
    color = "rgba(255,107,107,.95)";
    if (rsiRounded > 80) {
      reason = `RSI is ${rsiRounded}, extremely overbought. The stock has rallied sharply and may be significantly overextended, raising the risk of a pullback.`;
    } else {
      reason = `RSI is ${rsiRounded}, indicating the stock is overbought. Buying momentum is weakening, and a price correction is increasingly likely.`;
    }
  } else if (rsiRounded >= 45 && rsiRounded <= 55) {
    signal = "hold";
    color = "rgba(231,238,252,.8)";
    reason = `RSI is ${rsiRounded}, which is in neutral territory. There is no clear directional momentum therefore a holding or waiting for a stronger signal is advisable.`;
  } else if (rsiRounded > 55) {
    signal = "hold";
    color = "rgba(231,238,252,.8)";
    reason = `RSI is ${rsiRounded}, trending toward overbought but not yet at a sell threshold. Momentum is bullish; consider monitoring for a move above 70.`;
  } else {
    signal = "hold";
    color = "rgba(231,238,252,.8)";
    reason = `RSI is ${rsiRounded}, trending toward oversold but not yet at a buy threshold. Momentum is bearish; consider monitoring for a drop below 30.`;
  }

  const daysUsed = data.length;

  return { rsi: rsiRounded, signal, reason, color, daysUsed };
}

function DetailedChart({ data, symbol, timePeriod, onTimePeriodChange }) {
  const [hoverIdx, setHoverIdx] = useState(null);

  if (!data || data.length < 2) {
    return (
      <div style={{ padding: "40px", textAlign: "center", color: "rgba(231,238,252,.55)" }}>
        <p>Chart data loading...</p>
      </div>
    );
  }

  const width = 700;
  const height = 280;
  const padding = { top: 20, right: 20, bottom: 46, left: 60 };
  const chartWidth = width - padding.left - padding.right;
  const chartHeight = height - padding.top - padding.bottom;

  const closes = data.map((d) => Number(d.c)).filter((n) => Number.isFinite(n));
  const min = Math.min(...closes);
  const max = Math.max(...closes);
  const range = max - min || 1;

  const coords = data.map((d, i) => {
    const x = padding.left + (i / (data.length - 1)) * chartWidth;
    const y =
      padding.top + chartHeight - ((Number(d.c) - min) / range) * chartHeight;
    return { i, x, y, t: d.t, c: Number(d.c) };
  });

  const points = coords.map((p) => `${p.x},${p.y}`).join(" ");

  function onMove(e) {
    const rect = e.currentTarget.getBoundingClientRect();
    const mouseX = e.clientX - rect.left;

    const clampedX = Math.max(
      padding.left,
      Math.min(width - padding.right, mouseX)
    );
    const ratio = (clampedX - padding.left) / chartWidth;
    const idx = Math.round(ratio * (data.length - 1));

    setHoverIdx(Math.max(0, Math.min(data.length - 1, idx)));
  }

  function onLeave() {
    setHoverIdx(null);
  }

  const hover = hoverIdx != null ? coords[hoverIdx] : null;

  const firstPrice = coords[0].c;
  const lastPrice = coords[coords.length - 1].c;
  const priceChange = lastPrice - firstPrice;
  const isPositive = priceChange >= 0;

  const timePeriods = [
    { label: "1W", value: "1week" },
    { label: "1M", value: "1month" },
    { label: "1Y", value: "1year" },
    { label: "ALL", value: "all" },
  ];

  const tickCount = 5;
  const xTicks = Array.from({ length: tickCount }, (_, k) => {
    const idx = Math.round((k / (tickCount - 1)) * (coords.length - 1));
    return coords[idx];
  });

  return (
    <div>
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          marginBottom: 16,
        }}
      >
        <h3 style={{ margin: 0, fontSize: 18, fontWeight: 700 }}>{symbol}</h3>
        <div style={{ display: "flex", gap: 8 }}>
          {timePeriods.map((period) => (
            <button
              key={period.value}
              onClick={() => onTimePeriodChange(period.value)}
              className={timePeriod === period.value ? "tab active" : "tab"}
              style={{ padding: "6px 14px", fontSize: 13, margin: 0, minWidth: "auto" }}
            >
              {period.label}
            </button>
          ))}
        </div>
      </div>

      <div
        className={isPositive ? "pos" : "neg"}
        style={{ fontSize: 15, fontWeight: 600, marginBottom: 16 }}
      >
        {isPositive ? "+" : ""}
        {money(priceChange)} ({((priceChange / firstPrice) * 100).toFixed(2)}%)
      </div>

      <svg
        width={width}
        height={height}
        style={{ display: "block", margin: "0 auto" }}
        onMouseMove={onMove}
        onMouseLeave={onLeave}
      >
        {[0, 0.25, 0.5, 0.75, 1].map((ratio) => {
          const y = padding.top + chartHeight * (1 - ratio);
          return (
            <g key={ratio}>
              <line
                x1={padding.left}
                y1={y}
                x2={width - padding.right}
                y2={y}
                stroke="rgba(255,255,255,0.08)"
                strokeWidth="1"
              />
              <text
                x={padding.left - 10}
                y={y + 4}
                textAnchor="end"
                fill="rgba(231,238,252,0.5)"
                fontSize="11"
              >
                {money(min + range * ratio)}
              </text>
            </g>
          );
        })}

        {xTicks.map((p, idx) => {
          const label = formatDateLabel(p.t, "short");
          const yAxis = padding.top + chartHeight;
          return (
            <g key={idx}>
              <line
                x1={p.x}
                y1={yAxis}
                x2={p.x}
                y2={yAxis + 6}
                stroke="rgba(255,255,255,0.14)"
                strokeWidth="1"
              />
              <text
                x={p.x}
                y={yAxis + 20}
                textAnchor="middle"
                fill="rgba(231,238,252,0.45)"
                fontSize="11"
              >
                {label}
              </text>
            </g>
          );
        })}

        <defs>
          <linearGradient id="chartGradient" x1="0" x2="0" y1="0" y2="1">
            <stop
              offset="0%"
              stopColor={isPositive ? "rgba(53,208,127,.95)" : "rgba(255,107,107,.95)"}
              stopOpacity="0.3"
            />
            <stop
              offset="100%"
              stopColor={isPositive ? "rgba(53,208,127,.95)" : "rgba(255,107,107,.95)"}
              stopOpacity="0"
            />
          </linearGradient>
        </defs>

        <polygon
          points={`${padding.left},${padding.top + chartHeight} ${points} ${width - padding.right},${padding.top + chartHeight}`}
          fill="url(#chartGradient)"
        />

        <polyline
          points={points}
          fill="none"
          stroke={isPositive ? "rgba(53,208,127,.95)" : "rgba(255,107,107,.95)"}
          strokeWidth="3"
          strokeLinecap="round"
          strokeLinejoin="round"
        />

        {hover && (
          <>
            <line
              x1={hover.x}
              y1={padding.top}
              x2={hover.x}
              y2={padding.top + chartHeight}
              stroke="rgba(231,238,252,0.25)"
              strokeWidth="1"
            />
            <circle cx={hover.x} cy={hover.y} r="5" fill="rgba(231,238,252,0.9)" />
            <circle cx={hover.x} cy={hover.y} r="10" fill="rgba(231,238,252,0.12)" />
          </>
        )}
      </svg>

      {hover && (
        <div
          style={{
            marginTop: 8,
            textAlign: "center",
            fontSize: 12,
            color: "rgba(231,238,252,.75)",
          }}
        >
          <span style={{ marginRight: 12 }}>{formatDateLabel(hover.t, "full")}</span>
          <span style={{ fontWeight: 700 }}>{money(hover.c)}</span>
        </div>
      )}
    </div>
  );
}

export default function BuySell() {
  const [selectedStock, setSelectedStock] = useState(null);
  const [stockData, setStockData] = useState(null);
  const [chartData, setChartData] = useState([]);
  const [loading, setLoading] = useState(false);
  const [orderType, setOrderType] = useState("buy");
  const [shares, setShares] = useState(1);
  const [orderMode, setOrderMode] = useState("shares");
  const [timePeriod, setTimePeriod] = useState("1month");
  const { learnMode, searchQuery, setSearchQuery } = useOutletContext();
  const [popularStocks, setPopularStocks] = useState([]);
  const [popularLoading, setPopularLoading] = useState(false);
  const [prediction, setPrediction] = useState(null);
  const [rsiData, setRsiData] = useState([]);

  useEffect(() => {
    if (!searchQuery || !searchQuery.trim()) return;

    const symbol = searchQuery.trim().toUpperCase();

    if (symbol && symbol !== selectedStock) {
      selectStock(symbol);
    }
  }, [searchQuery, selectedStock]);

  useEffect(() => {
    if (rsiData.length >= 15) {
      const result = predictRSI(rsiData);
      setPrediction(result);
    } else {
      setPrediction(null);
    }
  }, [rsiData]);

  useEffect(() => {
    async function fetchPopularStocks() {
      setPopularLoading(true);
      try {
        const res = await fetch(`${API_BASE}/api/symbols`);
        if (!res.ok) throw new Error("Failed to fetch popular stocks");

        const data = await res.json();

        const mappedStocks = (data || []).map((stock) => ({
          symbol: stock.ticker,
          name: stock.name,
        }));

        setPopularStocks(mappedStocks);
      } catch (error) {
        console.error("Error fetching popular stocks:", error);
        setPopularStocks([]);
      } finally {
        setPopularLoading(false);
      }
    }

    fetchPopularStocks();
  }, []);

  async function selectStock(symbol) {
    setSelectedStock(symbol);
    setLoading(true);
    setTimePeriod("1month");

    try {
      await fetchSnapshotAndSetStockData(symbol);
      await fetchChartData(symbol, "1month");
      await fetchRsiData(symbol);
    } catch (error) {
      console.error("Error fetching stock data:", error);
    } finally {
      setLoading(false);
    }
  }

  function mapPeriodToLimit(period) {
    return period === "1week"
      ? 7
      : period === "1month"
      ? 30
      : period === "1year"
      ? 365
      : period === "all"
      ? 2000
      : 30;
  }

  async function fetchRsiData(symbol) {
    try {
      if (!symbol) return setRsiData([]);
      // Fetch up to 1 year (365 trading days) for RSI calculation
      const url = `${API_BASE}/api/market/bars?symbol=${encodeURIComponent(symbol)}&tf=1d&limit=365`;
      const res = await fetch(url);
      if (!res.ok) {
        console.error("Failed to fetch RSI data", res.status);
        return setRsiData([]);
      }
      const body = await res.json();
      const bars = Array.isArray(body?.data) ? body.data : [];
      const sorted = [...bars].sort((a, b) => new Date(a.t) - new Date(b.t));
      setRsiData(sorted);
    } catch (err) {
      console.error("Error fetching RSI data:", err);
      setRsiData([]);
    }
  }


  async function fetchSnapshotAndSetStockData(symbol) {
    try {
      if (!symbol) return;

      const url = `${API_BASE}/api/market/bars?symbol=${encodeURIComponent(symbol)}&tf=1d&limit=2`;
      const res = await fetch(url);

      if (!res.ok) {
        console.warn("snapshot fetch failed", res.status, await res.text());
        setStockData(null);
        return;
      }

      const body = await res.json();
      const bars = Array.isArray(body?.data) ? body.data : [];

      if (!bars || bars.length === 0) {
        setStockData(null);
        return;
      }

      const sorted = [...bars].sort((a, b) => new Date(a.t) - new Date(b.t));
      const latest = sorted[sorted.length - 1];
      const prev = sorted.length > 1 ? sorted[sorted.length - 2] : null;

      const price = Number(latest.c ?? latest.close ?? latest.Close);
      const prevPrice = prev ? Number(prev.c ?? prev.close ?? prev.Close) : null;
      const change =
        prevPrice != null && Number.isFinite(prevPrice) ? price - prevPrice : 0;
      const changePct =
        prevPrice != null && Number.isFinite(prevPrice) && prevPrice !== 0
          ? (change / prevPrice) * 100
          : 0;
      const volume = Number(latest.v ?? latest.volume ?? latest.Volume) || null;

      setStockData({
        price,
        change,
        changePct,
        volume,
      });
    } catch (err) {
      console.error("Error fetching snapshot:", err);
      setStockData(null);
    }
  }

  async function fetchChartData(symbol, period) {
    try {
      if (!symbol) return setChartData([]);

      const limit = mapPeriodToLimit(period);
      const url = `${API_BASE}/api/market/bars?symbol=${encodeURIComponent(symbol)}&tf=1d&limit=${limit}`;

      const res = await fetch(url);
      if (!res.ok) {
        console.error("Failed to fetch bars", res.status, await res.text());
        return setChartData([]);
      }

      const body = await res.json();
      const bars = Array.isArray(body?.data) ? body.data : [];
      const sorted = [...bars].sort((a, b) => new Date(a.t) - new Date(b.t));

      setChartData(sorted); // ✅ keep date + close
      console.log("last bar raw:", sorted[sorted.length - 1]);
      console.log("last bar parsed:", new Date(sorted[sorted.length - 1]?.t).toString());
      console.log("last bar iso:", new Date(sorted[sorted.length - 1]?.t).toISOString());
    } catch (err) {
      console.error("Error fetching chart data:", err);
      setChartData([]);
    }
  }

  function handleTimePeriodChange(period) {
    setTimePeriod(period);
    if (selectedStock) {
      fetchChartData(selectedStock, period);
    }
  }

  async function executeOrder() {
    if (!selectedStock || !stockData) {
      alert("Please select a stock first");
      return;
    }

    const qty = Number(shares);
    if (!Number.isFinite(qty) || qty <= 0) {
      alert("Enter a valid number of shares / amount.");
      return;
    }

    let portfolioId = localStorage.getItem("kcq_portfolioId");
    try {
      if (!portfolioId) {
        const pRes = await fetch(`${API_BASE}/portfolio`);
        if (!pRes.ok) {
          console.warn("Could not fetch portfolios", pRes.status, await pRes.text());
        } else {
          const portfolios = await pRes.json();
          if (Array.isArray(portfolios) && portfolios.length > 0) {
            portfolioId = portfolios[0].portfolioId;
            if (portfolioId) localStorage.setItem("kcq_portfolioId", portfolioId);
          }
        }
      }
    } catch (err) {
      console.error("Error fetching portfolios:", err);
    }

    if (!portfolioId) {
      alert(
        "No portfolio found. Create a portfolio on the backend (or set kcq_portfolioId in localStorage) before executing trades."
      );
      return;
    }

    const payload = {
      portfolioId,
      ticker: selectedStock,
      quantity: qty,
      tradeType: orderType === "buy" ? "BUY" : "SELL",
    };

    try {
      const token = localStorage.getItem("kcq_token");
      const headers = {
        "Content-Type": "application/json",
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      };

      const res = await fetch(`${API_BASE}/trade`, {
        method: "POST",
        headers,
        body: JSON.stringify(payload),
      });

      if (!res.ok) {
        let msg = `Server returned ${res.status}`;
        try {
          const body = await res.json();
          msg = body?.message || body?.detail || JSON.stringify(body);
        } catch (e) {
          const txt = await res.text();
          if (txt) msg = txt;
        }
        console.error("Trade failed:", res.status, msg);
        alert("Trade failed: " + msg);
        return;
      }

      let result = null;
      try {
        result = await res.json();
      } catch (e) {}

      alert(`${orderType === "buy" ? "Buy" : "Sell"} order executed successfully!`);
      console.log("Trade response:", result);

      await fetchSnapshotAndSetStockData(selectedStock);
      await fetchChartData(selectedStock, timePeriod);
    } catch (err) {
      console.error("Error executing trade:", err);
      alert("Error executing trade. See console for details.");
    }
  }

  const estimatedCost =
    stockData && orderMode === "shares" ? stockData.price * shares : shares;

  return (
    <div className="main">
      {selectedStock && (
        <>
          <div style={{ marginBottom: 20 }}>
            <button
              className="btn"
              onClick={() => {
                setSelectedStock(null);
                setStockData(null);
                setChartData([]);
                setRsiData([]);
                setPrediction(null);
                setSearchQuery("");
              }}
              style={{
                display: "inline-flex",
                alignItems: "center",
                gap: 8,
                padding: "10px 16px",
                fontSize: 14,
                fontWeight: 600,
              }}
            >
              ← Back
            </button>
          </div>

          <div>
            {loading ? (
              <div style={{ textAlign: "center", padding: "60px 20px" }}>
                <div
                  style={{
                    width: 40,
                    height: 40,
                    border: "3px solid rgba(255,255,255,.1)",
                    borderTop: "3px solid rgba(53,208,127,.95)",
                    borderRadius: "50%",
                    margin: "0 auto 16px",
                    animation: "spin 1s linear infinite",
                  }}
                />
                <p style={{ color: "rgba(231,238,252,.65)" }}>
                  Loading {selectedStock} data...
                </p>
              </div>
            ) : (
              <>
                <div className="kpiRow" style={{ marginBottom: 20 }}>
                  <LearnHint
                    enabled={learnMode}
                    text="This is the stock's ticker symbol, the short code used to identify it in the market."
                  >
                    <div className="kpiCard">
                      <div className="kpiLabel">Symbol</div>
                      <div style={{ fontSize: 32, fontWeight: 900, letterSpacing: ".2px" }}>
                        {selectedStock}
                      </div>
                    </div>
                  </LearnHint>

                  {stockData && (
                    <>
                      <LearnHint
                        enabled={learnMode}
                        text="Current Price is the latest available market price for one share of this stock."
                      >
                        <div className="kpiCard">
                          <div className="kpiLabel">Current Price</div>
                          <div className="kpiValue">{money(stockData.price)}</div>
                        </div>
                      </LearnHint>

                      <LearnHint
                        enabled={learnMode}
                        text="Today's Change shows how much the stock moved compared with the previous closing price."
                      >
                        <div className="kpiCard">
                          <div className="kpiLabel">Today's Change</div>
                          <div className={`kpiValue ${stockData.change >= 0 ? "pos" : "neg"}`}>
                            {stockData.change >= 0 ? "+" : ""}
                            {money(stockData.change)}
                          </div>
                          <div className="kpiSub">{stockData.changePct?.toFixed(2)}% change</div>
                        </div>
                      </LearnHint>
                    </>
                  )}
                </div>

                {stockData && (
                  <div className="kpiRow" style={{ marginBottom: 20 }}>
                    <LearnHint
                      enabled={learnMode}
                      text="Today's High is the highest estimated price shown for the stock during the day."
                    >
                      <div className="kpiCard">
                        <div className="kpiLabel">Today's High</div>
                        <div className="kpiValue" style={{ fontSize: 28 }}>
                          {stockData.price ? money(stockData.price * 1.02) : "—"}
                        </div>
                      </div>
                    </LearnHint>

                    <LearnHint
                      enabled={learnMode}
                      text="Today's Low is the lowest estimated price shown for the stock during the day."
                    >
                      <div className="kpiCard">
                        <div className="kpiLabel">Today's Low</div>
                        <div className="kpiValue" style={{ fontSize: 28 }}>
                          {stockData.price ? money(stockData.price * 0.98) : "—"}
                        </div>
                      </div>
                    </LearnHint>

                    <LearnHint
                      enabled={learnMode}
                      text="Volume is the number of shares traded during the day. Higher volume means more trading activity."
                    >
                      <div className="kpiCard">
                        <div className="kpiLabel">Volume</div>
                        <div className="kpiValue" style={{ fontSize: 28 }}>
                          {stockData.volume ? stockData.volume.toLocaleString() : "—"}
                        </div>
                      </div>
                    </LearnHint>
                  </div>
                )}

                <LearnHint
                  enabled={learnMode}
                  text="This chart shows historical price movement for the selected stock across the chosen time period."
                  width={320}>
                  <div className="chartCard" style={{ marginBottom: 20 }}>
                    <DetailedChart
                      data={chartData}
                      symbol={selectedStock}
                      timePeriod={timePeriod}
                      onTimePeriodChange={handleTimePeriodChange}
                    />
                    <div
                      style={{
                        padding: "6px 2px 2px",
                        color: "rgba(231,238,252,.55)",
                        fontSize: 12,
                        textAlign: "center",
                      }}
                    >
                      Historical data · Delayed quotes · Powered by backend
                    </div>
                  </div>
                </LearnHint>

                {prediction && (
                  <div
                    style={{
                      border: "1px solid rgba(255,255,255,.08)",
                      borderRadius: 16,
                      background: "rgba(255,255,255,.03)",
                      padding: "18px 22px",
                      marginBottom: 20,
                    }}
                  >
                    <div
                      style={{
                        fontSize: 13,
                        color: "rgba(231,238,252,.6)",
                        textTransform: "uppercase",
                        letterSpacing: ".05em",
                        marginBottom: 12,
                        fontWeight: 600,
                      }}
                    >
                      Predictive Market Insight
                    </div>

                    {/* RSI gauge row */}
                    <div
                      style={{
                        display: "flex",
                        alignItems: "center",
                        gap: 18,
                        marginBottom: 14,
                      }}
                    >
                      {/* RSI value badge */}
                      <div
                        style={{
                          minWidth: 72,
                          height: 72,
                          borderRadius: "50%",
                          border: `3px solid ${prediction.color}`,
                          display: "flex",
                          flexDirection: "column",
                          alignItems: "center",
                          justifyContent: "center",
                          background: "rgba(0,0,0,.20)",
                          flexShrink: 0,
                        }}
                      >
                        <span
                          style={{
                            fontSize: 22,
                            fontWeight: 800,
                            color: prediction.color,
                            lineHeight: 1,
                          }}
                        >
                          {prediction.rsi}
                        </span>
                        <span
                          style={{
                            fontSize: 10,
                            color: "rgba(231,238,252,.5)",
                            fontWeight: 600,
                            letterSpacing: ".04em",
                            marginTop: 2,
                          }}
                        >
                          RSI
                        </span>
                      </div>

                      {/* RSI bar scale */}
                      <div style={{ flex: 1 }}>
                        <div
                          style={{
                            position: "relative",
                            height: 10,
                            borderRadius: 6,
                            background:
                              "linear-gradient(to right, rgba(53,208,127,.7) 0%, rgba(53,208,127,.7) 30%, rgba(231,238,252,.25) 30%, rgba(231,238,252,.25) 70%, rgba(255,107,107,.7) 70%, rgba(255,107,107,.7) 100%)",
                            marginBottom: 6,
                          }}
                        >
                          {/* Marker */}
                          <div
                            style={{
                              position: "absolute",
                              top: "50%",
                              left: `${Math.min(Math.max(prediction.rsi, 2), 98)}%`,
                              transform: "translate(-50%, -50%)",
                              width: 14,
                              height: 14,
                              borderRadius: "50%",
                              background: prediction.color,
                              border: "2px solid rgba(255,255,255,.85)",
                              boxShadow: `0 0 6px ${prediction.color}`,
                            }}
                          />
                        </div>
                        <div
                          style={{
                            display: "flex",
                            justifyContent: "space-between",
                            fontSize: 10,
                            color: "rgba(231,238,252,.4)",
                            fontWeight: 600,
                          }}
                        >
                          <span>0 — Oversold</span>
                          <span>50</span>
                          <span>Overbought — 100</span>
                        </div>
                      </div>
                    </div>

                    {/* Signal + reason */}
                    <div
                      style={{
                        fontSize: 15,
                        lineHeight: 1.6,
                        color: "rgba(231,238,252,.9)",
                      }}
                    >
                      Signal:{" "}
                      <strong
                        style={{
                          color: prediction.color,
                          textTransform: "uppercase",
                          letterSpacing: ".04em",
                        }}
                      >
                        {prediction.signal}
                      </strong>
                      <br />
                      <span
                        style={{
                          fontSize: 13,
                          color: "rgba(231,238,252,.65)",
                          lineHeight: 1.7,
                        }}
                      >
                        {prediction.reason}
                      </span>
                    </div>

                    <div
                      style={{
                        marginTop: 10,
                        fontSize: 11,
                        color: "rgba(231,238,252,.4)",
                      }}
                    >
                      Based on {prediction.daysUsed} days of price history · 14-period RSI (Wilder's smoothing)
                    </div>
                  </div>
                )}

                <LearnHint
                  enabled={learnMode}
                  text="This is the order entry panel where you choose whether to buy or sell, enter shares or dollars, and review the estimated trade value."
                  width={340}
                >
                  <div
                    style={{
                      border: "1px solid rgba(255,255,255,.08)",
                      borderRadius: 16,
                      background: "rgba(255,255,255,.03)",
                      padding: "20px 24px",
                      marginBottom: 20,
                    }}
                  >
                    <div
                      style={{
                        display: "flex",
                        justifyContent: "space-between",
                        alignItems: "center",
                        marginBottom: 20,
                        paddingBottom: 16,
                        borderBottom: "1px solid rgba(255,255,255,.08)",
                      }}
                    >
                      <h2 style={{ margin: 0, fontSize: 20, fontWeight: 700 }}>
                        {orderType === "buy" ? "Buy" : "Sell"} {selectedStock}
                      </h2>

                      <LearnHint
                        enabled={learnMode}
                        text="Choose Buy to purchase shares or Sell to sell shares you already own."
                        width={260}>
                        <div className="tabs" style={{ margin: 0 }}>
                          <div
                            className={`tab ${orderType === "buy" ? "active" : ""}`}
                            onClick={() => setOrderType("buy")}>
                            Buy
                          </div>
                          <div
                            className={`tab ${orderType === "sell" ? "active" : ""}`}
                            onClick={() => setOrderType("sell")}>
                            Sell
                          </div>
                        </div>
                      </LearnHint>
                    </div>

                    <div
                      style={{
                        display: "grid",
                        gridTemplateColumns: "1fr 1fr",
                        gap: 20,
                        marginBottom: 20,
                      }}
                    >
                      <div>
                        <LearnHint
                          enabled={learnMode}
                          text="Order Type controls whether you enter an exact number of shares or a dollar amount to estimate shares."
                          width={300}
                        >
                          <div
                            style={{
                              marginBottom: 12,
                              fontSize: 13,
                              color: "rgba(231,238,252,.65)",
                              fontWeight: 600,
                              textTransform: "uppercase",
                              letterSpacing: ".05em",
                            }}
                          >
                            Order Type
                          </div>
                        </LearnHint>

                        <LearnHint
                          enabled={learnMode}
                          text="Shares lets you enter an exact share count. Dollars lets you enter an amount of money and estimate how many shares that buys."
                          width={320}
                        >
                          <div className="tabs" style={{ margin: 0 }}>
                            <div
                              className={`tab ${orderMode === "shares" ? "active" : ""}`}
                              onClick={() => setOrderMode("shares")}
                              style={{ flex: 1, textAlign: "center" }}
                            >
                              Shares
                            </div>
                            <div
                              className={`tab ${orderMode === "dollars" ? "active" : ""}`}
                              onClick={() => setOrderMode("dollars")}
                              style={{ flex: 1, textAlign: "center" }}
                            >
                              Dollars
                            </div>
                          </div>
                        </LearnHint>
                      </div>

                      <div>
                        <LearnHint
                          enabled={learnMode}
                          text={
                            orderMode === "shares"
                              ? "Enter the number of shares you want to buy or sell."
                              : "Enter the dollar amount you want to use for this order."
                          }
                          width={300}
                        >
                          <div
                            style={{
                              marginBottom: 12,
                              fontSize: 13,
                              color: "rgba(231,238,252,.65)",
                              fontWeight: 600,
                              textTransform: "uppercase",
                              letterSpacing: ".05em",
                            }}
                          >
                            {orderMode === "shares"
                              ? "Number of Shares"
                              : "Amount in Dollars"}
                          </div>
                        </LearnHint>

                        <LearnHint
                          enabled={learnMode}
                          text="Use this input and the plus or minus buttons to adjust your trade size."
                          width={260}
                        >
                          <div style={{ display: "flex", gap: 8 }}>
                            <input
                              type="number"
                              value={shares}
                              onChange={(e) =>
                                setShares(Math.max(0, parseFloat(e.target.value) || 0))
                              }
                              min="0"
                              step={orderMode === "shares" ? "1" : "0.01"}
                              style={{
                                flex: 1,
                                padding: "12px 14px",
                                borderRadius: 10,
                                border: "1px solid rgba(255,255,255,.10)",
                                background: "rgba(0,0,0,.16)",
                                color: "rgba(231,238,252,1)",
                                fontSize: 16,
                                fontWeight: 600,
                                outline: "none",
                              }}
                            />
                            <button
                              className="btn"
                              onClick={() => setShares((s) => Math.max(0, s - 1))}
                              style={{ padding: "12px 16px", minWidth: 40 }}
                            >
                              −
                            </button>
                            <button
                              className="btn"
                              onClick={() => setShares((s) => s + 1)}
                              style={{ padding: "12px 16px", minWidth: 40 }}
                            >
                              +
                            </button>
                          </div>
                        </LearnHint>
                      </div>
                    </div>

                    <LearnHint
                      enabled={learnMode}
                      text="This summary shows the current market price, the number of shares involved, and the estimated total cost or credit for your order."
                      width={340}
                    >
                      <div
                        style={{
                          background: "rgba(0,0,0,.16)",
                          borderRadius: 12,
                          padding: 16,
                          marginBottom: 20,
                        }}
                      >
                        <div
                          style={{
                            display: "flex",
                            justifyContent: "space-between",
                            padding: "8px 0",
                            fontSize: 14,
                            color: "rgba(231,238,252,.7)",
                          }}
                        >
                          <span>Market Price</span>
                          <span style={{ fontWeight: 600, color: "rgba(231,238,252,1)" }}>
                            {stockData ? money(stockData.price) : "—"}
                          </span>
                        </div>

                        <div
                          style={{
                            display: "flex",
                            justifyContent: "space-between",
                            padding: "8px 0",
                            fontSize: 14,
                            color: "rgba(231,238,252,.7)",
                          }}
                        >
                          <span>{orderMode === "shares" ? "Shares" : "Estimated Shares"}</span>
                          <span style={{ fontWeight: 600, color: "rgba(231,238,252,1)" }}>
                            {orderMode === "shares"
                              ? shares
                              : stockData
                              ? (shares / stockData.price).toFixed(4)
                              : "—"}
                          </span>
                        </div>

                        <div
                          style={{
                            display: "flex",
                            justifyContent: "space-between",
                            padding: "12px 0 4px",
                            fontSize: 15,
                            fontWeight: 600,
                            borderTop: "1px solid rgba(255,255,255,.08)",
                            marginTop: 8,
                          }}
                        >
                          <span>Estimated {orderType === "buy" ? "Cost" : "Credit"}</span>
                          <span style={{ fontSize: 18 }}>{money(estimatedCost)}</span>
                        </div>
                      </div>
                    </LearnHint>

                    <LearnHint
                      enabled={learnMode}
                      text="This button submits your simulated order for educational use."
                      width={260}
                    >
                      <button
                        className="btn"
                        onClick={executeOrder}
                        disabled={shares <= 0}
                        style={{
                          width: "100%",
                          padding: "14px",
                          fontSize: 16,
                          fontWeight: 700,
                          background:
                            orderType === "buy"
                              ? "rgba(53,208,127,.95)"
                              : "rgba(255,107,107,.95)",
                          border: "none",
                          borderRadius: 10,
                          cursor: shares <= 0 ? "not-allowed" : "pointer",
                          opacity: shares <= 0 ? 0.5 : 1,
                        }}
                      >
                        Review {orderType === "buy" ? "Buy" : "Sell"} Order
                      </button>
                    </LearnHint>

                    <div
                      style={{
                        marginTop: 12,
                        textAlign: "center",
                        fontSize: 12,
                        color: "rgba(231,238,252,.55)",
                        fontStyle: "italic",
                      }}
                    >
                      This is an educational platform. No real buy or sell transactions are executed.
                    </div>
                  </div>
                </LearnHint>
              </>
            )}
          </div>
        </>
      )}

      {!selectedStock && (
        <div>
          <LearnHint
            enabled={learnMode}
            text="These are popular stocks you can click to explore. Selecting one loads its chart, price data, and trading panel."
            width={320}
          >
            <h2 style={{ fontSize: 24, fontWeight: 700, marginBottom: 16 }}>
              Popular Stocks
            </h2>
          </LearnHint>

          {popularLoading ? (
            <div style={{ color: "rgba(231,238,252,.65)" }}>
              Loading popular stocks...
            </div>
          ) : popularStocks.length === 0 ? (
            <div style={{ color: "rgba(231,238,252,.55)" }}>
              No popular stocks available
            </div>
          ) : (
            <div
              style={{
                display: "grid",
                gridTemplateColumns: "repeat(auto-fill, minmax(240px, 1fr))",
                gap: 16,
              }}
            >
              {popularStocks.map((stock) => (
                <LearnHint
                  key={stock.symbol}
                  enabled={learnMode}
                  text={`Click to load ${stock.symbol}, the ticker for ${stock.name}, and view its chart and order panel.`}
                  width={320}
                >
                  <div
                    className="kpiCard"
                    style={{ cursor: "pointer" }}
                    onClick={() => selectStock(stock.symbol)}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.background = "rgba(255,255,255,.06)";
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.background = "rgba(255,255,255,.03)";
                    }}
                  >
                    <div className="kpiLabel">{stock.symbol}</div>
                    <div style={{ fontSize: 18, fontWeight: 700, marginTop: 4 }}>
                      {stock.name}
                    </div>
                  </div>
                </LearnHint>
              ))}
            </div>
          )}
        </div>
      )}

      <style>{`
        @keyframes spin {
          to { transform: rotate(360deg); }
        }
        .btn {
          padding: 8px 14px;
          border-radius: 10px;
          border: 1px solid rgba(255,255,255,.10);
          background: rgba(255,255,255,.06);
          color: rgba(231,238,252,.9);
          cursor: pointer;
          font-size: 14px;
          font-weight: 600;
        }
        .btn:hover {
          background: rgba(255,255,255,.10);
        }
        .btn:disabled {
          opacity: 0.5;
          cursor: not-allowed;
        }
      `}</style>
    </div>
  );
}