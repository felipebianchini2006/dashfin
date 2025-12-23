"use client";

import React, { useEffect, useMemo, useState } from "react";
import type { BudgetDto, CategoryDto } from "@/lib/api/types";

function normalizeAmount(v: string): number | null {
  const t = v.trim().replace(",", ".");
  if (!t) return null;
  const n = Number(t);
  return Number.isFinite(n) ? n : null;
}

function pct(spent: number, limit: number) {
  if (limit <= 0) return 0;
  return Math.max(0, Math.min(200, (spent / limit) * 100));
}

const controlStyle: React.CSSProperties = {
  borderRadius: 10,
  border: "1px solid var(--border)",
  padding: "8px 10px",
  background: "rgba(255, 255, 255, 0.02)",
  color: "var(--text)"
};

export function BudgetsEditor({
  categories,
  budgets,
  savingCategoryId,
  onSave
}: {
  categories: CategoryDto[];
  budgets: BudgetDto[];
  savingCategoryId: string | null;
  onSave: (categoryId: string, amount: number) => void;
}) {
  const budgetByCategory = useMemo(() => {
    const m = new Map<string, BudgetDto>();
    for (const b of budgets) m.set(b.categoryId, b);
    return m;
  }, [budgets]);

  const [draft, setDraft] = useState<Record<string, string>>({});

  useEffect(() => {
    const next: Record<string, string> = {};
    for (const c of categories) {
      const b = budgetByCategory.get(c.id);
      next[c.id] = b ? String(b.limitAmount.toFixed(2)) : "";
    }
    setDraft(next);
  }, [budgetByCategory, categories]);

  const categoriesSorted = useMemo(() => [...categories].sort((a, b) => a.name.localeCompare(b.name)), [categories]);

  if (categoriesSorted.length === 0) return <div className="muted">Nenhuma categoria.</div>;

  return (
    <table className="table" style={{ marginTop: 12 }}>
      <thead>
        <tr>
          <th>Categoria</th>
          <th style={{ width: 160 }}>Meta</th>
          <th style={{ width: 140 }}>Gasto</th>
          <th style={{ width: 100 }}>%</th>
          <th style={{ width: 220 }}>Progresso</th>
          <th style={{ width: 120 }} />
        </tr>
      </thead>
      <tbody>
        {categoriesSorted.map((c) => {
          const b = budgetByCategory.get(c.id);
          const limit = b?.limitAmount ?? 0;
          const spent = b?.spentAmount ?? 0;
          const percent = limit > 0 ? pct(spent, limit) : 0;
          const over = limit > 0 && spent > limit;

          const value = draft[c.id] ?? "";
          const n = normalizeAmount(value);
          const isSaving = savingCategoryId === c.id;
          const changed = (b ? b.limitAmount.toFixed(2) : "") !== value.trim();

          return (
            <tr key={c.id}>
              <td>{c.name}</td>
              <td>
                <input
                  value={value}
                  onChange={(e) => setDraft((d) => ({ ...d, [c.id]: e.target.value }))}
                  inputMode="decimal"
                  placeholder="—"
                  style={{ ...controlStyle, width: "100%" }}
                />
              </td>
              <td style={{ color: over ? "var(--danger)" : undefined }}>{spent.toFixed(2)}</td>
              <td style={{ color: over ? "var(--danger)" : undefined }}>{limit > 0 ? `${(spent / limit * 100).toFixed(1)}%` : "—"}</td>
              <td>
                <div style={{ height: 10, borderRadius: 999, background: "rgba(148,163,184,0.12)", overflow: "hidden" }}>
                  <div
                    style={{
                      width: `${Math.min(100, percent).toFixed(1)}%`,
                      height: "100%",
                      background: over ? "rgba(248,113,113,0.55)" : "rgba(96,165,250,0.55)"
                    }}
                  />
                </div>
              </td>
              <td>
                <button
                  className="btn primary"
                  disabled={isSaving || n === null || n < 0 || !changed}
                  onClick={() => onSave(c.id, n ?? 0)}
                >
                  {isSaving ? "Salvando..." : "Salvar"}
                </button>
              </td>
            </tr>
          );
        })}
      </tbody>
    </table>
  );
}
