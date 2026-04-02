-- DELETE 테스트
DELETE FROM inventories
WHERE bin_id = (SELECT id FROM bins WHERE label = 'BIN-001');
