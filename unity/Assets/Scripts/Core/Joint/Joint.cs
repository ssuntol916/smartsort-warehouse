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

    /**
     * @brief  두 선을 참조하여 조인트를 생성한다.
     * @param  lineA    오브젝트 A 의 기준 선
     * @param  lineB    오브젝트 B 의 기준 선
     */
    protected Joint(Line lineA, Line lineB)
    {
        LineA = lineA;
        LineB = lineB;
    }

    /**
     * @brief  두 면을 참조하여 조인트를 생성한다.
     * @param  planeA   오브젝트 A 의 기준 면
     * @param  planeB   오브젝트 B 의 기준 면
     */
    protected Joint(Plane planeA, Plane planeB)
    {
        PlaneA = planeA;
        PlaneB = planeB;
    }

    /**
     * @brief  선과 면을 참조하여 조인트를 생성한다.
     *         선-면 구속 조건에서 사용한다.
     * @param  line     오브젝트 A 의 기준 선
     * @param  plane    오브젝트 B 의 기준 면
     */
    protected Joint(Line line, Plane plane)
    {
        LineA = line;
        PlaneA = plane;
    }

    public Line LineA { get; }      // 오브젝트 A 의 기준 선
    public Line LineB { get; }      // 오브젝트 B 의 기준 선
    public Plane PlaneA { get; }    // 오브젝트 A 의 기준 면
    public Plane PlaneB { get; }    // 오브젝트 B 의 기준 면

    /**
     * @brief  구속 조건을 적용한다.
     *         파생 클래스에서 구현.
     */
    public abstract bool ApplyConstraint();

    /**
     * @brief  조인트가 유효한 상태인지 검증한다.
     *         파생 클래스에서 구현.
     * @return bool  유효하면 true, 아니면 false
     */
    public abstract bool IsValid();
}