-- 0003 - Performance indexes & extensions
-- Notes:
-- - Safe to run multiple times (IF NOT EXISTS).
-- - Requires privileges to CREATE EXTENSION (pg_trgm).

BEGIN;

CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- Transactions: support ORDER BY (occurred_at desc, created_at desc) and keyset pagination.
CREATE INDEX IF NOT EXISTS ix_transactions_user_id_occurred_at_created_at_id_desc
  ON transactions(user_id, occurred_at DESC, created_at DESC, id DESC);

-- Text search acceleration for ILIKE '%term%'.
CREATE INDEX IF NOT EXISTS ix_transactions_description_trgm
  ON transactions USING gin (description gin_trgm_ops);

CREATE INDEX IF NOT EXISTS ix_transactions_notes_trgm
  ON transactions USING gin (notes gin_trgm_ops);

-- Dashboards/budgets: common spend filter (expenses only, ignore excluded).
CREATE INDEX IF NOT EXISTS ix_transactions_budget_spend
  ON transactions(user_id, category_id, occurred_at)
  WHERE amount < 0 AND ignore_in_dashboard = false;

COMMIT;

