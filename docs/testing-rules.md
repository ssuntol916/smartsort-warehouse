# 개발 및 테스트 규칙

> SmartSort Project — Unity Digital Twin  
> 작성일: 2026-03-24 | 작성자: 이현화

---

## 목차

1. [테스트 작성 규칙](#1-테스트-작성-규칙)
2. [테스트 실행 절차](#2-테스트-실행-절차)

---

## 1. 테스트 작성 규칙

### 언제 테스트를 작성해야 하는가

아래 경우에 해당하면 반드시 테스트를 작성한다.

| 상황 | 이유 |
|---|---|
| 새로운 클래스를 구현했을 때 | 기능이 의도대로 동작하는지 검증 |
| 기존 코드를 수정했을 때 | 기존 기능이 망가지지 않았는지 확인 |
| 버그를 수정했을 때 | 같은 버그가 다시 발생하지 않도록 방지 |
| 계산 로직이 포함된 경우 | 수치 오차, 엣지 케이스 검증 필요 |

---

### 어떤 클래스를 테스트하는가

**EditMode 테스트 대상 (이 프로젝트 기준)**

`MonoBehaviour` 를 상속받지 않는 순수 C# 클래스는 모두 EditMode 에서 테스트한다.

- 계산 로직 클래스 (예: `Line.cs`, `Plane.cs`)
- 데이터 처리 클래스
- 상태 머신 로직

**PlayMode 테스트 대상**

씬, 오브젝트, 물리엔진이 필요한 클래스는 PlayMode 에서 테스트한다.

- `MonoBehaviour` 상속 클래스
- `Rigidbody`, 충돌 감지
- 코루틴, 애니메이션

---

### 테스트 파일 위치 규칙

```
Assets/
└── Tests/
    └── Editor/          ← EditMode 테스트 파일은 여기에
        ├── LineTest.cs
        └── Tests.asmdef
```

- 테스트 파일은 반드시 `Tests/Editor/` 폴더 안에 넣는다
- `Editor` 폴더에 넣는 이유: 테스트 코드가 실제 게임 빌드에 포함되지 않도록 하기 위함
- `.asmdef` 파일은 하나로 폴더 안의 모든 테스트 파일을 커버한다. 테스트 파일을 추가할 때마다 새로 만들 필요 없다

---

### 테스트 파일 네이밍 규칙

| 항목 | 규칙 | 예시 |
|---|---|---|
| 파일 이름 | `{클래스명}Test.cs` | `LineTest.cs` |
| 클래스 이름 | `{클래스명}Test` | `public class LineTest` |
| 메서드 이름 | `{메서드명}_{조건}_{기대결과}` | `IsParallel_BasicParallel_ReturnsTrue` |

---

### 테스트 케이스 작성 기준

기본 케이스만 작성하지 않는다. 아래 케이스를 모두 포함한다.

- **기본 케이스**: 가장 일반적인 입력값
- **경계 케이스**: 시작점, 끝점, 중간점 등 경계에 있는 값
- **예외 케이스**: 반대 방향, 3D 공간, 음수 좌표 등
- **실패 케이스**: `null` 반환, `false` 반환이 맞는 상황
- **복합 케이스**: 여러 메서드를 연쇄적으로 검증

> **부동소수점 주의** `float` 계산 결과를 `==` 로 비교하지 않는다. 반드시 허용 오차(`delta`)를 지정한다.
> ```csharp
> // 잘못된 방법
> Assert.AreEqual(1f, result.x);
>
> // 올바른 방법
> Assert.AreEqual(1f, result.x, 0.0001f);
> ```

---

## 2. 테스트 실행 절차

### 실행 전 체크리스트

테스트를 실행하기 전에 아래 항목을 확인한다.

- [ ] `LineTest.cs` 와 `Tests.asmdef` 가 같은 폴더(`Tests/Editor/`) 안에 있는가
- [ ] `Tests.asmdef` 가 `SmartSortScripts` 를 참조하고 있는가
- [ ] `Tests.asmdef` 의 Platforms 설정에서 `Editor` 가 Include 되어 있는가
- [ ] Console 창에 컴파일 에러가 없는가

---

### 실행 순서

**1단계 — Test Runner 열기**

```
Unity 상단 메뉴 → Window → General → Test Runner
```

**2단계 — EditMode 탭 선택**

창 상단에서 반드시 **EditMode** 탭을 선택한다. (PlayMode 아님)

**3단계 — Run All**

`Run All` 버튼을 클릭한다.

**4단계 — 결과 확인**

| 결과 | 의미 | 다음 행동 |
|---|---|---|
| ✅ 전부 초록 | 전체 통과 | PR 진행 가능 |
| ❌ 빨간 X 있음 | 일부 실패 | 실패한 테스트 클릭 → 에러 메시지 확인 → 수정 후 재실행 |

---

### 실패 메시지 읽는 법

빨간 X 테스트를 클릭하면 하단에 에러 메시지가 표시된다.

```
Expected: True
But was:  False

LineTest.IsCoincident_OppositeDirection_SameLine_ReturnsTrue
```

- 마지막 줄: 실패한 테스트 메서드 이름
- `Expected` / `But was`: 기대값 vs 실제값
- 메서드 이름을 보고 `Line.cs` 의 해당 로직 확인

---

### PR 전 필수 확인

코드를 PR 하기 전에 반드시 아래를 완료한다.

- [ ] `Run All` 실행 후 전체 Pass 확인
- [ ] 새로 추가한 기능에 대한 테스트 케이스 작성 완료
- [ ] Console 에 에러 및 경고 없음

> **규칙** 테스트가 하나라도 Fail 인 상태로 PR 을 올리지 않는다.

---

## 참고 — 트러블슈팅

설정 중 문제가 발생하면 아래를 확인한다.

| 증상 | 원인 | 해결 |
|---|---|---|
| Test Runner 에 목록이 안 보임 | `.asmdef` 파일 없거나 위치 잘못됨 | `Tests.asmdef` 와 `LineTest.cs` 가 같은 폴더인지 확인 |
| `Line` 클래스를 찾을 수 없음 | `Tests.asmdef` 에 `SmartSortScripts` 참조 없음 | Inspector 에서 참조 추가 후 Apply |
| Cyclic dependencies 에러 | 참조 방향이 반대로 됨 | `SmartSortScripts.asmdef` 에서 `Tests` 참조 제거 |
| EditMode 에 테스트 안 보임 | Platforms 설정 문제 | `Tests.asmdef` → Inspector → `Any Platform` 해제 → `Editor` 체크 → Apply |

자세한 설정 방법은 `Unity_TestRunner_가이드_v2.docx` 를 참고한다.

---

*SmartSort Project — Unity Digital Twin*
