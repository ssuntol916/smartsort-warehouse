-- ============================================================
-- 전체 테이블 Row Level Security 활성화
-- ============================================================

ALTER TABLE parts        ENABLE ROW LEVEL SECURITY;
ALTER TABLE grid_cells   ENABLE ROW LEVEL SECURITY;
ALTER TABLE bins         ENABLE ROW LEVEL SECURITY;
ALTER TABLE inventories  ENABLE ROW LEVEL SECURITY;
ALTER TABLE transactions ENABLE ROW LEVEL SECURITY;
