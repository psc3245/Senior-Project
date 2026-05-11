export default function PriceChart({ data }) {
    const w = 900, h = 220, pad = 16;
    const min = Math.min(...data), max = Math.max(...data);
    const span = Math.max(1e-9, max - min);
  
    const pts = data.map((v, i) => {
      const x = pad + (i * (w - pad * 2)) / (data.length - 1);
      const y = pad + (1 - (v - min) / span) * (h - pad * 2);
      return [x, y];
    });
  
    const d = pts.map(([x,y], i) => `${i===0 ? "M" : "L"} ${x} ${y}`).join(" ");
  
    return (
      <svg className="chartArea" viewBox={`0 0 ${w} ${h}`} preserveAspectRatio="none">
        {/* light grid line */}
        <line x1="0" y1={h-1} x2={w} y2={h-1} className="chartGrid" />
        <path d={d} className="chartLine" />
      </svg>
    );
  }
  