"use client";

import React, { useState } from "react";
import { useCategories, useCreateCategory } from "@/lib/api/hooks";

export default function CategoriesPage() {
  const categories = useCategories();
  const create = useCreateCategory();

  const [name, setName] = useState("");
  const [parentId, setParentId] = useState<string>("");

  return (
    <div className="container">
      <div className="row">
        <div className="card" style={{ flex: "1 1 520px" }}>
          <h2 style={{ marginTop: 0 }}>Categorias</h2>
          <table className="table">
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
                await create.mutateAsync({ name, parent_id: parentId || null });
                setName("");
                setParentId("");
              }}
            >
              {create.isPending ? "Salvando..." : "Criar"}
            </button>
            {create.isError ? <div className="error" style={{ marginTop: 10 }}>Falha ao criar categoria.</div> : null}
          </div>
        </div>
      </div>
    </div>
  );
}

