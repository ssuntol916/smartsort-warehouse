// ============================================================
// 파일명  : RevoluteJoint.cs
// 역할    : 회전 조인트 클래스
// 작성자  : 이현화
// 작성일  : 2026-04-01
// 수정이력: 
// ============================================================

using UnityEngine;

public class RevoluteJoint : Joint
{
    private Line _lineA;                 // 오브젝트 A 의 회전축 Line
    private Line _lineB;                 // 오브젝트 B 의 회전축 Line
    private float _currentAngle;         // 현재 회전 각도 (degree)
    private float _minAngle;             // 최소 회전 각도 (degree)
    private float _maxAngle;             // 최대 회전 각도 (degree)

    private bool _isLineConstrained;     // lineA ↔ lineB 구속 등록됐는지

    public float CurrentAngle => _currentAngle;            // 현재 회전 각도
    public float MinAngle => _minAngle;                    // 최소 회전 각도
    public float MaxAngle => _maxAngle;                    // 최대 회전 각도
    public bool IsLineConstrained => _isLineConstrained;   // Line 구속 등록 여부

    /**
     * @brief  현재 사용중인 회전축(라인B 방향)을 반환한다.
     *         lineB가 null이면 lineA의 방향을 반환하고, 그마저 없으면 World X축을 반환한다.
     */
    public Vector3 Axis => (_lineB != null) ? _lineB.Direction : ((_lineA != null) ? _lineA.Direction : Vector3.right);

    /**
     * @brief  두 선을 회전축으로 하는 회전 조인트를 생성한다.
     *         lineA 와 lineB 가 일치(Coincident)할 때 유효한 조인트가 된다.
     * @param  lineA      오브젝트 A 의 회전축 Line
     * @param  lineB      오브젝트 B 의 회전축 Line
     * @param  minAngle   최소 회전 각도 (degree)
     * @param  maxAngle   최대 회전 각도 (degree)
     */
    public RevoluteJoint(Line lineA, Line lineB,
                         float minAngle, float maxAngle) : base(lineA, lineB)
    {
        _lineA = lineA;
        _lineB = lineB;
        _minAngle = minAngle;
        _maxAngle = maxAngle;
        _currentAngle = 0f;
    }

    /**
     * @brief  회전 각도를 설정한다.
     *         min/max 범위를 초과하면 클램프 처리한다.
     * @param  angle    설정할 회전 각도 (degree)
     */
    public void SetAngle(float angle)
    {
        _currentAngle = Mathf.Clamp(angle, _minAngle, _maxAngle);
    }

    /**
    * @brief  회전축을 기준으로 오브젝트 B 의 현재 방향과 초기 기준 방향 사이의 부호 있는 각도를 반환한다.
    * @param  fromDirection  초기 기준 방향 벡터 (오브젝트 B 의 처음 방향)
    * @param  toDirection    현재 방향 벡터 (오브젝트 B 의 현재 방향)
    * @param  axis           회전축 벡터 (_rotationAxis)
    * @return float          부호 있는 각도 (degree)
    */
    public static float GetCurrentAngle(Vector3 fromDirection, Vector3 toDirection, Vector3 axis)
    {
        // Vector3.SignedAngle을 사용하여 축을 기준으로 한 -180도 ~ 180도 사이의 각도를 반환
        return Vector3.SignedAngle(fromDirection, toDirection, axis);
    }

    /**
     * @brief  현재 각도를 구하고 클램프한 뒤, 회전이 적용된 Quaternion 을 반환한다.
     *         (SliderJoint의 GetClampedPosition 에 해당)
     * @param  fromDirection   초기 기준 방향 벡터
     * @param  toDirection     현재 방향 벡터
     * @param  axis            회전축 벡터
     * @param  initialRotation 오브젝트 B의 초기 로테이션 값 (기준점)
     * @return Quaternion      클램프 처리된 최종 회전값
     */
    public Quaternion GetClampedRotation(Vector3 fromDirection, Vector3 toDirection, Vector3 axis, Quaternion initialRotation)
    {
        // 1. 현재 회전된 각도 계산
        float angle = GetCurrentAngle(fromDirection, toDirection, axis);

        // 2. min/max 범위 안으로 클램프 처리 (RevoluteJoint의 SetAngle 내부에서 _currentAngle 업데이트됨)
        SetAngle(angle);

        // 3. 클램프된 각도와 회전축을 기반으로 최종 회전값(Quaternion) 계산
        // (AngleAxis로 만든 회전량에 초기 회전값을 곱해서 최종 각도를 구함)
        return Quaternion.AngleAxis(_currentAngle, axis) * initialRotation;
    }

    /**
     * @brief  회전 구속 조건을 확인하고 상태를 갱신한다.
     *         현재는 구속 조건 충족 여부만 검사하며, 실제 회전 보정은 OnConstrained() 에서 수행된다.
     *         아래 조건을 확인하여 구속 상태를 저장한다.
     *         ① lineA 와 lineB 가 일치 → _isLineConstrained 에 저장
     * @return bool  구속 상태(_isLineConstrained) 반환
     */
    public override bool ApplyConstraint()
    {
        _isLineConstrained = _lineA.IsCoincident(_lineB);
        return _isLineConstrained;
    }

    /**
     * @brief  회전 조인트가 유효한 상태인지 검증한다.
     *         아래 두 조건을 모두 만족하면 유효하다.
     *         ① lineA 와 lineB 가 일치하는지 확인 → Line.IsCoincident()
     *         ② min/max 범위가 올바른지 확인 (minAngle < maxAngle)
     * @return bool  유효하면 true, 아니면 false
     */
    public override bool IsValid()
    {
        bool isValid = true;

        if (!_lineA.IsCoincident(_lineB))
        {
            isValid = false;
            Debug.LogWarning("회전 조인트가 유효하지 않은 상태입니다. (Line 불일치)");
        }

        if (_minAngle > _maxAngle)
        {
            isValid = false;
            Debug.LogWarning("회전 조인트가 유효하지 않은 상태입니다. (min/max 범위 불일치)");
        }

        return isValid;
    }
}