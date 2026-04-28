// ============================================================
// 파일명  : Line.cs
// 역할    : 기구학 계산을 위한 선(Line) 기반 클래스
// 작성자  : 이현화
// 작성일  : 2026-03-24
// 수정이력: 2026-04-24 - Tolerance 를 1e-6f → 0.02f 로 조정
//                        (ConstraintAssemblyWindow Edge 선택 시 발생하는 미세 좌표 오차 허용)
// ============================================================

using UnityEngine;

public class Line
{
    // [2026.04.24 수정] Tolerance 를 1e-6f → 0.01f → 0.02f 로 조정
    //                  ConstraintAssemblyWindow 에서 Edge 선택 시 발생하는
    //                  미세 좌표 오차(약 0.01 수준)를 허용하기 위함
    private const float Tolerance = 0.02f;

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
        // 1. 두 선의 방향 외적과 위치 외적을 계산
        //    → GetCrossVectors() 로 외적 계산하기
        GetCrossVectors(other, out Vector3 crossDirection, out Vector3 crossPosition);

        // 2. 반환 조건
        //    crossDirection < Tolerance → 방향이 평행 or 일치
        //    crossPosition < Tolerance → 위치가 동일 (일치만 true)
        //    ex) 두 선이 완전히 겹치는 일치선 → true
        //        평행선 및 그 이외 → false
        return crossDirection.magnitude < Tolerance && crossPosition.magnitude < Tolerance;
    }

    /**
     * @brief  두 선이 평행한지 판별한다.
     *         방향 벡터의 외적(Cross Product) 크기가 0에 가까우면 평행으로 판별한다.
     * @param  other    비교할 대상 Line
     * @return bool     평행이면 true, 아니면 false
     */
    public bool IsParallel(Line other)
    {
        // 1. 두 선의 방향 외적과 위치 외적을 계산
        //    → GetCrossVectors() 로 외적 계산하기
        GetCrossVectors(other, out Vector3 crossDirection, out Vector3 crossPosition);

        // 2. 반환 조건
        //    crossDirection < Tolerance → 방향이 평행 or 일치
        //    crossPosition > Tolerance → 위치가 달라 일치가 아님 (평행만 true)
        //    ex) 두 선이 y축 방향으로 1칸 떨어진 평행선 → true
        //        일치선 및 그 이외 → false
        return crossDirection.magnitude < Tolerance && crossPosition.magnitude > Tolerance;
    }

    /**
     * @brief  두 선의 방향 외적과 위치 외적을 계산한다.
     * @param  other        비교할 대상 Line
     * @param  crossDir     방향벡터 외적 결과 (out)
     * @param  crossPos     위치벡터 외적 결과 (out)
     */
    private void GetCrossVectors(Line other, out Vector3 crossDirection, out Vector3 crossPosition)
    {
        // 1. 두 선의 방향벡터 외적 계산
        //    외적이 0이면 → 두 선의 방향이 평행 or 일치
        //    외적이 0이 아니면 → 두 선이 교차 or 꼬인 관계
        crossDirection = Vector3.Cross(Direction, other.Direction);

        // 2. 이 선의 시작점 → other 선의 시작점 방향벡터
        //    필요한 이유 → 방향만 같아도 평행 or 일치 둘 다 해당되기 때문에
        //    위치가 다른지 확인하기 위해 시작점 사이의 방향벡터를 구함
        //    "끝점 - 시작점" 규칙: other.PointA - _pointA
        Vector3 dirToOther = other.PointA - _pointA;

        // 3. 방향벡터와 dir 의 외적 계산
        //    크기 = 0 이면 → 두 선이 같은 선상 (일치)
        //    크기 > 0 이면 → 두 선이 다른 위치에 있음 (평행)
        crossPosition = Vector3.Cross(Direction, dirToOther);
    }

    /**
     * @brief  두 선이 수직인지 판별한다.
     *         방향 벡터의 내적(Dot Product) 값이 0에 가까우면 수직으로 판별한다.
     * @param  other    비교할 대상 Line
     * @return bool     수직이면 true, 아니면 false
     */
    public bool IsPerpendicular(Line other)
    {
        // 1. 두 선의 방향벡터 내적 계산
        //    Dot(A, B) = |A| × |B| × cos(θ)
        //    수직 판별은 각도만 중요하므로 거리값이 필요없음 → Direction(normalized) 사용
        //    두 방향벡터가 normalized(길이=1) 이므로 → Dot = cos(θ)
        //    수직이면 cos(90°) = 0 → Dot = 0
        //    즉 Dot 결과가 0에 가까우면 두 선이 수직
        float dot = Vector3.Dot(Direction, other.Direction);

        // 2. 반환 조건
        //    Mathf.Abs 를 쓰는 이유: Dot 결과가 음수가 나올 수 있어서 절대값으로 변환
        //    ex) Direction=(1,0,0), other.Direction=(0,1,0) → Dot=0 → 수직 ✅
        //        Direction=(1,0,0), other.Direction=(1,0,0) → Dot=1 → 수직 아님 ❌
        return Mathf.Abs(dot) < Tolerance;
    }

    /**
     * @brief  두 선의 교점을 계산한다.
     *         두 선이 평행하거나 교점이 존재하지 않으면 null을 반환한다.
     * @param  other    교점을 구할 대상 Line
     * @return Vector3? 교점 좌표, 교점이 없으면 null
     */
    public Vector3? Intersect(Line other)
    {
        // 1. 두 선이 평행하거나 일치하면 → null 반환
        //    평행/일치면 만나는 점이 없거나 무한히 많아서 교점 정의 불가
        if (IsParallel(other) || IsCoincident(other))
            return null;

        // 2. 두 시작점 사이 벡터 (B - A)
        //    "끝점 - 시작점" 규칙 : A(이 선의 시작점) → B(상대 선의 시작점) 방향벡터
        Vector3 startPointDiff = other.PointA - _pointA;

        // 3. 두 방향벡터 외적 (u × v)
        //    외적이 0이면 → 평행 or 일치 (1번에서 이미 걸러짐)
        Vector3 directionCross = Vector3.Cross(Direction, other.Direction);

        // 4. t 계산
        //    공식: t = ((B-A) × v) · (u × v) / |u × v|²
        //
        //    기호 설명:
        //    × → 외적(Cross)  결과: Vector3
        //    · → 내적(Dot)    결과: float
        //
        //    단계별 계산:
        //    1단계: (B-A) × v  → Vector3.Cross(startPointDiff, other.Direction)     → Vector3
        //    2단계: (u × v)    → directionCross                                     → Vector3
        //    3단계: 1단계 · 2단계 → Vector3.Dot(1단계, 2단계)                       → float (분자)
        //           Vector3를 숫자(float)로 바꾸기 위해 내적 사용
        //    4단계: / |u × v|² → / directionCross.sqrMagnitude(벡터의 크기 제곱)   → t (float)
        float t = Vector3.Dot(Vector3.Cross(startPointDiff, other.Direction), directionCross) / directionCross.sqrMagnitude;

        // 5. 교점 후보 계산
        //    this 객체의 공식 (A + t*u) 에 t 를 대입해서 교점 후보 좌표 계산
        //    아직 other 객체 위에 있는지 확인 안됐으므로 "후보" 임
        Vector3 intersectPoint = _pointA + Direction * t;

        // 6. 꼬인 관계 확인
        //    꼬여있을 수 있으니 Contains 로 교점이 other 위에 있는지 확인
        if (!other.Contains(intersectPoint))
            return null;

        return intersectPoint;

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
        // 1. Distance() 로 point와 선 사이의 거리 계산
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
        Vector3 dirAB = Direction;

        // 2. _pointA → point 로 향하는 벡터 구하기 (AP벡터)
        //    마찬가지로 "투영할 점(point) - 시작점(_pointA)" 순서
        Vector3 vectorAP = point - _pointA;

        // 3. AP벡터를 선 방향(dirAB)으로 투영한 거리 t 구하기
        //    Dot(AP, D) = |AP| × |D| × cos(θ)
        //    D - dirAB는 normalized(길이=1) 해야 하는 이유:
        //    → |D| = 1 이 되어야 공식에서 제거되고 t = |AP| × cos(θ) = 실제 거리값이 나옴
        //    → normalized 안 하면 |D| 값이 t 에 곱해져서 실제 거리가 아닌 뻥튀기된 값이 나옴
        //    ex) dirAB=(4,0,0) normalized 안함 → t = 12 → Q=(48,0,0) 엉뚱한 곳 ❌
        //    ex) dirAB=(1,0,0) normalized 함  → t = 3  → Q=(3,0,0)  정확한 곳 ✅
        //    AP - vectorAP는 normalized 하지 않음 → |AP| 거리값이 살아있어야 t가 실제 거리로 나옴
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