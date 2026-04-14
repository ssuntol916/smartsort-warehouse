# 버전 이력

- v1.0 (최초 작성, 2026.03.30.)
- v1.1 (백엔드를 Supabase 클라우드에서 Raspberry Pi 5 자체 호스팅 PostgreSQL 17로 전환. 권한 모델을 `anon`/`service_role` Supabase 롤에서 PostgreSQL 네이티브 롤로 교체, 접속 정보 `.env`를 `DATABASE_URL` 기반으로 변경, 2026.04.14.)

# 1. 개요

본 문서는 SmartSort Warehouse 프로젝트의 PostgreSQL 데이터베이스 스키마를 정의한다. 5개의 핵심 테이블로 구성되며, 부품 마스터 관리부터 그리드 셀 위치, Bin 보관, 실시간 재고 현황, 입출고 이력까지 전체 창고 운영 데이터를 관리한다. DB는 Raspberry Pi 5 (8GB) 한 대에서 자체 호스팅한다.

## 1.1 DB 서버 정보

| 항목 | 값 |
| --- | --- |
| 호스트 | `smartsort-server.local` (mDNS) / 고정 IP는 공유기 DHCP 예약 |
| 포트 | 5432 (LAN 내부만 허용, `ufw` 로 `192.168.0.0/24` 대역 개방) |
| PostgreSQL 버전 | 17 (Pi OS Lite 64-bit Trixie 기본 저장소) |
| 데이터베이스 | `smartsort_db` |
| 접속 사용자 | `smartsort` (애플리케이션 전용) / `postgres` (슈퍼유저, 관리 전용) |
| 인증 방식 | `scram-sha-256` (`pg_hba.conf` 에 LAN 대역 허용 줄 추가) |

## 1.2 마이그레이션 파일 위치

DDL 원본은 Supabase CLI 시절 타임스탬프 기반 SQL 파일로 작성되어 `supabase/migrations/` 아카이브에 그대로 남아있다. Pi 5 자체 호스팅 전환 후에는 해당 SQL을 수동으로 적용하거나, `psql -f` 로 일괄 실행한다. 향후에는 Liquibase / sqitch / Alembic 등 독립 마이그레이션 도구 도입을 검토한다.

```
supabase/migrations/            ← 아카이브 (참조용, 새 마이그레이션은 이곳에 추가하지 않는다)
├── 20260330100000_create_parts.sql
├── 20260330100001_create_grid_cells.sql
├── 20260330100002_create_bins.sql
├── 20260330100003_create_inventories.sql
├── 20260330100004_create_transactions.sql
├── 20260330100005_create_updated_at_trigger.sql
├── 20260330100006_enable_rls.sql
└── 20260330100007_create_rls_policies.sql
```

---

# 2. 테이블 구조

## 2.1 parts — 부품 종류 마스터

AI 비전 분류 결과와 매핑되는 부품 종류를 정의한다.

| 컬럼 | 타입 | 제약 | 기본값 | 설명 |
| --- | --- | --- | --- | --- |
| id | uuid | PK | gen_random_uuid() | 부품 고유 ID |
| name | varchar(255) | NOT NULL, UNIQUE | — | 부품명 (예: M3 볼트) |
| category | varchar(100) | NOT NULL | — | 분류 (예: bolt, nut, capacitor) |
| image_url | text | NULLABLE | — | 참고 이미지 URL (Storage 경로) |
| created_at | timestamptz | NOT NULL | now() | 생성 시각 |
| updated_at | timestamptz | NOT NULL | now() | 수정 시각 (트리거 자동 갱신) |

인덱스: `idx_parts_name (name)`, `idx_parts_category (category)`

---

## 2.2 grid_cells — 그리드 셀 위치 마스터

3×3×3 그리드 랙의 각 셀 좌표를 정의한다. 총 27개 셀이 존재할 수 있다.

| 컬럼 | 타입 | 제약 | 기본값 | 설명 |
| --- | --- | --- | --- | --- |
| id | uuid | PK | gen_random_uuid() | 셀 고유 ID |
| pos_x | smallint | NOT NULL, CHECK (0 ≤ x < 3) | — | X 좌표 (열 방향) |
| pos_y | smallint | NOT NULL, CHECK (0 ≤ y < 3) | — | Y 좌표 (행 방향) |
| pos_z | smallint | NOT NULL, CHECK (0 ≤ z < 3) | — | Z 좌표 (층 방향) |
| label | varchar(50) | UNIQUE, NULLABLE | — | 셀 레이블 (예: A1-L0) |
| created_at | timestamptz | NOT NULL | now() | 생성 시각 |
| updated_at | timestamptz | NOT NULL | now() | 수정 시각 (트리거 자동 갱신) |

