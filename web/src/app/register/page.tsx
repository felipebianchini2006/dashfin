"use client";

import React, { useState } from "react";
import { useAuth } from "@/lib/auth/AuthContext";
import { useRouter } from "next/navigation";

export default function RegisterPage() {
  const auth = useAuth();
  const router = useRouter();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  return (
    <div className="container">
      <div className="card" style={{ maxWidth: 420 }}>
        <h2 style={{ marginTop: 0 }}>Criar conta</h2>
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
                await auth.register(email, password);
                await auth.login(email, password);
                router.replace("/app/dashboard");
              } catch (e: any) {
                setError(e?.response?.data?.detail ?? "Falha no cadastro.");
              } finally {
                setLoading(false);
              }
            }}
          >
            {loading ? "Criando..." : "Criar"}
          </button>
          <button className="btn" onClick={() => router.push("/login")}>
            Voltar
          </button>
        </div>
      </div>
    </div>
  );
}

