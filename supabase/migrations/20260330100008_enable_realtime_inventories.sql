-- inventories 테이블을 Realtime publication에 추가
-- Unity 클라이언트가 postgres_changes 이벤트를 실시간 수신할 수 있도록 설정

ALTER PUBLICATION supabase_realtime ADD TABLE inventories;
