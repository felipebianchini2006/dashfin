"use client";

import React from "react";
import type { AccountDto, CategoryDto } from "@/lib/api/types";

export type TransactionsFilterDraft = {
  from: string;
  to: string;
  accountId: string;
  categoryId: string;
  type: "" | "1" | "2";
  min: string;
  max: string;
  q: string;
};

export function TransactionsFilters({
  draft,
  accounts,
  categories,
  onChange,
  onApply,
  onReset
}: {
  draft: TransactionsFilterDraft;
  accounts: AccountDto[];
  categories: CategoryDto[];
  onChange: (patch: Partial<TransactionsFilterDraft>) => void;
  onApply: () => void;
  onReset: () => void;
}) {
  return (
    <div className="card">
      <div className="row" style={{ alignItems: "flex-end" }}>
        <div className="field">
          <label>From</label>
          <input type="date" value={draft.from} onChange={(e) => onChange({ from: e.target.value })} />
        </div>
        <div className="field">
          <label>To</label>
          <input type="date" value={draft.to} onChange={(e) => onChange({ to: e.target.value })} />
        </div>

        <div className="field" style={{ minWidth: 220, flex: "1 1 240px" }}>
          <label>Busca (description/notes)</label>
          <input
            value={draft.q}
            onChange={(e) => onChange({ q: e.target.value })}
            placeholder="Ex.: UBER"
            onKeyDown={(e) => e.key === "Enter" && onApply()}
          />
        </div>

        <div className="field" style={{ minWidth: 180 }}>
          <label>Conta</label>
          <select value={draft.accountId} onChange={(e) => onChange({ accountId: e.target.value })}>
            <option value="">Todas</option>
            {accounts.map((a) => (
              <option key={a.id} value={a.id}>
                {a.name}
              </option>
            ))}
          </select>
        </div>

        <div className="field" style={{ minWidth: 200 }}>
          <label>Categoria</label>
          <select value={draft.categoryId} onChange={(e) => onChange({ categoryId: e.target.value })}>
            <option value="">Todas</option>
            {categories.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
        </div>

        <div className="field" style={{ width: 140 }}>
          <label>Tipo</label>
          <select value={draft.type} onChange={(e) => onChange({ type: e.target.value as any })}>
            <option value="">Todos</option>
            <option value="1">Entrada</option>
            <option value="2">Saída</option>
          </select>
        </div>

        <div className="field" style={{ width: 140 }}>
          <label>Min (abs)</label>
          <input value={draft.min} onChange={(e) => onChange({ min: e.target.value })} inputMode="decimal" placeholder="0.00" />
        </div>

        <div className="field" style={{ width: 140 }}>
          <label>Max (abs)</label>
          <input value={draft.max} onChange={(e) => onChange({ max: e.target.value })} inputMode="decimal" placeholder="0.00" />
        </div>

        <button className="btn primary" onClick={onApply}>
          Aplicar
        </button>
        <button className="btn" onClick={onReset}>
          Limpar
        </button>
      </div>
    </div>
  );
}

