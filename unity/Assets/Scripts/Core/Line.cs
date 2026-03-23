// ============================================================
// 파일명  : Line.cs
// 역할    : 기구학 계산을 위한 선(Line) 기반 클래스
// 작성자  : 
// 작성일  : 
// 수정이력: 
// ============================================================

using UnityEngine;

public class Line
{
    // 허용 오차 (부동소수점 비교 시 사용)
    private const float Tolerance = 1e-6f;

    private Vector3 _pointA;    // 선의 시작점
    private Vector3 _pointB;    // 선의 끝점

    public Vector3 PointA => _pointA;                               // 시작점
    public Vector3 PointB => _pointB;                               // 끝점
    public Vector3 Direction => (_pointB - _pointA).normalized;     // 방향 벡터 (정규화)
    public Vector3 Midpoint => (_pointA + _pointB) * 0.5f;          // 중점

    /**
     * @brief  두 점으로 선을 생성한다.
     * @param  pointA   선의 시작점
     * @param  pointB   선의 끝점
     */
    public Line(Vector3 pointA, Vector3 pointB)
    {
        _pointA = pointA;
        _pointB = pointB;
    }

    // 두 선의 관계

    /**
     * @brief  두 선이 동일 선상에 있는지 판별한다.
     *         방향 벡터가 평행하고, other의 시작점이 이 선 위에 있을 때 true를 반환한다.
     * @param  other    비교할 대상 Line
     * @return bool     동일 선상이면 true, 아니면 false
     */
    public bool IsCoincident(Line other)
    {

    }

    /**
     * @brief  두 선이 평행한지 판별한다.
     *         방향 벡터의 외적(Cross Product) 크기가 0에 가까우면 평행으로 판별한다.
     * @param  other    비교할 대상 Line
     * @return bool     평행이면 true, 아니면 false
     */
    public bool IsParallel(Line other)
    {

    }

    /**
     * @brief  두 선이 수직인지 판별한다.
     *         방향 벡터의 내적(Dot Product) 값이 0에 가까우면 수직으로 판별한다.
     * @param  other    비교할 대상 Line
     * @return bool     수직이면 true, 아니면 false
     */
    public bool IsPerpendicular(Line other)
    {

    }

    /**
     * @brief  두 선의 교점을 계산한다.
     *         두 선이 평행하거나 교점이 존재하지 않으면 null을 반환한다.
     * @param  other    교점을 구할 대상 Line
     * @return Vector3? 교점 좌표, 교점이 없으면 null
     */
    public Vector3? Intersect(Line other)
    {

    }

    // 점과 선의 관계

    /**
     * @brief  점이 선 위에 있는지 판별한다.
     *         점을 선에 투영한 결과와 원래 점 사이의 거리가 허용 오차 이내이면 true를 반환한다.
     * @param  point    판별할 점의 좌표 (Vector3)
     * @return bool     선 위에 있으면 true, 아니면 false
     */
    public bool Contains(Vector3 point)
    {

    }

    /**
     * @brief  점을 선에 투영(정사영)한 좌표를 반환한다.
     * @param  point    투영할 점의 좌표 (Vector3)
     * @return Vector3  선 위에 투영된 점의 좌표
     */
    public Vector3 Project(Vector3 point)
    {

    }

    /**
     * @brief  점과 선 사이의 최단 거리를 반환한다.
     * @param  point    거리를 측정할 점의 좌표 (Vector3)
     * @return float    점과 선 사이의 최단 거리
     */
    public float Distance(Vector3 point)
    {

    }
}