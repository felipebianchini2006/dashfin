# Observability

## Log enrichment (Serilog)

Every request is enriched with:

- `RequestId`: `HttpContext.TraceIdentifier`
- `CorrelationId`: propagated via `X-Correlation-Id` (see `CorrelationIdMiddleware`)
- `UserId`: extracted from JWT claims (if authenticated)
- `ImportId`: extracted from URL paths like `/imports/{id}` (when applicable)

Implementation:
- `src/Finance.Api/Middleware/CorrelationIdMiddleware.cs`
- `src/Finance.Api/Middleware/RequestLogContextMiddleware.cs`
- `src/Finance.Api/Program.cs`

### Example request log

Fields vary by sink/template, but the key properties are present:

```
Request finished HTTP/1.1 GET /imports 200 in 34.21ms
  UserId=0f5d... CorrelationId=9a31... RequestId=0HMX... ImportId=null
```

## Import pipeline logs

`ImportProcessor` emits stage-by-stage logs with duration and counters to diagnose parser/dedupe issues quickly.

Implementation:
- `src/Finance.Application/Imports/Processing/ImportProcessor.cs`

### Example import logs

```
Starting import processing (provider=local) importId=... userId=...
Import stage read_pdf done in 3ms (bytes=81234) importId=... userId=...
Import stage extract_text done in 40ms (pages=1, lines=120) importId=... userId=...
Import stage detect_layout done in 0ms (layout=NubankConta) importId=... userId=...
Import stage parse done in 5ms (parser=NubankCheckingPdfParser, defaultYear=2025) importId=... userId=...
Import stage normalize_dedupe_keys done in 0ms (parsed=34, unique=33, skipped=80, errors=1) importId=... userId=...
Import stage load_existing_fingerprints done in 12ms (existing=10) importId=... userId=...
Import stage load_category_rules done in 2ms importId=... userId=...
Import stage upsert_rows done in 9ms (rows=115) importId=... userId=...
Import stage insert_transactions done in 18ms (candidates=23) importId=... userId=...
Import DONE (parsed=33, inserted=23, deduped=10, errors=1) importId=... userId=...
Import pipeline completed in 112ms importId=... userId=...
```

If there are race-condition conflicts on insertion, the processor logs the retry and how many duplicates were dropped:

```
Transaction insert had conflicts; retrying with duplicates removed importId=...
Import insert retry: removed 5 duplicates after conflict detection importId=...
```

## Health endpoints

Endpoints:
- `GET /health/live` – liveness (always `200`)
- `GET /health` – readiness summary (db + hangfire + storage)
- `GET /health/db`
- `GET /health/hangfire`
- `GET /health/storage`

Sample response:

```json
{
  "status": "Healthy",
  "totalDurationMs": 12.3,
  "checks": {
    "db": { "status": "Healthy", "durationMs": 4.1, "description": "Database reachable.", "error": null },
    "hangfire": { "status": "Healthy", "durationMs": 1.2, "description": "Hangfire storage reachable.", "error": null },
    "storage": { "status": "Healthy", "durationMs": 7.0, "description": "Storage ok (provider=local).", "error": null }
  }
}
```

If Hangfire is disabled (`Hangfire:Enabled=false`), the health will report `"Hangfire disabled."` as a healthy check.

