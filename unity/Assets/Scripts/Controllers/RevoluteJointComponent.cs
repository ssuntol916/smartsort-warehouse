// ============================================================
// 파일명  : RevoluteJointComponent.cs
// 역할    : RevoluteJoint 를 씬 오브젝트에 연결하는 MonoBehaviour 컴포넌트
// 작성자  : 이현화
// 작성일  : 2026-04-01
// 수정이력: 2026-04-22 - _lineAPointA/B, _lineBPointA/B 추가 (ConstraintAssemblyWindow 에서 저장)
//                      - InitializeChild() 에서 저장된 점 좌표로 Line 생성하도록 변경
//                      - OnValidateJoint() 에서 저장된 점 좌표로 Line 생성하도록 변경
//                      - ObjectBRotationAxis, LineBCenter 프로퍼티 추가
//                      - OnClampedCallback 추가 (클램프 후 동기화용)
//                      - OnConstrained() 에서 _objectBRotationAxis 매 프레임 갱신 제거
//                        (회전축 불안정으로 인한 각도 계산 오류 수정)
//                      - Rigidbody 제거, Transform 직접 제어 방식으로 리팩토링
//                        (디지털 트윈 특성상 물리 엔진 불필요)
//                      - _rotationAxis 제거 (미사용 필드, _lineBPointA/B 로 대체됨)
//                      - _useClamp 추가 (클램프 사용 여부 선택 가능, 기본값 true)
// ============================================================

using UnityEngine;

public class RevoluteJointComponent : JointComponent
{
    [SerializeField] private float _minAngle = -90f;                    // 최소 회전 각도 - 오브젝트 B 초기 회전 상태 기준 (degree)
    [SerializeField] private float _maxAngle = 90f;                     // 최대 회전 각도 - 오브젝트 B 초기 회전 상태 기준 (degree)
    [SerializeField] private bool _useClamp = true;                     // 클램프 사용 여부 (false 면 무한 회전 가능)

    // [2026.04.22 추가] ConstraintAssemblyWindow 에서 저장된 회전축 Line 점 좌표
    [SerializeField] private Vector3 _lineAPointA;  // Object A 회전축 Line 시작점
    [SerializeField] private Vector3 _lineAPointB;  // Object A 회전축 Line 끝점
    [SerializeField] private Vector3 _lineBPointA;  // Object B 회전축 Line 시작점
    [SerializeField] private Vector3 _lineBPointB;  // Object B 회전축 Line 끝점

    private RevoluteJoint _joint;           // RevoluteJoint.cs 인스턴스
    private Vector3 _initialDirection;      // 초기 기준 방향 (각도 측정용)
    private Quaternion _initialRotation;    // 초기 회전값 (최종 회전 적용용)
    private Vector3 _objectBRotationAxis;   // 오브젝트 B 기준 회전축

    public float CurrentAngle => _joint?.CurrentAngle ?? 0f;        // 현재 회전 각도
    public Vector3 ObjectBRotationAxis => _objectBRotationAxis;     // [2026.04.22 추가] 오브젝트 B 기준 회전축
    public Vector3 LineBCenter => _lineBPointA;                     // [2026.04.22 추가] lineB 시작점 (위치 보정 기준)

    // [2026.04.22 추가] 클램프 후 콜백 (RevoluteFreezeTestManager 동기화용)
    public System.Action<Quaternion> OnClampedCallback;

