-- ============================================================
-- 입출고 이력 테이블
-- ============================================================

CREATE TABLE IF NOT EXISTS transactions (
  id         uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  bin_id     uuid        NOT NULL
             REFERENCES bins (id) ON DELETE CASCADE ON UPDATE CASCADE,
  part_id    uuid        NOT NULL
             REFERENCES parts (id) ON DELETE CASCADE ON UPDATE CASCADE,
  delta      bigint      NOT NULL,
  type       varchar(20) NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(),

  CONSTRAINT chk_transactions_type CHECK (type IN ('inbound', 'outbound', 'adjustment'))
);

-- 인덱스
CREATE INDEX idx_transactions_bin_id     ON transactions (bin_id);
CREATE INDEX idx_transactions_part_id    ON transactions (part_id);
CREATE INDEX idx_transactions_type       ON transactions (type);
CREATE INDEX idx_transactions_created_at ON transactions (created_at);

-- 코멘트
COMMENT ON TABLE  transactions       IS '입출고 이력 테이블 (추가 전용)';
COMMENT ON COLUMN transactions.delta IS '변화량 (양수: 입고, 음수: 출고)';
COMMENT ON COLUMN transactions.type  IS '타입: inbound | outbound | adjustment';
