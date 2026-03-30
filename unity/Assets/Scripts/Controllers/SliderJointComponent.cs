// ============================================================
// 파일명  : SliderJointComponent.cs
// 역할    : SliderJoint 를 씬 오브젝트에 연결하는 MonoBehaviour 컴포넌트
// 작성자  : 이현화
// 작성일  : 2026-03-30
// 수정이력: 
// ============================================================

using UnityEngine;

public class SliderJointComponent : MonoBehaviour
{
    [SerializeField] private Transform _objectA;                         // 오브젝트 A (이동축 기준)
    [SerializeField] private Transform _objectB;                         // 오브젝트 B (이동 대상)
    [SerializeField] private float _minPosition = 0f;                    // 최소 이동 범위 (mm)
    [SerializeField] private float _maxPosition = 100f;                  // 최대 이동 범위 (mm)
    [SerializeField] private Vector3 _moveDirection = Vector3.right;     // 슬라이더 이동 방향

    private SliderJoint _joint;                      // SliderJoint.cs 인스턴스
    private Rigidbody _rigidbodyA;                   // 오브젝트 A 리지드바디
    private Rigidbody _rigidbodyB;                   // 오브젝트 B 리지드바디
    private RigidbodyConstraints _freezeConstraint;  // 이동 방향 기준 프리즈 조건 (캐시)

    private bool _wasConstrained;                    // 이전 프레임 구속 상태
    private float _actualMinPosition;                // B 초기 위치 기준 실제 최소 이동 범위
    private float _actualMaxPosition;                // B 초기 위치 기준 실제 최대 이동 범위

    public float CurrentPosition => _joint?.CurrentPosition ?? 0f;   // 현재 슬라이더 위치

    /**
     * @brief  오브젝트 A·B 의 Transform 을 기반으로 이동축 Line 과
     *         기준 Plane 을 생성하고 SliderJoint 를 초기화한다.
     *         이동방향: Inspector 에서 설정 (기본값: Vector3.right)
     *         - 이동축 Line: transform.position → transform.position + _moveDirection
     *         - 기준 Plane: transform.forward 를 법선 벡터로 하는 면
     *         세 점(position, position+right, position+forward) 으로 정의
     */
    private void Awake()
    {
        // 이동 방향 벡터 정규화 (GetProjectedDistance 거리 계산 및 위치 계산 정확도 보장)
        _moveDirection = _moveDirection.normalized;
        
        _freezeConstraint = GetFreezeConstraintByDirection();

        Debug.Log($"SliderJointComponent: 이동축이 {_moveDirection} 으로 설정되었습니다. \nInspector 에서도 설정된 이동축 확인이 가능합니다.");

        // 오브젝트 리지드바디 생성 및 고정 오브젝트(Kinematic) 설정 확인
        _rigidbodyA = InitializeRigidbody(_objectA, true);
        _rigidbodyB = InitializeRigidbody(_objectB, false);

        InitializeJoint();

    }

    //TODO: 추후에 다시 확인 - 프리즈 대신 '구속 조건이 풀리면 → 다시 구속 위치로 이동' 구현 예정
    /**
     * @brief  매 물리 프레임(FixedUpdate) SliderJoint.ApplyConstraint() 를 호출하여 구속 조건 등록 상태를 확인한다.
     *         구속 조건이 참이면 이동 방향 축을 제외한 나머지를 프리즈하고 클램프를 적용한다.
     *         ※ 프리즈는 구속 조건 확인을 위한 임시 코드로 추후 변경 예정
     */
    private void Update()
    {
        if (_joint == null || !_joint.IsValid()) return;

        // 구속 조건 등록 상태 확인
        bool isConstrained = _joint.ApplyConstraint();

        // 구속 상태가 변경됐을 때만 로그 출력
        if (isConstrained != _wasConstrained)
        {
            Debug.Log($"SliderJoint 구속 상태 변경: {(isConstrained ? "구속됨" : "해제됨")}");
            _wasConstrained = isConstrained;
        }

        // 구속 조건이 참이면 프리즈, 아니면 해제
        if (isConstrained)
        {
            _rigidbodyB.constraints = _freezeConstraint;

            // 오브젝트 B 의 현재 위치를 lineA 에 투영하여 거리를 계산하고
            // min/max 범위 안에서만 움직이도록 클램프 처리
            Vector3 clampedPos = _joint.GetClampedPosition(_objectB.position, _objectA.position, _moveDirection);

            if (Vector3.Distance(_objectB.position, clampedPos) > 0.001f)
            {
                _rigidbodyB.MovePosition(clampedPos);
            }
        }
        else
        {
            _rigidbodyB.constraints = RigidbodyConstraints.None;
        }

    }

