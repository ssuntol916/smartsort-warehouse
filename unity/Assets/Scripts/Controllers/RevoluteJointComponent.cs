// ============================================================
// 파일명  : RevoluteJointComponent.cs
// 역할    : RevoluteJoint 를 씬 오브젝트에 연결하는 MonoBehaviour 컴포넌트
// 작성자  : 이현화
// 작성일  : 2026-04-01
// 수정이력: 
// ============================================================

using UnityEngine;

public class RevoluteJointComponent : JointComponent
{
    [SerializeField] private float _minAngle = -90f;                     // 최소 회전 각도 - 오브젝트 B 초기 회전 상태 기준 (degree)
    [SerializeField] private float _maxAngle = 90f;                      // 최대 회전 각도 - 오브젝트 B 초기 회전 상태 기준 (degree)
    [SerializeField] private Vector3 _rotationAxis = Vector3.right;      // 회전축(월드기준)

    private RevoluteJoint _joint;              // RevoluteJoint.cs 인스턴스
    private Vector3 _initialDirection;         // 초기 기준 방향 (각도 측정용)
    private Quaternion _initialRotation;       // 초기 회전값 (최종 회전 적용용)
    private Vector3 _objectBRotationAxis;      // 오브젝트 B 기준 회전축

    public float CurrentAngle => _joint?.CurrentAngle ?? 0f;    // 현재 회전 각도
    public Vector3 ObjectBRotationAxis => _objectBRotationAxis;     // 오브젝트 B 기준 회전축


