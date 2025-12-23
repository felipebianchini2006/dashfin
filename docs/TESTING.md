# Testing Strategy

This repo uses a layered test strategy:

- **Unit tests (fast, deterministic)**: pure logic and application handlers/services.
- **Parser tests (fixtures, no PDFs)**: statement parsers consume extracted lines (`PdfTextPage.Lines`), so tests use line fixtures instead of real PDFs.
- **API + Postgres integration tests**: `Finance.Api` hosted in-memory with a real Postgres database to validate SQL behavior (ILIKE, JSONB, etc).
- **E2E (Playwright)**: exercises the real UI + API flow.

## Unit tests

Project: `tests/Finance.Application.Tests`

Coverage targets:
- Fingerprint + normalization: `tests/Finance.Application.Tests/ImportFingerprintTests.cs`
- Category rule matching/priority: `tests/Finance.Application.Tests/CategoryRulesHandlersTests.cs`
- Budgets/alerts/forecast: `tests/Finance.Application.Tests/BudgetsHandlersTests.cs`, `tests/Finance.Application.Tests/AlertsTests.cs`, `tests/Finance.Application.Tests/ForecastTests.cs`

Run:

```bash
dotnet test tests/Finance.Application.Tests/Finance.Application.Tests.csproj
```

## Parser tests (line fixtures)

Parsers operate over already-extracted lines (`PdfTextPage.Lines`) so we keep fixtures as text files:

- Fixtures: `tests/Finance.Application.Tests/Fixtures/Nubank/*.txt`
- Tests: `tests/Finance.Application.Tests/NubankParserFixtureTests.cs`

Run (same command as unit tests):

```bash
dotnet test tests/Finance.Application.Tests/Finance.Application.Tests.csproj
```

## API integration tests (API + Postgres)

Project: `tests/Finance.Api.IntegrationTests`

What it validates:
- HTTP routing/auth + user scoping
- Postgres-specific query behavior (ILIKE filters)
- Import → transactions → dashboard flow (using a test-friendly PDF extractor and inline import processing)

Requirements:
- Postgres reachable via `TEST_POSTGRES_CONNECTION` (admin DB, usually `postgres`), e.g.:
  - `Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres`

Run:

```bash
export TEST_POSTGRES_CONNECTION="Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres"
dotnet test tests/Finance.Api.IntegrationTests/Finance.Api.IntegrationTests.csproj
```

## E2E (Playwright)

Location: `web/e2e`

This flow assumes the API runs with a test-friendly PDF extractor and inline import processing:

- `Hangfire:Enabled=false`
- `Imports:JobQueue=inline`
- `Imports:PdfTextExtractor=plaintext`

Example (terminal 1): start Postgres (docker) and API

```bash
docker run --rm -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:16
```

```bash
export ConnectionStrings__Default="Host=localhost;Port=5432;Database=dashfin_e2e;Username=postgres;Password=postgres"
export Hangfire__Enabled=false
export Imports__JobQueue=inline
export Imports__PdfTextExtractor=plaintext
export FileStorage__Provider=local
export FileStorage__Local__RootPath=var/e2e-files
dotnet run --project src/Finance.Api --urls http://localhost:5000
```

Terminal 2: run E2E

```bash
cd web
npm ci
npx playwright install --with-deps
npm run e2e
```

Notes:
- The refresh cookie is httpOnly and cross-site. E2E does not rely on refresh working; it uses the access token returned by login during the same session.

## CI example (GitHub Actions)

See `.github/workflows/ci.yml`.

