# 버전 이력

- v1.0 (최초 작성, 2026.03.30.)

# 1. 개요

본 문서는 SmartSort Warehouse 프로젝트의 Supabase 데이터베이스 스키마를 정의한다. 5개의 핵심 테이블로 구성되며, 부품 마스터 관리부터 그리드 셀 위치, Bin 보관, 실시간 재고 현황, 입출고 이력까지 전체 창고 운영 데이터를 관리한다.

## 1.1 Supabase 프로젝트 정보

| 항목 | 값 |
| --- | --- |
| 프로젝트명 | SmartSort-Warehouse |
| 리전 | ap-northeast-2 (서울) |
| PostgreSQL 버전 | 17.6 |
| Project ID | riyooeethhzxcewghrtm |

## 1.2 마이그레이션 파일 위치

모든 DDL은 `supabase/migrations/` 폴더에 타임스탬프 기반 SQL 파일로 관리된다.

```
supabase/migrations/
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

# 5. Row Level Security (RLS) 정책

모든 테이블에 RLS가 활성화되어 있으며, 역할별로 접근 권한이 분리된다.

## 5.1 역할 정의

| 역할 | 사용처 | 설명 |
| --- | --- | --- |
| anon | Unity 클라이언트 (anon key) | 읽기 전용 — 재고 조회 및 Realtime 구독 |
| authenticated | 인증된 사용자 | 읽기 전용 — 향후 웹 대시보드 등 확장용 |
| service_role | MQTT 브리지 (RPi), Edge Functions | 전체 CRUD — 입고·출고·데이터 관리 |

## 5.2 테이블별 정책 요약

| 테이블 | anon | authenticated | service_role |
| --- | --- | --- | --- |
| parts | SELECT | SELECT | ALL |
| grid_cells | SELECT | SELECT | ALL |
| bins | SELECT | SELECT | ALL |
| inventories | SELECT | SELECT | ALL |
| transactions | SELECT | SELECT | SELECT, INSERT, DELETE |

transactions 테이블은 이력 보존 원칙에 따라 service_role도 UPDATE가 불가하다. 잘못된 기록은 `adjustment` 타입의 새 트랜잭션으로 보정한다.

## 5.3 정책 목록 (총 17개)

| 정책명 | 테이블 | 역할 | 명령 |
| --- | --- | --- | --- |
| parts: anon select | parts | anon | SELECT |
| parts: authenticated select | parts | authenticated | SELECT |
| parts: service_role all | parts | service_role | ALL |
| grid_cells: anon select | grid_cells | anon | SELECT |
| grid_cells: authenticated select | grid_cells | authenticated | SELECT |
| grid_cells: service_role all | grid_cells | service_role | ALL |
| bins: anon select | bins | anon | SELECT |
| bins: authenticated select | bins | authenticated | SELECT |
| bins: service_role all | bins | service_role | ALL |
| inventories: anon select | inventories | anon | SELECT |
| inventories: authenticated select | inventories | authenticated | SELECT |
| inventories: service_role all | inventories | service_role | ALL |
| transactions: anon select | transactions | anon | SELECT |
| transactions: authenticated select | transactions | authenticated | SELECT |
| transactions: service_role select | transactions | service_role | SELECT |
| transactions: service_role insert | transactions | service_role | INSERT |
| transactions: service_role delete | transactions | service_role | DELETE |

---

# 6. 환경변수 및 접속 정보

## 6.1 API 키 종류

| 키 | 용도 | RLS 적용 |
| --- | --- | --- |
| anon key | Unity 클라이언트, 공개 조회 | 적용됨 (SELECT만 가능) |
| service_role key | MQTT 브리지, Edge Functions | 우회 (전체 CRUD) |

키는 Supabase Dashboard → Settings → API 에서 확인할 수 있다.

## 6.2 .env 파일 구성

`.env` 파일은 각 컴포넌트 폴더에 위치하며, `.gitignore`에 의해 버전 관리에서 제외된다.

```
# Supabase 접속 정보
SUPABASE_URL=https://<project-id>.supabase.co
SUPABASE_ANON_KEY=<anon-key>
SUPABASE_SERVICE_ROLE_KEY=<service-role-key>
```

보안 주의사항: `service_role` 키는 RLS를 우회하므로, 서버 측 환경(RPi, Edge Functions)에서만 사용하고 클라이언트 코드(Unity, 웹 프론트엔드)에는 절대 포함하지 않는다.

---

# 7. 향후 확장 고려사항

| 항목 | 내용 | 관련 이슈 |
| --- | --- | --- |
| Realtime 활성화 | inventories 테이블을 `supabase_realtime` publication에 추가하여 Unity 실시간 구독 지원 | #41 |
| Unity Realtime 연동 | WarehouseManager.cs에서 Supabase Realtime 채널 구독 구현 | #42 |
| Edge Functions | 셀 할당 로직 (동일 부품 셀 우선 → 빈 셀 배정) TypeScript 구현 | #40 |
| 그리드 확장 | grid_cells의 CHECK 제약 (0~2)을 설정값으로 변경하여 4×4×3, 5×5×3 등 확장 지원 | — |
| transactions 파티셔닝 | 이력 데이터 대량 축적 시 created_at 기준 월별 파티셔닝 적용 | — |
