"use client";

import React from "react";
import Link from "next/link";
import { useImportRowsError, useImportWithPolling } from "@/lib/api/hooks";
import { ImportStatus } from "@/lib/api/types";
import { ImportStatusBadge } from "@/components/imports/ImportStatusBadge";
import { ImportSummary } from "@/components/imports/ImportSummary";

export default function ImportDetailPage({ params }: { params: { id: string } }) {
  const q = useImportWithPolling(params.id);
  const imp = q.data;

  const status = imp?.status;
  const processing = status === ImportStatus.Uploaded || status === ImportStatus.Processing;
  const showErrors = status === ImportStatus.Done || status === ImportStatus.Failed;
  const rows = useImportRowsError(params.id, { enabled: showErrors });

  return (
    <div className="container">
      <div className="row" style={{ alignItems: "center", justifyContent: "space-between" }}>
        <div>
          <h2 style={{ margin: 0 }}>Import</h2>
          <div className="muted" style={{ marginTop: 6 }}>
            <code>{params.id}</code>
          </div>
        </div>
        <div style={{ display: "flex", gap: 10, alignItems: "center" }}>
          {status !== undefined ? <ImportStatusBadge status={status} /> : null}
          <Link className="btn" href="/app/imports">
            Voltar
          </Link>
        </div>
      </div>

      <div className="card" style={{ marginTop: 16 }}>
        {q.isLoading ? <div>Carregando...</div> : null}
        {q.isError ? <div className="error">Falha ao carregar import.</div> : null}

        {imp ? (
          <>
            <div className="row" style={{ alignItems: "flex-end", justifyContent: "space-between" }}>
              <div>
                <div className="muted">Conta</div>
                <div>{imp.account?.name ?? "—"}</div>
              </div>
              <div>
                <div className="muted">Criado em</div>
                <div>{imp.createdAt}</div>
              </div>
              <div className="muted">{processing ? "Atualizando com backoff..." : "—"}</div>
            </div>

            {imp.errorMessage ? (
              <div className="error" style={{ marginTop: 12 }}>
                {imp.errorMessage}
              </div>
            ) : null}

            <div style={{ marginTop: 12 }}>
              <h3 style={{ margin: 0, marginBottom: 10 }}>Resumo</h3>
              <ImportSummary summaryJson={imp.summaryJson} />
            </div>

            <div style={{ marginTop: 16 }}>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                <h3 style={{ margin: 0 }}>Auditoria</h3>
                <Link href={`/app/imports/${params.id}/rows`} style={{ color: "var(--primary)" }}>
                  ver erros (rows ERROR)
                </Link>
              </div>
              <div className="muted" style={{ marginTop: 8 }}>
                O endpoint suporta apenas <code>status=ERROR</code>.
              </div>

              {showErrors ? (
                <div style={{ marginTop: 12 }}>
                  {rows.isLoading ? <div className="muted">Carregando erros...</div> : null}
                  {rows.isError ? <div className="error">Falha ao carregar erros.</div> : null}
                  {rows.data ? (
                    <>
                      <div className="muted" style={{ marginBottom: 8 }}>
                        Total de erros: {rows.data.length}
                      </div>
                      <table className="table">
                        <thead>
                          <tr>
                            <th>#</th>
                            <th>Página</th>
                            <th>Código</th>
                            <th>Mensagem</th>
                          </tr>
                        </thead>
                        <tbody>
                          {rows.data.slice(0, 5).map((r) => (
                            <tr key={r.id}>
                              <td>{r.rowIndex}</td>
                              <td>{r.pageNumber ?? "—"}</td>
                              <td>{r.errorCode ?? "—"}</td>
                              <td>{r.errorMessage ?? "—"}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                      {rows.data.length > 5 ? (
                        <div className="muted" style={{ marginTop: 8 }}>
                          Mostrando 5 de {rows.data.length}.
                        </div>
                      ) : null}
                    </>
                  ) : null}
                </div>
              ) : (
                <div className="muted" style={{ marginTop: 12 }}>
                  Disponível quando o import finalizar.
                </div>
              )}
            </div>
          </>
        ) : null}
      </div>
    </div>
  );
}
