# 버전 이력

- v1.0 (최초 작성, 2026.03.22.)
- v1.01 (커밋 메시지 `WIP` 타입 추가, 2026.03.24.)
- v1.02 (4-way 셔틀 방향 전환 메커니즘 변경: 크랭크 임펠러 → 래크 앤 피니언 Y축 승강, 2026.03.26.)
- v1.03 (백엔드를 Supabase 클라우드에서 Raspberry Pi 5 자체 서버 스택(PostgreSQL + Mosquitto + llama-server)으로 전환. Unity는 Supabase Realtime 대신 MQTT를 직접 구독, 2026.04.14.)

# 1. 프로젝트 개요

스마트 분류 디지털 트윈 프로젝트는 볼트·너트·전해콘덴서 등 50 mm 이하 소형 기계 부품을 카메라로 촬영하고 AI API 비전 모델로 자동 인식·분류한 뒤, 컨베이어를 통해 그리드 랙의 지정 위치에 자동 입고하는 통합 관리 시스템이다. 실시간 3차원 재고 현황은 Unity 디지털 트윈으로 시각화된다.

## 1.1 프로젝트 목표

- 비전 촬영을 통한 부품 자동 분류
- 정밀 위치 정지 컨베이어 구현
- 그리드 랙과 4-way 셔틀을 통한 다수 부품 분류·보관·출고
- 각 그리드 구역 별 부품 종류 및 재고 수량 실시간 DB 관리
- Unity 디지털 트윈을 통해, 전체 3D 시각화 및 재고 조회

## 1.2 기대 효과

| 항목 | 기대 효과 |
| --- | --- |
| 작업 효율 | 수작업 분류 시간 90% 이상 절감 |
| 재고 정확도 | 실시간 자동 집계로 오차 최소화 |
| 공간 활용 | 그리드 배치로 좁은 공간에 효율적인 적재 |
| 디지털 트윈 | 물리 창고와 가상 모델의 실시간 동기화 |
| 확장성 | 그리드 랙 추가로 보관 종류 확장 |

---

# 2. 시스템 아키텍처

시스템은 크게 세 레이어로 구성된다:

① 하드웨어 레이어 (MCU·센서·구동부)
② 서버 레이어 (Raspberry Pi 5 자체 호스팅: PostgreSQL + Mosquitto + llama-server, 외부 AI API 비전 분류)
③ 소프트웨어 레이어 (대시보드·Unity 디지털 트윈)

## 2.1 전체 데이터 흐름

| 단계 | 구성 요소 | 역할 |
| --- | --- | --- |
| ① 투입 | 투입 슈트 + Pi Camera v2 | 부품 낙하 → IR 센서 감지 → 촬영 |
| ② 인식 | RPi-Zero 2W + Gemini API | 이미지 전송 → 부품 분류 |
| ③ 전송 | MQTT (Wi-Fi) → Pi 5 Mosquitto 브로커 | 분류 결과를 `warehouse/classify` 토픽으로 발행 |
| ④ Bin 투입 | 투입 컨베이어 → 대기 Bin | 컨베이어 말단에 대기 중인 Bin으로 분류된 부품 투입 |
| ⑤ Bin 이송 | 4-way 셔틀 (X·Y·Z축) | 셔틀이 Bin을 집어 X·Y·Z 방향으로 목적 그리드 위치까지 정밀 이송 |
| ⑥ 입고 확인 | IR / 엔드스톱 센서 | Bin 도착 및 부품 입고 확인 후 완료 신호 |
| ⑦ 기록 | Pi 5 PostgreSQL (mqtt-bridge) | 그리드 위치(X·Y·Z)·Bin ID·수량·타임스탬프를 브리지가 INSERT/UPDATE, 동시에 상태 토픽을 재발행 |
| ⑧ 시각화 | Unity 디지털 트윈 | (a) `warehouse/status/*`·`warehouse/inventory/*` MQTT 구독 → 3D 셔틀·재고 실시간 렌더링 / (b) PostgreSQL(Npgsql) 직접 조회 → 대시보드 재고 현황·입출고 이력 표시 |

## 2.2 시스템 구성도

투입구(고정) 아래에 Pi Camera v2가 설치되어 있고, 부품을 촬영하면 RPi-Zero 2W가 Gemini API를 호출한다.

분류 결과는 MQTT로 Pi 5 서버(smartsort-server.local)의 Mosquitto 브로커에 전달되고, `rpi/mqtt-bridge/`의 Python 서비스가 해당 부품이 보관될 그리드 위치(X·Y·Z 좌표)를 계산하여 4-way 셔틀에 이송 명령을 전송한다.

투입 컨베이어 말단에 대기 중인 Bin으로 분류된 부품이 투입되면, 4-way 셔틀이 해당 Bin을 집어 X·Y 2축 수평 이동 후 Z축 리프트로 목적 층까지 수직 이동하여 그리드 랙의 목적 위치에 정밀 정지 후 입고한다.