    /**
     * @brief  회전축 Line 을 생성하고 RevoluteJoint 를 초기화한다.
     *         초기 기준 방향(_initialDirection) 과 초기 회전값(_initialRotation) 을 저장한다.
     */
    protected override void InitializeChild()
    {
        // [2026.04.22 수정] 저장된 점 좌표로 Line 생성
        Line lineA = new Line(_lineAPointA, _lineAPointB);
        Line lineB = new Line(_lineBPointA, _lineBPointB);

        // [2026.04.22 수정] lineB 방향으로 초기 회전축 설정
        _objectBRotationAxis = lineB.Direction;

        // RevoluteJoint.cs 인스턴스 생성
        _joint = new RevoluteJoint(lineA, lineB, _minAngle, _maxAngle);

        // 초기 회전값 저장 (클램프 계산의 기준점)
        _initialRotation = _objectB.rotation;

        // [2026.04.22 수정] 회전축과 수직인 벡터를 월드 좌표로 구한다
        Vector3 perpendicular = Vector3.Cross(_objectBRotationAxis, Vector3.forward);

        // [2026.04.22 수정] 회전축과 forward 가 평행할 때 forward 대신 right 로 대체
        if (perpendicular.magnitude < ApplyTolerance)
            perpendicular = Vector3.Cross(_objectBRotationAxis, Vector3.right);

        // 초기 기준 방향 저장 (각도 측정의 기준점)
        _initialDirection = perpendicular.normalized;
    }

    /**
     * @brief  RevoluteJoint 유효성 검사.
     * @return bool  유효하면 true
     */
    protected override bool IsJointValid()
    {
        if (_joint == null) return false;
        return _joint.IsValid();
    }

    /**
     * @brief  RevoluteJoint 구속 조건 적용.
     * @return bool  구속 중이면 true
     */
    protected override bool ApplyJointConstraint()
    {
        return _joint.ApplyConstraint();
    }

    //TODO: 추후에 다시 확인 - 프리즈 대신 '구속 조건이 풀리면 → 다시 구속 위치로 이동' 구현 예정
    //      현재는 구속 조건 확인을 위한 임시 프리즈 처리
    /**
     * @brief  구속 상태일 때 오브젝트 B 의 회전을 클램프하여 적용한다.
     *         _useClamp 가 false 이면 클램프 없이 각도 상태값만 동기화한다.
     *         Transform 직접 제어로 회전을 적용한다.
     */
    protected override void OnConstrained()
    {
        // [2026.04.22 수정] _objectBRotationAxis 매 프레임 갱신 제거
        // InitializeChild() 에서 한 번만 설정한 값을 그대로 사용
        Quaternion deltaRotation = _objectB.rotation * Quaternion.Inverse(_initialRotation);
        Vector3 currentDirection = (deltaRotation * _initialDirection).normalized;  // 현재 방향 벡터

        float currentAngle = RevoluteJoint.GetCurrentAngle(
            _initialDirection, currentDirection, _objectBRotationAxis);  // 현재 회전 각도

        // [2026.04.22 추가] 클램프 비활성화 시 각도 상태값만 동기화하고 회전은 건드리지 않음
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
            _objectB.rotation = clampedRot;  // Transform 직접 제어
            OnClampedCallback?.Invoke(clampedRot);
        }
        else
        {
            _joint.SetAngle(currentAngle);  // 범위 내에 있을 때 각도 상태값 동기화
        }
    }

    /**
     * @brief  외부에서 회전 각도를 설정한다.
     *         ShuttleController.cs 에서 호출한다.
     * @param  angle    설정할 회전 각도 (degree)
     */
    public void SetAngle(float angle)
    {
        if (_joint == null) return;

        _joint.SetAngle(angle);  // 내부 상태값 업데이트 (min/max 클램프 처리됨)

        // [2026.04.22 수정] MoveRotation → Transform 직접 제어
        _objectB.rotation = Quaternion.AngleAxis(_joint.CurrentAngle, _objectBRotationAxis) * _initialRotation;
    }

    /**
     * @brief  Inspector 에서 값 변경 시 Line 을 재생성하고 RevoluteJoint 를 재초기화한다.
     *         ※ Play 모드에서는 실행되지 않는다.
     */
    protected override void OnValidateJoint()
    {
        // [2026.04.22 수정] 저장된 점 좌표로 Line 생성
        if (_lineAPointA == Vector3.zero && _lineAPointB == Vector3.zero) return;

        Line lineA = new Line(_lineAPointA, _lineAPointB);
        Line lineB = new Line(_lineBPointA, _lineBPointB);
        _joint = new RevoluteJoint(lineA, lineB, _minAngle, _maxAngle);
    }
}