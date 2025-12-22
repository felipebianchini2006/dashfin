# Migrações (EF Core) + notas de performance/EF

## Estratégia de migrações (recomendado)

1. **Code-first com EF Core Migrations**
   - Manter o schema como fonte de verdade no código (DbContext + configurações Fluent API).
   - Gerar migrações pequenas e incrementais por módulo (Auth/Accounts/Imports/Transactions/etc.).

2. **Baseline**
   - Primeira migração: `0001_Initial` contendo:
     - `CREATE EXTENSION` (`pgcrypto`, `citext`)
     - tabelas + constraints + índices essenciais
   - Migrations posteriores:
     - mudanças de parser/normalização (novos campos em `import_rows`/`transactions`)
     - novos módulos (budgets/alerts) em migrações separadas

3. **Aplicação em ambientes**
   - Dev: `dotnet ef database update`
   - CI/CD: gerar script idempotente e aplicar no deploy:
     - `dotnet ef migrations script --idempotent -o artifacts/migrations.sql`
   - Produção: aplicar via pipeline (ou ferramenta de migração) com lock de deploy (evitar concorrência).

4. **Não versionar credenciais**
   - Nunca colocar senha de banco em SQL/migrations/repo.
   - Usar `ConnectionStrings__Default` via variável de ambiente/secret manager.

## Observações de performance (Postgres)

- **Índices obrigatórios já previstos**
  - `transactions`: `UNIQUE (user_id, fingerprint)` + índices por `(user_id, occurred_at DESC)` e combinações por conta/categoria.
  - `budgets`: `UNIQUE (user_id, category_id, month)`.
- **Ordenação DESC em índice**
  - Postgres usa o índice para `ORDER BY occurred_at DESC` com eficiência quando o predicado começa por `user_id`.
- **Foco em cardinalidade**
  - Sempre filtrar por `user_id` nos endpoints; evita scans caros e melhora cache/localidade do índice.
- **JSONB**
  - Usar `metadata`/`raw_data` com parcimônia; indexar JSONB só quando houver consultas reais por campos internos.
- **Import grande**
  - Inserções em lote (batch) e transação por `ImportBatch` (no job) ajudam muito.
  - Evitar “upsert 1 por 1” em volumes altos: preferir batching e/ou staging.

## Pontos de atenção no EF Core (Npgsql)

### Precisão numérica
- Mapear valores monetários como `decimal` com `numeric(18,2)`:
  - Fluent API: `HasPrecision(18, 2)`.
- Evitar `double/float` para dinheiro.

### Concurrency (concorrência otimista)
- Opções comuns:
  1) **`xmin` como token de concorrência** (Postgres)
     - Bom para updates concorrentes em recursos como `categories`, `budgets`, `alert_rules`.
  2) **coluna `version` bigint** (manual)
     - Útil se quiser controle explícito independente do provider.
- Definir padrão de tratamento: retornar `409 Conflict` via ProblemDetails ao detectar conflito.

### Índices/constraints via migrations
- Criar índices com `HasIndex(...)` e `IsUnique()`; para DESC:
  - EF Core/Npgsql suporta `HasIndex(e => new { e.UserId, e.OccurredAt }).HasDatabaseName(...);`
  - Para `DESC` especificamente, às vezes é mais confiável usar `migrationBuilder.Sql("CREATE INDEX ... DESC")` (dependendo da versão).

### Enums e checks
- Para colunas `smallint` (ex.: `accounts.type`, `category_rules.match_type`), usar enums C# + conversão para `short`.
- Para colunas `text` com CHECK (ex.: `imports.status`, `import_rows.status`), usar enum C# convertido para string ou `const string`.

### Idempotência e dedupe
- `transactions.fingerprint` deve ser determinístico e calculado no backend (mesma entrada → mesmo hash).
- Confiar no `UNIQUE (user_id, fingerprint)` como última barreira; tratar violação (23505) como “já importado”.

## Artefatos

- DDL de referência (SQL): `db/schema.sql`