## 2.3 레포지토리 폴더 구조
 
```
SmartSort-Warehouse/
├── .gitignore                  ← 레포 루트 공통 (DS_Store 등)
├── README.md                   ← 프로젝트 소개
├── docs/                       ← 문서
│   ├── project-overview.md     ← 프로젝트 전체 기획·설계 문서
│   ├── team-roles.md           ← 팀 구성 및 업무 분장
│   └── (draw.io, svg 파일 등)
├── unity/                      ← Unity 디지털 트윈
│   ├── .gitignore              ← Unity 전용 (Library/, Temp/ 등)
│   ├── Assets/
│   ├── Packages/
│   └── ProjectSettings/
├── firmware/                   ← ESP32 PlatformIO 펌웨어
│   ├── esp32-1-conveyor/       ← 투입 컨베이어 제어
│   └── esp32-2-shuttle/        ← 4-way 셔틀 제어
├── rpi/                        ← Raspberry Pi Python 스크립트
│   ├── camera/                 ← picamera2 촬영 모듈
│   ├── classifier/             ← Gemini API 분류 모듈
│   └── mqtt-bridge/            ← MQTT ↔ PostgreSQL 브리지 (Pi 5에서 상주 실행)
└── supabase/                   ← 초기 설계 시 사용한 마이그레이션 아카이브 (참조용)
    └── migrations/             ← Pi 5 PostgreSQL로 이관된 DDL 원본
```
 
---

# 3. 하드웨어 설계

## 3.1 비전 분류 스테이션

### 3.1.1 카메라 모듈

- 사용 카메라: Raspberry Pi Camera Module v2 (Sony IMX219, 8MP, 1080p30)
- 연결: CSI 인터페이스 (RPi-Zero 2W와 직결)
- 조명: LED 링 라이트 — 일정한 조도 확보, 그림자 및 반사 최소화
- 촬영 위치: 투입 컨베이어 정지 지점 상단 고정 마운트 (하향 수직 촬영)
- 해상도 설정: 1280×720 (정확도 우선)
- Python 라이브러리: picamera2 (libcamera 기반)

### 3.1.2 투입 컨베이어

- MCU: ESP32 WIFI + 블루투스 듀얼 모드 WROOM 32 USB C타입
- 벨트 타입: GT2 타이밍 벨트 2mm 피치 (슬립 없는 정밀 이송)
- 구동 모터: NEMA17 스텝 모터 + A4988 드라이버
- 정밀도: 20T 풀리 기준 1스텝 = 0.2mm
- 벨트 길이: 300 mm
- 센서: TCRT5000 IR 반사 센서 — 부품 감지 및 촬영 트리거
- 동작: 부품 감지 → 정밀 정지 → 촬영 트리거 → 분류 완료 후 재출발

## 3.2 그리드 랙 보관 모듈

### 3.2.1 그리드 랙 구조

격자형 3×3×3 그리드 랙에 Bin을 배치한다. 4-way 셔틀이 X·Y 2축으로 이동하여 지정 열·행 위치에 정렬한 뒤, Z축 리프트로 목적 층의 Bin을 정밀 이송·입고한다.

| 파라미터 | 값 | 비고 |
| --- | --- | --- |
| 그리드 크기 | 3 × 3 × 3 (열 × 행 × 층) | 총 27칸 |
| Bin 크기 | 80 × 80 × 70 mm | 50 mm 이하 부품 수용 |
| 총 보관 위치 수 | 27칸 | 그리드 확장으로 증가 |
| 셔틀 방향 전환 | 래크 앤 피니언 슬라이더 (MG996R 서보 모터) | Y축 바퀴 승강으로 접지 전환 |
| Z축 리프트 | 층별 수직 이동 | MG996R × 2 |
| 랙 소재 | PLA 3D 프린팅(레일) + 알루미늄 프로파일(프레임) | 강성 우선 |
| 위치 정밀도 | 셀 간격 단위 정밀 정지 | MG90S 서보 모터 제어 |

### 3.2.2 4-way 셔틀 구동

**방향 전환 메커니즘 (래크 앤 피니언 슬라이더)**

X축 바퀴는 셔틀 본체에 고정 장착되어 초기 상태에서는 X축 레일에 접지된 상태이다. Y축 바퀴는 수직 슬라이더에 장착되어 있으며, MG996R 서보 모터와 래크 앤 피니언 기구를 통해 위아래로 승강한다. Y축 바퀴가 하강하면 Y축 레일에 접지되어 Y 방향 이동이 가능하고, 상승하면 X축 바퀴만 접지되어 X 방향 이동 상태가 된다.

