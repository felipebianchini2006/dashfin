"use client";

import React, { useState } from "react";
import { yyyyMm01 } from "@/lib/date";
import { useBudgets, useCategories, useUpsertBudget } from "@/lib/api/hooks";
import { BudgetsEditor } from "@/components/budgets/BudgetsEditor";
import { Notice } from "@/components/Notice";
import { errorMessage } from "@/lib/api/errorMessage";

function normalizeMonth(value: string) {
  if (!value) return value;
  const m = value.slice(0, 7);
  if (m.length !== 7) return value;
  return `${m}-01`;
}

export default function BudgetsPage() {
  const [month, setMonth] = useState(() => yyyyMm01(new Date()));
  const budgets = useBudgets(month);
  const categories = useCategories();
  const upsert = useUpsertBudget();

  const [notice, setNotice] = useState<{ kind: "success" | "error"; message: string } | null>(null);
  const [savingCategoryId, setSavingCategoryId] = useState<string | null>(null);

  return (
    <div className="container">
      {notice ? (
        <div style={{ marginBottom: 12 }}>
          <Notice kind={notice.kind} message={notice.message} onClose={() => setNotice(null)} />
        </div>
      ) : null}

      <div className="card">
        <div className="row" style={{ alignItems: "flex-end", justifyContent: "space-between" }}>
          <h2 style={{ marginTop: 0 }}>Budgets</h2>
          <div className="field">
            <label>Mês</label>
            <input type="date" value={month} onChange={(e) => setMonth(normalizeMonth(e.target.value))} />
          </div>
        </div>

        <div className="muted" style={{ marginTop: 6 }}>
          Progresso considera apenas despesas e ignora <code>ignore_in_dashboard=true</code>.
        </div>

        {budgets.isLoading || categories.isLoading ? <div className="muted" style={{ marginTop: 12 }}>Carregando...</div> : null}
        {budgets.isError ? <div className="error" style={{ marginTop: 12 }}>Falha ao carregar budgets.</div> : null}
        {categories.isError ? <div className="error" style={{ marginTop: 12 }}>Falha ao carregar categorias.</div> : null}

        {!budgets.isError && !categories.isError ? (
          <BudgetsEditor
            categories={categories.data ?? []}
            budgets={budgets.data ?? []}
            savingCategoryId={savingCategoryId}
            onSave={async (categoryId, amount) => {
              setNotice(null);
              setSavingCategoryId(categoryId);
              try {
                await upsert.mutateAsync({ category_id: categoryId, month, amount });
                setNotice({ kind: "success", message: "Budget salvo." });
              } catch (e) {
                setNotice({ kind: "error", message: errorMessage(e, "Falha ao salvar budget.") });
              } finally {
                setSavingCategoryId(null);
              }
            }}
          />
        ) : null}
      </div>
    </div>
  );
}
