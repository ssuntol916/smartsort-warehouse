// ============================================================
// 파일명  : RevoluteJoint.cs
// 역할    : 회전 조인트 클래스
// 작성자  : 
// 작성일  : 
// 수정이력: 
// ============================================================

using UnityEngine;

public class RevoluteJoint : Joint
{
    private float _currentAngle;   // 현재 회전 각도 (degree)
    private float _minAngle;       // 최소 회전 각도 (degree)
    private float _maxAngle;       // 최대 회전 각도 (degree)

    public float CurrentAngle => _currentAngle;   // 현재 회전 각도
    public float MinAngle => _minAngle;           // 최소 회전 각도
    public float MaxAngle => _maxAngle;           // 최대 회전 각도

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

    }

    /**
     * @brief  회전 구속 조건을 적용한다.
     *         lineA 와 lineB 가 일치하도록 오브젝트 B 의 위치를 보정하고,
     *         현재 각도를 기준으로 lineA 축 방향의 회전 변환을 계산한다.
     */
    public override void ApplyConstraint()
    {

    }

    /**
     * @brief  회전 조인트가 유효한 상태인지 검증한다.
     *         lineA 와 lineB 가 평행하고 min/max 범위가 올바르면 유효하다.
     * @return bool  유효하면 true, 아니면 false
     */
    public override bool IsValid()
    {
        return false;
    }
}