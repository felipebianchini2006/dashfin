"use client";

import React, { useState } from "react";
import { AlertEventStatus } from "@/lib/api/types";
import { useAlerts, useUpdateAlertStatus } from "@/lib/api/hooks";
import { Notice } from "@/components/Notice";
import { errorMessage } from "@/lib/api/errorMessage";

export default function AlertsPage() {
  const [status, setStatus] = useState<string>("");
  const parsed = status ? (Number(status) as AlertEventStatus) : undefined;
  const alerts = useAlerts(parsed);
  const update = useUpdateAlertStatus();
  const [notice, setNotice] = useState<{ kind: "success" | "error"; message: string } | null>(null);

  return (
    <div className="container">
      {notice ? (
        <div style={{ marginBottom: 12 }}>
          <Notice kind={notice.kind} message={notice.message} onClose={() => setNotice(null)} />
        </div>
      ) : null}

      <div className="card">
        <div className="row" style={{ alignItems: "flex-end", justifyContent: "space-between" }}>
          <h2 style={{ marginTop: 0 }}>Alertas</h2>
          <div className="field">
            <label>Status</label>
            <select value={status} onChange={(e) => setStatus(e.target.value)}>
              <option value="">Todos</option>
              <option value={AlertEventStatus.New}>NEW</option>
              <option value={AlertEventStatus.Read}>READ</option>
              <option value={AlertEventStatus.Dismissed}>DISMISSED</option>
            </select>
          </div>
        </div>

        {alerts.isLoading ? <div className="muted" style={{ marginTop: 12 }}>Carregando...</div> : null}
        {alerts.isError ? <div className="error" style={{ marginTop: 12 }}>Falha ao carregar alertas.</div> : null}
        {(alerts.data?.length ?? 0) === 0 && !alerts.isLoading && !alerts.isError ? (
          <div className="muted" style={{ marginTop: 12 }}>
            Nenhum alerta.
          </div>
        ) : null}

        {(alerts.data?.length ?? 0) > 0 ? (
          <table className="table" style={{ marginTop: 12 }}>
            <thead>
              <tr>
                <th>Quando</th>
                <th>Título</th>
                <th>Status</th>
                <th>Ações</th>
              </tr>
            </thead>
            <tbody>
              {(alerts.data ?? []).map((a) => (
                <tr key={a.id}>
                  <td>{a.occurredAt.slice(0, 10)}</td>
                  <td>
                    <div>{a.title}</div>
                    {a.body ? <div className="muted">{a.body}</div> : null}
                  </td>
                  <td>{AlertEventStatus[a.status]}</td>
                  <td style={{ display: "flex", gap: 8 }}>
                    <button
                      className="btn"
                      disabled={update.isPending || a.status === AlertEventStatus.Read}
                      onClick={async () => {
                        setNotice(null);
                        try {
                          await update.mutateAsync({ id: a.id, status: AlertEventStatus.Read });
                          setNotice({ kind: "success", message: "Alerta marcado como READ." });
                        } catch (e) {
                          setNotice({ kind: "error", message: errorMessage(e, "Falha ao atualizar alerta.") });
                        }
                      }}
                    >
                      Marcar lido
                    </button>
                    <button
                      className="btn danger"
                      disabled={update.isPending || a.status === AlertEventStatus.Dismissed}
                      onClick={async () => {
                        setNotice(null);
                        try {
                          await update.mutateAsync({ id: a.id, status: AlertEventStatus.Dismissed });
                          setNotice({ kind: "success", message: "Alerta dispensado." });
                        } catch (e) {
                          setNotice({ kind: "error", message: errorMessage(e, "Falha ao atualizar alerta.") });
                        }
                      }}
                    >
                      Dispensar
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : null}
      </div>
    </div>
  );
}
