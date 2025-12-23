"use client";

import React, { useEffect, useMemo, useState } from "react";
import { useUpdateTransaction } from "@/lib/api/hooks";
import type { CategoryDto, CategoryRuleSuggestionDto, TransactionDto } from "@/lib/api/types";

const EMPTY_GUID = "00000000-0000-0000-0000-000000000000";

function formatAmount(amount: number) {
  const sign = amount < 0 ? "-" : "";
  const abs = Math.abs(amount);
  return `${sign}${abs.toFixed(2)}`;
}

const controlStyle: React.CSSProperties = {
  borderRadius: 10,
  border: "1px solid var(--border)",
  padding: "8px 10px",
  background: "rgba(255, 255, 255, 0.02)",
  color: "var(--text)",
  width: "100%"
};

function TransactionRow({
  tx,
  accountName,
  categories,
  onSuggestedRule
}: {
  tx: TransactionDto;
  accountName: string;
  categories: CategoryDto[];
  onSuggestedRule?: (s: CategoryRuleSuggestionDto) => void;
}) {
  const update = useUpdateTransaction();
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const categoryValue = tx.categoryId ?? EMPTY_GUID;
  const [categoryId, setCategoryId] = useState<string>(categoryValue);
  const [notesDraft, setNotesDraft] = useState<string>(tx.notes ?? "");

  useEffect(() => {
    setCategoryId(tx.categoryId ?? EMPTY_GUID);
  }, [tx.categoryId]);

  useEffect(() => {
    setNotesDraft(tx.notes ?? "");
  }, [tx.notes]);

  const notesChanged = notesDraft !== (tx.notes ?? "");

  async function patch(vars: { category_id?: string; notes?: string; ignore_in_dashboard?: boolean }) {
    setError(null);
    setSaving(true);
    try {
      const res = await update.mutateAsync({ id: tx.id, ...vars });
      if (res.suggestedRule) onSuggestedRule?.(res.suggestedRule);
    } catch (e: any) {
      setError(e?.response?.data?.detail ?? "Falha ao salvar.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <tr>
      <td style={{ whiteSpace: "nowrap" }}>{tx.occurredAt.slice(0, 10)}</td>
      <td style={{ minWidth: 260 }}>
        <div>{tx.description}</div>
      </td>
      <td style={{ whiteSpace: "nowrap" }}>{accountName}</td>
      <td style={{ minWidth: 220 }}>
        <select
          value={categoryId}
          style={controlStyle}
          disabled={saving}
          onChange={async (e) => {
            const next = e.target.value;
            if (next === categoryId) return;
            setCategoryId(next);
            await patch({ category_id: next });
          }}
        >
          <option value={EMPTY_GUID}>—</option>
          {categories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
      </td>
      <td style={{ textAlign: "right", whiteSpace: "nowrap", color: tx.amount < 0 ? "var(--danger)" : undefined }}>
        {formatAmount(tx.amount)}
      </td>
      <td style={{ minWidth: 260 }}>
        <div style={{ display: "flex", gap: 10, alignItems: "center" }}>
          <input
            value={notesDraft}
            style={controlStyle}
            disabled={saving}
            onChange={(e) => setNotesDraft(e.target.value)}
            placeholder="—"
            onKeyDown={(e) => {
              if (e.key === "Enter") patch({ notes: notesDraft });
              if (e.key === "Escape") setNotesDraft(tx.notes ?? "");
            }}
          />
          <button className="btn" disabled={saving || !notesChanged} onClick={() => patch({ notes: notesDraft })}>
            Salvar
          </button>
        </div>
      </td>
      <td style={{ textAlign: "center", width: 80 }}>
        <input
          type="checkbox"
          checked={tx.ignoreInDashboard}
          disabled={saving}
          onChange={(e) => patch({ ignore_in_dashboard: e.target.checked })}
          title="ignore_in_dashboard"
        />
      </td>
      <td style={{ width: 140 }}>
        <div className="muted" style={{ fontSize: 12 }}>
          {saving ? "Salvando..." : tx.type === 1 ? "Entrada" : "Saída"}
        </div>
        {error ? (
          <div className="error" style={{ marginTop: 4 }}>
            {error}
          </div>
        ) : null}
      </td>
    </tr>
  );
}

export function TransactionsTable({
  transactions,
  accountsById,
  categories,
  onSuggestedRule
}: {
  transactions: TransactionDto[];
  accountsById: Record<string, string>;
  categories: CategoryDto[];
  onSuggestedRule?: (s: CategoryRuleSuggestionDto) => void;
}) {
  const categoriesSorted = useMemo(
    () => [...categories].sort((a, b) => a.name.localeCompare(b.name)),
    [categories]
  );

  return (
    <table className="table">
      <thead>
        <tr>
          <th>Data</th>
          <th>Descrição</th>
          <th>Conta</th>
          <th>Categoria</th>
          <th style={{ textAlign: "right" }}>Valor</th>
          <th>Notes</th>
          <th style={{ textAlign: "center" }}>Ignore</th>
          <th style={{ width: 140 }}>Status</th>
        </tr>
      </thead>
      <tbody>
        {transactions.map((t) => (
          <TransactionRow
            key={t.id}
            tx={t}
            accountName={accountsById[t.accountId] ?? "—"}
            categories={categoriesSorted}
            onSuggestedRule={onSuggestedRule}
          />
        ))}
      </tbody>
    </table>
  );
}
