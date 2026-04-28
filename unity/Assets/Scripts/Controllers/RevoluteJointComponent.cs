// ============================================================
// 파일명  : RevoluteJointComponent.cs
// 역할    : RevoluteJoint 를 씬 오브젝트에 연결하는 MonoBehaviour 컴포넌트.
//           회전축(Line) 을 기반으로 RevoluteJoint 를 초기화하고
//           외부에서 SetAngle() 로 회전 각도를 직접 제어한다.
//           셔틀이 이동해도 부모 기준 로컬 좌표(_localLineBCenter) 를
//           월드 좌표로 역변환하여 LineBCenter 를 올바르게 유지한다.
//
// 작성자  : 이현화
// 작성일  : 2026-04-01
// 수정이력: 2026-04-27 - _lineAPointA/B, _lineBPointA/B 추가
//                        ObjectBRotationAxis, LineBCenter 프로퍼티 추가
//                        OnClampedCallback 추가
//                        _useClamp 추가 (기본값 true)
//                        Rigidbody 제거, Transform 직접 제어 방식으로 리팩토링
//                        _localLineBCenter 추가 (부모 기준 로컬 좌표)
//                        LineBCenter 프로퍼티를 로컬 좌표 기반으로 변경
// ============================================================

using UnityEngine;

public class RevoluteJointComponent : JointComponent
{
    // ============================================================
    // Inspector 필드
    // ============================================================

    [Header("회전 범위")]
    [SerializeField] private float _minAngle = -90f; // 최소 회전 각도 (degree, ObjectB 초기 회전 기준)
    [SerializeField] private float _maxAngle = 90f; // 최대 회전 각도 (degree, ObjectB 초기 회전 기준)
    [SerializeField] private bool _useClamp = true; // 클램프 사용 여부 (false 면 무한 회전)

    [Header("회전축 Line 좌표 (ConstraintAssemblyWindow 저장값)")]
    [SerializeField] private Vector3 _lineAPointA; // ObjectA 회전축 시작점
    [SerializeField] private Vector3 _lineAPointB; // ObjectA 회전축 끝점
    [SerializeField] private Vector3 _lineBPointA; // ObjectB 회전축 시작점 (LineBCenter 기준)
    [SerializeField] private Vector3 _lineBPointB; // ObjectB 회전축 끝점

    // ============================================================
    // 런타임 상태
    // ============================================================

    private RevoluteJoint _joint;
    private Vector3 _initialDirection;    // 초기 기준 방향 (각도 측정 기준점)
    private Quaternion _initialRotation;     // 초기 회전값 (최종 회전 적용 기준점)
    private Vector3 _objectBRotationAxis; // ObjectB 기준 회전축 방향
    private Vector3 _localLineBCenter;    // 부모 기준 로컬 LineBCenter (셔틀 이동 추적용)

    // ============================================================
    // 프로퍼티
    // ============================================================

    public float CurrentAngle => _joint?.CurrentAngle ?? 0f;
    public Vector3 ObjectBRotationAxis => _objectBRotationAxis;

    /// 셔틀이 이동해도 부모→월드 변환으로 항상 올바른 LineBCenter 를 반환한다.
    public Vector3 LineBCenter => _objectB.parent != null
        ? _objectB.parent.TransformPoint(_localLineBCenter)
        : _localLineBCenter;

    // ============================================================
    // 이벤트
    // ============================================================

    /// 클램프 발생 시 외부로 보정된 회전값을 전달한다. (SpurGearController 동기화용)
    public System.Action<Quaternion> OnClampedCallback;

    // ============================================================
    // JointComponent 구현
    // ============================================================

