"use client";

import React, { useEffect, useState } from "react";
import { useMe, useUpdateMe } from "@/lib/api/hooks";
import { Notice } from "@/components/Notice";
import { errorMessage } from "@/lib/api/errorMessage";

export default function SettingsPage() {
  const me = useMe();
  const update = useUpdateMe();

  const [timezone, setTimezone] = useState("");
  const [currency, setCurrency] = useState("");
  const [theme, setTheme] = useState<string>("system");
  const [compactMode, setCompactMode] = useState(false);
  const [notice, setNotice] = useState<{ kind: "success" | "error"; message: string } | null>(null);

  useEffect(() => {
    if (!me.data) return;
    setTimezone(me.data.timezone);
    setCurrency(me.data.currency);
    setTheme(me.data.displayPreferences.theme ?? "system");
    setCompactMode(!!me.data.displayPreferences.compactMode);
  }, [me.data]);

  const dirty =
    me.data &&
    (timezone !== me.data.timezone ||
      currency !== me.data.currency ||
      theme !== me.data.displayPreferences.theme ||
      compactMode !== me.data.displayPreferences.compactMode);

  return (
    <div className="container">
      <div className="card">
        <h2 style={{ marginTop: 0 }}>Settings</h2>
        {notice ? (
          <div style={{ marginBottom: 12 }}>
            <Notice kind={notice.kind} message={notice.message} onClose={() => setNotice(null)} />
          </div>
        ) : null}

        {me.isLoading ? <div className="muted">Carregando...</div> : null}
        {me.isError ? <div className="error">Falha ao carregar perfil.</div> : null}

        {me.data ? (
          <>
            <div className="row" style={{ marginTop: 12 }}>
              <div style={{ flex: "1 1 260px" }}>
                <div className="muted">Email</div>
                <div>{me.data.email}</div>
              </div>
            </div>

            <div className="row" style={{ marginTop: 16, alignItems: "flex-end" }}>
              <div className="field" style={{ flex: "1 1 260px" }}>
                <label>Timezone</label>
                <input value={timezone} onChange={(e) => setTimezone(e.target.value)} placeholder="America/Sao_Paulo" />
              </div>
              <div className="field" style={{ width: 160 }}>
                <label>Currency</label>
                <input value={currency} onChange={(e) => setCurrency(e.target.value.toUpperCase())} placeholder="BRL" />
              </div>
              <div className="field" style={{ width: 180 }}>
                <label>Tema</label>
                <select value={theme} onChange={(e) => setTheme(e.target.value)}>
                  <option value="system">system</option>
                  <option value="light">light</option>
                  <option value="dark">dark</option>
                </select>
              </div>
              <label className="muted" style={{ display: "flex", gap: 10, alignItems: "center", paddingBottom: 6 }}>
                <input type="checkbox" checked={compactMode} onChange={(e) => setCompactMode(e.target.checked)} />
                Compact mode
              </label>
              <button
                className="btn primary"
                disabled={!dirty || update.isPending}
                onClick={async () => {
                  setNotice(null);
                  try {
                    await update.mutateAsync({
                      timezone,
                      currency,
                      displayPreferences: { theme, compactMode }
                    });
                    setNotice({ kind: "success", message: "Configurações salvas." });
                  } catch (e) {
                    setNotice({ kind: "error", message: errorMessage(e, "Falha ao salvar configurações.") });
                  }
                }}
              >
                {update.isPending ? "Salvando..." : "Salvar"}
              </button>
            </div>
          </>
        ) : null}
      </div>
    </div>
  );
}
