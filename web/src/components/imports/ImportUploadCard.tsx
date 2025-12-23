"use client";

import React, { useMemo, useState } from "react";
import { api } from "@/lib/api/client";
import { useAccounts } from "@/lib/api/hooks";
import { AccountType, ProblemDetails } from "@/lib/api/types";
import axios from "axios";

const MAX_BYTES = 20 * 1024 * 1024;

export function ImportUploadCard({ onUploaded }: { onUploaded?: (importId: string) => void }) {
  const accounts = useAccounts();

  const importableAccounts = useMemo(
    () => (accounts.data ?? []).filter((a) => a.type === AccountType.Checking || a.type === AccountType.CreditCard),
    [accounts.data]
  );

  const [accountId, setAccountId] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [progress, setProgress] = useState<number>(0);
  const [error, setError] = useState<string | null>(null);
  const [importId, setImportId] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function upload() {
    setError(null);
    setImportId(null);

    if (!accountId) {
      setError("Selecione uma conta (CHECKING ou CREDIT_CARD).");
      return;
    }
    if (!file) {
      setError("Selecione um PDF.");
      return;
    }
    if (!file.name.toLowerCase().endsWith(".pdf") && file.type !== "application/pdf") {
      setError("Arquivo inválido: esperado PDF.");
      return;
    }
    if (file.size > MAX_BYTES) {
      setError(`Arquivo muito grande: limite ${(MAX_BYTES / (1024 * 1024)).toFixed(0)}MB.`);
      return;
    }

    const fd = new FormData();
    fd.append("account_id", accountId);
    fd.append("pdf", file);

    setBusy(true);
    setProgress(0);
    try {
      const res = await api.post<{ importId: string }>("/imports", fd, {
        headers: { "Content-Type": "multipart/form-data" },
        onUploadProgress: (evt) => {
          const total = evt.total ?? file.size;
          const pct = total ? Math.round((evt.loaded / total) * 100) : 0;
          setProgress(pct);
        }
      });

      setImportId(res.data.importId);
      onUploaded?.(res.data.importId);
    } catch (e) {
      if (axios.isAxiosError<ProblemDetails>(e)) {
        setError(e.response?.data?.detail ?? "Falha no upload.");
      } else {
        setError("Falha no upload.");
      }
    } finally {
      setBusy(false);
      setProgress(0);
    }
  }

  return (
    <div className="card">
      <h2 style={{ marginTop: 0 }}>Enviar PDF</h2>
      <div className="row" style={{ alignItems: "flex-end" }}>
        <div className="field" style={{ minWidth: 260 }}>
          <label>Conta (CHECKING / CREDIT_CARD)</label>
          <select value={accountId} onChange={(e) => setAccountId(e.target.value)}>
            <option value="">Selecione</option>
            {importableAccounts.map((a) => (
              <option key={a.id} value={a.id}>
                {a.name} ({AccountType[a.type]})
              </option>
            ))}
          </select>
        </div>
        <div className="field" style={{ minWidth: 320 }}>
          <label>PDF</label>
          <input type="file" accept="application/pdf" onChange={(e) => setFile(e.target.files?.[0] ?? null)} />
        </div>
        <button className="btn primary" disabled={busy} onClick={upload}>
          {busy ? `Enviando... ${progress}%` : "Enviar"}
        </button>
      </div>

      {busy ? (
        <div style={{ marginTop: 12 }}>
          <div className="muted">Upload em progresso</div>
          <div style={{ height: 8, borderRadius: 999, background: "rgba(148,163,184,0.12)", overflow: "hidden", marginTop: 6 }}>
            <div style={{ width: `${progress}%`, height: "100%", background: "rgba(96,165,250,0.6)" }} />
          </div>
        </div>
      ) : null}

      {importId ? (
        <div style={{ marginTop: 12 }}>
          Import criado: <code>{importId}</code>
        </div>
      ) : null}
      {error ? (
        <div className="error" style={{ marginTop: 12 }}>
          {error}
        </div>
      ) : null}
      <div className="muted" style={{ marginTop: 12 }}>
        Dica: em dev HTTP, o cookie de refresh pode exigir HTTPS se <code>RefreshTokenSecure=true</code>.
      </div>
    </div>
  );
}

