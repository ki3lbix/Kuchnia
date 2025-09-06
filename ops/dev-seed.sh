
#!/usr/bin/env bash
set -euo pipefail
export PGPASSWORD=${PGPASSWORD:-app}
export PGHOST=${PGHOST:-localhost}
export PGUSER=${PGUSER:-app}
export PGDATABASE=${PGDATABASE:-catering}
export PGPORT=${PGPORT:-5432}

for i in {1..40}; do
  if pg_isready -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDATABASE" >/dev/null 2>&1; then
    break
  fi
  sleep 1
done

psql -c "INSERT INTO clients(name, type) VALUES ('Szkoła nr 1','School') ON CONFLICT DO NOTHING;"
psql -c "INSERT INTO recipes(name) VALUES ('Spaghetti') ON CONFLICT DO NOTHING;"
psql -c "INSERT INTO products(name, unit, category, min_stock, max_stock) VALUES 
  ('Makaron', 'kg', 'Suche', 5, 50),
  ('Sos pomidorowy', 'kg', 'Sosy', 10, 100)
ON CONFLICT DO NOTHING;"

# powiąż recepturę z produktami (jeśli nie istnieje)
psql -v ON_ERROR_STOP=1 <<'SQL'
DO $$
DECLARE rid uuid;
DECLARE pid1 uuid;
DECLARE pid2 uuid;
BEGIN
  SELECT recipe_id INTO rid FROM recipes WHERE name='Spaghetti' LIMIT 1;
  SELECT product_id INTO pid1 FROM products WHERE name='Makaron' LIMIT 1;
  SELECT product_id INTO pid2 FROM products WHERE name='Sos pomidorowy' LIMIT 1;
  IF rid IS NOT NULL AND pid1 IS NOT NULL THEN
    BEGIN
      INSERT INTO recipe_items(recipe_id, product_id, qty_per_portion, loss_pct, diet_variant) 
      VALUES (rid, pid1, 0.08, 0, 'standard');
    EXCEPTION WHEN unique_violation THEN NULL; END;
  END IF;
  IF rid IS NOT NULL AND pid2 IS NOT NULL THEN
    BEGIN
      INSERT INTO recipe_items(recipe_id, product_id, qty_per_portion, loss_pct, diet_variant) 
      VALUES (rid, pid2, 0.15, 0, 'standard');
    EXCEPTION WHEN unique_violation THEN NULL; END;
  END IF;
END$$;
SQL

# Partie makaronu (FEFO)
psql -c "INSERT INTO batches(product_id, qty_received, qty_available, expiry_date, cost_per_unit)
  SELECT product_id, 10, 10, CURRENT_DATE + 7, 5.50 FROM products WHERE name='Makaron' LIMIT 1;"
psql -c "INSERT INTO batches(product_id, qty_received, qty_available, expiry_date, cost_per_unit)
  SELECT product_id, 8, 8, CURRENT_DATE + 3, 5.40 FROM products WHERE name='Makaron' LIMIT 1;"

echo "[seed] OK"
