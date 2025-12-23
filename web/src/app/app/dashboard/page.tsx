"use client";

import React, { useMemo, useState } from "react";
import { yyyyMm01 } from "@/lib/date";
import { useDashboardBalances, useDashboardCategories, useDashboardSummary, useDashboardTimeseries } from "@/lib/api/hooks";

export default function DashboardPage() {
  const [month, setMonth] = useState(() => yyyyMm01(new Date()));

  const summary = useDashboardSummary(month);
  const categories = useDashboardCategories(month);
  const timeseries = useDashboardTimeseries(month);
  const balances = useDashboardBalances();

  const totalDaily = useMemo(() => {
    const items = timeseries.data?.items ?? [];
    const sum = items.reduce((acc, p) => acc + p.spentAmount, 0);
    return sum;
  }, [timeseries.data]);

  return (
    <div className="container">
      <div className="row" style={{ alignItems: "flex-end" }}>
        <div className="field">
          <label>Mês</label>
          <input type="date" value={month} onChange={(e) => setMonth(e.target.value)} />
        </div>
        <div className="muted" style={{ paddingBottom: 4 }}>
          Métricas ignoram lançamentos com <code>ignore_in_dashboard=true</code>.
        </div>
      </div>

      <div className="row" style={{ marginTop: 16 }}>
        <div className="card" style={{ flex: "1 1 260px" }}>
          <div className="muted">Entradas (CHECKING)</div>
          <div style={{ fontSize: 22, marginTop: 6 }}>{summary.data?.incomeAmount?.toFixed?.(2) ?? "—"}</div>
        </div>
        <div className="card" style={{ flex: "1 1 260px" }}>
          <div className="muted">Saídas (CHECKING)</div>
          <div style={{ fontSize: 22, marginTop: 6 }}>{summary.data?.checkingOutAmount?.toFixed?.(2) ?? "—"}</div>
        </div>
        <div className="card" style={{ flex: "1 1 260px" }}>
          <div className="muted">Gasto cartão (mês)</div>
          <div style={{ fontSize: 22, marginTop: 6 }}>{summary.data?.creditCardSpendAmount?.toFixed?.(2) ?? "—"}</div>
        </div>
        <div className="card" style={{ flex: "1 1 260px" }}>
          <div className="muted">Saldo líquido do mês</div>
          <div style={{ fontSize: 22, marginTop: 6 }}>{summary.data?.netCashAmount?.toFixed?.(2) ?? "—"}</div>
        </div>
      </div>

      <div className="row" style={{ marginTop: 16 }}>
        <div className="card" style={{ flex: "2 1 520px" }}>
          <div style={{ display: "flex", justifyContent: "space-between" }}>
            <div>Timeseries (saídas por dia)</div>
            <div className="muted">Total: {totalDaily.toFixed(2)}</div>
          </div>
          <div style={{ marginTop: 12, maxHeight: 260, overflow: "auto" }}>
            <table className="table">
              <thead>
                <tr>
                  <th>Dia</th>
                  <th>Gasto</th>
                </tr>
              </thead>
              <tbody>
                {(timeseries.data?.items ?? []).map((p) => (
                  <tr key={p.date}>
                    <td>{p.date}</td>
                    <td>{p.spentAmount.toFixed(2)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        <div className="card" style={{ flex: "1 1 360px" }}>
          <div>Top categorias (despesas)</div>
          <div style={{ marginTop: 12 }}>
            <table className="table">
              <thead>
                <tr>
                  <th>Categoria</th>
                  <th>Gasto</th>
                </tr>
              </thead>
              <tbody>
                {(summary.data?.topCategories ?? []).map((c) => (
                  <tr key={c.categoryId}>
                    <td>{c.categoryName}</td>
                    <td>{c.spentAmount.toFixed(2)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="muted" style={{ marginTop: 10 }}>
            Distribuição completa: {categories.data?.items.length ?? 0} categorias
          </div>
        </div>
      </div>

      <div className="row" style={{ marginTop: 16 }}>
        <div className="card" style={{ flex: "1 1 520px" }}>
          <div>Progresso de budgets</div>
          <div style={{ marginTop: 12 }}>
            <table className="table">
              <thead>
                <tr>
                  <th>Categoria</th>
                  <th>Gasto</th>
                  <th>Meta</th>
                  <th>%</th>
                </tr>
              </thead>
              <tbody>
                {(summary.data?.budgetProgress ?? []).map((b) => (
                  <tr key={b.categoryId}>
                    <td>{b.categoryName}</td>
                    <td>{b.spentAmount.toFixed(2)}</td>
                    <td>{b.limitAmount.toFixed(2)}</td>
                    <td style={{ color: b.isOverBudget ? "var(--danger)" : undefined }}>{b.progressPercent.toFixed(1)}%</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        <div className="card" style={{ flex: "1 1 520px" }}>
          <div>Saldos</div>
          <div className="row" style={{ marginTop: 12 }}>
            <div style={{ flex: "1 1 240px" }}>
              <div className="muted">Total guardado (SAVINGS)</div>
              <div style={{ fontSize: 20, marginTop: 6 }}>{balances.data?.totalSaved?.toFixed?.(2) ?? "—"}</div>
            </div>
            <div style={{ flex: "1 1 240px" }}>
              <div className="muted">Cartão em aberto</div>
              <div style={{ fontSize: 20, marginTop: 6 }}>{balances.data?.creditCardOpen?.toFixed?.(2) ?? "—"}</div>
            </div>
            <div style={{ flex: "1 1 240px" }}>
              <div className="muted">Patrimônio líquido (simples)</div>
              <div style={{ fontSize: 20, marginTop: 6 }}>{balances.data?.netWorth?.toFixed?.(2) ?? "—"}</div>
            </div>
          </div>
          <div style={{ marginTop: 12 }}>
            <table className="table">
              <thead>
                <tr>
                  <th>Conta</th>
                  <th>Saldo</th>
                </tr>
              </thead>
              <tbody>
                {(balances.data?.checkingAccounts ?? []).map((a) => (
                  <tr key={a.accountId}>
                    <td>{a.name}</td>
                    <td>
                      {a.balance.toFixed(2)} <span className="muted">{a.currency}</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
}

