
-- 02-reservations.sql: adds inventory_reservations table for soft allocations
CREATE TABLE IF NOT EXISTS inventory_reservations (
  reservation_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  product_id UUID NOT NULL REFERENCES products(product_id) ON DELETE RESTRICT,
  plan_id UUID NOT NULL REFERENCES production_plans(plan_id) ON DELETE CASCADE,
  qty_reserved NUMERIC(14,3) NOT NULL CHECK (qty_reserved > 0),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  created_by TEXT
);
CREATE INDEX IF NOT EXISTS idx_reservations_plan ON inventory_reservations(plan_id);
