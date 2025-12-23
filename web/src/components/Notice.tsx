"use client";

import React from "react";

export type NoticeKind = "success" | "error" | "info";

function styles(kind: NoticeKind): React.CSSProperties {
  const base: React.CSSProperties = {
    borderRadius: 12,
    padding: "10px 12px",
    border: "1px solid var(--border)",
    background: "rgba(255,255,255,0.02)",
    display: "flex",
    justifyContent: "space-between",
    alignItems: "flex-start",
    gap: 12
  };

  if (kind === "success") return { ...base, borderColor: "rgba(34,197,94,0.35)", background: "rgba(34,197,94,0.08)" };
  if (kind === "error") return { ...base, borderColor: "rgba(248,113,113,0.35)", background: "rgba(248,113,113,0.08)" };
  return { ...base };
}

export function Notice({
  kind,
  title,
  message,
  onClose
}: {
  kind: NoticeKind;
  title?: string;
  message: string;
  onClose?: () => void;
}) {
  return (
    <div style={styles(kind)} role={kind === "error" ? "alert" : undefined}>
      <div>
        {title ? <div style={{ fontWeight: 600, marginBottom: 2 }}>{title}</div> : null}
        <div className={kind === "error" ? "error" : "muted"} style={{ fontSize: 13 }}>
          {message}
        </div>
      </div>
      {onClose ? (
        <button className="btn" onClick={onClose}>
          OK
        </button>
      ) : null}
    </div>
  );
}

