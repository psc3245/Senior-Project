import {
  LineChart,
  Line,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
  CartesianGrid,
} from "recharts";

const money = (n) =>
  typeof n === "number" && Number.isFinite(n)
    ? n.toLocaleString(undefined, { style: "currency", currency: "USD", maximumFractionDigits: 0 })
    : "—";

const pctText = (n) => {
  if (typeof n !== "number" || !Number.isFinite(n)) return "—";
  const sign = n >= 0 ? "+" : "";
  return `${sign}${n.toFixed(2)}%`;
};

function formatShortDate(value) {
  if (!value) return "";
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  return `${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function CustomTooltip({ active, payload }) {
  if (!active || !payload || !payload.length) return null;

  const point = payload[0].payload;
  const gainClass = point.dayChange >= 0 ? "#34d399" : "#f87171";

  return (
    <div
      style={{
        background: "rgba(15, 23, 42, 0.96)",
        border: "1px solid rgba(255,255,255,0.10)",
        borderRadius: 12,
        padding: "10px 12px",
        color: "#e5eefc",
        boxShadow: "0 10px 30px rgba(0,0,0,0.25)",
        minWidth: 170,
      }}
    >
      <div style={{ fontSize: 12, opacity: 0.7, marginBottom: 6 }}>
        {formatShortDate(point.date)}
        </div>
      <div style={{ fontSize: 18, fontWeight: 700, marginBottom: 4 }}>
        {money(point.totalValue)}
      </div>
      <div style={{ fontSize: 13, color: gainClass }}>
        {point.dayChange >= 0 ? "+" : ""}
        {money(point.dayChange)} ({pctText(point.dayChangePct)})
      </div>
    </div>
  );
}

export default function PriceChart({ data = [] }) {
  if (!Array.isArray(data) || data.length === 0) {
    return (
      <div
        style={{
          height: 320,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          color: "rgba(231,238,252,.55)",
          fontSize: 14,
        }}
      >
        No portfolio history yet.
      </div>
    );
  }

  const values = data
    .map((d) => Number(d.totalValue))
    .filter((n) => Number.isFinite(n));

    const min = Math.min(...values);
    const max = Math.max(...values);

    const padding = (max - min) * 0.15 || 500;

  return (
    <div style={{ width: "100%", height: 320 }}>
      <ResponsiveContainer width="100%" height="100%">
        <LineChart
          data={data}
          margin={{ top: 12, right: 20, left: 12, bottom: 8 }}
        >
          <CartesianGrid
            vertical={false}
            stroke="rgba(255,255,255,0.07)"
          />

          <XAxis
            dataKey="date"
            type="category"
            interval={0}
            ticks={data.map((d) => d.date)}
            tickFormatter={formatShortDate}
            tick={{ fill: "rgba(231,238,252,.55)", fontSize: 12 }}
            axisLine={{ stroke: "rgba(255,255,255,0.10)" }}
            tickLine={{ stroke: "rgba(255,255,255,0.10)" }}
            allowDuplicatedCategory={false}
            />

          <YAxis
            domain={[min - padding, max + padding * 0.35]}
            tickFormatter={(v) => `$${Math.round(v / 1000)}k`}
            tick={{ fill: "rgba(231,238,252,.55)", fontSize: 12 }}
            axisLine={false}
            tickLine={false}
            width={52}
          />

          <Tooltip
            content={<CustomTooltip />}
            cursor={{ stroke: "rgba(255,255,255,0.12)", strokeWidth: 1 }}
          />

          <Line
            type="linear"
            dataKey="totalValue"
            stroke="#34d399"
            strokeWidth={4}
            dot={false}
            activeDot={{
              r: 5,
              stroke: "#34d399",
              strokeWidth: 2,
              fill: "#0f172a",
            }}
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}