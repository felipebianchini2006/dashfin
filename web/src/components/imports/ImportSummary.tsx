"use client";

import React, { useMemo } from "react";

type Summary = {
  layout?: string;
  period?: { start?: string; end?: string };
  counts?: { parsed?: number; inserted?: number; deduped?: number; errors?: number };
  totals?: { inAmount?: number; outAmount?: number; cardSpend?: number };
};

function safeParse(json?: string | null): Summary | null {
  if (!json) return null;
  try {
    return JSON.parse(json) as Summary;
  } catch {
    return null;
  }
}

export function ImportSummary({ summaryJson }: { summaryJson: string | null }) {
  const summary = useMemo(() => safeParse(summaryJson), [summaryJson]);

  if (!summary) return <div className="muted">Sem summary_json.</div>;

  return (
    <div className="row">
      <div style={{ flex: "1 1 220px" }}>
        <div className="muted">Layout</div>
        <div>{summary.layout ?? "—"}</div>
      </div>
      <div style={{ flex: "1 1 260px" }}>
        <div className="muted">Período</div>
        <div>
          {(summary.period?.start ?? "—").slice(0, 10)} → {(summary.period?.end ?? "—").slice(0, 10)}
        </div>
      </div>
      <div style={{ flex: "1 1 280px" }}>
        <div className="muted">Contagens</div>
        <div>
          parsed={summary.counts?.parsed ?? 0} • inserted={summary.counts?.inserted ?? 0} • deduped={summary.counts?.deduped ?? 0} •
          errors={summary.counts?.errors ?? 0}
        </div>
      </div>
      <div style={{ flex: "1 1 320px" }}>
        <div className="muted">Totais</div>
        <div>
          in={Number(summary.totals?.inAmount ?? 0).toFixed(2)} • out={Number(summary.totals?.outAmount ?? 0).toFixed(2)} •
          card={Number(summary.totals?.cardSpend ?? 0).toFixed(2)}
        </div>
      </div>
    </div>
  );
}