| Y축 슬라이더 상태 | 접지 바퀴 | 이동 가능 방향 |
| --- | --- | --- |
| 상승 (Y축 바퀴 올림) | X축 바퀴 접지 | X축 (열 방향) 이동 |
| 하강 (Y축 바퀴 내림) | Y축 바퀴 접지 | Y축 (행 방향) 이동 |

**구동 구성**

- 방향 전환: MG996R 서보 모터 + 래크 앤 피니언 (Y축 바퀴 승강으로 접지 전환)
- X·Y 축 주행: MG90S 서보 모터 × 4 (X축 2개 + Y축 2개, 바퀴 직결 PWM 제어)
- Z축 리프트: MG996R 서보 모터 × 2 (스풀/벨트 감아올리기 방식, Bin 양측 동시 리프팅)
- 위치 확인: 엔드스톱 스위치로 홈 포지션 (0, 0, 0) 캘리브레이션

### 3.2.3 그리드 배치 및 셔틀 이송

투입 컨베이어 말단에서 Bin으로 부품이 투입되면, 4-way 셔틀이 해당 Bin을 집어 그리드 랙의 목적 위치(X, Y, Z)까지 정밀 이송한다.

| **구분** | **구동 방식** | **역할** |
| --- | --- | --- |
| 4-way 셔틀 방향 전환 | MG996R 서보 모터 + 래크 앤 피니언 | Y축 바퀴 승강으로 이동 축 전환 |
| 4-way 셔틀 X축 | MG90S 서보 모터 × 2 (바퀴 직결) | 그리드 열(Column) 방향 정밀 이동 |
| 4-way 셔틀 Y축 | MG90S 서보 모터 × 2 (바퀴 직결) | 그리드 행(Row) 방향 정밀 이동 |
| 4-way 셔틀 Z축 | MG996R 서보 모터 × 2 (스풀/벨트 리프팅) | 그리드 층(Layer) 방향 수직 이동 |
| 투입 컨베이어 | GT2 타이밍 벨트 | 촬영 지점 정밀 정지 및 Bin 투입 |

## 3.3 컨베이어 정밀 제어 설계

### 3.3.1 GT2 타이밍 벨트 위치 계산

- 벨트 피치: 2 mm
- 풀리 잇수: 20T (피치 원 둘레 = 40 mm)
- NEMA17 스텝 각도: 1.8° (200 스텝/회전)
- 1 스텝 이동 거리: 40 mm ÷ 200 = 0.2 mm
- 촬영 정지 지점까지 이동 거리 예시 (150 mm): 750 스텝
- 마이크로 스텝(1/16) 적용 시 정밀도: 0.0125 mm

### 3.3.2 정밀 정지 시퀀스

| 단계 | 동작 | 담당 |
| --- | --- | --- |
| 1 | 부품 투입 → IR 센서 감지 → 컨베이어 저속 전환 | ESP32 #1 |
| 2 | 촬영 지점 좌표에서 스텝 카운트로 정밀 정지 | ESP32 #1 |
| 3 | Pi Camera v2 촬영 → Gemini API 분류 | RPi-Zero 2W |
| 4 | 목적 그리드 좌표(X, Y, Z) 수신 → 셔틀 이동 명령 전송 | 서버 → ESP32 #2 |
| 5 | Y축 바퀴 상승(X축 접지) → X축 목적 열까지 정밀 이동 | ESP32 #2 |
| 6 | Y축 바퀴 하강(Y축 접지) → Y축 목적 행까지 정밀 이동 | ESP32 #2 |
| 7 | Z축 리프트 → 목적 층까지 수직 이동 → Bin 입고 확인 → 완료 신호 | ESP32 #2 |
| 8 | DB 업데이트 → 상태 토픽(`warehouse/status/*`·`warehouse/inventory/*`) 발행 → Unity | mqtt-bridge (Pi 5) |

## 3.4 MCU 구성 요약

| MCU / SBC | 역할 | 통신 |
| --- | --- | --- |
| ESP32 #1 | 투입 컨베이어 NEMA17 제어 + IR 센서 + 촬영 트리거 | Wi-Fi MQTT |
| ESP32 #2 | 4-way 셔틀 전체 제어 (래크 앤 피니언 Y축 승강 MG996R + MG90S X·Y축 + MG996R Z축 리프트) | Wi-Fi MQTT |
| Raspberry Pi Zero 2W | Pi Camera v2 제어 + Gemini API 호출 + MQTT 발행 (`warehouse/classify`) | Wi-Fi MQTT |
| Raspberry Pi 5 (smartsort-server) | Mosquitto 브로커(1883) + PostgreSQL 17(5432) + llama-server(8080) + mqtt-bridge | Ethernet/Wi-Fi |

## 3.5 부품 보관 용량

| 구성 | 그리드 크기 | 총 보관 위치 |
| --- | --- | --- |
| 기본 구성 | 3 × 3 × 3 | 27칸 |
| 확장 구성 A | 4 × 4 × 3 | 48칸 |
| 확장 구성 B | 5 × 5 × 3 | 75칸 |

