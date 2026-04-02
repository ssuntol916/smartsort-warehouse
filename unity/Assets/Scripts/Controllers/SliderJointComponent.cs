// ============================================================
// 파일명  : SliderJointComponent.cs
// 역할    : SliderJoint 를 씬 오브젝트에 연결하는 MonoBehaviour 컴포넌트
// 작성자  : 이현화
// 작성일  : 2026-04-01
// 수정이력: 
// ============================================================

using UnityEngine;

public class SliderJointComponent : JointComponent
{
    [SerializeField] private float _minPosition = 0f;                    // 최소 이동 범위 - 오브젝트 A 위치 기준 절대값 (mm)
    [SerializeField] private float _maxPosition = 100f;                  // 최대 이동 범위 - 오브젝트 A 위치 기준 절대값 (mm)
    [SerializeField] private Vector3 _moveDirection = Vector3.right;     // 슬라이더 이동 방향

    private SliderJoint _joint;          // SliderJoint.cs 인스턴스

    public float CurrentPosition => _joint?.CurrentPosition ?? 0f;   // 현재 슬라이더 위치
    public Vector3 MoveDirection => _moveDirection;                  // 슬라이더 이동 방향

    /**
     * @brief  이동 방향 벡터 정규화 후 SliderJoint 를 초기화한다.
     */
    protected override void InitializeChild()
    {
        // 이동 방향 벡터 정규화 (GetProjectedDistance 거리 계산 및 위치 계산 정확도 보장)
        _moveDirection = _moveDirection.normalized;

        InitializeJoint();

        Debug.Log($"SliderJointComponent: 이동축이 {_moveDirection} 으로 설정되었습니다. \nInspector 에서도 설정된 이동축 확인이 가능합니다.");
    }

    /**
     * @brief  SliderJoint 유효성 검사.
     * @return bool  유효하면 true
     */
    protected override bool IsJointValid()
    {
        if (_joint == null) return false;

        return _joint.IsValid();
    }

    /**
     * @brief  SliderJoint 구속 조건 적용.
     * @return bool  구속 중이면 true
     */
    protected override bool ApplyJointConstraint()
    {
        return _joint.ApplyConstraint();
    }

    //TODO: 추후에 다시 확인 - 프리즈 대신 '구속 조건이 풀리면 → 다시 구속 위치로 이동' 구현 예정
    //      현재는 구속 조건 확인을 위한 임시 프리즈 처리
    /**
     * @brief  구속 상태일 때 슬라이더 위치를 제한한다.
     *         범위를 벗어난 경우에만 클램프하여 강제 이동시키고,
     *         범위 내에 있을 때는 프리즈된 축 내에서 자유롭게 움직이도록 로직 상태만 동기화한다.
     */
    protected override void OnConstrained()
    {
        // 현재 물리적 위치에서의 거리를 로컬로 계산
        float currentPos = Vector3.Dot(_objectB.position - _objectA.position, _moveDirection);

        // 범위를 벗어났을 때만 클램프 적용
        // 범위 내에 있을 때는 _currentPosition을 직접 측정값으로 동기화하여 떨림 방지
        if (currentPos < _minPosition || currentPos > _maxPosition)
        {
            Vector3 clampedPos = _joint.GetClampedPosition(_objectB.position, _objectA.position, _moveDirection);
            _rigidbodyB.MovePosition(clampedPos);
        }
        else
        {
            // 범위 내에 있을 때는 단순히 현재 위치를 상태값에 반영
            _joint.SetPosition(currentPos);
        }
    }

    /**
     * @brief  외부에서 슬라이더 위치를 설정한다.
     *         ShuttleController.cs 에서 X·Y 바퀴 위치 제어 시 호출한다.
     *         Rigidbody.MovePosition()과 Transform.position을 즉시 동기화하여
     *         Update() → OnConstrained() 실행 시 타이밍 충돌을 방지하고
     *         프레임 지연 없이 즉각적인 위치 제어를 수행한다.
     * @param  position    설정할 슬라이더 위치 (mm)
     */
    public void SetPosition(float position)
    {
        if (_joint == null || _rigidbodyB == null) return;

        // 로직 클래스의 상태값을 먼저 업데이트 (내부에서 min/max 클램프 처리됨)
        _joint.SetPosition(position);

        // 클램프된 결과값(_joint.CurrentPosition)을 바탕으로 목표 월드 좌표 계산
        Vector3 targetWorldPos = _objectA.position + (_moveDirection.normalized * _joint.CurrentPosition);

        // 물리 이동 명령 및 Transform 즉시 동기화
        // Transform을 즉시 반영하여 다음 Update/FixedUpdate에서
        // OnConstrained()가 이전 위치를 기준으로 재계산하는 문제를 방지
        _rigidbodyB.MovePosition(targetWorldPos);
        _objectB.position = targetWorldPos;
    }

    /**
     * @brief  Inspector 에서 값 변경 시 Line·Plane 을 재생성하고 SliderJoint 를 재초기화한다.
     *         ※ Play 모드에서는 실행되지 않는다.
     */
    protected override void OnValidateJoint()
    {
        InitializeJoint();
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
        // _objectA.right, _objectA.forward 로 면의 세 점 정의
        Plane planeA = new Plane(_objectA.position,
                                 _objectA.position + _objectA.right,
                                 _objectA.position + _objectA.forward);
        Plane planeB = new Plane(_objectB.position,
                                 _objectB.position + _objectB.right,
                                 _objectB.position + _objectB.forward);

        // SliderJoint.cs 인스턴스 생성
        _joint = new SliderJoint(lineA, lineB,
                                 planeA, planeB,
                                 _minPosition, _maxPosition);
    }

    /**
     * @brief  슬라이더 이동 방향 축을 감지하여 해당 축만 열어두고
     *         나머지 이동 및 회전을 프리즈한 RigidbodyConstraints 를 반환한다.
     * @return RigidbodyConstraints  이동 방향 축만 열어둔 프리즈 조건
     */
    protected override RigidbodyConstraints GetFreezeConstraintByDirection()
    {
        // 이동 방향과 각 축의 유사도 계산
        // Dot 결과값이 클수록 해당 축과 방향이 일치함을 의미
        // ex) _moveDirection = (1,0,0) 이면 dotX = 1.0, dotY = 0.0, dotZ = 0.0
        float dotX = Mathf.Abs(Vector3.Dot(_moveDirection, Vector3.right));
        float dotY = Mathf.Abs(Vector3.Dot(_moveDirection, Vector3.up));
        float dotZ = Mathf.Abs(Vector3.Dot(_moveDirection, Vector3.forward));

        // 세 축 중 Dot 값이 가장 큰 축이 이동 방향 축
        // 해당 축의 FreezePosition 만 제외하고 나머지는 전부 프리즈
        if (dotX >= dotY && dotX >= dotZ)
            return RigidbodyConstraints.FreezeAll & ~RigidbodyConstraints.FreezePositionX;
        else if (dotY >= dotX && dotY >= dotZ)
            return RigidbodyConstraints.FreezeAll & ~RigidbodyConstraints.FreezePositionY;
        else
            return RigidbodyConstraints.FreezeAll & ~RigidbodyConstraints.FreezePositionZ;
    }
}