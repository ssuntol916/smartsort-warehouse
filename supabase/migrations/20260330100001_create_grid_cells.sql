-- ============================================================
-- 그리드 셀 위치 마스터 테이블 (3x3x3)
-- ============================================================

CREATE TABLE IF NOT EXISTS grid_cells (
  id         uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  pos_x      smallint    NOT NULL,
  pos_y      smallint    NOT NULL,
  pos_z      smallint    NOT NULL,
  label      varchar(50) UNIQUE,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),

  CONSTRAINT uq_grid_cells_position UNIQUE (pos_x, pos_y, pos_z),
  CONSTRAINT chk_grid_cells_pos_x   CHECK  (pos_x >= 0 AND pos_x < 3),
  CONSTRAINT chk_grid_cells_pos_y   CHECK  (pos_y >= 0 AND pos_y < 3),
  CONSTRAINT chk_grid_cells_pos_z   CHECK  (pos_z >= 0 AND pos_z < 3)
);

-- 인덱스
CREATE INDEX idx_grid_cells_position ON grid_cells (pos_x, pos_y, pos_z);
CREATE INDEX idx_grid_cells_label    ON grid_cells (label);

-- 코멘트
COMMENT ON TABLE  grid_cells       IS '3x3x3 그리드 셀 위치 마스터 테이블';
COMMENT ON COLUMN grid_cells.pos_x IS 'X 좌표 (0-2, 열 방향)';
COMMENT ON COLUMN grid_cells.pos_y IS 'Y 좌표 (0-2, 행 방향)';
COMMENT ON COLUMN grid_cells.pos_z IS 'Z 좌표 (0-2, 층 방향)';
COMMENT ON COLUMN grid_cells.label IS '셀 레이블 (예: A1-L0)';
