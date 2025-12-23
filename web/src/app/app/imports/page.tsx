"use client";

import React, { useState } from "react";
import { useAccounts, useUploadImport } from "@/lib/api/hooks";

export default function ImportsPage() {
  const accounts = useAccounts();
  const upload = useUploadImport();

  const [accountId, setAccountId] = useState("");
  const [file, setFile] = useState<File | null>(null);

  return (
    <div className="container">
      <div className="card">
        <h2 style={{ marginTop: 0 }}>Importações</h2>

        <div className="row">
          <div className="field" style={{ minWidth: 260 }}>
            <label>Conta</label>
            <select value={accountId} onChange={(e) => setAccountId(e.target.value)}>
              <option value="">Selecione</option>
              {(accounts.data ?? []).map((a) => (
                <option key={a.id} value={a.id}>
                  {a.name}
                </option>
              ))}
            </select>
          </div>
          <div className="field" style={{ minWidth: 320 }}>
            <label>PDF</label>
            <input type="file" accept="application/pdf" onChange={(e) => setFile(e.target.files?.[0] ?? null)} />
          </div>
          <button
            className="btn primary"
            disabled={!accountId || !file || upload.isPending}
            onClick={async () => {
              if (!file) return;
              await upload.mutateAsync({ accountId, file });
            }}
          >
            {upload.isPending ? "Enviando..." : "Enviar"}
          </button>
        </div>

        {upload.isSuccess ? (
          <div style={{ marginTop: 12 }}>
            <div>Import criado:</div>
            <div className="muted" style={{ marginTop: 6 }}>
              importId: <code>{upload.data.importId}</code>
            </div>
            <div className="muted" style={{ marginTop: 6 }}>
              O processamento roda em background; consulte o backend via <code>GET /imports/&lt;id&gt;</code>.
            </div>
          </div>
        ) : null}

        {upload.isError ? (
          <div className="error" style={{ marginTop: 12 }}>
            Falha ao enviar PDF.
          </div>
        ) : null}
      </div>
    </div>
  );
}

