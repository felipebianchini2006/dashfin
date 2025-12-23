"use client";

import React, { useMemo, useState } from "react";
import { yyyyMm01 } from "@/lib/date";
import { useDashboard, useDashboardBalances, useDashboardCategories, useDashboardSummary, useDashboardTimeseries } from "@/lib/api/hooks";
import { LineChart } from "@/components/charts/LineChart";
import { BarChart } from "@/components/charts/BarChart";
import { PieChart } from "@/components/charts/PieChart";

function normalizeMonth(value: string) {
  if (!value) return value;
  // accept YYYY-MM-DD and normalize to first day
  const m = value.slice(0, 7);
  if (m.length !== 7) return value;
  return `${m}-01`;
}

function fmtMoney(v: number | null | undefined) {
  if (v === null || v === undefined || Number.isNaN(v)) return "—";
  return v.toFixed(2);
}

export default function DashboardPage() {
  const [month, setMonth] = useState(() => yyyyMm01(new Date()));
  const [distMode, setDistMode] = useState<"bar" | "pie">("bar");

  const summary = useDashboardSummary(month);
  const categories = useDashboardCategories(month);
  const timeseries = useDashboardTimeseries(month);
  const balances = useDashboardBalances();
  const dashboard = useDashboard(month);

  const totalDaily = useMemo(() => {
    const items = timeseries.data?.items ?? [];
    const sum = items.reduce((acc, p) => acc + p.spentAmount, 0);
    return sum;
  }, [timeseries.data]);

  const timeseriesPoints = useMemo(
    () =>
      (timeseries.data?.items ?? []).map((p) => ({
        x: p.date.slice(8, 10),
        y: p.spentAmount
      })),
    [timeseries.data]
  );

  const distItems = useMemo(
    () => (categories.data?.items ?? []).slice(0, 10).map((c) => ({ label: c.categoryName, value: c.spentAmount })),
    [categories.data]
  );

  const riskCategories = useMemo(() => {
    const cats = dashboard.data?.forecast?.categories ?? [];
    return cats
      .filter((c) => c.riskOfExceedingBudget)
      .sort((a, b) => (b.projectedTotal ?? 0) - (a.projectedTotal ?? 0))
      .slice(0, 8);
  }, [dashboard.data]);

  return (
    <div className="container">
      <div className="row" style={{ alignItems: "flex-end" }}>
        <div className="field">
          <label>Mês</label>
          <input
            type="date"
            value={month}
            onChange={(e) => setMonth(normalizeMonth(e.target.value))}
          />
        </div>
        <div className="muted" style={{ paddingBottom: 4 }}>
          Métricas ignoram lançamentos com <code>ignore_in_dashboard=true</code>.
        </div>
      </div>

      <div className="row" style={{ marginTop: 16 }}>
        <div className="card" style={{ flex: "1 1 260px" }}>
          <div className="muted">Entradas (CHECKING)</div>
          <div style={{ fontSize: 22, marginTop: 6 }}>
            {summary.isLoading ? "Carregando..." : summary.isError ? "Erro" : fmtMoney(summary.data?.incomeAmount)}
          </div>
        </div>
        <div className="card" style={{ flex: "1 1 260px" }}>
          <div className="muted">Saídas (CHECKING)</div>
          <div style={{ fontSize: 22, marginTop: 6 }}>
            {summary.isLoading ? "Carregando..." : summary.isError ? "Erro" : fmtMoney(summary.data?.checkingOutAmount)}
          </div>
        </div>
        <div className="card" style={{ flex: "1 1 260px" }}>
          <div className="muted">Gasto cartão (mês)</div>
          <div style={{ fontSize: 22, marginTop: 6 }}>
            {summary.isLoading ? "Carregando..." : summary.isError ? "Erro" : fmtMoney(summary.data?.creditCardSpendAmount)}
          </div>
        </div>
        <div className="card" style={{ flex: "1 1 260px" }}>
          <div className="muted">Saldo líquido do mês</div>
          <div style={{ fontSize: 22, marginTop: 6 }}>
            {summary.isLoading ? "Carregando..." : summary.isError ? "Erro" : fmtMoney(summary.data?.netCashAmount)}
          </div>
        </div>
      </div>

      <div className="row" style={{ marginTop: 16 }}>
        <div className="card" style={{ flex: "2 1 520px" }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "baseline" }}>
            <div>Série diária (gasto/dia)</div>
            <div className="muted">Total: {fmtMoney(totalDaily)}</div>
          </div>
          <div style={{ marginTop: 12 }}>
            {timeseries.isLoading ? (
              <div className="muted">Carregando...</div>
            ) : timeseries.isError ? (
              <div className="error">Falha ao carregar série diária.</div>
            ) : (
              <LineChart points={timeseriesPoints} />
            )}
          </div>
        </div>

        <div className="card" style={{ flex: "1 1 360px" }}>
          <div>Top categorias (despesas)</div>
          <div style={{ marginTop: 12 }}>
            {summary.isLoading ? (
              <div className="muted">Carregando...</div>
            ) : summary.isError ? (
              <div className="error">Falha ao carregar top categorias.</div>
            ) : (summary.data?.topCategories?.length ?? 0) === 0 ? (
              <div className="muted">Sem dados no período.</div>
            ) : (
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
            )}
          </div>
          <div className="muted" style={{ marginTop: 10 }}>
            Distribuição: {categories.data?.items.length ?? 0} categorias
          </div>
        </div>
      </div>

      <div className="row" style={{ marginTop: 16 }}>
        <div className="card" style={{ flex: "1 1 520px" }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
            <div>Distribuição por categoria</div>
            <div style={{ display: "flex", gap: 10 }}>
              <button className="btn" onClick={() => setDistMode("bar")} disabled={distMode === "bar"}>
                Barra
              </button>
              <button className="btn" onClick={() => setDistMode("pie")} disabled={distMode === "pie"}>
                Pizza
              </button>
            </div>
          </div>

          <div style={{ marginTop: 12 }}>
            {categories.isLoading ? (
              <div className="muted">Carregando...</div>
            ) : categories.isError ? (
              <div className="error">Falha ao carregar distribuição.</div>
            ) : distMode === "bar" ? (
              <BarChart items={distItems} />
            ) : (
              <PieChart slices={distItems} />
            )}
          </div>
          <div className="muted" style={{ marginTop: 10 }}>
            Mostrando top {distItems.length} categorias por gasto.
          </div>
        </div>

        <div className="card" style={{ flex: "1 1 520px" }}>
          <div>Patrimônio</div>
          <div className="row" style={{ marginTop: 12 }}>
            <div style={{ flex: "1 1 240px" }}>
              <div className="muted">Saldo CHECKING (total)</div>
              <div style={{ fontSize: 20, marginTop: 6 }}>
                {balances.isLoading
                  ? "Carregando..."
                  : balances.isError
                    ? "Erro"
                    : fmtMoney((balances.data?.checkingAccounts ?? []).reduce((acc, a) => acc + a.balance, 0))}
              </div>
            </div>
            <div style={{ flex: "1 1 240px" }}>
              <div className="muted">Total guardado (SAVINGS)</div>
              <div style={{ fontSize: 20, marginTop: 6 }}>
                {balances.isLoading ? "Carregando..." : balances.isError ? "Erro" : fmtMoney(balances.data?.totalSaved)}
              </div>
            </div>
            <div style={{ flex: "1 1 240px" }}>
              <div className="muted">Patrimônio líquido (simples)</div>
              <div style={{ fontSize: 20, marginTop: 6 }}>
                {balances.isLoading ? "Carregando..." : balances.isError ? "Erro" : fmtMoney(balances.data?.netWorth)}
              </div>
            </div>
          </div>
          <div style={{ marginTop: 12 }}>
            {balances.isLoading ? (
              <div className="muted">Carregando...</div>
            ) : balances.isError ? (
              <div className="error">Falha ao carregar saldos.</div>
            ) : (
              <table className="table">
                <thead>
                  <tr>
                    <th>Conta CHECKING</th>
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
            )}
          </div>
        </div>
      </div>

      <div className="row" style={{ marginTop: 16 }}>
        <div className="card" style={{ flex: "1 1 520px" }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "baseline" }}>
            <div>Forecast (despesas)</div>
            <div className="muted">{dashboard.isFetching ? "Atualizando..." : "—"}</div>
          </div>

          {dashboard.isLoading ? (
            <div className="muted" style={{ marginTop: 12 }}>
              Carregando...
            </div>
          ) : dashboard.isError ? (
            <div className="muted" style={{ marginTop: 12 }}>
              Forecast indisponível.
            </div>
          ) : (
            <div style={{ marginTop: 12 }}>
              <div className="row">
                <div style={{ flex: "1 1 220px" }}>
                  <div className="muted">Gasto até {dashboard.data?.forecast.asOfDate}</div>
                  <div style={{ fontSize: 20, marginTop: 6 }}>{fmtMoney(dashboard.data?.forecast.totalSpentToDate)}</div>
                </div>
                <div style={{ flex: "1 1 220px" }}>
                  <div className="muted">Projeção total do mês</div>
                  <div style={{ fontSize: 20, marginTop: 6 }}>{fmtMoney(dashboard.data?.forecast.totalProjected)}</div>
                </div>
                <div style={{ flex: "1 1 220px" }}>
                  <div className="muted">Risco de estourar metas</div>
                  <div style={{ fontSize: 20, marginTop: 6, color: riskCategories.length ? "var(--danger)" : undefined }}>
                    {riskCategories.length ? `${riskCategories.length} categoria(s)` : "Nenhum"}
                  </div>
                </div>
              </div>

              {riskCategories.length ? (
                <div style={{ marginTop: 12 }}>
                  <div className="muted" style={{ marginBottom: 8 }}>
                    Categorias em risco (projeção &gt; budget)
                  </div>
                  <table className="table">
                    <thead>
                      <tr>
                        <th>Categoria</th>
                        <th>Gasto</th>
                        <th>Projeção</th>
                        <th>Meta</th>
                      </tr>
                    </thead>
                    <tbody>
                      {riskCategories.map((c) => (
                        <tr key={c.categoryId}>
                          <td>{c.categoryName}</td>
                          <td>{c.spentToDate.toFixed(2)}</td>
                          <td style={{ color: "var(--danger)" }}>{c.projectedTotal.toFixed(2)}</td>
                          <td>{c.budgetLimit?.toFixed(2) ?? "—"}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <div className="muted" style={{ marginTop: 12 }}>
                  Sem categorias em risco (ou sem budgets cadastrados).
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
