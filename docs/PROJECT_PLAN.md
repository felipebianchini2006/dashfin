# Dashfin — Plano de Implementação (MVP → v1)

Aplicação web de finanças pessoais com foco em importação de PDFs do Nubank (Conta e Cartão), normalização de transações, deduplicação idempotente e visualizações (dashboard, orçamento e alertas).

## Objetivo e escopo

- Importar PDFs do **Nubank Conta** e **Nubank Cartão**.
- Extrair e auditar linhas brutas (`import_rows`), normalizar em `transactions` e **deduplicar** de forma idempotente (reimportações/reprocessamentos não duplicam).
- Permitir categorização (manual e via regras), orçamento mensal e alertas.
- Fornecer dashboard com visão de gastos/receitas e tendências.

## Decisões técnicas (obrigatórias) + justificativa curta

- **Frontend: Next.js + TypeScript**
  - SSR/CSR híbrido, bom DX, roteamento e ecossistema; TS reduz bugs em UI e integrações.
- **Backend: ASP.NET Core .NET 8 Web API**
  - Performance, maturidade do ecossistema, observabilidade e integração natural com MediatR/Hangfire.
- **Banco: PostgreSQL**
  - Robusto para agregações, índices avançados e boa relação custo/benefício.
- **Arquitetura: Clean Architecture (Domain/Application/Infrastructure/API)**
  - Regras de negócio isoladas (Domain/Application), integrações em Infrastructure, entrega em API; facilita evolução do parsing.
- **CQRS leve com MediatR**
  - Comandos/queries explícitos, handlers testáveis; evita complexidade desnecessária.
- **FluentValidation**
  - Validação consistente na borda (API) e em comandos; mensagens claras para UX.
- **Serilog**
  - Logs estruturados e correlação; essencial para debug e auditoria de importações.
- **ProblemDetails (RFC 7807)**
  - Padroniza erros de API e melhora a experiência de consumo/diagnóstico.
- **Jobs: Hangfire + Postgres**
  - Pipeline assíncrono (parsing/dedupe/reprocess), histórico e retentativa com storage consistente.
- **Armazenamento de PDF**
  - **Dev:** filesystem local
  - **Prod:** storage S3 compatível (ex.: AWS S3/MinIO)
  - Abstração evita acoplamento ao filesystem e facilita escala/retention.

## Módulos do produto

- **Auth**
- **Accounts**
- **Imports**
- **Transactions**
- **Categories/Rules**
- **Budgets**
- **Alerts**
- **Dashboard**
- **Jobs**

## Modelo de domínio (núcleo mínimo)

- **User**: identidade e isolamento multi-tenant (por usuário).
- **Account**: conta (ex.: Nubank Conta).
- **Card/CreditAccount**: cartão/conta de crédito (Nubank Cartão).
- **ImportBatch**: upload + metadados (origem, tipo, hash do arquivo, status, timestamps).
- **ImportRow** (auditoria): registro extraído do PDF com payload bruto, página/posição, status e erros/warnings.
- **Transaction** (normalizada): data, descrição, valor (decimal), tipo (debit/credit), moeda, origem, referências externas quando existirem.
- **Category**: categoria de lançamento.
- **Rule**: regras de categorização (contains/regex, prioridade, escopo por conta/cartão, sinal/valor).
- **Budget**: orçamento mensal (total e por categoria).
- **Alert**: eventos/avisos (orçamento estourado, anomalia, fatura próxima etc.).

## Pipeline de importação (PDF → transações)

1. **Upload**
   - Salvar PDF (provider de storage), calcular `file_sha256`, criar `ImportBatch` com status `Uploaded`.
2. **Enfileirar Job**
   - `ParseImportBatch(batchId)` via Hangfire.
3. **Parsing**
   - Extrair texto/tabelas e mapear em `ImportRow` (inclui `row_hash`, página/posição e payload bruto).
4. **Normalização**
   - Converter `ImportRow` → `TransactionCandidate` (datas, timezone, sinal, formatação, validação).
5. **Dedupe idempotente**
   - Chave determinística por usuário + origem + campos normalizados (data, valor, descrição normalizada, parcela, identificadores do PDF quando existirem) + `file_sha256`.
   - Persistência protegida por **constraint única** e/ou **upsert** para impedir duplicatas em reprocessamentos.
6. **Pós-processo**
   - Aplicar regras de categorias, recalcular agregados necessários e marcar batch como `Completed` ou `CompletedWithWarnings`.
7. **Observabilidade**
   - Logs correlacionados por `batchId` + métricas (tempo de parsing, qtd linhas, qtd dedupes).

## Plano incremental (MVP → v1)

### MVP (entregar valor rápido e seguro)

1. **Foundation/Infra**
   - Setup do monorepo (Next + .NET), migrations, logging, ProblemDetails, configuração por ambiente.
2. **Auth**
   - Cadastro/login/sessão; proteção de rotas e endpoints.
3. **Accounts**
   - CRUD mínimo de contas/cartões (Nubank Conta/Cartão) por usuário.
