// ============================================================
// 파일명  : Joint.cs
// 역할    : 기구학 구속 조건을 위한 조인트 추상 기반 클래스
// 작성자  : 
// 작성일  : 
// 수정이력: 
// ============================================================

using UnityEngine;

public abstract class Joint
{
    // 허용 오차 (부동소수점 비교 시 사용)
    protected const float Tolerance = 1e-6f;

    private Vector3 _position;   // 조인트 위치
    private Vector3 _axis;       // 조인트 축 방향 벡터

    public Vector3 Position => _position;               // 조인트 위치
    public Vector3 Axis => _axis.normalized;            // 조인트 축 방향 벡터 (정규화)

    /**
     * @brief  조인트 위치와 축 방향으로 조인트를 생성한다.
     * @param  position  조인트 위치 (Vector3)
     * @param  axis      조인트 축 방향 벡터 (Vector3)
     */
    protected Joint(Vector3 position, Vector3 axis)
    {
        _position = position;
        _axis = axis;
    }

    /**
     * @brief  구속 조건을 적용한다.
     *         파생 클래스에서 구현.
     */
    public abstract void ApplyConstraint();

    /**
     * @brief  조인트가 유효한 상태인지 검증한다.
     *         파생 클래스에서 구현.
     * @return bool  유효하면 true, 아니면 false
     */
    public abstract bool IsValid();
}