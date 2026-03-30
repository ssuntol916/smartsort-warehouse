-- ============================================================
-- RLS 정책 생성
--
-- 역할 구분
--   anon / authenticated : SELECT 만 허용 (Unity 클라이언트 등)
--   service_role         : 전체 CRUD 허용 (MQTT 브리지, Edge Functions)
-- ============================================================

-- ────────────────────────────────────────────
-- parts
-- ────────────────────────────────────────────
CREATE POLICY "parts: anon select"
  ON parts FOR SELECT
  TO anon
  USING (true);

CREATE POLICY "parts: authenticated select"
  ON parts FOR SELECT
  TO authenticated
  USING (true);

CREATE POLICY "parts: service_role all"
  ON parts FOR ALL
  TO service_role
  USING (true)
  WITH CHECK (true);

-- ────────────────────────────────────────────
-- grid_cells
-- ────────────────────────────────────────────
CREATE POLICY "grid_cells: anon select"
  ON grid_cells FOR SELECT
  TO anon
  USING (true);

CREATE POLICY "grid_cells: authenticated select"
  ON grid_cells FOR SELECT
  TO authenticated
  USING (true);

CREATE POLICY "grid_cells: service_role all"
  ON grid_cells FOR ALL
  TO service_role
  USING (true)
  WITH CHECK (true);

-- ────────────────────────────────────────────
-- bins
-- ────────────────────────────────────────────
CREATE POLICY "bins: anon select"
  ON bins FOR SELECT
  TO anon
  USING (true);

CREATE POLICY "bins: authenticated select"
  ON bins FOR SELECT
  TO authenticated
  USING (true);

CREATE POLICY "bins: service_role all"
  ON bins FOR ALL
  TO service_role
  USING (true)
  WITH CHECK (true);

-- ────────────────────────────────────────────
-- inventories
-- ────────────────────────────────────────────
CREATE POLICY "inventories: anon select"
  ON inventories FOR SELECT
  TO anon
  USING (true);

CREATE POLICY "inventories: authenticated select"
  ON inventories FOR SELECT
  TO authenticated
  USING (true);

CREATE POLICY "inventories: service_role all"
  ON inventories FOR ALL
  TO service_role
  USING (true)
  WITH CHECK (true);

-- ────────────────────────────────────────────
-- transactions (이력 — INSERT/SELECT/DELETE 만 허용, UPDATE 불가)
-- ────────────────────────────────────────────
CREATE POLICY "transactions: anon select"
  ON transactions FOR SELECT
  TO anon
  USING (true);

CREATE POLICY "transactions: authenticated select"
  ON transactions FOR SELECT
  TO authenticated
  USING (true);

CREATE POLICY "transactions: service_role select"
  ON transactions FOR SELECT
  TO service_role
  USING (true);

CREATE POLICY "transactions: service_role insert"
  ON transactions FOR INSERT
  TO service_role
  WITH CHECK (true);

CREATE POLICY "transactions: service_role delete"
  ON transactions FOR DELETE
  TO service_role
  USING (true);
