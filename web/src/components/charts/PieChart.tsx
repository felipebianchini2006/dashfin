"use client";

import React, { useMemo } from "react";
import { chartColors } from "./chartColors";

type Slice = { label: string; value: number };

function polarToCartesian(cx: number, cy: number, r: number, angleDeg: number) {
  const angleRad = ((angleDeg - 90) * Math.PI) / 180.0;
  return { x: cx + r * Math.cos(angleRad), y: cy + r * Math.sin(angleRad) };
}

function describeArc(cx: number, cy: number, r: number, startAngle: number, endAngle: number) {
  const start = polarToCartesian(cx, cy, r, endAngle);
  const end = polarToCartesian(cx, cy, r, startAngle);
  const largeArcFlag = endAngle - startAngle <= 180 ? "0" : "1";
  return ["M", start.x, start.y, "A", r, r, 0, largeArcFlag, 0, end.x, end.y, "L", cx, cy, "Z"].join(" ");
}

export function PieChart({
  slices,
  height = 240
}: {
  slices: Slice[];
  height?: number;
}) {
  const width = 720;
  const cx = 160;
  const cy = height / 2;
  const r = Math.min(100, height / 2 - 10);

  const { arcs, total } = useMemo(() => {
    const t = slices.reduce((acc, s) => acc + Math.max(0, s.value), 0);
    let angle = 0;
    const a = slices.map((s, idx) => {
      const v = Math.max(0, s.value);
      const delta = t > 0 ? (v / t) * 360 : 0;
      const start = angle;
      const end = angle + delta;
      angle = end;
      return {
        label: s.label,
        value: s.value,
        pct: t > 0 ? v / t : 0,
        d: describeArc(cx, cy, r, start, end),
        color: chartColors[idx % chartColors.length]
      };
    });
    return { arcs: a, total: t };
  }, [cy, r, slices]);

  if (slices.length === 0) return <div className="muted">Sem dados.</div>;
  if (total <= 0) return <div className="muted">Sem dados (total 0).</div>;

  return (
    <svg width="100%" height={height} viewBox={`0 0 ${width} ${height}`} style={{ display: "block" }}>
      {arcs.map((a) => (
        <path key={a.label} d={a.d} fill={a.color} opacity={0.6} stroke="rgba(17,24,39,0.6)" strokeWidth={1} />
      ))}

      <g transform={`translate(320, 18)`}>
        {arcs.slice(0, 10).map((a, i) => (
          <g key={`${a.label}:${i}`} transform={`translate(0, ${i * 20})`}>
            <rect x={0} y={4} width={12} height={12} rx={3} fill={a.color} opacity={0.7} />
            <text x={18} y={14} fill="rgba(229,231,235,0.95)" fontSize="12">
              {a.label.length > 28 ? a.label.slice(0, 28) + "…" : a.label}
            </text>
            <text x={360} y={14} fill="rgba(148,163,184,0.95)" fontSize="12" textAnchor="end">
              {(a.pct * 100).toFixed(1)}% • {a.value.toFixed(2)}
            </text>
          </g>
        ))}
      </g>
    </svg>
  );
}

