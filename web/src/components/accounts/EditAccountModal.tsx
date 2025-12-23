"use client";

import React, { useEffect, useMemo, useState } from "react";
import { Modal } from "@/components/Modal";
import { AccountType, type AccountDto } from "@/lib/api/types";
import { useUpdateAccount } from "@/lib/api/hooks";
import { errorMessage } from "@/lib/api/errorMessage";

function parseNumberOrNull(v: string): number | null {
  const normalized = v.trim().replace(",", ".");
  if (!normalized) return null;
  const n = Number(normalized);
  return Number.isFinite(n) ? n : null;
}

export function EditAccountModal({
  open,
  account,
  onClose
}: {
  open: boolean;
  account: AccountDto;
  onClose: () => void;
}) {
  const update = useUpdateAccount();
  const [name, setName] = useState(account.name);
  const [type, setType] = useState<AccountType>(account.type);
  const [initialBalance, setInitialBalance] = useState<string>(String(account.initialBalance ?? 0));
  const [notice, setNotice] = useState<{ kind: "success" | "error"; message: string } | null>(null);

  useEffect(() => {
    setName(account.name);
    setType(account.type);
    setInitialBalance(String(account.initialBalance ?? 0));
    setNotice(null);
  }, [account]);

  const isCreditCard = type === AccountType.CreditCard;

  const changed = useMemo(() => {
    const ib = parseNumberOrNull(initialBalance);
    return {
      name: name !== account.name ? name : undefined,
      type: type !== account.type ? type : undefined,
      initialBalance:
        isCreditCard
          ? undefined
          : ib !== null && ib !== account.initialBalance
            ? ib
            : undefined
    };
  }, [account.initialBalance, account.name, account.type, initialBalance, isCreditCard, name, type]);

  const hasChanges = changed.name !== undefined || changed.type !== undefined || changed.initialBalance !== undefined;

  async function save() {
    setNotice(null);
    try {
      await update.mutateAsync({
        id: account.id,
        name: changed.name,
        type: changed.type,
        initialBalance: changed.initialBalance ?? undefined
      });
      setNotice({ kind: "success", message: "Conta atualizada." });
    } catch (e) {
      setNotice({ kind: "error", message: errorMessage(e, "Falha ao atualizar conta.") });
    }
  }

  return (
    <Modal
      open={open}
      title="Editar conta"
      onClose={() => {
        if (!update.isPending) onClose();
      }}
      footer={
        <div className="row" style={{ justifyContent: "flex-end" }}>
          <button className="btn" disabled={update.isPending} onClick={onClose}>
            Fechar
          </button>
          <button className="btn primary" disabled={update.isPending || !hasChanges} onClick={save}>
            {update.isPending ? "Salvando..." : "Salvar"}
          </button>
        </div>
      }
    >
      <div className="row" style={{ alignItems: "flex-end" }}>
        <div className="field" style={{ flex: "1 1 260px" }}>
          <label>Nome</label>
          <input value={name} onChange={(e) => setName(e.target.value)} />
        </div>

        <div className="field" style={{ width: 220 }}>
          <label>Tipo</label>
          <select
            value={type}
            onChange={(e) => {
              const next = Number(e.target.value) as AccountType;
              setType(next);
              if (next === AccountType.CreditCard) setInitialBalance("0");
            }}
          >
            <option value={AccountType.Checking}>CHECKING</option>
            <option value={AccountType.Savings}>SAVINGS</option>
            <option value={AccountType.CreditCard}>CREDIT_CARD</option>
          </select>
        </div>

        <div className="field" style={{ width: 220 }}>
          <label>Saldo inicial</label>
          <input
            value={initialBalance}
            onChange={(e) => setInitialBalance(e.target.value)}
            inputMode="decimal"
            disabled={isCreditCard}
            placeholder={isCreditCard ? "0 (CREDIT_CARD)" : "0.00"}
          />
        </div>
      </div>

      <div className="muted" style={{ marginTop: 10, fontSize: 13 }}>
        Saldo atual: <code>{account.balance?.toFixed?.(2) ?? "—"}</code> • Moeda: <code>{account.currency}</code>
      </div>

      {isCreditCard ? (
        <div className="muted" style={{ marginTop: 8, fontSize: 13 }}>
          Para <code>CREDIT_CARD</code>, <code>saldo inicial</code> é sempre 0. A API pode bloquear alteração de tipo se houver transações.
        </div>
      ) : null}

      {notice ? (
        <div className={notice.kind === "error" ? "error" : "muted"} style={{ marginTop: 12 }}>
          {notice.message}
        </div>
      ) : null}
    </Modal>
  );
}

