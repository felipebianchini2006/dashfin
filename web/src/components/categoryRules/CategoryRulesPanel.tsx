"use client";

import React, { useMemo, useState } from "react";
import { Notice } from "@/components/Notice";
import { errorMessage } from "@/lib/api/errorMessage";
import { useCategoryRules, useCreateCategoryRule } from "@/lib/api/hooks";
import { CategoryRuleMatchType, type CategoryDto } from "@/lib/api/types";

export function CategoryRulesPanel({ categories }: { categories: CategoryDto[] }) {
  const rules = useCategoryRules();
  const create = useCreateCategoryRule();
  const [notice, setNotice] = useState<{ kind: "success" | "error"; message: string } | null>(null);

  const [pattern, setPattern] = useState("");
  const [matchType, setMatchType] = useState<CategoryRuleMatchType>(CategoryRuleMatchType.Contains);
  const [categoryId, setCategoryId] = useState<string>("");
  const [priority, setPriority] = useState<string>("100");
  const [isActive, setIsActive] = useState(true);

  const sorted = useMemo(() => {
    const items = rules.data ?? [];
    return [...items].sort((a, b) => a.priority - b.priority || b.createdAt.localeCompare(a.createdAt));
  }, [rules.data]);

  async function submit() {
    setNotice(null);
    const p = pattern.trim();
    if (!p) {
      setNotice({ kind: "error", message: "Pattern é obrigatório." });
      return;
    }
    if (!categoryId) {
      setNotice({ kind: "error", message: "Selecione uma categoria." });
      return;
    }
    const pr = Number(priority);
    if (!Number.isFinite(pr)) {
      setNotice({ kind: "error", message: "Priority inválido." });
      return;
    }

    try {
      await create.mutateAsync({
        pattern: p,
        match_type: matchType,
        category_id: categoryId,
        priority: pr,
        is_active: isActive
      });
      setPattern("");
      setMatchType(CategoryRuleMatchType.Contains);
      setPriority("100");
      setIsActive(true);
      setNotice({ kind: "success", message: "Regra criada." });
    } catch (e) {
      setNotice({ kind: "error", message: errorMessage(e, "Falha ao criar regra.") });
    }
  }

  return (
    <div className="card" style={{ marginTop: 16 }}>
      <div className="row" style={{ justifyContent: "space-between", alignItems: "center" }}>
        <h2 style={{ marginTop: 0 }}>Regras de categoria</h2>
        <div className="muted">{rules.isFetching ? "Atualizando..." : "—"}</div>
      </div>

      {notice ? (
        <div style={{ marginBottom: 12 }}>
          <Notice kind={notice.kind} message={notice.message} onClose={() => setNotice(null)} />
        </div>
      ) : null}

      {rules.isLoading ? <div className="muted">Carregando...</div> : null}
      {rules.isError ? <div className="error">Falha ao carregar regras.</div> : null}

      {(sorted.length ?? 0) === 0 && !rules.isLoading && !rules.isError ? <div className="muted">Nenhuma regra.</div> : null}

      {sorted.length > 0 ? (
        <table className="table" style={{ marginTop: 12 }}>
          <thead>
            <tr>
              <th>Priority</th>
              <th>Match</th>
              <th>Pattern</th>
              <th>Categoria</th>
              <th>Ativa</th>
            </tr>
          </thead>
          <tbody>
            {sorted.map((r) => (
              <tr key={r.id}>
                <td>{r.priority}</td>
                <td>{CategoryRuleMatchType[r.matchType]}</td>
                <td>
                  <code>{r.pattern}</code>
                </td>
                <td>{categories.find((c) => c.id === r.categoryId)?.name ?? "—"}</td>
                <td>{r.isActive ? "sim" : "não"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : null}

      <div style={{ marginTop: 16 }}>
        <h3 style={{ margin: 0 }}>Criar regra</h3>
        <div className="row" style={{ marginTop: 12, alignItems: "flex-end" }}>
          <div className="field" style={{ flex: "1 1 280px" }}>
            <label>Pattern</label>
            <input value={pattern} onChange={(e) => setPattern(e.target.value)} placeholder="Ex.: UBER" />
          </div>
          <div className="field" style={{ width: 180 }}>
            <label>Match type</label>
            <select value={matchType} onChange={(e) => setMatchType(Number(e.target.value) as CategoryRuleMatchType)}>
              <option value={CategoryRuleMatchType.Contains}>CONTAINS</option>
              <option value={CategoryRuleMatchType.Regex}>REGEX</option>
            </select>
          </div>
          <div className="field" style={{ minWidth: 240, flex: "1 1 240px" }}>
            <label>Categoria</label>
            <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)}>
              <option value="">Selecione</option>
              {categories.map((c) => (
                <option value={c.id} key={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          </div>
          <div className="field" style={{ width: 140 }}>
            <label>Priority</label>
            <input value={priority} onChange={(e) => setPriority(e.target.value)} inputMode="numeric" />
          </div>
          <label className="muted" style={{ display: "flex", gap: 10, alignItems: "center", paddingBottom: 6 }}>
            <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
            Ativa
          </label>
          <button className="btn primary" disabled={create.isPending} onClick={submit}>
            {create.isPending ? "Salvando..." : "Criar"}
          </button>
        </div>
        {matchType === CategoryRuleMatchType.Regex ? (
          <div className="muted" style={{ marginTop: 10, fontSize: 13 }}>
            Regex roda no backend com timeout para evitar ReDoS.
          </div>
        ) : null}
      </div>
    </div>
  );
}