    /**
     * @brief  회전축 Line 을 생성하고 RevoluteJoint 를 초기화한다.
     *         초기 기준 방향(_initialDirection) 과 초기 회전값(_initialRotation) 을 저장한다.
     */
    protected override void InitializeChild()
    {
        _objectBRotationAxis = _rotationAxis; // 초기 축 설정

        // 오브젝트 A·B 의 Transform 에서 회전축 Line 생성
        Line lineA = new Line(_objectA.position,
                              _objectA.position + _rotationAxis);
        Line lineB = new Line(_objectB.position,
                              _objectB.position + _rotationAxis);

        // RevoluteJoint.cs 인스턴스 생성
        _joint = new RevoluteJoint(lineA, lineB, _minAngle, _maxAngle);

        // 초기 회전값 저장 (클램프 계산의 기준점)
        _initialRotation = _objectB.rotation;

        // 생성한 lineB의 방향을 회전축으로 사용
        Vector3 axis = lineB.Direction;

        // 회전축과 수직인 벡터를 월드 좌표로 구한다
        Vector3 perpendicular = Vector3.Cross(axis, Vector3.forward);

        // 회전축과 forward 가 평행할 때 forward 대신 right 로 대체
        if (perpendicular.magnitude < ApplyTolerance)
        {
            perpendicular = Vector3.Cross(axis, Vector3.right);
        }

        // 초기 기준 방향을 월드 좌표로 저장 (오브젝트 회전 적용 없이)
        // 이 벡터는 각도 측정의 기준점이 된다
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
     */
    protected override void OnConstrained()
    {
        // 구속 상태가 확인된 이후에는 실제로 생성된 lineB의 방향을 회전축으로 사용한다
        // RevoluteJoint.Axis는 lineB가 존재하면 그 방향을 반환하므로 이를 우선 사용
        _objectBRotationAxis = _joint != null ? _joint.Axis : (_objectA.rotation * _rotationAxis);

        // 순수 회전량 계산: 초기 회전의 역회전 * 현재 회전
        // 이렇게 하면 초기 상태 대비 얼마나 회전했는지만 알 수 있다
        Quaternion deltaRotation = _objectB.rotation * Quaternion.Inverse(_initialRotation);

        // 현재 방향: 초기 기준 방향을 순수 회전량만큼 회전시킨 결과
        Vector3 currentDirection = (deltaRotation * _initialDirection).normalized;

        // 현재 회전 각도를 계산한다
        float currentAngle = RevoluteJoint.GetCurrentAngle(_initialDirection, currentDirection, _objectBRotationAxis);

        // 클램프가 필요한 경우만 적용 (min/max 범위 밖일 때만)
        // 범위 안에 있으면 회전을 건드리지 않아 덜덜거림을 방지한다
        if (currentAngle < _minAngle || currentAngle > _maxAngle)
        {
            Quaternion clampedRot = _joint.GetClampedRotation(
                _initialDirection, currentDirection, _objectBRotationAxis, _initialRotation);
            _rigidbodyB.MoveRotation(clampedRot);
        }
    }

    /**
     * @brief  외부에서 회전 각도를 설정한다.
     *         ShuttleController.cs 에서 임펠러 각도 제어 시 호출한다.
     *         Rigidbody.MoveRotation()과 Transform.rotation을 즉시 동기화하여
     *         Update() → OnConstrained() 실행 시 타이밍 충돌을 방지하고
     *         프레임 지연 없이 즉각적인 회전 제어를 수행한다.
     * @param  angle    설정할 회전 각도 (degree)
     */
    public void SetAngle(float angle)
    {
        if (_joint == null || _rigidbodyB == null) return;

        // 로직 클래스의 상태값을 먼저 업데이트 (내부에서 min/max 클램프 처리됨)
        _joint.SetAngle(angle);

        // 클램프된 결과값(_joint.CurrentAngle)을 바탕으로 목표 회전값 계산
        Quaternion targetRotation = Quaternion.AngleAxis(_joint.CurrentAngle, _objectBRotationAxis) * _initialRotation;

        // 물리 회전 명령 및 Transform 즉시 동기화
        // Transform을 즉시 반영하여 다음 Update/FixedUpdate에서
        // OnConstrained()가 이전 회전을 기준으로 재계산하는 문제를 방지
        _rigidbodyB.MoveRotation(targetRotation);
        _objectB.rotation = targetRotation;
    }

    /**
     * @brief  Inspector 에서 값 변경 시 Line 을 재생성하고 RevoluteJoint 를 재초기화한다.
     *         ※ Play 모드에서는 실행되지 않는다.
     */
    protected override void OnValidateJoint()
    {
        // 오브젝트 A·B 의 Transform 에서 회전축 Line 생성
        Line lineA = new Line(_objectA.position,
                              _objectA.position + _rotationAxis);
        Line lineB = new Line(_objectB.position,
                              _objectB.position + _rotationAxis);

        _joint = new RevoluteJoint(lineA, lineB, _minAngle, _maxAngle);
    }

    /**
     * @brief  회전 축을 감지하여 해당 축의 회전만 열어두고
     *         나머지 이동 및 회전을 프리즈한 RigidbodyConstraints 를 반환한다.
     * @return RigidbodyConstraints  회전축의 회전만 열어둔 프리즈 조건
     */
    protected override RigidbodyConstraints GetFreezeConstraintByDirection()
    {
        // 지정된 회전축 방향과 각 축의 유사도(내적) 계산
        // Dot 결과값의 절댓값이 클수록 해당 축과 방향이 일치함을 의미
        // ex) _rotationAxis = (1,0,0) 이면 dotX = 1.0, dotY = 0.0, dotZ = 0.0
        float dotX = Mathf.Abs(Vector3.Dot(_objectBRotationAxis, Vector3.right));
        float dotY = Mathf.Abs(Vector3.Dot(_objectBRotationAxis, Vector3.up));
        float dotZ = Mathf.Abs(Vector3.Dot(_objectBRotationAxis, Vector3.forward));

        // 세 축 중 Dot 값이 가장 큰 축이 실제 회전축
        // FreezeAll(모두 고정) 상태에서 해당 축의 회전(FreezeRotation) 제한만 해제
        if (dotX >= dotY && dotX >= dotZ)
            return RigidbodyConstraints.FreezeAll & ~RigidbodyConstraints.FreezeRotationX;
        else if (dotY >= dotX && dotY >= dotZ)
            return RigidbodyConstraints.FreezeAll & ~RigidbodyConstraints.FreezeRotationY;
        else
            return RigidbodyConstraints.FreezeAll & ~RigidbodyConstraints.FreezeRotationZ;
    }
}