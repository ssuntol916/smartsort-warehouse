-- ============================================================
-- SmartSort Warehouse 시드 데이터
-- 마이그레이션 적용 후 실행: supabase db reset 또는 supabase db push --include-seed
-- ============================================================

-- 1. 부품 마스터
INSERT INTO parts (name, category) VALUES
  ('M3 Bolt',       'bolt'),
  ('M3 Nut',        'nut'),
  ('625ZZ Bearing', 'bearing'),
  ('Tact Switch',   'switch'),
  ('20T K Pulley',  'pulley'),
  ('Heat Sink',     'heatsink')
ON CONFLICT (name) DO NOTHING;

-- 2. 그리드 셀 27개 (3×3×3)
INSERT INTO grid_cells (pos_x, pos_y, pos_z, label) VALUES
  -- Layer 0
  (0, 0, 0, 'A0-L0'), (0, 1, 0, 'A1-L0'), (0, 2, 0, 'A2-L0'),
  (1, 0, 0, 'B0-L0'), (1, 1, 0, 'B1-L0'), (1, 2, 0, 'B2-L0'),
  (2, 0, 0, 'C0-L0'), (2, 1, 0, 'C1-L0'), (2, 2, 0, 'C2-L0'),
  -- Layer 1
  (0, 0, 1, 'A0-L1'), (0, 1, 1, 'A1-L1'), (0, 2, 1, 'A2-L1'),
  (1, 0, 1, 'B0-L1'), (1, 1, 1, 'B1-L1'), (1, 2, 1, 'B2-L1'),
  (2, 0, 1, 'C0-L1'), (2, 1, 1, 'C1-L1'), (2, 2, 1, 'C2-L1'),
  -- Layer 2
  (0, 0, 2, 'A0-L2'), (0, 1, 2, 'A1-L2'), (0, 2, 2, 'A2-L2'),
  (1, 0, 2, 'B0-L2'), (1, 1, 2, 'B1-L2'), (1, 2, 2, 'B2-L2'),
  (2, 0, 2, 'C0-L2'), (2, 1, 2, 'C1-L2'), (2, 2, 2, 'C2-L2')
ON CONFLICT (pos_x, pos_y, pos_z) DO NOTHING;

-- 3. Bin 생성 (각 셀에 1개씩, 총 27개)
INSERT INTO bins (cell_id, label)
SELECT id, 'BIN-' || LPAD(ROW_NUMBER() OVER (ORDER BY pos_z, pos_y, pos_x)::text, 3, '0')
FROM grid_cells
ON CONFLICT (label) DO NOTHING;
