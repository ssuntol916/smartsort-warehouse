// ============================================================
// 파일명  : Plane.cs
// 역할    : 기구학 계산을 위한 면(Plane) 기반 클래스
// 작성자  : 
// 작성일  : 
// 수정이력: 
// ============================================================

using UnityEngine;

public class Plane
{
    // 허용 오차 (부동소수점 비교 시 사용)
    private const float Tolerance = 1e-6f;

    private Vector3 _pointA;   // 면을 정의하는 첫 번째 점
    private Vector3 _pointB;   // 면을 정의하는 두 번째 점
    private Vector3 _pointC;   // 면을 정의하는 세 번째 점

    public Vector3 Normal => Vector3.Cross(
        _pointB - _pointA,
        _pointC - _pointA).normalized;                             // 법선 벡터 (정규화)
    public Vector3 Centroid => (_pointA + _pointB + _pointC) / 3f; // 무게중심

    /**
     * @brief  세 점으로 면을 생성한다.
     *         세 점이 일직선 위에 있으면 면을 정의할 수 없다.
     * @param  pointA   면을 정의하는 첫 번째 점
     * @param  pointB   면을 정의하는 두 번째 점
     * @param  pointC   면을 정의하는 세 번째 점
     */
    public Plane(Vector3 pointA, Vector3 pointB, Vector3 pointC)
    {
        _pointA = pointA;
        _pointB = pointB;
        _pointC = pointC;
    }

    // 두 면의 관계

    /**
     * @brief  두 면이 평행한지 판별한다.
     *         법선 벡터의 외적 크기가 0에 가까우면 평행으로 판별한다.
     * @param  other    비교할 대상 Plane
     * @return bool     평행이면 true, 아니면 false
     */
    public bool IsParallel(Plane other)
    {

    }

    /**
     * @brief  두 면이 수직인지 판별한다.
     *         법선 벡터의 내적 값이 0에 가까우면 수직으로 판별한다.
     * @param  other    비교할 대상 Plane
     * @return bool     수직이면 true, 아니면 false
     */
    public bool IsPerpendicular(Plane other)
    {

    }

    /**
     * @brief  두 면이 동일 평면 위에 있는지 판별한다.
     *         법선 벡터가 평행하고, other의 한 점이 이 면 위에 있을 때 true를 반환한다.
     * @param  other    비교할 대상 Plane
     * @return bool     동일 평면이면 true, 아니면 false
     */
    public bool IsCoincident(Plane other)
    {

    }

    /**
     * @brief  선이 면 위에 포함되어 있는지 판별한다.
     *         선의 두 끝점이 모두 면 위에 있을 때 true를 반환한다.
     * @param  line     판별할 대상 Line
     * @return bool     면 위에 포함되어 있으면 true, 아니면 false
     */
    public bool Contains(Line line)
    {

    }
}