---

# 4. 소프트웨어 설계

## 4.1 비전 AI - Gemini API 파이프라인

Google Gemini API를 사용하여 별도의 학습 데이터 수집·모델 파인튜닝 없이 부품 목록을
프롬프트로 정의하여 즉시 분류하도록 설계한다.

### 4.1.1 Gemini API 구성

| 항목 | 내용 |
| --- | --- |
| 모델 | gemini-1.5-flash (속도·비용 최적) |
| 입력 | Pi Camera v2 JPEG 이미지 (base64 인코딩) |
| 출력 | {"part": "M3볼트", "confidence": 0.94, "reason": "근거"} |
| 평균 응답 시간 | 500ms ~ 2초 (네트워크 포함) |
| 예상 비용 | 하루 500회 기준 월 수백 원 수준 |
| API 키 관리 | Raspberry Pi 환경변수 (.env) 저장 |

### 4.1.2 신뢰도 임계값 처리

| 신뢰도 | 처리 방법 |
| --- | --- |
| 0.85 이상 | 즉시 입고 처리 |
| 0.75 ~ 0.84 | 입고 처리 + DB에 확인필요 태그 기록 |
| 0.75 미만 | 컨베이어 역방향 후 재촬영 (최대 3회) |
| 3회 연속 실패 | 미분류 보관함으로 이송 + 관리자 알림 |

## 4.2 MQTT 메시지 구조

| 토픽 | 발행자 | 페이로드 예시 |
| --- | --- | --- |
| warehouse/classify | Raspberry Pi | {"part":"M3볼트","conf":0.94,"ts":1721000000} |
| warehouse/cmd/conveyor | 서버 | {"id":"esp32_1","steps":750,"dir":"fwd"} |
| warehouse/cmd/shuttle | 서버 | {"target_x":2,"target_y":1,"target_z":3,"bin_id":"B-021"} |
| warehouse/status/shuttle | ESP32 #2 | {"x":2,"y":1,"z":3,"bin_id":"B-021","part":"M3볼트","qty":1} |
| warehouse/alert | 서버 | {"type":"low_confidence","part":"unknown","action":"manual"} |

## 4.3 백엔드 - Raspberry Pi 5 자체 서버 스택

Supabase 클라우드 의존을 제거하고, Raspberry Pi 5 (8GB) 한 대에 Mosquitto·PostgreSQL·llama-server를 함께 올린 자체 호스팅 스택으로 전환했다. 호스트명은 `smartsort-server.local` (mDNS) 이며 LAN 내부에서만 접근 가능하다.

### 4.3.1 Pi 5 서버 구성 요소

| 구성 요소 | 포트 | 역할 | 비고 |
| --- | --- | --- | --- |
| Mosquitto | 1883 | MQTT 브로커 (ESP32·RPi·Unity 공용 허브) | `warehouse/#` 전 토픽 중계 |
| PostgreSQL 17 | 5432 | 재고·이력·그리드 랙 마스터 데이터 영속화 | `shared_buffers` 2GB, LAN 대역 `scram-sha-256` |
| llama-server | 8080 | Qwen2.5-Coder 1.5B Q4_K_M 기반 Text-to-SQL | systemd `MemoryMax=2G` 로 타 프로세스와 공존 |
| mqtt-bridge (Python) | — | MQTT ↔ PostgreSQL 다리 (재고 INSERT/UPDATE, 상태 토픽 재발행) | systemd 상주, `rpi/mqtt-bridge/` |

### 4.3.2 데이터베이스 스키마

| 테이블 | 주요 컬럼 | 설명 |
| --- | --- | --- |
| parts | id, name, category, image_url | 부품 종류 마스터 |
| grid_cells | id, pos_x, pos_y, pos_z, label | 그리드 셀 위치 마스터 |
| bins | id, cell_id, label | Bin 마스터 |
| inventories | bin_id, part_id, qty, updated_at | 현재 재고 현황 |
| transactions | id, bin_id, part_id, delta, type, created_at | 입출고 이력 |

### 4.3.3 MQTT ↔ PostgreSQL 브리지 (mqtt-bridge)

Pi 5 위에서 경량 Python 서비스가 Mosquitto의 `warehouse/classify`·`warehouse/status/*` 토픽을 구독하고, 결과를 psycopg로 PostgreSQL에 기록한다. 기록과 동시에 inventories 변경 이벤트를 `warehouse/inventory/<bin_id>` 토픽으로 재발행하여 Unity가 별도 DB 쿼리 없이도 실시간 재고 변화를 즉시 수신할 수 있게 한다. (Unity의 대시보드 화면은 별도로 PostgreSQL을 직접 조회하며, 자세한 내용은 §4.4.2 참조.)

### 4.3.4 셀 할당 로직 — Python 서비스

