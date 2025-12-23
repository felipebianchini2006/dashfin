"use client";

import React, { useState } from "react";
import { useAuth } from "@/lib/auth/AuthContext";
import { useRouter, useSearchParams } from "next/navigation";

export default function LoginPage() {
  const auth = useAuth();
  const router = useRouter();
  const params = useSearchParams();
  const next = params.get("next") ?? "/app/dashboard";

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  return (
    <div className="container">
      <div className="card" style={{ maxWidth: 420 }}>
        <h2 style={{ marginTop: 0 }}>Login</h2>
        <div className="field">
          <label>Email</label>
          <input value={email} onChange={(e) => setEmail(e.target.value)} placeholder="user@example.com" />
        </div>
        <div className="field" style={{ marginTop: 12 }}>
          <label>Senha</label>
          <input value={password} onChange={(e) => setPassword(e.target.value)} type="password" />
        </div>
        {error ? (
          <div className="error" style={{ marginTop: 12 }}>
            {error}
          </div>
        ) : null}
        <div style={{ display: "flex", gap: 10, marginTop: 16 }}>
          <button
            className="btn primary"
            disabled={loading}
            onClick={async () => {
              setLoading(true);
              setError(null);
              try {
                await auth.login(email, password);
                router.replace(next);
              } catch (e: any) {
                setError(e?.response?.data?.detail ?? "Falha no login.");
              } finally {
                setLoading(false);
              }
            }}
          >
            {loading ? "Entrando..." : "Entrar"}
          </button>
          <button className="btn" onClick={() => router.push("/register")}>
            Criar conta
          </button>
        </div>
        <div className="muted" style={{ marginTop: 12 }}>
          O refresh token fica em cookie httpOnly; o access token fica apenas em memória.
        </div>
      </div>
    </div>
  );
}

