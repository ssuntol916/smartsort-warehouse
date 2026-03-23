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
        // 1. 두 선의 방향벡터 외적 계산
        //    외적이 0이면 → 두 선의 방향이 평행 or 일치
        //    외적이 0이 아니면 → 두 선이 교차 or 꼬인 관계
        Vector3 crossDirection = Vector3.Cross(Direction, other.Direction);

        // 2. 이 선의 시작점 → other 선의 시작점 방향벡터
        //    필요한 이유 → 방향만 같아도 평행 or 일치 둘 다 해당되기 때문에
        //    위치가 다른지 확인하기 위해 시작점 사이의 방향벡터를 구함
        //    "끝점 - 시작점" 규칙: other.PointA - _pointA
        Vector3 dirToOther = other.PointA - _pointA;

        // 3. 방향벡터와 dir 의 외적 계산
        //    크기 = 0 이면 → 두 선이 같은 선상 (일치)
        //    크기 > 0 이면 → 두 선이 다른 위치에 있음 (평행)
        Vector3 crossPosition = Vector3.Cross(Direction, dirToOther);

        // 4. 반환 조건
        //    crossDirection < Tolerance → 방향이 평행 (평행 or 일치)
        //    crossPosition > Tolerance → 위치가 달라 일치가 아님 (평행만 true)
        //    ex) 두 선이 y축 방향으로 1칸 떨어진 평행선 → true
        //        두 선이 완전히 겹치는 일치선 → false
        return crossDirection.magnitude < Tolerance && crossPosition.magnitude > Tolerance;
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
        // 1. point와 투영점 사이 거리 계산
        //    점이 선 위에 있으면 거리 = 0
        float distance = Distance(point);

        // 2. 거리가 Tolerance 이내면 true
        //    == 0 이 아닌 이유: 컴퓨터 부동소수점 오차 때문에 정확히 0이 나오는 경우가 거의 없음
        //    ex) 0.0000001f 처럼 아주 작은 값이 나올 수 있음
        //    → Tolerance(0.000001f) 이내면 0으로 취급
        return distance < Tolerance;
    }

    /**
     * @brief  점을 선에 투영(정사영)한 좌표를 반환한다.
     * @param  point    투영할 점의 좌표 (Vector3)
     * @return Vector3  선 위에 투영된 점의 좌표
     */
    public Vector3 Project(Vector3 point)
    {
        // 1. 선의 방향 벡터 구하기
        //    Direction 프로퍼티 = (_pointB - _pointA).normalized
        //    벡터 방향은 반드시 "끝점 - 시작점" 로 계산한다
        //    반대로 하면 dirAB 방향이 뒤집혀서 투영점 Q가 엉뚱한 곳에 찍힘
        Vector3 dirAB = Direction;

        // 2. _pointA → point 로 향하는 벡터 구하기 (AP벡터)
        //    마찬가지로 "투영할 점(point) - 시작점(_pointA)" 순서
        Vector3 vectorAP = point - _pointA;

        // 3. AP벡터를 선 방향(dirAB)으로 투영한 거리 t 구하기
        //    Dot(AP, D) = |AP| × |D| × cos(θ)
        //    dirAB는 normalized(길이=1) 이므로 → |D| = 1
        //    결국 Dot(AP, D) = |AP| × cos(θ) = t
        //    즉 Dot이 cos(θ) 계산을 내부에서 자동으로 처리해준다
        //    → AP 벡터 중에서 선 방향 성분만 꺼낸 값 = A에서 수직점까지의 거리
        //    ex) vectorAP=(3,5,0), dirAB=(1,0,0) → Dot = 3 (y성분 5는 버려지고 x성분 3만 남음 = A→Q 거리)
        float t = Vector3.Dot(vectorAP, dirAB);

        // 4. 투영점 Q 좌표 계산
        //    Q = A + D * t
        //    A에서 출발해서 선 방향(dirAB)으로 t만큼 이동하면 수직점에 도착
        Vector3 projectPoint = _pointA + dirAB * t;

        return projectPoint;
    }

    /**
     * @brief  점과 선 사이의 최단 거리를 반환한다.
     * @param  point    거리를 측정할 점의 좌표 (Vector3)
     * @return float    점과 선 사이의 최단 거리
     */
    public float Distance(Vector3 point)
    {
        // 1. point 를 선에 투영시켜 투영점 구함
        //    투영점 = point 에서 선에 수직으로 내린 점
        //    → Project() 로 투영점 구하기
        Vector3 projectedPoint = Project(point);

        // 2. point와 투영점 사이 거리 계산
        //    점이 선 위에 있으면 거리 = 0
        float distance = Vector3.Distance(point, projectedPoint);

        return distance;
    }
}