부품 분류 완료 후 목적 그리드 셀(X, Y, Z)을 결정하는 로직은 mqtt-bridge 내부 모듈(또는 별도 Python 서비스)로 구현한다. 동일 부품 셀 우선 → 빈 셀 순서로 자동 배정하며, 결정 결과는 `warehouse/cmd/shuttle` 토픽으로 발행한다. (초기 설계는 Supabase Edge Function이었으나, 자체 호스팅 전환으로 Deno/TypeScript 의존성을 제거했다.)

### 4.3.5 자연어 DB 질의 (Text-to-SQL)

llama-server에 Qwen2.5-Coder 1.5B가 상주하므로, 관리자가 자연어로 재고를 묻고 싶을 때 프롬프트 → SQL 생성 → PostgreSQL 실행 파이프라인을 거쳐 결과를 반환할 수 있다. 상세 스펙과 예제는 llama.cpp 구축 가이드 §8을 참조한다.

## 4.4 Unity 디지털트윈

### 4.4.1 씬 구성

- 3D 모델: 그리드 랙, 4-way 셔틀, 컨베이어, 부품 프리팹을 CATIA / Blender 로 모델링 후 임포트
- 셔틀 이동 애니메이션: 서보 모터의 실시간 각도값을 수신하여 래크 앤 피니언 슬라이더 기구학 계산을 통해 X·Y·Z 실제 위치를 산출하고, 물리적 동작과 동기화하여 재생
- UI Overlay: 그리드 셀 클릭 시 부품명·수량·최근 입고 시간 팝업
- 색상 표시: 빈 셀 회색 / 재고 있음 파란색 / 가득 참 주황색

### 4.4.2 백엔드 연동 (C#)

Unity는 Pi 5 서버 스택과 **두 채널**로 통신한다. 용도가 다르므로 양쪽 모두 유지한다.

**(a) 실시간 이벤트 — MQTT 구독**

Pi 5의 Mosquitto 브로커(1883)에 MQTT 클라이언트(MQTTnet 등)로 접속해 `warehouse/status/*`·`warehouse/inventory/*` 토픽을 구독한다. 수신한 페이로드를 파싱하여 즉시 3D 셔틀 애니메이션과 재고 색상 표시를 갱신한다. Supabase Realtime(WebSocket)을 대체하는 경로다.

**(b) 대시보드 조회 — PostgreSQL 직접 접속 (Npgsql)**

Unity가 `Npgsql` 패키지로 Pi 5 PostgreSQL(5432)에 직접 접속해 재고 현황·입출고 이력·부품 마스터 등을 SELECT 조회한다. 대시보드 패널의 필터·정렬·페이지네이션·검색처럼 실시간 push만으로 다루기 어려운 뷰가 여기에 해당한다. 접속 자격증명은 **Unity 전용 읽기전용 롤** `smartsort_client` 를 사용한다 (상세는 `docs/database-schema.md` §5.1 참조). 쓰기 권한이 없으므로 Unity에서 실수로 INSERT/UPDATE/DELETE가 나가도 DB에서 거부된다.

### 4.4.3 주요 C# 스크립트

| 스크립트 | 역할 |
| --- | --- |
| WarehouseManager.cs | Mosquitto MQTT 구독(`warehouse/status/*`, `warehouse/inventory/*`), 재고 변경 이벤트 수신 및 씬 동기화 |
| DashboardRepository.cs | Npgsql로 PostgreSQL에 접속해 재고 현황·입출고 이력·부품 마스터 조회. `smartsort_client` 읽기전용 자격증명 사용 |
| ShuttleController.cs | 서보 모터 각도값 수신 → 래크 앤 피니언 슬라이더 기구학 계산으로 X·Y·Z 실제 위치 산출 → 셔틀 이동 애니메이션 동기화 재생 |
| GridCell.cs | 그리드 셀 클릭 이벤트, 부품명·수량 팝업 처리 |
| ConveyorAnimator.cs | GT2 타이밍 벨트 이동 애니메이션 (스텝 기반) |
| UIManager.cs | 재고 검색, 필터, 그리드 전체 현황 UI 관리 (DashboardRepository 호출) |

---

# 5. 개발 로드맵