제약: `UNIQUE (pos_x, pos_y, pos_z)` — 동일 좌표에 중복 셀 방지

인덱스: `idx_grid_cells_position (pos_x, pos_y, pos_z)`, `idx_grid_cells_label (label)`

---

## 2.3 bins — Bin 마스터

그리드 셀에 배치되는 물리적 Bin을 정의한다. 각 Bin은 하나의 grid_cell에 소속된다.

| 컬럼 | 타입 | 제약 | 기본값 | 설명 |
| --- | --- | --- | --- | --- |
| id | uuid | PK | gen_random_uuid() | Bin 고유 ID |
| cell_id | uuid | NOT NULL, FK → grid_cells.id | — | 현재 저장된 그리드 셀 |
| label | varchar(50) | UNIQUE, NULLABLE | — | Bin 레이블 (예: BIN-001) |
| is_full | boolean | NOT NULL | false | 용량 만족 여부 |
| created_at | timestamptz | NOT NULL | now() | 생성 시각 |
| updated_at | timestamptz | NOT NULL | now() | 수정 시각 (트리거 자동 갱신) |

FK 정책: `ON DELETE RESTRICT ON UPDATE CASCADE` — 참조 중인 grid_cell 삭제 차단

인덱스: `idx_bins_cell_id (cell_id)`, `idx_bins_is_full (is_full)`

---

## 2.4 inventories — 현재 재고 현황

Bin 내부에 보관 중인 부품의 현재 수량을 관리한다. 한 Bin에 같은 부품은 하나의 레코드로 관리된다.

| 컬럼 | 타입 | 제약 | 기본값 | 설명 |
| --- | --- | --- | --- | --- |
| id | uuid | PK | gen_random_uuid() | 재고 레코드 ID |
| bin_id | uuid | NOT NULL, FK → bins.id | — | Bin ID |
| part_id | uuid | NOT NULL, FK → parts.id | — | 부품 ID |
| qty_count | bigint | NOT NULL, CHECK (≥ 0) | 0 | 현재 수량 |
| created_at | timestamptz | NOT NULL | now() | 생성 시각 |
| updated_at | timestamptz | NOT NULL | now() | 수정 시각 (트리거 자동 갱신) |

제약: `UNIQUE (bin_id, part_id)` — Bin당 부품 종류별 1개 레코드

FK 정책: `ON DELETE CASCADE ON UPDATE CASCADE` — 부모(bins, parts) 삭제 시 함께 삭제

인덱스: `idx_inventories_bin_id (bin_id)`, `idx_inventories_part_id (part_id)`

---

## 2.5 transactions — 입출고 이력

모든 입고·출고·조정 이벤트를 시간순으로 기록한다. 추가 전용(append-only) 설계로, UPDATE는 RLS 정책에서 차단된다.

| 컬럼 | 타입 | 제약 | 기본값 | 설명 |
| --- | --- | --- | --- | --- |
| id | uuid | PK | gen_random_uuid() | 트랜잭션 ID |
| bin_id | uuid | NOT NULL, FK → bins.id | — | Bin ID |
| part_id | uuid | NOT NULL, FK → parts.id | — | 부품 ID |
| delta | bigint | NOT NULL | — | 변화량 (양수: 입고, 음수: 출고) |
| type | varchar(20) | NOT NULL, CHECK | — | 타입 |
| created_at | timestamptz | NOT NULL | now() | 발생 시각 |

type 허용값: `inbound` (입고), `outbound` (출고), `adjustment` (수량 보정)

FK 정책: `ON DELETE CASCADE ON UPDATE CASCADE`

인덱스: `idx_transactions_bin_id (bin_id)`, `idx_transactions_part_id (part_id)`, `idx_transactions_type (type)`, `idx_transactions_created_at (created_at)`

---

# 3. 테이블 간 관계

5개 테이블의 참조 관계는 다음과 같다.

```
parts (부품 마스터)
  ├──< inventories.part_id    (1:N)
  └──< transactions.part_id   (1:N)

grid_cells (그리드 셀 마스터)
  └──< bins.cell_id            (1:N)

bins (Bin 마스터)
  ├──< inventories.bin_id      (1:N)
  └──< transactions.bin_id     (1:N)
```

요약하면, parts와 grid_cells가 최상위 마스터이고, bins가 grid_cells에 종속된다. inventories와 transactions는 bins와 parts를 동시에 참조하는 구조이다.

---

# 4. 트리거 및 함수

