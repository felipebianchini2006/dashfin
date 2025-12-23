"use client";

import React, { useMemo } from "react";
import { chartColors } from "./chartColors";

type Item = { label: string; value: number };

export function BarChart({
  items,
  height = 260
}: {
  items: Item[];
  height?: number;
}) {
  const width = 720;
  const padding = { left: 10, right: 10, top: 10, bottom: 10 };

  const rows = useMemo(() => {
    const max = Math.max(0, ...items.map((i) => i.value));
    return items.map((it, idx) => ({
      ...it,
      pct: max > 0 ? it.value / max : 0,
      color: chartColors[idx % chartColors.length]
    }));
  }, [items]);

  if (items.length === 0) return <div className="muted">Sem dados.</div>;

  // Render as horizontal bars for readability.
  const rowH = 28;
  const h = Math.max(height, padding.top + padding.bottom + rowH * rows.length + 8);

  return (
    <svg width="100%" height={h} viewBox={`0 0 ${width} ${h}`} style={{ display: "block" }}>
      {rows.map((r, i) => {
        const y = padding.top + i * rowH;
        const barW = Math.round((width - padding.left - padding.right - 240) * r.pct);
        return (
          <g key={`${r.label}:${i}`}>
            <text x={padding.left} y={y + 18} fill="rgba(229,231,235,0.95)" fontSize="12">
              {r.label.length > 26 ? r.label.slice(0, 26) + "…" : r.label}
            </text>
            <rect x={240} y={y + 7} width={width - 240 - padding.right} height={14} rx={7} fill="rgba(148,163,184,0.12)" />
            <rect x={240} y={y + 7} width={barW} height={14} rx={7} fill={r.color} opacity={0.55} />
            <text x={width - padding.right} y={y + 18} fill="rgba(148,163,184,0.95)" fontSize="12" textAnchor="end">
              {r.value.toFixed(2)}
            </text>
          </g>
        );
      })}
    </svg>
  );
}