| 단계 | 주요 작업 | 산출물 |
| --- | --- | --- |
| 1단계
기반 구축 | • 전체 UML Diagram 작성
• Pi 5 서버에 PostgreSQL 17 설치, 스키마·권한 생성
• Pi 5 서버에 Mosquitto 설치 및 토픽 구조 확정
• Pi 5 서버에 llama.cpp 빌드 및 llama-server systemd 등록 (Text-to-SQL)
• Git 레포지토리 브랜치 전략 적용 및 팀 권한 설정
• ESP32 개발 환경(PlatformIO) 세팅 및 Wi-Fi MQTT 연결 테스트 | UML Diagram, Pi 5 서버 스택 구동, MQTT 송수신 확인, Git 레포 |
| 2단계
하드웨어 MVP | • NEMA17 + A4988 + GT2 컨베이어 정밀 정지 구현 (스텝 카운트 기반)
• TCRT5000 IR 센서 디바운스 및 촬영 트리거 로직
• 4-way 셔틀 래크 앤 피니언 Y축 승강 기구 조립 및 방향 전환 동작 검증
• MG90S X·Y축 주행 및 엔드스톱 홈 포지션 캘리브레이션
• MG996R Z축 스풀/벨트 리프팅 동작 및 층별 정밀 정지 검증 | 컨베이어 정밀 이송 동작, 셔틀 X·Y·Z 이동 동작 |
| 3단계
비전 AI 연동 | • RPi-Zero 2W에 picamera2 설치 및 하향 촬영 세팅
• Gemini API 호출 파이프라인 구현 (base64 인코딩 → JSON 수신)
• 신뢰도 임계값 처리 로직 (0.85 이상 / 재촬영 / 미분류 분기)
• mqtt-bridge → Pi 5 PostgreSQL inventories 테이블 기록 및 상태 토픽 재발행 | 부품 자동 분류 성공률 85% 이상, DB 자동 기록 |
| 4단계
Unity 디지털 트윈 | • Blender / CATIA 그리드 랙·셔틀·컨베이어 3D 모델 임포트
• MQTT C# 구독 (WarehouseManager.cs, `warehouse/status/*`·`warehouse/inventory/*`)
• ShuttleController.cs 이동 애니메이션 및 셀 색상 표시
• GridCell.cs 클릭 팝업 (부품명·수량·입고 시각) | Unity 씬에서 실시간 재고 반영 확인 |
| 5단계
통합 테스트 | • 하드웨어 ↔ 백엔드 ↔ Unity 엔드투엔드 시나리오 테스트
• 네트워크 끊김 / Gemini 타임아웃 / 셔틀 위치 오차 등 위험 요소 재현 및 검증
• 미분류 보관함 이송 및 관리자 알림 동작 확인 | 통합 테스트 체크리스트 완료 |
| 6단계
마무리 및 발표 준비 | • 버그 수정 및 코드 리팩토링
• 부품 목록(BOM) 수량·용도 최종 기재
• 시연 시나리오 준비 (볼트·너트·전해콘덴서 3종 분류 데모)
• 프로젝트 문서 최종 정리 및 README 작성 | 최종 발표 자료, 완성된 프로젝트 문서 |

---

# 6. 기술 스택 및 부품 목록

## 6.1 기술 스택

| 분야 | 기술 |
| --- | --- |
| MCU / 펌웨어 | ESP32 (PlatformIO), Arduino IDE, C/C++ |
| 카메라 | Raspberry Pi Camera Module v2, picamera2 (libcamera) |
| 비전 AI | Python, Google Gemini API (gemini-1.5-flash), OpenCV |
| 통신 | MQTT (Mosquitto), REST API |
| 백엔드 / DB | Raspberry Pi 5 자체 호스팅 (PostgreSQL 17, Mosquitto, llama-server) |
| 3D 디지털 트윈 | Unity 6.3 LTS, C#, Blender, CATIA |
| 개발 도구 | VS Code, PlatformIO, Git |

## 6.2 주요 하드웨어 부품 목록

| 부품 | 모델 / 사양 |
| --- | --- |
| ESP32 보드 | ESP32-DevKitC |
| RPi-Zero 2W |  |
| Pi Camera Module v2 | Sony IMX219, 8MP |
| NEMA17 스텝 모터 | 투입 컨베이어 구동 |
| A4988 스텝 드라이버 |  |
| GT2 타이밍 벨트 | 투입 컨베이어용 |
| GT2 풀리 | 20T |
| MG90S 서보 모터 | 4-way 셔틀 X·Y축 주행 × 4 |
| MG996R 서보 모터 | 4-way 셔틀 Z축 리프트 × 2 + Y축 승강(래크 앤 피니언) × 1 = 총 3개 |
| IR 반사 센서 | TCRT5000 |
| 엔드스톱 스위치 | 셔틀 홈 포지션 캘리브레이션용 |
| LED 링 라이트 | 5V, 백색 |
| 전원 공급 | 12V 5A SMPS + 5V 3A |
| PLA | 레일·프레임 소재 |
| 알루미늄 프로파일 | 그리드 랙 프레임 |

---

# 7. 위험 요소 및 대응 방안

