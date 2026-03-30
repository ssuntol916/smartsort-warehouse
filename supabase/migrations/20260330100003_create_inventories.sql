-- ============================================================
-- 현재 재고 현황 테이블
-- ============================================================

CREATE TABLE IF NOT EXISTS inventories (
  id         uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  bin_id     uuid        NOT NULL
             REFERENCES bins (id) ON DELETE CASCADE ON UPDATE CASCADE,
  part_id    uuid        NOT NULL
             REFERENCES parts (id) ON DELETE CASCADE ON UPDATE CASCADE,
  qty_count  bigint      NOT NULL DEFAULT 0,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),

  CONSTRAINT uq_inventories_bin_part UNIQUE (bin_id, part_id),
  CONSTRAINT chk_inventories_qty     CHECK  (qty_count >= 0)
);

-- 인덱스
CREATE INDEX idx_inventories_bin_id  ON inventories (bin_id);
CREATE INDEX idx_inventories_part_id ON inventories (part_id);

-- 코멘트
COMMENT ON TABLE  inventories           IS '현재 재고 현황 테이블';
COMMENT ON COLUMN inventories.bin_id    IS 'Bin ID (FK → bins)';
COMMENT ON COLUMN inventories.part_id   IS '부품 ID (FK → parts)';
COMMENT ON COLUMN inventories.qty_count IS '현재 수량 (0 이상)';