## 4.1 update_updated_at()

`updated_at` 컬럼을 자동으로 현재 시각으로 갱신하는 공용 트리거 함수이다.

적용 대상 (BEFORE UPDATE 트리거):

| 테이블 | 트리거명 |
| --- | --- |
| parts | trg_parts_updated_at |
| grid_cells | trg_grid_cells_updated_at |
| bins | trg_bins_updated_at |
| inventories | trg_inventories_updated_at |

transactions 테이블은 추가 전용이므로 `updated_at` 컬럼과 트리거가 없다.

---

# 5. 권한 모델 (PostgreSQL 롤 기반)

Supabase 자체 호스팅 전환 후에는 기존 `anon` / `authenticated` / `service_role` 롤과 RLS 정책을 제거하고, **PostgreSQL 네이티브 롤로 권한을 분리**한다. Unity 클라이언트는 실시간 이벤트는 MQTT로 받되, 대시보드(재고 현황·입출고 이력 등)는 PostgreSQL에 Npgsql로 직접 접속해 조회한다. 따라서 Unity 전용 **읽기전용 롤**을 별도로 두어 자격증명 노출 시의 영향 범위를 제한한다.

## 5.1 롤 정의

| 롤 | 사용처 | 권한 |
| --- | --- | --- |
| `smartsort_app` | mqtt-bridge, 셀 할당 서비스, Text-to-SQL 실행기 | `smartsort_db` 에 대한 `CONNECT`, `public` 스키마 `USAGE`, 5개 테이블 CRUD |
| `smartsort_client` | Unity 디지털 트윈 (Npgsql 직접 접속, 대시보드 조회) | 5개 테이블 `SELECT` 만 |
| `smartsort_ro` | 관리자 조회·BI 도구 (DBeaver, pgAdmin) | 5개 테이블 `SELECT` 만 |
| `postgres` | 슈퍼유저 | 마이그레이션·백업·롤 관리 전용 |

`smartsort_client` 와 `smartsort_ro` 는 권한은 같지만 **사용처와 자격증명이 분리**되어 있다. Unity 빌드가 배포된 PC에서 자격증명이 노출되더라도 관리자 접속을 즉시 차단할 필요 없이 `smartsort_client` 비밀번호만 회수하면 된다.

## 5.2 테이블별 권한 요약

| 테이블 | smartsort_app | smartsort_client | smartsort_ro | postgres |
| --- | --- | --- | --- | --- |
| parts | SELECT, INSERT, UPDATE, DELETE | SELECT | SELECT | ALL |
| grid_cells | SELECT, INSERT, UPDATE, DELETE | SELECT | SELECT | ALL |
| bins | SELECT, INSERT, UPDATE, DELETE | SELECT | SELECT | ALL |
| inventories | SELECT, INSERT, UPDATE, DELETE | SELECT | SELECT | ALL |
| transactions | SELECT, INSERT, DELETE | SELECT | SELECT | ALL |

transactions 테이블은 이력 보존 원칙에 따라 `smartsort_app` 도 UPDATE가 불가하다. 잘못된 기록은 `adjustment` 타입의 새 트랜잭션으로 보정한다. 권한은 `GRANT` / `REVOKE` 문으로 직접 부여한다.

```sql
CREATE ROLE smartsort_app    LOGIN PASSWORD :'app_password';
CREATE ROLE smartsort_client LOGIN PASSWORD :'client_password';
CREATE ROLE smartsort_ro     LOGIN PASSWORD :'ro_password';

GRANT CONNECT ON DATABASE smartsort_db
  TO smartsort_app, smartsort_client, smartsort_ro;
GRANT USAGE ON SCHEMA public
  TO smartsort_app, smartsort_client, smartsort_ro;

-- 애플리케이션 (쓰기 포함)
GRANT SELECT, INSERT, UPDATE, DELETE
  ON parts, grid_cells, bins, inventories TO smartsort_app;
GRANT SELECT, INSERT, DELETE ON transactions TO smartsort_app;

-- Unity 디지털 트윈 대시보드 (읽기 전용)
GRANT SELECT ON parts, grid_cells, bins, inventories, transactions
  TO smartsort_client;

-- 관리자 조회 (읽기 전용)
GRANT SELECT ON parts, grid_cells, bins, inventories, transactions
  TO smartsort_ro;
```

RLS 정책은 전면 제거했으며, 필요 시 개별 테이블에 재활성화할 수 있다.

---

# 6. 환경변수 및 접속 정보

## 6.1 인증 수단

