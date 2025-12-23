"use client";

import React, { useMemo, useState } from "react";
import { useAccounts, useCategories, useTransactions } from "@/lib/api/hooks";
import { yyyyMm01 } from "@/lib/date";

export default function TransactionsPage() {
  const [month, setMonth] = useState(() => yyyyMm01(new Date()));
  const [q, setQ] = useState("");
  const [page, setPage] = useState(1);
  const [accountId, setAccountId] = useState<string>("");
  const [categoryId, setCategoryId] = useState<string>("");
  const [type, setType] = useState<string>("");

  const accounts = useAccounts();
  const categories = useCategories();

  const from = `${month}T00:00:00Z`;
  const to = useMemo(() => {
    const d = new Date(`${month}T00:00:00Z`);
    d.setUTCMonth(d.getUTCMonth() + 1);
    d.setUTCDate(d.getUTCDate() - 1);
    const y = d.getUTCFullYear();
    const m = String(d.getUTCMonth() + 1).padStart(2, "0");
    const day = String(d.getUTCDate()).padStart(2, "0");
    return `${y}-${m}-${day}T23:59:59Z`;
  }, [month]);

  const tx = useTransactions({
    page,
    page_size: 50,
    q: q.length ? q : undefined,
    account_id: accountId || undefined,
    category_id: categoryId || undefined,
    type: type ? (Number(type) as 1 | 2) : undefined,
    from,
    to
  });

  return (
    <div className="container">
      <div className="card">
        <div className="row" style={{ alignItems: "flex-end" }}>
          <div className="field">
            <label>Mês</label>
            <input type="date" value={month} onChange={(e) => setMonth(e.target.value)} />
          </div>
          <div className="field" style={{ minWidth: 220 }}>
            <label>Busca</label>
            <input
              value={q}
              onChange={(e) => setQ(e.target.value)}
              placeholder="description/notes"
              onKeyDown={(e) => e.key === "Enter" && setPage(1)}
            />
          </div>
          <div className="field">
            <label>Conta</label>
            <select value={accountId} onChange={(e) => setAccountId(e.target.value)}>
              <option value="">Todas</option>
              {(accounts.data ?? []).map((a) => (
                <option value={a.id} key={a.id}>
                  {a.name}
                </option>
              ))}
            </select>
          </div>
          <div className="field">
            <label>Categoria</label>
            <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)}>
              <option value="">Todas</option>
              {(categories.data ?? []).map((c) => (
                <option value={c.id} key={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          </div>
          <div className="field">
            <label>Tipo</label>
            <select value={type} onChange={(e) => setType(e.target.value)}>
              <option value="">Todos</option>
              <option value="1">Entrada</option>
              <option value="2">Saída</option>
            </select>
          </div>
          <button className="btn" onClick={() => setPage(1)}>
            Filtrar
          </button>
        </div>

        {tx.isError ? <div className="error" style={{ marginTop: 12 }}>Falha ao carregar transações.</div> : null}

        <div style={{ marginTop: 12 }}>
          <table className="table">
            <thead>
              <tr>
                <th>Data</th>
                <th>Descrição</th>
                <th>Conta</th>
                <th>Categoria</th>
                <th>Valor</th>
              </tr>
            </thead>
            <tbody>
              {(tx.data?.items ?? []).map((t) => (
                <tr key={t.id}>
                  <td>{t.occurredAt.slice(0, 10)}</td>
                  <td>
                    {t.description} {t.notes ? <span className="muted">({t.notes})</span> : null}
                  </td>
                  <td>{(accounts.data ?? []).find((a) => a.id === t.accountId)?.name ?? "—"}</td>
                  <td>{(categories.data ?? []).find((c) => c.id === t.categoryId)?.name ?? "—"}</td>
                  <td style={{ color: t.amount < 0 ? "var(--danger)" : undefined }}>{t.amount.toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <div className="row" style={{ justifyContent: "space-between", alignItems: "center", marginTop: 12 }}>
            <div className="muted">
              Total: {tx.data?.totalCount ?? 0} • Página {tx.data?.page ?? page}
            </div>
            <div style={{ display: "flex", gap: 10 }}>
              <button className="btn" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}>
                Anterior
              </button>
              <button
                className="btn"
                disabled={(tx.data?.items.length ?? 0) < 50}
                onClick={() => setPage((p) => p + 1)}
              >
                Próxima
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
