-- Seed default categories for existing users (idempotent).
-- Requires: pgcrypto (gen_random_uuid) and citext (categories.name).

BEGIN;

INSERT INTO categories (id, user_id, name, parent_id, color, created_at)
SELECT
  gen_random_uuid(),
  u.id,
  v.name,
  NULL,
  NULL,
  now()
FROM users u
CROSS JOIN (
  VALUES
    ('Alimentação'),
    ('Transporte'),
    ('Moradia'),
    ('Saúde'),
    ('Lazer'),
    ('Assinaturas'),
    ('Educação'),
    ('Compras'),
    ('Viagem'),
    ('Impostos/Taxas'),
    ('Outros'),
    ('Transferências/Interno')
) AS v(name)
ON CONFLICT (user_id, name) DO NOTHING;

COMMIT;

