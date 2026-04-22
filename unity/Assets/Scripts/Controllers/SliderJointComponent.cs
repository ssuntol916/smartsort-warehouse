// ============================================================
// 파일명  : SliderJointComponent.cs
// 역할    : SliderJoint 를 씬 오브젝트에 연결하는 MonoBehaviour 컴포넌트
// 작성자  : 이현화
// 작성일  : 2026-04-01
// 수정이력: 2026-04-22 - _lineAPointA/B, _lineBPointA/B 추가 (ConstraintAssemblyWindow 에서 저장)
//                      - _planeAPointA/B/C, _planeBPointA/B/C 추가 (ConstraintAssemblyWindow 에서 저장)
//                      - InitializeJoint() 에서 저장된 점 좌표로 Line, Plane 생성하도록 변경
//                      - _initialPosition 추가 (SetPosition 기준점으로 사용)
//                      - SetPosition() 에서 _objectA.position 대신 _initialPosition 사용
//                      - OnConstrained() 기준점을 _initialPosition 으로 통일
//                        (SetPosition() 과 기준점 불일치로 인한 위치 누적 오차 수정)
//                      - Rigidbody 제거, Transform 직접 제어 방식으로 리팩토링
//                        (디지털 트윈 특성상 물리 엔진 불필요)
// ============================================================

using UnityEngine;

public class SliderJointComponent : JointComponent
{
    [SerializeField] private float _minPosition = 0f;                    // 최소 이동 범위 - 오브젝트 A 위치 기준 절대값 (mm)
    [SerializeField] private float _maxPosition = 100f;                  // 최대 이동 범위 - 오브젝트 A 위치 기준 절대값 (mm)
    [SerializeField] private Vector3 _moveDirection = Vector3.right;     // 슬라이더 이동 방향

    // [2026.04.22 추가] ConstraintAssemblyWindow 에서 저장된 이동축 Line 점 좌표
    [SerializeField] private Vector3 _lineAPointA;  // Object A 이동축 Line 시작점
    [SerializeField] private Vector3 _lineAPointB;  // Object A 이동축 Line 끝점
    [SerializeField] private Vector3 _lineBPointA;  // Object B 이동축 Line 시작점
    [SerializeField] private Vector3 _lineBPointB;  // Object B 이동축 Line 끝점

    // [2026.04.22 추가] ConstraintAssemblyWindow 에서 저장된 기준 Plane 점 좌표
    [SerializeField] private Vector3 _planeAPointA;  // Object A 기준 Plane 점 A
    [SerializeField] private Vector3 _planeAPointB;  // Object A 기준 Plane 점 B
    [SerializeField] private Vector3 _planeAPointC;  // Object A 기준 Plane 점 C
    [SerializeField] private Vector3 _planeBPointA;  // Object B 기준 Plane 점 A
    [SerializeField] private Vector3 _planeBPointB;  // Object B 기준 Plane 점 B
    [SerializeField] private Vector3 _planeBPointC;  // Object B 기준 Plane 점 C

    private SliderJoint _joint;             // SliderJoint.cs 인스턴스
    private Vector3 _initialPosition;       // [2026.04.22 추가] 오브젝트 B 초기 위치 (SetPosition 기준점)

    public float CurrentPosition => _joint?.CurrentPosition ?? 0f;   // 현재 슬라이더 위치
    public Vector3 MoveDirection => _moveDirection;                   // 슬라이더 이동 방향

    /**
     * @brief  이동 방향 벡터 정규화 후 SliderJoint 를 초기화한다.
     */
    protected override void InitializeChild()
    {
        // 이동 방향 벡터 정규화 (GetProjectedDistance 거리 계산 및 위치 계산 정확도 보장)
        _moveDirection = _moveDirection.normalized;

        // [2026.04.22 추가] 오브젝트 B 초기 위치 저장 (SetPosition 기준점)
        _initialPosition = _objectB.position;

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
     *         범위 내에 있을 때는 _initialPosition 기준 현재 위치를 상태값에 반영한다.
     *         SetPosition() 과 동일한 기준점(_initialPosition) 을 사용하여
     *         _joint.CurrentPosition 누적 오차를 방지한다.
     */
    protected override void OnConstrained()
    {
        // [2026.04.22 수정] _initialPosition 기준으로 현재 위치 계산
        // SetPosition() 과 동일한 기준점을 사용하여 _joint.CurrentPosition 일관성 유지
        float currentPos = Vector3.Dot(_objectB.position - _initialPosition, _moveDirection);

        // 범위를 벗어났을 때만 클램프 적용
        // 범위 내에 있을 때는 _currentPosition 을 직접 측정값으로 동기화하여 떨림 방지
        if (currentPos < _minPosition || currentPos > _maxPosition)
        {
            // [2026.04.22 수정] _initialPosition 기준으로 클램프 위치 계산 (SetPosition() 과 기준점 통일)
            // [2026.04.22 수정] MovePosition → Transform 직접 제어
            _objectB.position = _joint.GetClampedPosition(_objectB.position, _initialPosition, _moveDirection);
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
     *         프레임 지연 없이 즉각적인 위치 제어를 수행한다.
     * @param  position    설정할 슬라이더 위치 (m)
     */
    public void SetPosition(float position)
    {
        if (_joint == null) return;

        // 로직 클래스의 상태값을 먼저 업데이트 (내부에서 min/max 클램프 처리됨)
        _joint.SetPosition(position);

        // [2026.04.22 수정] _initialPosition 기준으로 목표 월드 좌표 계산
        // OnConstrained() 와 동일한 기준점(_initialPosition) 을 사용하여
        // _joint.CurrentPosition 누적 오차 방지
        // [2026.04.22 수정] MovePosition → Transform 직접 제어
        _objectB.position = _initialPosition + (_moveDirection.normalized * _joint.CurrentPosition);
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

        // [2026.04.22 수정] 저장된 점 좌표로 Line 생성
        Line lineA = new Line(_lineAPointA, _lineAPointB);
        Line lineB = new Line(_lineBPointA, _lineBPointB);

        // [2026.04.22 수정] 저장된 점 좌표로 Plane 생성
        Plane planeA = new Plane(_planeAPointA, _planeAPointB, _planeAPointC);
        Plane planeB = new Plane(_planeBPointA, _planeBPointB, _planeBPointC);

        // SliderJoint.cs 인스턴스 생성
        _joint = new SliderJoint(lineA, lineB, planeA, planeB, _minPosition, _maxPosition);
    }
}