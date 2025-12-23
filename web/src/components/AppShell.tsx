"use client";

import React from "react";
import { NavLink } from "@/components/NavLink";
import { useAuth } from "@/lib/auth/AuthContext";
import { useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";

export function AppShell({ children }: { children: React.ReactNode }) {
  const auth = useAuth();
  const router = useRouter();
  const qc = useQueryClient();

  return (
    <div>
      <div style={{ borderBottom: "1px solid var(--border)", background: "rgba(17,24,39,0.7)" }}>
        <div className="container" style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
          <div style={{ display: "flex", gap: 10, flexWrap: "wrap" }}>
            <NavLink href="/app/dashboard" label="Dashboard" />
            <NavLink href="/app/transactions" label="Transações" />
            <NavLink href="/app/imports" label="Importações" />
            <NavLink href="/app/accounts" label="Contas" />
            <NavLink href="/app/categories" label="Categorias" />
            <NavLink href="/app/budgets" label="Orçamentos" />
            <NavLink href="/app/alerts" label="Alertas" />
            <NavLink href="/app/settings" label="Config" />
          </div>
          <button
            className="btn danger"
            onClick={async () => {
              await auth.logout();
              await qc.clear();
              router.replace("/login");
            }}
          >
            Sair
          </button>
        </div>
      </div>
      <div>{children}</div>
    </div>
  );
}

