"use client";

import React from "react";
import Link from "next/link";
import { useImportRowsError } from "@/lib/api/hooks";
import { ImportRowStatus } from "@/lib/api/types";

export default function ImportRowsErrorPage({ params }: { params: { id: string } }) {
  const rows = useImportRowsError(params.id);

  return (
    <div className="container">
      <div className="row" style={{ alignItems: "center", justifyContent: "space-between" }}>
        <div>
          <h2 style={{ margin: 0 }}>Auditoria (rows ERROR)</h2>
          <div className="muted" style={{ marginTop: 6 }}>
            importId: <code>{params.id}</code>
          </div>
        </div>
        <Link className="btn" href={`/app/imports/${params.id}`}>
          Voltar
        </Link>
      </div>

      <div className="card" style={{ marginTop: 16 }}>
        {rows.isLoading ? <div>Carregando...</div> : null}
        {rows.isError ? <div className="error">Falha ao carregar auditoria.</div> : null}

        <table className="table" style={{ marginTop: 12 }}>
          <thead>
            <tr>
              <th>#</th>
              <th>Página</th>
              <th>Status</th>
              <th>Código</th>
              <th>Mensagem</th>
            </tr>
          </thead>
          <tbody>
            {(rows.data ?? []).map((r) => (
              <tr key={r.id}>
                <td>{r.rowIndex}</td>
                <td>{r.pageNumber ?? "—"}</td>
                <td>{ImportRowStatus[r.status]}</td>
                <td>{r.errorCode ?? "—"}</td>
                <td>{r.errorMessage ?? "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>

        <div className="muted" style={{ marginTop: 12 }}>
          Total: {rows.data?.length ?? 0}
        </div>
      </div>
    </div>
  );
}

