-- Dashfin - PostgreSQL schema (DDL)
-- Notes:
-- - Do NOT store DB passwords in migrations or SQL files.
-- - Requires extensions: pgcrypto (UUID) and citext (case-insensitive email).

BEGIN;

CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS citext;
-- For ILIKE '%...%' search acceleration on large tables.
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- =========================
-- Users (minimal / optional)
-- =========================
-- If you use ASP.NET Core Identity, you can:
-- 1) map Identity's user table to "users" (table renaming), OR
-- 2) change all FKs to point to Identity's users table (e.g., "AspNetUsers").
CREATE TABLE IF NOT EXISTS users (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  email citext NOT NULL UNIQUE,
  display_name text NULL,
  password_hash text NOT NULL DEFAULT '',
  timezone text NOT NULL DEFAULT 'America/Sao_Paulo',
  currency char(3) NOT NULL DEFAULT 'BRL',
  display_preferences jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at timestamptz NOT NULL DEFAULT now()
);

-- =====================
-- Refresh tokens (JWT)
-- =====================
CREATE TABLE IF NOT EXISTS user_refresh_tokens (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  token_hash char(64) NOT NULL UNIQUE,
  expires_at timestamptz NOT NULL,
  revoked_at timestamptz NULL,
  replaced_by_token_hash char(64) NULL,
  revoked_reason text NULL,
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_user_refresh_tokens_user_id ON user_refresh_tokens(user_id);

-- =========
-- Accounts
-- =========
-- type: CHECKING=1, CREDIT_CARD=2, SAVINGS=3
CREATE TABLE IF NOT EXISTS accounts (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  type smallint NOT NULL,
  name text NOT NULL,
  institution text NULL,
  currency char(3) NOT NULL DEFAULT 'BRL',
  initial_balance numeric(18,2) NOT NULL DEFAULT 0,
  created_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_accounts_type CHECK (type IN (1, 2, 3))
);

CREATE INDEX IF NOT EXISTS ix_accounts_user_id ON accounts(user_id);

-- =======
-- Imports
-- =======
-- status: UPLOADED, PROCESSING, DONE, FAILED
CREATE TABLE IF NOT EXISTS imports (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  account_id uuid NULL REFERENCES accounts(id) ON DELETE SET NULL,
  status text NOT NULL,
  file_name text NOT NULL,
  file_size_bytes bigint NULL,
  file_sha256 char(64) NOT NULL,
  storage_provider text NOT NULL DEFAULT 'local',
  storage_key text NOT NULL,
  summary_json jsonb NULL,
  processed_at timestamptz NULL,
  error_message text NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_imports_status CHECK (status IN ('UPLOADED', 'PROCESSING', 'DONE', 'FAILED'))
);

CREATE INDEX IF NOT EXISTS ix_imports_user_id_created_at_desc ON imports(user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_imports_user_id_status ON imports(user_id, status);
CREATE INDEX IF NOT EXISTS ix_imports_account_id_created_at_desc ON imports(account_id, created_at DESC);
CREATE UNIQUE INDEX IF NOT EXISTS ux_imports_user_id_file_sha256 ON imports(user_id, file_sha256);

-- ===========
-- Import rows
-- ===========
-- status: PARSED, SKIPPED, ERROR
CREATE TABLE IF NOT EXISTS import_rows (
  id bigserial PRIMARY KEY,
  import_id uuid NOT NULL REFERENCES imports(id) ON DELETE CASCADE,
  user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  row_index integer NOT NULL,
  page_number integer NULL,
  row_sha256 char(64) NULL,
  status text NOT NULL,
  raw_text text NULL,
  raw_data jsonb NULL,
  error_code text NULL,
  error_message text NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_import_rows_status CHECK (status IN ('PARSED', 'SKIPPED', 'ERROR'))
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_import_rows_import_id_row_index ON import_rows(import_id, row_index);
CREATE INDEX IF NOT EXISTS ix_import_rows_user_id_created_at_desc ON import_rows(user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_import_rows_import_id_status ON import_rows(import_id, status);

-- ==========
-- Categories
-- ==========
CREATE TABLE IF NOT EXISTS categories (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  name citext NOT NULL,
  parent_id uuid NULL REFERENCES categories(id) ON DELETE SET NULL,
  color text NULL,
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_categories_user_id_name ON categories(user_id, name);
CREATE INDEX IF NOT EXISTS ix_categories_user_id ON categories(user_id);
CREATE INDEX IF NOT EXISTS ix_categories_user_id_parent_id ON categories(user_id, parent_id);

-- ===============
-- Category rules
-- ===============
-- match_type: CONTAINS=1, REGEX=2
CREATE TABLE IF NOT EXISTS category_rules (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  category_id uuid NOT NULL REFERENCES categories(id) ON DELETE CASCADE,
  match_type smallint NOT NULL,
  pattern text NOT NULL,
  account_id uuid NULL REFERENCES accounts(id) ON DELETE CASCADE,
  priority integer NOT NULL DEFAULT 100,
  is_active boolean NOT NULL DEFAULT true,
  min_amount numeric(18,2) NULL,
  max_amount numeric(18,2) NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_category_rules_match_type CHECK (match_type IN (1, 2))
);

CREATE INDEX IF NOT EXISTS ix_category_rules_user_id_active_priority ON category_rules(user_id, is_active, priority);
CREATE INDEX IF NOT EXISTS ix_category_rules_category_id ON category_rules(category_id);
CREATE INDEX IF NOT EXISTS ix_category_rules_account_id ON category_rules(account_id);

-- =============
-- Transactions
-- =============
-- amount is signed numeric(18,2). Convention:
-- - expenses: negative; income: positive (or decide at app level, but keep it consistent).
CREATE TABLE IF NOT EXISTS transactions (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  account_id uuid NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  category_id uuid NULL REFERENCES categories(id) ON DELETE SET NULL,
  import_id uuid NULL REFERENCES imports(id) ON DELETE SET NULL,
  import_row_id bigint NULL REFERENCES import_rows(id) ON DELETE SET NULL,
  occurred_at timestamptz NOT NULL,
  description text NOT NULL,
  notes text NULL,
  ignore_in_dashboard boolean NOT NULL DEFAULT false,
  amount numeric(18,2) NOT NULL,
  currency char(3) NOT NULL DEFAULT 'BRL',
  fingerprint char(64) NOT NULL,
  metadata jsonb NULL,
  created_at timestamptz NOT NULL DEFAULT now()
);

-- Required indexes
CREATE UNIQUE INDEX IF NOT EXISTS ux_transactions_user_id_fingerprint ON transactions(user_id, fingerprint);
CREATE INDEX IF NOT EXISTS ix_transactions_user_id_occurred_at_desc ON transactions(user_id, occurred_at DESC);
-- Helps ORDER BY occurred_at DESC, created_at DESC and future keyset pagination.
CREATE INDEX IF NOT EXISTS ix_transactions_user_id_occurred_at_created_at_id_desc ON transactions(user_id, occurred_at DESC, created_at DESC, id DESC);
CREATE INDEX IF NOT EXISTS ix_transactions_user_id_account_id_occurred_at_desc ON transactions(user_id, account_id, occurred_at DESC);
CREATE INDEX IF NOT EXISTS ix_transactions_user_id_category_id_occurred_at_desc ON transactions(user_id, category_id, occurred_at DESC);

-- Extra helpful indexes
CREATE INDEX IF NOT EXISTS ix_transactions_import_id ON transactions(import_id);
CREATE INDEX IF NOT EXISTS ix_transactions_import_row_id ON transactions(import_row_id);

-- Text search (ILIKE) performance:
-- - Recommended indexes (choose based on query patterns):
CREATE INDEX IF NOT EXISTS ix_transactions_description_trgm ON transactions USING gin (description gin_trgm_ops);
CREATE INDEX IF NOT EXISTS ix_transactions_notes_trgm ON transactions USING gin (notes gin_trgm_ops);

-- Budget progress queries:
-- - Summing monthly spend benefits from filtering by user_id + category_id + occurred_at.
-- - Existing index `ix_transactions_user_id_category_id_occurred_at_desc` usually suffices; if needed, consider a partial index:
CREATE INDEX IF NOT EXISTS ix_transactions_budget_spend
ON transactions(user_id, category_id, occurred_at)
WHERE amount < 0 AND ignore_in_dashboard = false;

-- ========
-- Budgets
-- ========
-- month must be the 1st day of month (DATE).
CREATE TABLE IF NOT EXISTS budgets (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  category_id uuid NOT NULL REFERENCES categories(id) ON DELETE CASCADE,
  month date NOT NULL,
  limit_amount numeric(18,2) NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_budgets_month_first_day CHECK (month = date_trunc('month', month)::date)
);

-- Required unique index
CREATE UNIQUE INDEX IF NOT EXISTS ux_budgets_user_id_category_id_month ON budgets(user_id, category_id, month);
CREATE INDEX IF NOT EXISTS ix_budgets_user_id_month ON budgets(user_id, month);

-- ===========
-- Alert rules
-- ===========
-- type: OVER_BUDGET=1, THRESHOLD=2
CREATE TABLE IF NOT EXISTS alert_rules (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  type smallint NOT NULL,
  name text NOT NULL,
  is_active boolean NOT NULL DEFAULT true,
  budget_id uuid NULL REFERENCES budgets(id) ON DELETE CASCADE,
  category_id uuid NULL REFERENCES categories(id) ON DELETE CASCADE,
  account_id uuid NULL REFERENCES accounts(id) ON DELETE CASCADE,
  threshold_amount numeric(18,2) NULL,
  threshold_percent numeric(18,2) NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_alert_rules_type CHECK (type IN (1, 2)),
  CONSTRAINT ck_alert_rules_thresholds CHECK (
    (type = 1 AND category_id IS NOT NULL)
    OR
    (type = 2 AND (threshold_amount IS NOT NULL OR threshold_percent IS NOT NULL))
  )
);

CREATE INDEX IF NOT EXISTS ix_alert_rules_user_id_active ON alert_rules(user_id, is_active);
CREATE INDEX IF NOT EXISTS ix_alert_rules_budget_id ON alert_rules(budget_id);
CREATE INDEX IF NOT EXISTS ix_alert_rules_category_id ON alert_rules(category_id);
CREATE INDEX IF NOT EXISTS ix_alert_rules_account_id ON alert_rules(account_id);

-- ============
-- Alert events
-- ============
-- status: NEW=1, READ=2, DISMISSED=3
CREATE TABLE IF NOT EXISTS alert_events (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  alert_rule_id uuid NOT NULL REFERENCES alert_rules(id) ON DELETE CASCADE,
  fingerprint char(64) NOT NULL,
  status smallint NOT NULL,
  occurred_at timestamptz NOT NULL,
  title text NOT NULL,
  body text NULL,
  payload jsonb NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_alert_events_status CHECK (status IN (1, 2, 3))
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_alert_events_user_id_fingerprint ON alert_events(user_id, fingerprint);
CREATE INDEX IF NOT EXISTS ix_alert_events_user_id_occurred_at_desc ON alert_events(user_id, occurred_at DESC);
CREATE INDEX IF NOT EXISTS ix_alert_events_user_id_status_occurred_at_desc ON alert_events(user_id, status, occurred_at DESC);
CREATE INDEX IF NOT EXISTS ix_alert_events_alert_rule_id_occurred_at_desc ON alert_events(alert_rule_id, occurred_at DESC);

COMMIT;
