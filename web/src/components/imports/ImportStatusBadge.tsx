"use client";

import React from "react";
import { ImportStatus } from "@/lib/api/types";

export function ImportStatusBadge({ status }: { status: ImportStatus }) {
  const label = ImportStatus[status] ?? String(status);
  const color =
    status === ImportStatus.Done
      ? "rgba(34,197,94,0.18)"
      : status === ImportStatus.Failed
        ? "rgba(248,113,113,0.18)"
        : status === ImportStatus.Processing
          ? "rgba(96,165,250,0.18)"
          : "rgba(148,163,184,0.12)";

  return (
    <span
      style={{
        display: "inline-block",
        padding: "4px 10px",
        borderRadius: 999,
        border: "1px solid var(--border)",
        background: color,
        fontSize: 12
      }}
    >
      {label.toUpperCase()}
    </span>
  );
}

