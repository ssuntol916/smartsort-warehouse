-- ============================================================
-- 부품 종류 마스터 테이블
-- ============================================================

CREATE TABLE IF NOT EXISTS parts (
  id         uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
  name       varchar(255) NOT NULL UNIQUE,
  category   varchar(100) NOT NULL,
  image_url  text,
  created_at timestamptz  NOT NULL DEFAULT now(),
  updated_at timestamptz  NOT NULL DEFAULT now()
);

-- 인덱스
CREATE INDEX idx_parts_name     ON parts (name);
CREATE INDEX idx_parts_category ON parts (category);

-- 코멘트
COMMENT ON TABLE  parts           IS '부품 종류 마스터 테이블';
COMMENT ON COLUMN parts.id        IS '부품 고유 ID (uuid)';
COMMENT ON COLUMN parts.name      IS '부품명 (예: M3 볼트)';
COMMENT ON COLUMN parts.category  IS '분류 (예: bolt, nut, capacitor)';
COMMENT ON COLUMN parts.image_url IS '참고 이미지 URL (Storage 경로)';
