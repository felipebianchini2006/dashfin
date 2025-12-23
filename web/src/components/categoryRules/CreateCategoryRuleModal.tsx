"use client";

import React, { useMemo, useState } from "react";
import { Modal } from "@/components/Modal";
import { useCreateCategoryRule } from "@/lib/api/hooks";
import { CategoryRuleMatchType, type CategoryDto, type CategoryRuleSuggestionDto } from "@/lib/api/types";

export function CreateCategoryRuleModal({
  open,
  suggestion,
  categories,
  onClose,
  onCreated
}: {
  open: boolean;
  suggestion: CategoryRuleSuggestionDto;
  categories: CategoryDto[];
  onClose: () => void;
  onCreated?: () => void;
}) {
  const create = useCreateCategoryRule();

  const [pattern, setPattern] = useState(suggestion.pattern);
  const [categoryId, setCategoryId] = useState(suggestion.categoryId);
  const [priority, setPriority] = useState(String(suggestion.priority));
  const [isActive, setIsActive] = useState(suggestion.isActive);
  const [error, setError] = useState<string | null>(null);

  const categoryName = useMemo(
    () => categories.find((c) => c.id === categoryId)?.name ?? "—",
    [categories, categoryId]
  );

  async function submit() {
    setError(null);
    const p = pattern.trim();
    if (!p) {
      setError("Pattern é obrigatório.");
      return;
    }

    const pr = Number(priority);
    if (!Number.isFinite(pr) || pr < 0) {
      setError("Priority inválido.");
      return;
    }

    try {
      await create.mutateAsync({
        pattern: p,
        match_type: CategoryRuleMatchType.Contains,
        category_id: categoryId,
        priority: pr,
        is_active: isActive
      });
      onCreated?.();
      onClose();
    } catch (e: any) {
      setError(e?.response?.data?.detail ?? "Falha ao criar regra.");
    }
  }

  return (
    <Modal
      open={open}
      title="Criar regra de categoria (CONTAINS)"
      onClose={() => {
        if (!create.isPending) onClose();
      }}
      footer={
        <div className="row" style={{ justifyContent: "flex-end" }}>
          <button className="btn" disabled={create.isPending} onClick={onClose}>
            Cancelar
          </button>
          <button className="btn primary" disabled={create.isPending} onClick={submit}>
            {create.isPending ? "Criando..." : "Criar regra"}
          </button>
        </div>
      }
    >
      <div className="muted" style={{ fontSize: 13 }}>
        Sugestão baseada na recategorização: <code>{suggestion.pattern}</code> → <code>{categoryName}</code>
      </div>

      <div className="row" style={{ marginTop: 12, alignItems: "flex-end" }}>
        <div className="field" style={{ flex: "1 1 360px" }}>
          <label>Pattern</label>
          <input value={pattern} onChange={(e) => setPattern(e.target.value)} placeholder="Ex.: UBER" />
        </div>

        <div className="field" style={{ flex: "1 1 240px" }}>
          <label>Categoria</label>
          <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)}>
            {categories.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
        </div>

        <div className="field" style={{ width: 120 }}>
          <label>Priority</label>
          <input value={priority} onChange={(e) => setPriority(e.target.value)} inputMode="numeric" />
        </div>
      </div>

      <div className="row" style={{ marginTop: 12, alignItems: "center", justifyContent: "space-between" }}>
        <div className="muted">
          Match type: <code>CONTAINS</code>
        </div>
        <label className="muted" style={{ display: "flex", gap: 10, alignItems: "center" }}>
          <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
          Ativa
        </label>
      </div>

      {error ? (
        <div className="error" style={{ marginTop: 12 }}>
          {error}
        </div>
      ) : null}
    </Modal>
  );
}

