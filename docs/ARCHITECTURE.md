# Clean Architecture (Finance)

## Objetivo

- Separar regras de negócio (Domain) de casos de uso (Application), integrações (Infrastructure) e entrega HTTP (API).
- Facilitar evolução do parsing/import, idempotência/dedupe e multi-tenant por `user_id`.

## Camadas e responsabilidades

### `Finance.Domain`
- Entidades e invariantes do domínio.
- Enums e value objects.
- Interface marker para ownership por usuário (`IUserOwnedEntity`).

### `Finance.Application`
- Casos de uso (Commands/Queries) via CQRS leve + MediatR.
- Contratos (ports) para persistência, relógio, storage e autenticação.
- Validação (FluentValidation) e behaviors do pipeline.
- Tipos comuns (Result/Error) e exceções de aplicação.

### `Finance.Infrastructure`
- Implementações dos contratos de `Finance.Application`.
- EF Core (Npgsql), DbContext e mapeamentos.
- Hangfire (jobs) e configurações de background processing.
- Storage de arquivos (Local dev e S3 compatível em prod).
- Implementação de emissão/validação de tokens (JWT).

### `Finance.Api`
- HTTP: Controllers, autenticação/autorizações, ProblemDetails e middlewares.
- Serilog e correlation id por request.
- Composição DI (`AddApplication`, `AddInfrastructure`).

## Multi-tenant por usuário

- `user_id` em todas as entidades “user-owned”.
- Padrão recomendado:
  - Handlers recebem `ICurrentUser.UserId`.
  - Repositórios/queries sempre filtram por `user_id`.
  - (Opcional) Global query filter no EF para entidades que implementam `IUserOwnedEntity` (exige cuidado com contexto design-time).

## Autenticação escolhida

- JWT (access token) + refresh token em cookie httpOnly.
- Persistência do refresh token (hash + expiração) deve existir no banco (coluna(s) em `users` ou tabela dedicada) — criar em migração quando implementar auth de verdade.

