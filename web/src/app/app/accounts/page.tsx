"use client";

import React, { useState } from "react";
import { AccountType } from "@/lib/api/types";
import { useAccounts, useCreateAccount } from "@/lib/api/hooks";

export default function AccountsPage() {
  const accounts = useAccounts();
  const create = useCreateAccount();

  const [name, setName] = useState("");
  const [type, setType] = useState<number>(AccountType.Checking);
  const [initialBalance, setInitialBalance] = useState<number>(0);

  return (
    <div className="container">
      <div className="row">
        <div className="card" style={{ flex: "1 1 520px" }}>
          <h2 style={{ marginTop: 0 }}>Contas</h2>
          <table className="table">
            <thead>
              <tr>
                <th>Nome</th>
                <th>Tipo</th>
                <th>Moeda</th>
                <th>Saldo</th>
              </tr>
            </thead>
            <tbody>
              {(accounts.data ?? []).map((a) => (
                <tr key={a.id}>
                  <td>{a.name}</td>
                  <td>{AccountType[a.type]}</td>
                  <td>{a.currency}</td>
                  <td>{a.balance ?? "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="card" style={{ flex: "1 1 360px" }}>
          <h2 style={{ marginTop: 0 }}>Nova conta</h2>
          <div className="field">
            <label>Nome</label>
            <input value={name} onChange={(e) => setName(e.target.value)} />
          </div>
          <div className="field" style={{ marginTop: 12 }}>
            <label>Tipo</label>
            <select value={type} onChange={(e) => setType(Number(e.target.value))}>
              <option value={AccountType.Checking}>CHECKING</option>
              <option value={AccountType.Savings}>SAVINGS</option>
              <option value={AccountType.CreditCard}>CREDIT_CARD</option>
            </select>
          </div>
          <div className="field" style={{ marginTop: 12 }}>
            <label>Saldo inicial</label>
            <input value={initialBalance} onChange={(e) => setInitialBalance(Number(e.target.value))} type="number" />
          </div>
          <div style={{ marginTop: 16 }}>
            <button
              className="btn primary"
              disabled={create.isPending}
              onClick={async () => {
                await create.mutateAsync({ name, type, initialBalance });
                setName("");
              }}
            >
              {create.isPending ? "Salvando..." : "Criar"}
            </button>
            {create.isError ? <div className="error" style={{ marginTop: 10 }}>Falha ao criar conta.</div> : null}
          </div>
        </div>
      </div>
    </div>
  );
}

