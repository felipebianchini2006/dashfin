"use client";

import React, { useState } from "react";
import { AccountType } from "@/lib/api/types";
import { useAccounts, useCreateAccount } from "@/lib/api/hooks";
import { Notice } from "@/components/Notice";
import { errorMessage } from "@/lib/api/errorMessage";
import { EditAccountModal } from "@/components/accounts/EditAccountModal";

export default function AccountsPage() {
  const accounts = useAccounts();
  const create = useCreateAccount();

  const [name, setName] = useState("");
  const [type, setType] = useState<number>(AccountType.Checking);
  const [initialBalance, setInitialBalance] = useState<string>("0");
  const [notice, setNotice] = useState<{ kind: "success" | "error"; message: string } | null>(null);
  const [editId, setEditId] = useState<string | null>(null);

  const selected = (accounts.data ?? []).find((a) => a.id === editId) ?? null;
  const createIsCard = type === AccountType.CreditCard;

  return (
    <div className="container">
      {notice ? (
        <div style={{ marginBottom: 12 }}>
          <Notice kind={notice.kind} message={notice.message} onClose={() => setNotice(null)} />
        </div>
      ) : null}

      <div className="row">
        <div className="card" style={{ flex: "1 1 520px" }}>
          <div className="row" style={{ justifyContent: "space-between", alignItems: "center" }}>
            <h2 style={{ marginTop: 0 }}>Contas</h2>
            <div className="muted">{accounts.isFetching ? "Atualizando..." : "—"}</div>
          </div>

          {accounts.isLoading ? <div className="muted">Carregando...</div> : null}
          {accounts.isError ? <div className="error">Falha ao carregar contas.</div> : null}

          {(accounts.data?.length ?? 0) === 0 && !accounts.isLoading && !accounts.isError ? (
            <div className="muted">Nenhuma conta.</div>
          ) : null}

          {(accounts.data?.length ?? 0) > 0 ? (
            <table className="table" style={{ marginTop: 12 }}>
              <thead>
                <tr>
                  <th>Nome</th>
                  <th>Tipo</th>
                  <th>Moeda</th>
                  <th>Saldo inicial</th>
                  <th>Saldo atual</th>
                  <th>Cartão (mês)</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {(accounts.data ?? []).map((a) => (
                  <tr key={a.id}>
                    <td>{a.name}</td>
                    <td>{AccountType[a.type]}</td>
                    <td>{a.currency}</td>
                    <td>{a.initialBalance.toFixed(2)}</td>
                    <td>{a.balance?.toFixed?.(2) ?? "—"}</td>
                    <td>{a.creditCardSpendThisMonth?.toFixed?.(2) ?? "—"}</td>
                    <td>
                      <button className="btn" onClick={() => setEditId(a.id)}>
                        Editar
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : null}
        </div>

        <div className="card" style={{ flex: "1 1 360px" }}>
          <h2 style={{ marginTop: 0 }}>Nova conta</h2>
          <div className="field">
            <label>Nome</label>
            <input value={name} onChange={(e) => setName(e.target.value)} />
          </div>
          <div className="field" style={{ marginTop: 12 }}>
            <label>Tipo</label>
            <select
              value={type}
              onChange={(e) => {
                const next = Number(e.target.value);
                setType(next);
                if (next === AccountType.CreditCard) setInitialBalance("0");
              }}
            >
              <option value={AccountType.Checking}>CHECKING</option>
              <option value={AccountType.Savings}>SAVINGS</option>
              <option value={AccountType.CreditCard}>CREDIT_CARD</option>
            </select>
          </div>
          <div className="field" style={{ marginTop: 12 }}>
            <label>Saldo inicial</label>
            <input
              value={initialBalance}
              onChange={(e) => setInitialBalance(e.target.value)}
              inputMode="decimal"
              disabled={createIsCard}
              placeholder={createIsCard ? "0 (CREDIT_CARD)" : "0.00"}
            />
          </div>
          <div style={{ marginTop: 16 }}>
            <button
              className="btn primary"
              disabled={create.isPending}
              onClick={async () => {
                setNotice(null);
                try {
                  const ib = Number(initialBalance.trim().replace(",", "."));
                  await create.mutateAsync({ name, type, initialBalance: Number.isFinite(ib) ? ib : 0 });
                  setName("");
                  setType(AccountType.Checking);
                  setInitialBalance("0");
                  setNotice({ kind: "success", message: "Conta criada." });
                } catch (e) {
                  setNotice({ kind: "error", message: errorMessage(e, "Falha ao criar conta.") });
                }
              }}
            >
              {create.isPending ? "Salvando..." : "Criar"}
            </button>
          </div>
        </div>
      </div>

      {selected ? (
        <EditAccountModal
          open={!!editId}
          account={selected}
          onClose={() => {
            setEditId(null);
          }}
        />
      ) : null}
    </div>
  );
}