4. **Imports (PDF)**
   - Upload, storage local (dev), criação de `ImportBatch` e job assíncrono.
   - Parsing inicial para Nubank Conta e Nubank Cartão (cobrir casos comuns).
   - Auditoria completa em `ImportRow`.
5. **Transactions**
   - Listagem com filtros (período/conta/cartão), busca por descrição, detalhes.
   - Dedupe idempotente garantido.
6. **Dashboard (básico)**
   - Totais do mês (receitas/despesas), top categorias (inclui “Sem categoria”), série mensal simples.

**Saída do MVP:** importar PDFs com confiabilidade (reimportação não duplica), visualizar transações e totais principais.

### v1 (produto “usável no dia a dia”)

1. **Categories/Rules**
   - CRUD de categorias e regras, prioridades e escopos.
   - Execução automática pós-import e reprocessamento sob demanda.
2. **Budgets**
   - Orçamento mensal por categoria + total, cálculo de realizado vs limite.
3. **Alerts**
   - Alertas de orçamento (>=80%, >100%), variação anômala (baseline simples), fatura próxima do fechamento.
4. **Dashboard (completo)**
   - Por categoria, por conta/cartão, evolução mensal, comparativo mês anterior, maiores gastos, drill-down para transações.
5. **Jobs (operacional)**
   - Reprocessar batch, retentativas, dashboard Hangfire protegido, limpeza/retention.
6. **S3 em produção**
   - Provider S3 compatível, (opcional) presigned URLs, lifecycle/retention.

## Riscos e mitigação

- **Parsing de PDF instável (layout muda / texto quebrado)**
  - Mitigação: pipeline tolerante a falhas (`ImportRow` + warnings), testes com “golden files”, parsers versionados por tipo, heurísticas e fallbacks (texto quando tabela falhar).
- **Dedupe incorreto (duplicar ou confundir transações distintas)**
  - Mitigação: chave composta determinística + constraint única + auditoria; expor “possíveis duplicatas” para revisão; permitir override manual (v1+).
- **Performance (PDF grande, muitas transações, dashboard lento)**
  - Mitigação: jobs assíncronos, paginação, índices adequados, agregações eficientes; materializar agregados só se necessário.
- **Inconsistência de valores/sinais (débito/crédito, estorno, parcelas)**
  - Mitigação: normalização explícita por origem (Conta vs Cartão), testes por casos, armazenar campos brutos e flags (estorno/ajuste).
- **Evolução de regras e recategorização**
  - Mitigação: engine determinística com reprocessamento, trilha de auditoria, regras com prioridade e escopo.

## Definition of Done (DoD) por módulo

### Auth
- Login/logout e proteção de rotas/endpoints.
- Expiração/refresh conforme política definida; erros em ProblemDetails.
- Testes de integração para fluxos críticos.

### Accounts
- CRUD com validação e isolamento por usuário.
- Índices básicos por `user_id`; UI simples de seleção.

### Imports
- Upload com limites (tamanho/tipo), hash do arquivo (`file_sha256`).
- Storage abstrato (Local + S3 compatível).
- `ImportBatch` com tracking de status e job de parsing.
- Reimport idempotente e reprocessamento previsível.

### Transactions
- Persistência normalizada com `decimal` (sem float).
- Filtros, busca e paginação; timezone consistente.
- Dedupe garantido por constraint + upsert.

### Categories/Rules
- CRUD de categorias e regras (prioridade, escopo, regex/contains).
- Aplicação automática pós-import + reprocessamento por batch/período.
- Ferramenta de preview/teste de regra em amostra.

### Budgets
- CRUD orçamento mensal e cálculo de realizado vs limite.
- Endpoints para dashboard e UX clara (progresso e detalhamento).
- Testes de cálculo.

### Alerts
- Engine simples (jobs diários e/ou ao importar).
- Persistência de alertas + estados (novo/lido).
- Notificação in-app (v1), extensível a email/push (futuro).

### Dashboard
- KPIs consistentes por período/base; drill-down para transações.
- Performance aceitável (definir SLO p95) e estados de fallback.

### Jobs
- Hangfire configurado com Postgres, retries/backoff.
- Dashboard Hangfire protegido.
- Jobs idempotentes e com logs correlacionados; políticas de retenção.

## Checklist de qualidade (global)

- **Dedupe idempotente:** constraint única + upsert; reprocessar batch não duplica.
- **Auditoria `import_rows`:** armazenar bruto, status, erros/warnings e vínculo com batch.
- **Índices essenciais:**
  - `(user_id, date)`
  - `(user_id, account_id, date)`
  - `(user_id, category_id, date)`
  - uniques de dedupe + FKs consistentes
- **Observabilidade:** correlation id (`batchId`, `userId`), logs Serilog, ProblemDetails padronizado.
- **Segurança:** autorização por usuário em tudo, limites de upload, validação forte (FluentValidation), secrets por ambiente.
- **UX:** import com progresso/status, erros acionáveis, estados vazios, filtros persistentes, “Sem categoria” bem tratada.
- **Dados financeiros:** `decimal`, moeda/locale consistentes, timezone definido, testes de arredondamento.
- **Operação:** migrations versionadas, seed opcional, backups e retenção de PDFs com política clara.

