"use client";

import React, { useMemo } from "react";

type Point = { x: string; y: number };

const PADDING = { left: 40, right: 14, top: 10, bottom: 24 };

export function LineChart({
  points,
  height = 220
}: {
  points: Point[];
  height?: number;
}) {
  const width = 720;

  const { path, maxY, minY, labels } = useMemo(() => {
    const ys = points.map((p) => p.y);
    const max = Math.max(0, ...ys);
    const min = Math.min(0, ...ys);
    const usableW = width - PADDING.left - PADDING.right;
    const usableH = height - PADDING.top - PADDING.bottom;

    const scaleX = (i: number) => (points.length <= 1 ? 0 : (i / (points.length - 1)) * usableW);
    const scaleY = (y: number) => {
      const denom = max - min || 1;
      const t = (y - min) / denom;
      return usableH - t * usableH;
    };

    const d = points
      .map((p, i) => {
        const x = PADDING.left + scaleX(i);
        const y = PADDING.top + scaleY(p.y);
        return `${i === 0 ? "M" : "L"} ${x.toFixed(2)} ${y.toFixed(2)}`;
      })
      .join(" ");

    const step = Math.max(1, Math.floor(points.length / 6));
    const labelIdx = new Set<number>();
    for (let i = 0; i < points.length; i += step) labelIdx.add(i);
    labelIdx.add(points.length - 1);

    const labs = points.map((p, i) => ({ i, x: PADDING.left + scaleX(i), label: p.x })).filter((l) => labelIdx.has(l.i));

    return { path: d, maxY: max, minY: min, labels: labs };
  }, [height, points]);

  if (points.length === 0) {
    return <div className="muted">Sem dados.</div>;
  }

  return (
    <svg width="100%" height={height} viewBox={`0 0 ${width} ${height}`} style={{ display: "block" }}>
      <line
        x1={PADDING.left}
        y1={height - PADDING.bottom}
        x2={width - PADDING.right}
        y2={height - PADDING.bottom}
        stroke="rgba(148, 163, 184, 0.25)"
      />
      <line
        x1={PADDING.left}
        y1={PADDING.top}
        x2={PADDING.left}
        y2={height - PADDING.bottom}
        stroke="rgba(148, 163, 184, 0.25)"
      />

      <path d={path} fill="none" stroke="rgba(96,165,250,0.9)" strokeWidth={2.2} />

      {labels.map((l) => (
        <text key={l.i} x={l.x} y={height - 8} fill="rgba(148,163,184,0.9)" fontSize="11" textAnchor="middle">
          {l.label}
        </text>
      ))}

      <text x={PADDING.left} y={12} fill="rgba(148,163,184,0.9)" fontSize="11">
        {maxY.toFixed(0)}
      </text>
      <text x={PADDING.left} y={height - PADDING.bottom - 6} fill="rgba(148,163,184,0.9)" fontSize="11">
        {minY.toFixed(0)}
      </text>
    </svg>
  );
}
