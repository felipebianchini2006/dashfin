"use client";

import React from "react";
import Link from "next/link";
import { ImportUploadCard } from "@/components/imports/ImportUploadCard";
import { useImportsWithPolling } from "@/lib/api/hooks";
import { ImportStatusBadge } from "@/components/imports/ImportStatusBadge";
import { ImportStatus } from "@/lib/api/types";
import { useRouter, useSearchParams } from "next/navigation";

function tryGetCounts(summaryJson: string | null | undefined): { parsed?: number; inserted?: number; deduped?: number; errors?: number } | null {
  if (!summaryJson) return null;
  try {
    const data = JSON.parse(summaryJson) as any;
    const counts = data?.counts;
    if (!counts || typeof counts !== "object") return null;
    const num = (v: any) => (typeof v === "number" ? v : undefined);
    return {
      parsed: num(counts.parsed),
      inserted: num(counts.inserted),
      deduped: num(counts.deduped),
      errors: num(counts.errors)
    };
  } catch {
    return null;
  }
}

export default function ImportsPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const status = searchParams.get("status") ?? "";

  const imports = useImportsWithPolling({ status: status || undefined, page: 1, page_size: 50 });
  const anyProcessing = (imports.data?.items ?? []).some((i) => i.status === ImportStatus.Uploaded || i.status === ImportStatus.Processing);

  function setStatus(next: string) {
    const sp = new URLSearchParams(searchParams.toString());
    if (!next) sp.delete("status");
    else sp.set("status", next);
    const qs = sp.toString();
    router.push(qs ? `/app/imports?${qs}` : "/app/imports");
  }

  return (
    <div className="container">
      <div className="row">
        <div style={{ flex: "1 1 520px" }}>
          <ImportUploadCard
            onUploaded={(id) => {
              imports.refetch();
              router.push(`/app/imports/${id}`);
            }}
          />
        </div>
        <div className="card" style={{ flex: "1 1 520px" }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
            <h2 style={{ marginTop: 0 }}>Imports</h2>
            <div style={{ display: "flex", gap: 10, alignItems: "center" }}>
              <span className="muted">
                {imports.isFetching ? "Atualizando..." : anyProcessing ? "Polling (backoff)..." : "—"}
              </span>
              <button className="btn" onClick={() => imports.refetch()}>
                Atualizar
              </button>
            </div>
          </div>

          <div className="row" style={{ marginTop: 10, alignItems: "flex-end", justifyContent: "space-between" }}>
            <div className="field" style={{ minWidth: 240 }}>
              <label>Status</label>
              <select value={status} onChange={(e) => setStatus(e.target.value)}>
                <option value="">Todos</option>
                <option value="Uploaded">UPLOADED</option>
                <option value="Processing">PROCESSING</option>
                <option value="Done">DONE</option>
                <option value="Failed">FAILED</option>
              </select>
            </div>
            <div className="muted">Total: {imports.data?.totalCount ?? 0}</div>
          </div>

          {imports.isLoading ? <div>Carregando...</div> : null}
          {imports.isError ? <div className="error">Falha ao carregar imports.</div> : null}

          {!imports.isLoading && !imports.isError && (imports.data?.items?.length ?? 0) === 0 ? (
            <div className="muted" style={{ marginTop: 12 }}>
              Nenhum import encontrado.
            </div>
          ) : null}

          {(imports.data?.items?.length ?? 0) > 0 ? (
            <table className="table" style={{ marginTop: 12 }}>
              <thead>
                <tr>
                  <th>Quando</th>
                  <th>Conta</th>
                  <th>Status</th>
                  <th>Resumo</th>
                  <th>Detalhe</th>
                </tr>
              </thead>
              <tbody>
                {(imports.data?.items ?? []).map((i) => {
                  const counts = tryGetCounts(i.summaryJson);
                  const summary =
                    counts && (counts.parsed ?? counts.inserted ?? counts.deduped ?? counts.errors) !== undefined
                      ? `parsed ${counts.parsed ?? "—"} • inserted ${counts.inserted ?? "—"} • deduped ${counts.deduped ?? "—"} • errors ${counts.errors ?? "—"}`
                      : "—";

                  return (
                    <tr key={i.id}>
                      <td>{i.createdAt.slice(0, 10)}</td>
                      <td>{i.account?.name ?? "—"}</td>
                      <td>
                        <ImportStatusBadge status={i.status} />
                      </td>
                      <td className="muted" style={{ fontSize: 13 }}>
                        {summary}
                      </td>
                      <td>
                        <Link href={`/app/imports/${i.id}`} style={{ color: "var(--primary)" }}>
                          abrir
                        </Link>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          ) : null}
        </div>
      </div>
    </div>
  );
}