    /**
     * @brief  회전축 Line 으로 RevoluteJoint 를 초기화한다.
     *         초기 회전값과 기준 방향을 저장하고
     *         LineBCenter 를 부모 기준 로컬 좌표로 저장하여
     *         셔틀 이동 후에도 올바른 위치를 반환할 수 있게 한다.
     */
    protected override void InitializeChild()
    {
        Line lineA = new Line(_lineAPointA, _lineAPointB);
        Line lineB = new Line(_lineBPointA, _lineBPointB);

        _objectBRotationAxis = lineB.Direction;
        _joint = new RevoluteJoint(lineA, lineB, _minAngle, _maxAngle);
        _initialRotation = _objectB.rotation;

        _localLineBCenter = _objectB.parent != null
            ? _objectB.parent.InverseTransformPoint(_lineBPointA)
            : _lineBPointA;

        // 회전축과 수직인 초기 기준 방향 벡터 계산
        // forward 와 평행할 경우 right 로 대체
        Vector3 perpendicular = Vector3.Cross(_objectBRotationAxis, Vector3.forward);
        if (perpendicular.magnitude < ApplyTolerance)
            perpendicular = Vector3.Cross(_objectBRotationAxis, Vector3.right);

        _initialDirection = perpendicular.normalized;
    }

    /**
     * @brief  RevoluteJoint 유효성 검사.
     * @return bool  유효하면 true
     */
    protected override bool IsJointValid()
    {
        return _joint != null && _joint.IsValid();
    }

    /**
     * @brief  RevoluteJoint 구속 조건을 적용한다.
     * @return bool  구속 중이면 true
     */
    protected override bool ApplyJointConstraint()
    {
        return _joint.ApplyConstraint();
    }

    /**
     * @brief  구속 상태일 때 ObjectB 의 회전을 클램프하여 적용한다.
     *         _useClamp=false 이면 클램프 없이 각도 상태값만 동기화한다.
     *         범위를 초과한 경우 GetClampedRotation() 으로 보정하고
     *         OnClampedCallback 이벤트를 발행한다.
     */
    protected override void OnConstrained()
    {
        Quaternion deltaRotation = _objectB.rotation * Quaternion.Inverse(_initialRotation);
        Vector3 currentDirection = (deltaRotation * _initialDirection).normalized;
        float currentAngle = RevoluteJoint.GetCurrentAngle(
            _initialDirection, currentDirection, _objectBRotationAxis);

        if (!_useClamp)
        {
            _joint.SetAngle(currentAngle);
            return;
        }

        if (currentAngle < _minAngle || currentAngle > _maxAngle)
        {
            // [2026.04.22 수정] MoveRotation → Transform 직접 제어
            Quaternion clampedRot = _joint.GetClampedRotation(
                _initialDirection, currentDirection, _objectBRotationAxis, _initialRotation);
            _objectB.rotation = clampedRot;
            OnClampedCallback?.Invoke(clampedRot);
        }
        else
        {
            _joint.SetAngle(currentAngle);
        }
    }

    /**
     * @brief  Inspector 값 변경 시 RevoluteJoint 를 재초기화한다.
     *         Play 모드에서는 실행되지 않는다.
     *         Line 좌표가 미설정(zero) 이면 초기화를 건너뛴다.
     */
    protected override void OnValidateJoint()
    {
        if (_lineAPointA == Vector3.zero && _lineAPointB == Vector3.zero) return;

        Line lineA = new Line(_lineAPointA, _lineAPointB);
        Line lineB = new Line(_lineBPointA, _lineBPointB);
        _joint = new RevoluteJoint(lineA, lineB, _minAngle, _maxAngle);
    }

    // ============================================================
    // 공개 메서드
    // ============================================================

    /**
     * @brief  회전 각도를 외부에서 직접 설정한다.
     *         내부 상태값 업데이트 후 ObjectB 의 rotation 을 갱신한다.
     * @param  angle  설정할 회전 각도 (degree)
     */
    public void SetAngle(float angle)
    {
        if (_joint == null) return;

        _joint.SetAngle(angle);
        _objectB.rotation = Quaternion.AngleAxis(_joint.CurrentAngle, _objectBRotationAxis) * _initialRotation;
    }
}