    /**
     * @brief  외부에서 슬라이더 위치를 설정한다.
     *         ShuttleController.cs 에서 X·Y 바퀴 위치 제어 시 호출한다.
     *         SliderJoint.SetPosition() 에 위임한다.
     * @param  position    설정할 슬라이더 위치 (mm)
     */
    public void SetPosition(float position)
    {
        _joint?.SetPosition(position);
    }

    /**
    * @brief  Inspector 에서 값 변경 시 Line·Plane 을 재생성하고 SliderJoint 를 재초기화한다.
    *         ※ Play 모드에서는 실행되지 않는다.
     */
    private void OnValidate()
    {
        if (Application.isPlaying) return;  // 여기로 이동
        InitializeJoint();
    }

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
    private Rigidbody InitializeRigidbody(Transform target, bool shouldBeKinematic)
    {
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = target.gameObject.AddComponent<Rigidbody>();
        }

        if(shouldBeKinematic && !rb.isKinematic)
        {
            rb.isKinematic = true;
            Debug.Log("SliderJointComponent: Object A 가 자동으로 고정되었습니다.\n고정 대상을 변경하려면 Inspector 에서 Is Kinematic 을 수동으로 설정해주세요.");
        }

        return rb;
    }

    /**
    * @brief  Awake 및 OnValidate 에서 공통으로 호출되는 조인트 초기화 메서드.
    *         이동축 Line 과 기준 Plane 을 생성하고 SliderJoint 인스턴스를 초기화한다.
    */
    private void InitializeJoint()
    {
        if (_objectA == null || _objectB == null) return;

        // 오브젝트 A·B 의 Transform 에서 이동축 Line 생성
        Line lineA = new Line(_objectA.position,
                              _objectA.position + _moveDirection);
        Line lineB = new Line(_objectB.position,
                              _objectB.position + _moveDirection);

        // 오브젝트 A·B 의 Transform 에서 기준 Plane 생성
        // transform.right, transform.forward 로 면의 세 점 정의
        Plane planeA = new Plane(_objectA.position,
                                 _objectA.position + _objectA.right,
                                 _objectA.position + _objectA.forward);
        Plane planeB = new Plane(_objectB.position,
                                 _objectB.position + _objectB.right,
                                 _objectB.position + _objectB.forward);

        // B 의 초기 위치를 이동축에 투영하여 초기 거리 계산
        float initialDistance = SliderJoint.GetProjectedDistance(
            lineA, _objectB.position, _objectA.position, _moveDirection);

        // min/max 를 B 의 초기 위치 기준 offset 으로 적용
        _actualMinPosition = initialDistance + _minPosition;
        _actualMaxPosition = initialDistance + _maxPosition;

        // SliderJoint.cs 인스턴스 생성
        _joint = new SliderJoint(lineA, lineB,
                                 planeA, planeB,
                                 _actualMinPosition, _actualMaxPosition);
    }

    /**
     * @brief  슬라이더 이동 방향 축을 감지하여 해당 축만 열어두고
     *         나머지 이동 및 회전을 프리즈한 RigidbodyConstraints 를 반환한다.
     * @return RigidbodyConstraints  이동 방향 축만 열어둔 프리즈 조건
     */
    private RigidbodyConstraints GetFreezeConstraintByDirection()
    {
        // 이동 방향과 각 축의 유사도 계산
        // Dot 결과값이 클수록 해당 축과 방향이 일치함을 의미
        // ex) _moveDirection = (1,0,0) 이면 dotX = 1.0, dotY = 0.0, dotZ = 0.0
        float dotX = Mathf.Abs(Vector3.Dot(_moveDirection, Vector3.right));
        float dotY = Mathf.Abs(Vector3.Dot(_moveDirection, Vector3.up));
        float dotZ = Mathf.Abs(Vector3.Dot(_moveDirection, Vector3.forward));

        RigidbodyConstraints moveConstraint;

        // 세 축 중 Dot 값이 가장 큰 축이 이동 방향 축
        // 해당 축의 FreezePosition 만 제외하고 나머지는 전부 프리즈
        if (dotX >= dotY && dotX >= dotZ)
            moveConstraint = RigidbodyConstraints.FreezeAll & ~RigidbodyConstraints.FreezePositionX;
        else if (dotY >= dotX && dotY >= dotZ)
            moveConstraint = RigidbodyConstraints.FreezeAll & ~RigidbodyConstraints.FreezePositionY;
        else
            moveConstraint = RigidbodyConstraints.FreezeAll & ~RigidbodyConstraints.FreezePositionZ;

        return moveConstraint;
    }
}