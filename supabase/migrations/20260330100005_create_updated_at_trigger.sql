-- ============================================================
-- updated_at 자동 갱신 트리거 함수 및 트리거
-- ============================================================

-- 공용 트리거 함수
CREATE OR REPLACE FUNCTION update_updated_at()
RETURNS TRIGGER AS $$
BEGIN
  NEW.updated_at = now();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- parts
CREATE TRIGGER trg_parts_updated_at
  BEFORE UPDATE ON parts
  FOR EACH ROW
  EXECUTE FUNCTION update_updated_at();

-- grid_cells
CREATE TRIGGER trg_grid_cells_updated_at
  BEFORE UPDATE ON grid_cells
  FOR EACH ROW
  EXECUTE FUNCTION update_updated_at();

-- bins
CREATE TRIGGER trg_bins_updated_at
  BEFORE UPDATE ON bins
  FOR EACH ROW
  EXECUTE FUNCTION update_updated_at();

-- inventories
CREATE TRIGGER trg_inventories_updated_at
  BEFORE UPDATE ON inventories
  FOR EACH ROW
  EXECUTE FUNCTION update_updated_at();
