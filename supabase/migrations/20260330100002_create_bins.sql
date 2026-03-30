-- ============================================================
-- Bin 마스터 테이블
-- ============================================================

CREATE TABLE IF NOT EXISTS bins (
  id         uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  cell_id    uuid        NOT NULL
             REFERENCES grid_cells (id) ON DELETE RESTRICT ON UPDATE CASCADE,
  label      varchar(50) UNIQUE,
  is_full    boolean     NOT NULL DEFAULT false,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
);

-- 인덱스
CREATE INDEX idx_bins_cell_id ON bins (cell_id);
CREATE INDEX idx_bins_is_full ON bins (is_full);

-- 코멘트
COMMENT ON TABLE  bins         IS 'Bin 마스터 테이블';
COMMENT ON COLUMN bins.cell_id IS '현재 저장된 그리드 셀 ID (FK → grid_cells)';
COMMENT ON COLUMN bins.label   IS 'Bin 레이블 (예: BIN-001)';
COMMENT ON COLUMN bins.is_full IS 'Bin 용량 만족 여부';
