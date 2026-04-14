# Git Convention

> Git 사용 규칙

---

## 커밋 메시지

### 형식

```
<타입>: <간결한 설명>
```

### 타입

| 타입       | 용도                          | 예시                                       |
| ---------- | ----------------------------- | ------------------------------------------ |
| `feat`     | 새로운 기능 추가              | `feat: 컨베이어 벨트 MQTT 제어 로직 추가`  |
| `fix`      | 버그 수정                     | `fix: MQTT 재연결 백오프 지연 수정`        |
| `docs`     | 문서 수정                     | `docs: README에 하드웨어 구성도 추가`      |
| `style`    | 코드 포맷팅 (기능 변경 없음)  | `style: Unity 스크립트 네이밍 통일`        |
| `refactor` | 리팩토링 (기능 변경 없음)     | `refactor: ESP32 센서 데이터 파싱 분리`    |
| `chore`    | 설정, 빌드 등 기타            | `chore: .gitignore에 Unity 캐시 추가`      |
| `WIP`      | 작업 중 (중간 백업)           | `WIP: Line 클래스 속성 정의까지`           |

### 규칙

- 한글로 작성 (타입 접두어만 영어)
- 50자 이내로 간결하게
- `WIP` 커밋은 PR 전에 squash로 정리

---

## 브랜치 전략

### 브랜치 구조

```
main                ← 항상 동작하는 안정 버전
├── feature/*       ← 기능 개발
├── fix/*           ← 버그 수정
└── docs/*          ← 문서 작업
```

### 네이밍

```
feature/conveyor-mqtt-control
feature/unity-digital-twin-ui
fix/stepper-motor-sequence-error
docs/update-readme
```

### 작업 흐름

```bash
# 1. main 최신 상태로 업데이트
git switch main
git pull

# 2. 작업 브랜치 생성
git switch -c feature/conveyor-mqtt-control

# 3. 작업 → 커밋 → push
git add .
git commit -m "feat: 컨베이어 벨트 전진/후진 MQTT 명령 구현"
git push -u origin feature/conveyor-mqtt-control

# 4. GitHub에서 PR 생성 → 리뷰 → main에 병합

# 5. 병합 후 브랜치 정리
git switch main
git pull
git branch -d feature/conveyor-mqtt-control
```

### 주의사항

- **main에 직접 커밋 금지** — 반드시 브랜치에서 작업 후 PR로 병합
- 브랜치 하나에 하나의 작업만 (기능 하나, 버그 하나)
- 장기간 방치된 브랜치는 main과 주기적으로 동기화

---

## PR (Pull Request)

### 제목

커밋 메시지와 동일한 형식 사용:

```
feat: 컨베이어 벨트 MQTT 제어 로직 추가
```

### 본문 템플릿

```markdown
## 작업 내용
- 어떤 작업을 했는지 간단히 설명

## 변경 파일
- 주요 변경 파일 목록

## 테스트
- [ ] 동작 확인 완료
- [ ] 기존 기능 영향 없음
```

### 병합 방식

- `Squash and merge` 사용 (WIP 커밋 정리됨)
- 병합 후 원격 브랜치 자동 삭제 설정 권장

---

## .gitignore

프로젝트에 반드시 포함할 항목:

```gitignore
# Unity
Library/
Temp/
Logs/
obj/
Build/
*.csproj
*.sln

# Python
__pycache__/
*.pyc
.venv/

# Node
node_modules/

# 환경 변수 / 시크릿
.env
.env.local

# OS
.DS_Store
Thumbs.db

# IDE
.vscode/
.idea/
```

---

## 충돌 해결

1. 충돌 파일을 VS Code에서 열기
2. `<<<<<<< HEAD` ~ `=======` ~ `>>>>>>>` 구간 확인
3. 올바른 코드만 남기고 정리
4. `git add <파일>` → `git commit`

> 충돌 해결이 어려우면 팀장에게 먼저 공유할 것