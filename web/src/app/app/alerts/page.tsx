"use client";

import React, { useState } from "react";
import { AlertEventStatus } from "@/lib/api/types";
import { useAlerts, useUpdateAlertStatus } from "@/lib/api/hooks";

export default function AlertsPage() {
  const [status, setStatus] = useState<string>("");
  const parsed = status ? (Number(status) as AlertEventStatus) : undefined;
  const alerts = useAlerts(parsed);
  const update = useUpdateAlertStatus();

  return (
    <div className="container">
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
                    onClick={() => update.mutate({ id: a.id, status: AlertEventStatus.Read })}
                  >
                    Marcar lido
                  </button>
                  <button
                    className="btn danger"
                    disabled={update.isPending || a.status === AlertEventStatus.Dismissed}
                    onClick={() => update.mutate({ id: a.id, status: AlertEventStatus.Dismissed })}
                  >
                    Dispensar
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>

        {alerts.isError ? <div className="error" style={{ marginTop: 12 }}>Falha ao carregar alertas.</div> : null}
      </div>
    </div>
  );
}

