// ============================================================
// 파일명  : JointComponent.cs
// 역할    : SliderJointComponent 와 RevoluteJointComponent 의 공통 기반 추상 클래스
//           구속 상태 로그, Update 공통 흐름을 담당한다.
// 작성자  : 이현화
// 작성일  : 2026-04-01
// 수정이력: 2026-04-22 - 구속 후 useGravity 활성화 코드 제거 (중력 비활성화 유지)
//                      - Rigidbody 제거, Transform 직접 제어 방식으로 리팩토링
//                        (디지털 트윈 특성상 물리 엔진 불필요)
//                      - ObjectB 프로퍼티 추가 (외부에서 오브젝트 B 접근용)
// ============================================================

using UnityEngine;

public abstract class JointComponent : MonoBehaviour
{
    [SerializeField] protected Transform _objectA;   // 오브젝트 A (기준)
    [SerializeField] protected Transform _objectB;   // 오브젝트 B (이동 또는 회전 대상)

    public Transform ObjectB => _objectB;  // [2026.04.22 추가] 오브젝트 B (이동 또는 회전 대상)

    protected bool _wasConstrained;                  // 이전 프레임 구속 상태 (구속 상태 변경 로그용)

    protected const float ApplyTolerance = 0.001f;   // 물리 적용 판별 허용 오차 (Line/Plane 의 Tolerance 1e-6f 와 구분)

    /**
     * @brief  공통 Awake 처리.
     *         유효성 검사 → 자식 전용 초기화 순서로 실행한다.
     */
    protected virtual void Awake()
    {
        if (_objectA == null || _objectB == null)
        {
            Debug.LogError($"{GetType().Name}: 필수 오브젝트(A 또는 B)가 할당되지 않았습니다.");
            enabled = false;  // Update 호출 중단 (NullReferenceException 방지)
            return;
        }

        InitializeChild();
    }

    // TODO: 추후에 다시 확인 - TestManager 와의 실행 순서 충돌 문제 해결 후 FixedUpdate 로 변경 예정
    //       현재는 구현 완료된 기능이 아니므로 Update 로 임시 처리
    /**
     * @brief  공통 Update 처리. (임시 - 추후 FixedUpdate 로 변경 예정)
     *         IsValid → ApplyConstraint → 구속 상태 변경 로그 → 구속/해제 분기 순서로 실행한다.
     */
    protected virtual void Update()
    {
        if (!IsJointValid()) return;

        bool isConstrained = ApplyJointConstraint();  // 구속 조건 적용 결과

        // 구속 상태가 변경됐을 때만 로그 출력
        if (isConstrained != _wasConstrained)
        {
            Debug.Log($"{GetType().Name} 구속 상태 변경: {(isConstrained ? "구속됨" : "해제됨")}");
            _wasConstrained = isConstrained;  // 이전 프레임 구속 상태 갱신
        }

        if (isConstrained)
        {
            OnConstrained();  // 구속 상태일 때 자식 클래스 전용 처리
        }
    }

    /**
     * @brief  Inspector 에서 값 변경 시 공통 유효성 검사 후 자식 전용 재초기화를 호출한다.
     *         ※ Play 모드에서는 실행되지 않는다.
     */
    protected void OnValidate()
    {
        if (Application.isPlaying) return;  // Play 모드에서는 실행하지 않는다
        if (_objectA == null || _objectB == null) return;
        OnValidateJoint();
    }

    /**
     * @brief  자식 클래스 전용 초기화.
     *         조인트 인스턴스 생성 및 자식 전용 필드 초기화를 여기서 수행한다.
     */
    protected abstract void InitializeChild();

    /**
     * @brief  Inspector 에서 값 변경 시 자식 클래스 전용 재초기화.
     *         Line·Plane·Joint 인스턴스를 자식에서 재생성한다.
     */
    protected abstract void OnValidateJoint();

    /**
     * @brief  조인트 유효성 검사.
     *         자식 클래스의 joint.IsValid() 를 호출한다.
     * @return bool  유효하면 true
     */
    protected abstract bool IsJointValid();

    /**
     * @brief  구속 조건 적용.
     *         자식 클래스의 joint.ApplyConstraint() 를 호출한다.
     * @return bool  구속 중이면 true
     */
    protected abstract bool ApplyJointConstraint();

    /**
     * @brief  구속 상태일 때 자식 클래스 전용 처리.
     *         Transform 직접 제어로 위치 또는 회전을 적용한다.
     */
    protected abstract void OnConstrained();
}