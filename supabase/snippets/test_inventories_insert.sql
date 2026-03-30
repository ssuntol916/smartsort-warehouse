-- INSERT 테스트
INSERT INTO inventories (bin_id, part_id, qty_count)
SELECT b.id, p.id, 10
FROM bins b, parts p
WHERE b.label = 'BIN-001' AND p.name = 'M3 Bolt'
ON CONFLICT (bin_id, part_id)
DO UPDATE SET qty_count = inventories.qty_count + 10;
