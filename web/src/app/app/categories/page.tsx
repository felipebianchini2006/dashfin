"use client";

import React, { useState } from "react";
import { useCategories, useCreateCategory } from "@/lib/api/hooks";
import { Notice } from "@/components/Notice";
import { errorMessage } from "@/lib/api/errorMessage";
import { CategoryRulesPanel } from "@/components/categoryRules/CategoryRulesPanel";

export default function CategoriesPage() {
  const categories = useCategories();
  const create = useCreateCategory();

  const [name, setName] = useState("");
  const [parentId, setParentId] = useState<string>("");
  const [notice, setNotice] = useState<{ kind: "success" | "error"; message: string } | null>(null);

  return (
    <div className="container">
      {notice ? (
        <div style={{ marginBottom: 12 }}>
          <Notice kind={notice.kind} message={notice.message} onClose={() => setNotice(null)} />
        </div>
      ) : null}

      <div className="row">
        <div className="card" style={{ flex: "1 1 520px" }}>
          <div className="row" style={{ justifyContent: "space-between", alignItems: "center" }}>
            <h2 style={{ marginTop: 0 }}>Categorias</h2>
            <div className="muted">{categories.isFetching ? "Atualizando..." : "—"}</div>
          </div>

          {categories.isLoading ? <div className="muted">Carregando...</div> : null}
          {categories.isError ? <div className="error">Falha ao carregar categorias.</div> : null}

          {(categories.data?.length ?? 0) === 0 && !categories.isLoading && !categories.isError ? (
            <div className="muted">Nenhuma categoria.</div>
          ) : null}

          {(categories.data?.length ?? 0) > 0 ? (
            <table className="table" style={{ marginTop: 12 }}>
              <thead>
                <tr>
                  <th>Nome</th>
                  <th>Pai</th>
                </tr>
              </thead>
              <tbody>
                {(categories.data ?? []).map((c) => (
                  <tr key={c.id}>
                    <td>{c.name}</td>
                    <td>{(categories.data ?? []).find((x) => x.id === c.parentId)?.name ?? "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : null}
        </div>

        <div className="card" style={{ flex: "1 1 360px" }}>
          <h2 style={{ marginTop: 0 }}>Nova categoria</h2>
          <div className="field">
            <label>Nome</label>
            <input value={name} onChange={(e) => setName(e.target.value)} />
          </div>
          <div className="field" style={{ marginTop: 12 }}>
            <label>Parent</label>
            <select value={parentId} onChange={(e) => setParentId(e.target.value)}>
              <option value="">(nenhum)</option>
              {(categories.data ?? []).map((c) => (
                <option value={c.id} key={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          </div>
          <div style={{ marginTop: 16 }}>
            <button
              className="btn primary"
              disabled={create.isPending}
              onClick={async () => {
                setNotice(null);
                try {
                  await create.mutateAsync({ name, parent_id: parentId || null });
                  setName("");
                  setParentId("");
                  setNotice({ kind: "success", message: "Categoria criada." });
                } catch (e) {
                  setNotice({ kind: "error", message: errorMessage(e, "Falha ao criar categoria.") });
                }
              }}
            >
              {create.isPending ? "Salvando..." : "Criar"}
            </button>
          </div>
        </div>
      </div>

      <CategoryRulesPanel categories={categories.data ?? []} />
    </div>
  );
}
