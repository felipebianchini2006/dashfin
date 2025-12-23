"use client";

import React, { useState } from "react";
import { yyyyMm01 } from "@/lib/date";
import { useBudgets, useCategories, useUpsertBudget } from "@/lib/api/hooks";

export default function BudgetsPage() {
  const [month, setMonth] = useState(() => yyyyMm01(new Date()));
  const budgets = useBudgets(month);
  const categories = useCategories();
  const upsert = useUpsertBudget();

  const [categoryId, setCategoryId] = useState("");
  const [amount, setAmount] = useState(0);

  return (
    <div className="container">
      <div className="row">
        <div className="card" style={{ flex: "1 1 520px" }}>
          <div className="row" style={{ alignItems: "flex-end", justifyContent: "space-between" }}>
            <h2 style={{ marginTop: 0 }}>Orçamentos</h2>
            <div className="field">
              <label>Mês</label>
              <input type="date" value={month} onChange={(e) => setMonth(e.target.value)} />
            </div>
          </div>

          <table className="table" style={{ marginTop: 12 }}>
            <thead>
              <tr>
                <th>Categoria</th>
                <th>Meta</th>
                <th>Gasto</th>
              </tr>
            </thead>
            <tbody>
              {(budgets.data ?? []).map((b) => (
                <tr key={b.id}>
                  <td>{(categories.data ?? []).find((c) => c.id === b.categoryId)?.name ?? b.categoryId}</td>
                  <td>{b.limitAmount.toFixed(2)}</td>
                  <td style={{ color: b.spentAmount > b.limitAmount ? "var(--danger)" : undefined }}>{b.spentAmount.toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="card" style={{ flex: "1 1 360px" }}>
          <h2 style={{ marginTop: 0 }}>Upsert budget</h2>
          <div className="field">
            <label>Categoria</label>
            <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)}>
              <option value="">Selecione</option>
              {(categories.data ?? []).map((c) => (
                <option value={c.id} key={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          </div>
          <div className="field" style={{ marginTop: 12 }}>
            <label>Meta (amount)</label>
            <input type="number" value={amount} onChange={(e) => setAmount(Number(e.target.value))} />
          </div>
          <button
            className="btn primary"
            style={{ marginTop: 16 }}
            disabled={!categoryId || upsert.isPending}
            onClick={async () => {
              await upsert.mutateAsync({ category_id: categoryId, month, amount });
            }}
          >
            {upsert.isPending ? "Salvando..." : "Salvar"}
          </button>
          {upsert.isError ? <div className="error" style={{ marginTop: 10 }}>Falha ao salvar budget.</div> : null}
        </div>
      </div>
    </div>
  );
}