| 항목 | 용도 | 비고 |
| --- | --- | --- |
| `smartsort_app` 비밀번호 | mqtt-bridge 및 서버 측 서비스 접속 | `.env` 에서 관리, Git 제외 |
| `smartsort_client` 비밀번호 | Unity 디지털 트윈 대시보드 조회 (Npgsql) | Unity 빌드에 내장되므로 읽기 전용 롤만 부여. 노출 시 비밀번호만 회수·재발급 |
| `smartsort_ro` 비밀번호 | 관리자 로컬 조회 도구 | 필요 시 발급, 공용 금지 |
| MQTT 사용자 (Mosquitto) | ESP32·RPi·Unity 브로커 접속 | DB 자격증명과 별도 관리 (`mosquitto_passwd`) |

PostgreSQL 슈퍼유저 `postgres` 비밀번호는 Pi 5 본체에서만 관리하며, `.env` 에 포함하지 않는다.

## 6.2 .env 파일 구성

`.env` 파일은 각 컴포넌트 폴더(`rpi/mqtt-bridge/` 등)에 위치하며, 루트 `.gitignore` 에 의해 버전 관리에서 제외된다. 예시 템플릿은 `.env.example` 에 둔다.

**서버 측 (rpi/mqtt-bridge 등, `smartsort_app` 사용)**

```
# Pi 5 PostgreSQL (쓰기 포함)
DATABASE_URL=postgresql://smartsort_app:<app_password>@smartsort-server.local:5432/smartsort_db

# Mosquitto MQTT 브로커
MQTT_HOST=smartsort-server.local
MQTT_PORT=1883
MQTT_USERNAME=<mqtt-user>
MQTT_PASSWORD=<mqtt-password>

# llama-server (Text-to-SQL, 선택)
LLAMA_URL=http://smartsort-server.local:8080
```

**Unity 클라이언트 (대시보드 조회, `smartsort_client` 사용)**

Unity는 `StreamingAssets/dashboard.env` 또는 빌드 설정 스크립트 객체에 아래 값을 주입한다. Git에는 `.env.example` 만 커밋한다.

```
# Pi 5 PostgreSQL (읽기 전용, 대시보드 조회)
DASHBOARD_DB_URL=Host=smartsort-server.local;Port=5432;Database=smartsort_db;Username=smartsort_client;Password=<client_password>

# Mosquitto MQTT (셔틀·재고 실시간 이벤트)
MQTT_HOST=smartsort-server.local
MQTT_PORT=1883
MQTT_USERNAME=<mqtt-user>
MQTT_PASSWORD=<mqtt-password>
```

보안 주의사항:

- 쓰기 권한이 있는 `smartsort_app` 자격증명은 서버 측 서비스(mqtt-bridge, 셀 할당 서비스)에서만 사용하며, Unity·프론트엔드 등 클라이언트 코드에는 절대 포함하지 않는다.
- `smartsort_client` 는 Unity 빌드에 자격증명이 내장되는 구조이므로, 권한을 `SELECT` 로만 엄격히 제한하고 LAN 대역(`192.168.0.0/24`) 외부에서는 `pg_hba.conf` 로 접근을 차단한다.
- 자격증명 노출이 의심되면 `smartsort_client` 비밀번호만 재설정하고 Unity 빌드를 재배포한다. `smartsort_app` 과 관리자 계정은 영향을 받지 않는다.

---

# 7. 향후 확장 고려사항

| 항목 | 내용 | 관련 이슈 |
| --- | --- | --- |
| 실시간 전송 구현 | mqtt-bridge가 inventories 변경 시 `warehouse/inventory/<bin_id>` 토픽을 재발행하도록 구현. Unity는 해당 토픽을 구독 | #41 |
| Unity MQTT 연동 | WarehouseManager.cs에서 MQTTnet 등으로 Mosquitto 구독 구현 | #42 |
| 셀 할당 로직 서비스화 | 동일 부품 셀 우선 → 빈 셀 배정 로직을 mqtt-bridge 내부 모듈 또는 별도 Python 서비스로 구현 | #40 |
| 그리드 확장 | grid_cells의 CHECK 제약 (0~2)을 설정값으로 변경하여 4×4×3, 5×5×3 등 확장 지원 | — |
| transactions 파티셔닝 | 이력 데이터 대량 축적 시 created_at 기준 월별 파티셔닝 적용 | — |
| 자동 백업 외부화 | pg_dump 야간 백업물을 NAS / 보조 Pi 로 복제 (현재는 Pi 5 로컬 7일 보관만) | — |
| 마이그레이션 도구 전환 | Supabase CLI 의존 제거를 위해 sqitch 또는 Alembic 도입 | — |
