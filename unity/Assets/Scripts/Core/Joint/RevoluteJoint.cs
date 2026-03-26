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
     * @brief  회전 조인트를 생성한다.
     * @param  position    조인트 위치 (Vector3)
     * @param  axis        회전축 방향 벡터 (Vector3)
     * @param  minAngle    최소 회전 각도 (degree)
     * @param  maxAngle    최대 회전 각도 (degree)
     */
    public RevoluteJoint(Vector3 position, Vector3 axis,
                         float minAngle, float maxAngle) : base(position, axis)
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
     *         현재 각도를 기준으로 회전축 방향의 회전 변환을 계산한다.
     */
    public override void ApplyConstraint()
    {

    }

    /**
     * @brief  회전 조인트가 유효한 상태인지 검증한다.
     *         min/max 범위가 올바르고 축 벡터가 영벡터가 아니면 유효하다.
     * @return bool  유효하면 true, 아니면 false
     */
    public override bool IsValid()
    {

    }
}