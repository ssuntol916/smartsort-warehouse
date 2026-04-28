// ============================================================
// 파일명  : SliderJointComponent.cs
// 역할    : SliderJoint 를 씬 오브젝트에 연결하는 MonoBehaviour 컴포넌트.
//           이동축(Line)과 기준면(Plane) 을 기반으로 SliderJoint 를 초기화하고
//           외부에서 SetPosition() 으로 슬라이더 위치를 직접 제어한다.
//           셔틀이 X/Z 축으로 이동해도 부모 기준 로컬 좌표(_localInitialPosition) 를
//           월드 좌표로 역변환하여 기준점을 올바르게 유지한다.
//
// 작성자  : 이현화
// 작성일  : 2026-04-01
// 수정이력: 2026-04-27 - _lineAPointA/B, _lineBPointA/B 추가
//                        _planeAPointA/B/C, _planeBPointA/B/C 추가
//                        _initialPosition 추가 (SetPosition 기준점)
//                        Rigidbody 제거, Transform 직접 제어 방식으로 리팩토링
//                        _localInitialPosition 추가 (부모 기준 로컬 좌표)
//                        _worldInitialY 제거
// ============================================================

using UnityEngine;

public class SliderJointComponent : JointComponent
{
    // ============================================================
    // Inspector 필드
    // ============================================================

    [Header("이동 범위")]
    [SerializeField] private float _minPosition = 0f;             // 최소 이동 범위 (m, ObjectA 기준)
    [SerializeField] private float _maxPosition = 100f;           // 최대 이동 범위 (m, ObjectA 기준)
    [SerializeField] private Vector3 _moveDirection = Vector3.right;  // 슬라이더 이동 방향

    [Header("이동축 Line 좌표 (ConstraintAssemblyWindow 저장값)")]
    [SerializeField] private Vector3 _lineAPointA; // ObjectA 이동축 시작점
    [SerializeField] private Vector3 _lineAPointB; // ObjectA 이동축 끝점
    [SerializeField] private Vector3 _lineBPointA; // ObjectB 이동축 시작점
    [SerializeField] private Vector3 _lineBPointB; // ObjectB 이동축 끝점

    [Header("기준 Plane 좌표 (ConstraintAssemblyWindow 저장값)")]
    [SerializeField] private Vector3 _planeAPointA; // ObjectA 기준 Plane 점 A
    [SerializeField] private Vector3 _planeAPointB; // ObjectA 기준 Plane 점 B
    [SerializeField] private Vector3 _planeAPointC; // ObjectA 기준 Plane 점 C
    [SerializeField] private Vector3 _planeBPointA; // ObjectB 기준 Plane 점 A
    [SerializeField] private Vector3 _planeBPointB; // ObjectB 기준 Plane 점 B
    [SerializeField] private Vector3 _planeBPointC; // ObjectB 기준 Plane 점 C

    // ============================================================
    // 런타임 상태
    // ============================================================

    private SliderJoint _joint;
    private Vector3 _initialPosition;      // Awake 시점 월드 좌표 (하위 호환용)
    private Vector3 _localInitialPosition; // 부모 기준 로컬 초기 좌표 (셔틀 이동 추적용)

    // ============================================================
    // 프로퍼티
    // ============================================================

    public float CurrentPosition => _joint?.CurrentPosition ?? 0f;
    public Vector3 MoveDirection => _moveDirection;

    // ============================================================
    // JointComponent 구현
    // ============================================================

    /**
     * @brief  이동 방향 벡터를 정규화하고 SliderJoint 를 초기화한다.
     *         부모 기준 로컬 좌표를 저장하여 셔틀 이동 후에도 기준점을 올바르게 유지한다.
     */
    protected override void InitializeChild()
    {
        _moveDirection = _moveDirection.normalized;

        _initialPosition = _objectB.position;
        _localInitialPosition = _objectB.parent != null
            ? _objectB.parent.InverseTransformPoint(_objectB.position)
            : _objectB.position;

        BuildJoint();

        Debug.Log($"[SliderJoint] 초기화 완료 | moveDirection={_moveDirection}");
    }

    /**
     * @brief  SliderJoint 유효성 검사.
     * @return bool  유효하면 true
     */
    protected override bool IsJointValid()
    {
        return _joint != null && _joint.IsValid();
    }

    /**
     * @brief  SliderJoint 구속 조건을 적용한다.
     * @return bool  구속 중이면 true
     */
    protected override bool ApplyJointConstraint()
    {
        return _joint.ApplyConstraint();
    }

    /**
     * @brief  구속 상태일 때 슬라이더 위치를 범위 내로 제한한다.
     *         부모 로컬 좌표 기반으로 월드 기준점을 계산하여
     *         셔틀 이동 후에도 올바른 위치를 유지한다.
     *         범위를 벗어난 경우에만 클램프를 적용하고,
     *         범위 내에 있을 때는 현재 위치를 상태값에 동기화한다.
     */
    protected override void OnConstrained()
    {
        Vector3 worldInitial = GetWorldInitial();
        float currentPos = Vector3.Dot(_objectB.position - worldInitial, _moveDirection);

        if (currentPos < _minPosition || currentPos > _maxPosition)
            _objectB.position = _joint.GetClampedPosition(_objectB.position, worldInitial, _moveDirection);
        else
            _joint.SetPosition(currentPos);
    }

    /**
     * @brief  Inspector 값 변경 시 SliderJoint 를 재초기화한다.
     *         Play 모드에서는 실행되지 않는다.
     */
    protected override void OnValidateJoint()
    {
        BuildJoint();
    }

    // ============================================================
    // 공개 메서드
    // ============================================================

    /**
     * @brief  슬라이더 위치를 외부에서 직접 설정한다.
     *         부모 로컬 좌표 기반으로 목표 월드 좌표를 계산하므로
     *         셔틀이 X/Z 로 이동해도 기준점이 올바르게 유지된다.
     * @param  position  설정할 슬라이더 위치 (m)
     */
    public void SetPosition(float position)
    {
        if (_joint == null) return;

        _joint.SetPosition(position);

        _objectB.position = GetWorldInitial() + _moveDirection.normalized * _joint.CurrentPosition;
    }

    // ============================================================
    // 내부 메서드
    // ============================================================

    /**
     * @brief  이동축 Line 과 기준 Plane 으로 SliderJoint 인스턴스를 생성한다.
     *         InitializeChild() 와 OnValidateJoint() 에서 공통으로 호출한다.
     */
    private void BuildJoint()
    {
        if (_objectA == null || _objectB == null) return;

        Line lineA = new Line(_lineAPointA, _lineAPointB);
        Line lineB = new Line(_lineBPointA, _lineBPointB);
        Plane planeA = new Plane(_planeAPointA, _planeAPointB, _planeAPointC);
        Plane planeB = new Plane(_planeBPointA, _planeBPointB, _planeBPointC);

        _joint = new SliderJoint(lineA, lineB, planeA, planeB, _minPosition, _maxPosition);
    }

    /**
     * @brief  부모 기준 로컬 초기 좌표를 월드 좌표로 변환하여 반환한다.
     *         부모가 없으면 로컬 좌표를 그대로 반환한다.
     * @return Vector3  현재 프레임 기준 슬라이더 초기 월드 위치
     */
    private Vector3 GetWorldInitial()
    {
        return _objectB.parent != null
            ? _objectB.parent.TransformPoint(_localInitialPosition)
            : _localInitialPosition;
    }
}