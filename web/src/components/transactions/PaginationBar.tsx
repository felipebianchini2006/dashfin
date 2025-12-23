"use client";

import React from "react";

export function PaginationBar({
  page,
  pageSize,
  totalCount,
  onPage
}: {
  page: number;
  pageSize: number;
  totalCount: number;
  onPage: (page: number) => void;
}) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const canPrev = page > 1;
  const canNext = page < totalPages;

  return (
    <div className="row" style={{ justifyContent: "space-between", alignItems: "center", marginTop: 12 }}>
      <div className="muted">
        Total: {totalCount} • Página {page} / {totalPages}
      </div>
      <div style={{ display: "flex", gap: 10 }}>
        <button className="btn" disabled={!canPrev} onClick={() => onPage(Math.max(1, page - 1))}>
          Anterior
        </button>
        <button className="btn" disabled={!canNext} onClick={() => onPage(Math.min(totalPages, page + 1))}>
          Próxima
        </button>
      </div>
    </div>
  );
}

