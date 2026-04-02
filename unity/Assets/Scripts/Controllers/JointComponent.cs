// ============================================================
// 파일명  : JointComponent.cs
// 역할    : SliderJointComponent 와 RevoluteJointComponent 의 공통 기반 추상 클래스
//           Rigidbody 초기화, 구속 상태 로그, Update 공통 흐름을 담당한다.
// 작성자  : 이현화
// 작성일  : 2026-04-01
// 수정이력: 
// ============================================================

using UnityEngine;

public abstract class JointComponent : MonoBehaviour
{
    [SerializeField] protected Transform _objectA;   // 오브젝트 A (기준)
    [SerializeField] protected Transform _objectB;   // 오브젝트 B (이동 또는 회전 대상)

    protected Rigidbody _rigidbodyA;                   // 오브젝트 A 리지드바디
    protected Rigidbody _rigidbodyB;                   // 오브젝트 B 리지드바디
    protected RigidbodyConstraints _freezeConstraint;  // 프리즈 조건 (캐시)

    protected bool _wasConstrained;                    // 이전 프레임 구속 상태

    protected const float ApplyTolerance = 0.001f;     // 물리 적용 판별 허용 오차 (Line/Plane 의 Tolerance 1e-6f 와 구분)

    /**
     * @brief  공통 Awake 처리.
     *         Rigidbody 초기화 → 중력 비활성화 → 프리즈 조건 계산 → 자식 전용 초기화(OnAwake) 순서로 실행한다.
     *         구속 확인 전 오브젝트 B 가 중력으로 떨어지는 것을 방지하기 위해 초기에 중력을 비활성화한다.
     */
    protected virtual void Awake()
    {
        if (_objectA == null || _objectB == null)
        {
            Debug.LogError($"{GetType().Name}: 필수 오브젝트(A 또는 B)가 할당되지 않았습니다.");
            enabled = false;  // Update 호출 중단 (NullReferenceException 방지)
            return;
        }
        _rigidbodyA = InitializeRigidbody(_objectA, true);
        _rigidbodyB = InitializeRigidbody(_objectB, false);

        // 구속 확인 전 중력으로 떨어지는 것을 방지하기 위해 초기에 중력 비활성화
        _rigidbodyB.useGravity = false;

        _freezeConstraint = GetFreezeConstraintByDirection();
        InitializeChild();
    }

    // TODO: 추후에 다시 확인 - TestManager 와의 실행 순서 충돌 문제 해결 후 FixedUpdate 로 변경 예정
    //       현재는 구현 완료된 기능이 아니므로 Update 로 임시 처리
    /**
     * @brief  공통 Update 처리. (임시 - 추후 FixedUpdate 로 변경 예정)
     *         IsValid → ApplyConstraint → 구속 상태 변경 로그 → 구속/해제 분기 순서로 실행한다.
     *         구속됨 상태일 때만 중력을 활성화하고, 구속 안 됨 상태일 때는 중력을 비활성화한다.
     */
    protected virtual void Update()
    {
        if (!IsJointValid()) return;

        bool isConstrained = ApplyJointConstraint();

        // 구속 상태가 변경됐을 때만 로그 출력
        if (isConstrained != _wasConstrained)
        {
            Debug.Log($"{GetType().Name} 구속 상태 변경: {(isConstrained ? "구속됨" : "해제됨")}");
            _wasConstrained = isConstrained;
        }

        if (isConstrained)
        {
            _rigidbodyB.useGravity = true;   // 구속됨 → 중력 활성화
            _rigidbodyB.constraints = _freezeConstraint;
            OnConstrained();
        }
        else
        {
            _rigidbodyB.useGravity = false;  // 구속 안 됨 → 중력 비활성화
            _rigidbodyB.constraints = RigidbodyConstraints.None;
        }
    }

    /**
    * @brief  Inspector 에서 값 변경 시 공통 유효성 검사 후 자식 전용 재초기화를 호출한다.
    *         ※ Play 모드에서는 실행되지 않는다.
    */
    protected void OnValidate()
    {
        if (Application.isPlaying) return;
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
     *         MovePosition 또는 MoveRotation 을 여기서 호출한다.
     */
    protected abstract void OnConstrained();

    /**
     * @brief  이동 또는 회전 방향 축을 감지하여 해당 축만 열어두고
     *         나머지를 프리즈한 RigidbodyConstraints 를 반환한다.
     *         자식 클래스에서 FreezePosition / FreezeRotation 중 하나를 선택하여 구현한다.
     * @return RigidbodyConstraints  축 방향에 맞는 프리즈 조건
     */
    protected abstract RigidbodyConstraints GetFreezeConstraintByDirection();

    /**
     * @brief  오브젝트의 Rigidbody 를 가져온다.
     *         Rigidbody 가 없으면 자동으로 생성한다.
     *         shouldBeKinematic 이 true 이고 Rigidbody 가 Kinematic 이 아니면
     *         자동으로 Kinematic 으로 설정하고 로그를 출력한다.
     * @param  target              대상 오브젝트의 Transform
     * @param  shouldBeKinematic   true 이면 Rigidbody 를 Kinematic 으로 설정한다.
     *                             (오브젝트 A 고정용)
     * @return Rigidbody           가져오거나 생성한 Rigidbody
     */
    protected Rigidbody InitializeRigidbody(Transform target, bool shouldBeKinematic)
    {
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = target.gameObject.AddComponent<Rigidbody>();
        }

        if (shouldBeKinematic && !rb.isKinematic)
        {
            rb.isKinematic = true;
            Debug.Log($"{GetType().Name}: Object A 가 자동으로 고정되었습니다.\n고정 대상을 변경하려면 Inspector 에서 Is Kinematic 을 수동으로 설정해주세요.");
        }

        return rb;
    }
}