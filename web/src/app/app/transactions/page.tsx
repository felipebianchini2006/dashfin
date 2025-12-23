"use client";

import React, { useMemo, useState } from "react";
import { useAccounts, useCategories, useTransactions } from "@/lib/api/hooks";
import { yyyyMm01 } from "@/lib/date";
import { TransactionsFilters, type TransactionsFilterDraft } from "@/components/transactions/TransactionsFilters";
import { TransactionsTable } from "@/components/transactions/TransactionsTable";
import { PaginationBar } from "@/components/transactions/PaginationBar";
import type { CategoryRuleSuggestionDto } from "@/lib/api/types";
import { CreateCategoryRuleModal } from "@/components/categoryRules/CreateCategoryRuleModal";

function endOfMonth(yyyyMm01Str: string) {
  const d = new Date(`${yyyyMm01Str}T00:00:00Z`);
  d.setUTCMonth(d.getUTCMonth() + 1);
  d.setUTCDate(0);
  const y = d.getUTCFullYear();
  const m = String(d.getUTCMonth() + 1).padStart(2, "0");
  const day = String(d.getUTCDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

function parseNumberOrUndefined(v: string) {
  const normalized = v.trim().replace(",", ".");
  const n = Number(normalized);
  return Number.isFinite(n) ? n : undefined;
}

export default function TransactionsPage() {
  const [page, setPage] = useState(1);
  const defaultMonth = useMemo(() => yyyyMm01(new Date()), []);
  const [draft, setDraft] = useState<TransactionsFilterDraft>(() => ({
    from: defaultMonth,
    to: endOfMonth(defaultMonth),
    accountId: "",
    categoryId: "",
    type: "",
    min: "",
    max: "",
    q: ""
  }));
  const [filters, setFilters] = useState<TransactionsFilterDraft>(draft);

  const accounts = useAccounts();
  const categories = useCategories();

  const from = filters.from ? `${filters.from}T00:00:00Z` : undefined;
  const to = filters.to ? `${filters.to}T23:59:59Z` : undefined;

  const min = filters.min ? parseNumberOrUndefined(filters.min) : undefined;
  const max = filters.max ? parseNumberOrUndefined(filters.max) : undefined;

  const tx = useTransactions({
    page,
    page_size: 50,
    q: filters.q.trim().length ? filters.q.trim() : undefined,
    min,
    max,
    account_id: filters.accountId || undefined,
    category_id: filters.categoryId || undefined,
    type: filters.type ? (Number(filters.type) as 1 | 2) : undefined,
    from,
    to
  });

  const accountsById = useMemo(() => {
    const m: Record<string, string> = {};
    for (const a of accounts.data ?? []) m[a.id] = a.name;
    return m;
  }, [accounts.data]);

  const categoriesById = useMemo(() => {
    const m: Record<string, string> = {};
    for (const c of categories.data ?? []) m[c.id] = c.name;
    return m;
  }, [categories.data]);

  const [suggestion, setSuggestion] = useState<CategoryRuleSuggestionDto | null>(null);
  const [suggestionOpen, setSuggestionOpen] = useState(false);

  return (
    <div className="container">
      <TransactionsFilters
        draft={draft}
        accounts={accounts.data ?? []}
        categories={categories.data ?? []}
        onChange={(patch) => setDraft((d) => ({ ...d, ...patch }))}
        onApply={() => {
          setFilters(draft);
          setPage(1);
        }}
        onReset={() => {
          const next: TransactionsFilterDraft = {
            from: defaultMonth,
            to: endOfMonth(defaultMonth),
            accountId: "",
            categoryId: "",
            type: "",
            min: "",
            max: "",
            q: ""
          };
          setDraft(next);
          setFilters(next);
          setPage(1);
        }}
      />

      {suggestion ? (
        <div className="card" style={{ marginTop: 16, borderColor: "rgba(96, 165, 250, 0.35)" }}>
          <div className="row" style={{ justifyContent: "space-between", alignItems: "center" }}>
            <div>
              <div>Quer criar uma regra automática para futuros lançamentos semelhantes?</div>
              <div className="muted" style={{ marginTop: 6, fontSize: 13 }}>
                CONTAINS <code>{suggestion.pattern}</code> → <code>{categoriesById[suggestion.categoryId] ?? "—"}</code>
              </div>
            </div>
            <div style={{ display: "flex", gap: 10 }}>
              <button className="btn" onClick={() => setSuggestion(null)}>
                Ignorar
              </button>
              <button className="btn primary" onClick={() => setSuggestionOpen(true)}>
                Criar regra
              </button>
            </div>
          </div>
        </div>
      ) : null}

      <div className="card" style={{ marginTop: 16 }}>
        <div className="row" style={{ justifyContent: "space-between", alignItems: "center" }}>
          <h2 style={{ margin: 0 }}>Transações</h2>
          <div className="muted">{tx.isFetching ? "Atualizando..." : "—"}</div>
        </div>

        {tx.isError ? <div className="error" style={{ marginTop: 12 }}>Falha ao carregar transações.</div> : null}
        {tx.isLoading ? <div style={{ marginTop: 12 }}>Carregando...</div> : null}

        {(tx.data?.items?.length ?? 0) === 0 && !tx.isLoading && !tx.isError ? (
          <div className="muted" style={{ marginTop: 12 }}>
            Nenhuma transação encontrada.
          </div>
        ) : null}

        {(tx.data?.items?.length ?? 0) > 0 ? (
          <div style={{ marginTop: 12 }}>
            <TransactionsTable
              transactions={tx.data!.items}
              accountsById={accountsById}
              categories={categories.data ?? []}
              onSuggestedRule={(s) => setSuggestion(s)}
            />
            <PaginationBar
              page={tx.data?.page ?? page}
              pageSize={tx.data?.pageSize ?? 50}
              totalCount={tx.data?.totalCount ?? 0}
              onPage={(p) => setPage(p)}
            />
          </div>
        ) : null}
      </div>

      {suggestion ? (
        <CreateCategoryRuleModal
          key={`${suggestion.categoryId}:${suggestion.pattern}`}
          open={suggestionOpen}
          suggestion={suggestion}
          categories={categories.data ?? []}
          onClose={() => setSuggestionOpen(false)}
          onCreated={() => setSuggestion(null)}
        />
      ) : null}
    </div>
  );
}