| 위험 요소 | 영향 | 대응 방안 |
| --- | --- | --- |
| Gemini API 응답 지연 | 높음 | 촬영 후 다음 부품 이송과 병렬 처리, 타임아웃 시 재시도 |
| 네트워크 끊김 | 높음 | 로컬 캐시 테이블 (최근 분류 결과 100건 저장), 오프라인 알림 |
| GT2 벨트 장력 변화로 위치 오차 | 중간 | 홈 포지션 주기적 캘리브레이션, 엔드스톱 센서 추가 |
| 래크 앤 피니언 Y축 승강 불완전 | 높음 | 슬라이더 위치 센서 추가, 승강 후 접지 확인 딜레이 적용 |
| 셔틀 위치 오차 (서보 과부하·레일 마찰) | 높음 | 엔드스톱 주기적 재캘리브레이션, 이동 전 토크 이상 감지 루틴 |
| MQTT 브로커 연결 끊김 | 중간 | Unity·ESP32·RPi 측 MQTT 클라이언트 자동 재연결(Keep-Alive + 지수 백오프), Mosquitto `persistent=true`로 재접속 시 누락 메시지 재전송, Pi 5 서버 watchdog로 Mosquitto 죽음 감지 |
| Pi 5 서버 단일 장애점 | 높음 | 무정전 전원(UPS) 또는 보조 배터리, PostgreSQL 매일 03:00 pg_dump 자동 백업(7일 보관), Active Cooler로 발열 제어(80°C 쓰로틀링 회피) |

---

# 8. 코드 규칙

## 8.1 코드 작성

### 8.1.1 명명법

**클래스**

모든 클래스명은 **PascalCase** 로 작성한다.

(예) ConveyorController, GeminiClassifier, MqttBridge, SlotAllocator, InventoryUpdater, WarehouseManager

**필드**

`private` 필드는 **camelCase** 에 언더 바(_)를 붙여 작성하며, `public` 프로퍼티(속성)는 **PascalCase** 로 작성한다. `private` 필드는 반드시 프로퍼티를 통해서만 접근할 수 있도록 한다.

(예) _id, _name, _type, _material

**함수**

모든 함수명은 **PascalCase** 로 작성하며, 동사 + 명사 조합으로 기능을 명확히 표현한다.

(예) MoveToPosition(), ReadIrSensor(), StopConveyor(), UpdateInventory(), OnInventoryChanged()

**지역 변수**

모든 지역 변수명은 의미를 알 수 있는 명확한 이름을 사용하며, **camelCase** 로 작성한다.

(예) targetStepCount, partName, confidenceScore, slotCount

### 8.1.2 주석 구조

파일 상단, 함수 선언부, 복잡한 로직 세 곳에 주석을 **필수**로 작성한다.

① 파일 상단 헤더 주석 (모든 파일 공통)

```csharp
// ============================================================
// 파일명  : ConveyorController.cs
// 역할    : 투입 컨베이어 NEMA17 스텝 모터 정밀 제어
// 작성자  : [이름]
// 작성일  : 2026-03-15
// 수정이력: 2026-03-15 - 마이크로스텝(1/16) 적용
// ============================================================
```

② 함수 선언부 주석

```csharp
/**
 * @brief  지정 스텝 수만큼 컨베이어를 정밀 이동한다.
 * @param  steps    이동할 스텝 수 (양수: 전진, 음수: 후진)
 * @param  speed    최대 속도 (steps/sec)
 * @return void
 */
void moveConveyor(int steps, int speed) { ... }
```

③ 인라인 주석 (복잡한 로직)

```csharp
// 목적 셀까지 최단 방향 이동 계산 (X → Y → Z 순차 이동)
int deltaX = targetX - currentX;
int deltaY = targetY - currentY;
int deltaZ = targetZ - currentZ;
```

### 8.1.3 체크 리스트

- [ ]  클래스, 필드, 함수 및 변수 등이 규칙에 따라 적절한 이름으로 작성되었는가?
- [ ]  클래스의 속성(프로퍼티)와 접근제한자를 적절히 사용하였는가?
- [ ]  파일 상단, 함수 선언부, 복잡한 로직에 대한 주석이 작성이 되었는가?
- [ ]  코드의 중복을 줄이고, 유지보수성을 높이는 방향으로 작성이 되었는가?
- [ ]  클래스의 상속, 함수 오버로드 및 오버라이드, `ref`, `out` 키워드, 구조체, 컬렉션, 제네릭, LINQ 등 현대적인 C# 문법으로 작성하였는가?

## 8.2 깃(Git)

### 8.2.1 브랜치 전략

| 브랜치 | 목적 | 규칙 |
| --- | --- | --- |
| `main` | 통합 개발 브랜치 | 직접 푸시 금지, PR 필수 |
| `feature/[기능명]` | 신규 기능 개발 | <대상> -(하이픈) <기능> 구조로 작성 |
| `hw/[하드웨어명]` | 펌웨어/하드웨어 전용 | <디바이드> -(하이픈) <기능> 구조로 작성 |
| `fix/[버그명]` | 버그 수정 | <대상 또는 기능> -(하이픈) <버그> 구조로 작성 |

**(예시)**

