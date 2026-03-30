-- UPDATE 테스트
UPDATE inventories
SET qty_count = qty_count + 5
WHERE bin_id = (SELECT id FROM bins WHERE label = 'BIN-001');
