"use client";

import React from "react";
import { useMe } from "@/lib/api/hooks";

export default function SettingsPage() {
  const me = useMe();

  return (
    <div className="container">
      <div className="card">
        <h2 style={{ marginTop: 0 }}>Config</h2>
        {me.isLoading ? <div>Carregando...</div> : null}
        {me.isError ? <div className="error">Falha ao carregar perfil.</div> : null}
        {me.data ? (
          <div className="row" style={{ marginTop: 12 }}>
            <div style={{ flex: "1 1 260px" }}>
              <div className="muted">Email</div>
              <div>{me.data.email}</div>
            </div>
            <div style={{ flex: "1 1 260px" }}>
              <div className="muted">Timezone</div>
              <div>{me.data.timezone}</div>
            </div>
            <div style={{ flex: "1 1 260px" }}>
              <div className="muted">Currency</div>
              <div>{me.data.currency}</div>
            </div>
          </div>
        ) : null}
      </div>
    </div>
  );
}