- **feature/** → `feature/shuttle-add-xyz-move`
- **fix/** → `fix/ir-sensor-noise`, `fix/mqtt-reconnect-backoff`
- **hw/** → `hw/esp32-2-shuttle-servo-control`, `hw/mg90s-config-pwm`

### 8.2.2 커밋 메시지 규칙

형식: `[타입]: [요약]` (한 줄, 50자 이내) 하나의 커밋은 하나의 논리적 변경만 포함한다.

| 타입 | 의미 | 예시 |
| --- | --- | --- |
| feat | 새 기능 추가 | feat: 셔틀 X·Y·Z 이동 시퀀스 구현 |
| fix | 버그 수정 | fix: IR 센서 디바운스 오류 수정 |
| docs | 문서 수정 | docs: Pi 5 서버 스택 아키텍처 반영 |
| refactor | 코드 개선 | refactor: 셀 할당 로직 함수 분리 |
| hw | 하드웨어/펌웨어 변경 | hw: MG90S PWM 듀티 사이클 조정 |
| test | 테스트 코드 추가 | test: 셔틀 위치 정밀도 단위 테스트 |
| chore | 빌드·설정 변경 | chore: PlatformIO 라이브러리 버전 업 |
| WIP | 작업 중 (중간 백업용) | WIP: 기구학 기반 Line 클래스 작성 중(1) |

### 8.2.3 PR(Pull Request) 규칙

- PR 제목은 커밋 메시지 형식과 동일하게 작성한다.
- PR 본문에는 변경 내용 요약, 테스트 방법, 관련 이슈 번호를 포함한다.
- 팀 구성원 모두의 승인 후 Merge한다.
- 머지 방식: Squash and merge (develop → main은 Merge commit)

**PR 본문 템플릿**

```
## 변경 내용
- 셔틀 X·Y·Z 이동 시퀀스 구현
- 엔드스톱 스위치 홈 포지션 캘리브레이션 루틴 추가

## 테스트 방법
1. ESP32 #2 플래싱 후 시리얼 모니터 확인
2. 셔틀 수동 명령 전송 후 그리드 셀 정렬 확인

## 관련 이슈
Closes #12
```

## 8.3 데이터 베이스

### 8.3.1 기본 원칙

| 항목 | 규칙 |
| --- | --- |
| 케이스 | snake_case |
| 언어 | 영어만 사용 |
| 복수형 | 테이블명은 항상 복수형으로 작성 |
| 구분자 | `_` 만 허용 (`-`, 공백 금지) |
| 예약어 | `user`, `order`, `table` 등 PostgreSQL 예약어 사용 금지 |
| 최대 길이 | 63자 이하 |

### 8.3.2 **테이블 목록**

| 테이블명 | 설명 | 분류 |
| --- | --- | --- |
| `parts` | 부품 종류 마스터 | 마스터 |
| `grid_cells` | 그리드 셀 위치 마스터 | 마스터 |
| `bins` | Bin 마스터 | 마스터 |
| `inventories` | 현재 재고 현황 | 운영 |
| `transactions` | 입출고 이력 | 이력 |

### 8.3.3 컬럼

**공통 컬럼**

| 컬럼명 | 타입 | 설명 |
| --- | --- | --- |
| `id` | `uuid` (기본) 또는 `bigint` | PK, `gen_random_uuid()` 기본값 (pgcrypto) |
| `created_at` | `timestamptz` | 생성 시각 (`DEFAULT now()`) |
| `updated_at` | `timestamptz` | 수정 시각 |

**타입 별 컬럼 접미어 규칙**

| 접미어 | 의미 | 예시 |
| --- | --- | --- |
| `_id` | FK 참조 | `part_id`, `cell_id`, `bin_id` |
| `_at` | 시각 (timestamptz) | `created_at`, `updated_at`, `stored_at` |
| `_no` | 번호 (순서, 정수) | `slot_no`, `bin_no` |
| `_count` | 수량 / 개수 | `qty_count`, `retry_count` |
| `_deg` | 각도 (degree) | `angle_deg`, `home_deg` |
| `_mm` | 길이 (millimeter) | `position_mm`, `distance_mm` |
| `_url` | URL 문자열 | `image_url`, `thumbnail_url` |
| `_flag` | boolean 상태 | `is_active`, `is_full`, `is_verified` |
| `_type` | 종류 구분 | `transaction_type`, `alert_type` |
| `_json` | JSONB 데이터 | `meta_json`, `config_json` |

### 8.3.4 금지 패턴

```
❌ Parts                  → 대문자 금지
❌ gridCells              → camelCase 금지
❌ grid-cells             → 하이픈 금지
❌ tbl_parts              → tbl_ 접두어 불필요
❌ part                   → 단수형 금지
❌ partId                 → camelCase 금지 (컬럼도 snake_case)
❌ flag                   → boolean은 is_ / has_ 접두어 필수
❌ time, date             → 시각 컬럼은 _at 접미어 필수
```