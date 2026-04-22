// ============================================================
// 파일명  : Plane.cs
// 역할    : 기구학 계산을 위한 면(Plane) 기반 클래스
// 작성자  : 이현화
// 작성일  : 2026-03-25
// 수정이력: 2026-04-22 - Tolerance 를 0.01f 로 조정 (확장프로그램 기반 면 선택 오차 허용)
//                      - GetCrossVectors() 에서 crossPosition 절대값 처리
//                        (법선 방향이 반대인 경우도 일치로 처리)
// ============================================================

using UnityEngine;

public class Plane
{
    // 허용 오차 (부동소수점 비교 시 사용)
    // [2026.04.22 수정] 확장프로그램 기반 면 선택 시 발생하는 미세 오차를 허용하기 위해 0.01f 로 조정
    private const float Tolerance = 0.01f;

    private Vector3 _pointA;   // 면을 정의하는 첫 번째 점
    private Vector3 _pointB;   // 면을 정의하는 두 번째 점
    private Vector3 _pointC;   // 면을 정의하는 세 번째 점

    public Vector3 PointA => _pointA;                               // 첫 번째 점
    public Vector3 PointB => _pointB;                               // 두 번째 점
    public Vector3 PointC => _pointC;                               // 세 번째 점
    public Vector3 Normal => Vector3.Cross(
        _pointB - _pointA,
        _pointC - _pointA).normalized;                              // 법선 벡터 (정규화)
    public Vector3 Centroid => (_pointA + _pointB + _pointC) / 3f;  // 무게중심

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
     *         단, 일치하는 것은 제외한다.
     * @param  other    비교할 대상 Plane
     * @return bool     평행이면 true, 아니면 false
     */
    public bool IsParallel(Plane other)
    {
        // 1. 두 면의 방향 외적과 위치 외적을 계산
        GetCrossVectors(other, out Vector3 crossDirection, out float crossPosition);

        // 2. 반환 조건
        //    crossDirection < Tolerance → 방향이 평행 or 일치
        //    crossPosition > Tolerance → 위치가 달라 일치가 아님 (평행만 true)
        //    ex) 두 면이 y축 방향으로 1칸 떨어진 평행면 → true
        //        같은 평면 및 그 이외 → false
        return crossDirection.magnitude < Tolerance && Mathf.Abs(crossPosition) > Tolerance;
    }

    /**
     * @brief  두 면이 수직인지 판별한다.
     *         법선 벡터의 내적 값이 0에 가까우면 수직으로 판별한다.
     * @param  other    비교할 대상 Plane
     * @return bool     수직이면 true, 아니면 false
     */
    public bool IsPerpendicular(Plane other)
    {
        // 1. 두 면의 방향벡터 내적 계산
        //    Dot(A, B) = |A| × |B| × cos(θ)
        //    수직 판별은 각도만 중요하므로 거리값이 필요없음 → Direction(normalized) 사용
        //    두 방향벡터가 normalized(길이=1) 이므로 → Dot = cos(θ)
        //    수직이면 cos(90°) = 0 → Dot = 0
        //    즉 Dot 결과가 0에 가까우면 두 면이 수직
        float dot = Vector3.Dot(Normal, other.Normal);

        // 2. 반환 조건
        //    Mathf.Abs 를 쓰는 이유: Dot 결과가 음수가 나올 수 있어서 절대값으로 변환
        //    ex) Normal=(1,0,0), other.Normal=(0,1,0) → Dot=0 → 수직 ✅
        //        Normal=(1,0,0), other.Normal=(1,0,0) → Dot=1 → 수직 아님 ❌
        return Mathf.Abs(dot) < Tolerance;
    }

    /**
     * @brief  두 면의 방향 외적과 위치 외적을 계산한다.
     * @param  other            비교할 대상 Plane
     * @param  crossDirection   방향벡터 외적 결과 (out)
     * @param  crossPosition    위치벡터 외적 결과 (out)
     */
    private void GetCrossVectors(Plane other, out Vector3 crossDirection, out float crossPosition)
    {
        // 1. 두 면의 방향벡터 외적 계산
        //    외적이 0이면 → 두 면의 방향이 평행 or 일치
        //    외적이 0이 아니면 → 두 면이 교차 or 꼬인 관계
        crossDirection = Vector3.Cross(Normal, other.Normal);

        // 2. 이 면의 시작점 → other 면의 시작점 방향벡터
        //    필요한 이유 → 방향만 같아도 평행 or 일치 둘 다 해당되기 때문에
        //    위치가 다른지 확인하기 위해 시작점 사이의 방향벡터를 구함
        //    "끝점 - 시작점" 규칙: other.PointA - _pointA
        Vector3 dirToOther = other.PointA - _pointA;

        // [2026.04.22 수정] 법선 방향이 반대인 경우도 일치로 처리
        // 3. 방향벡터와 dir 의 내적 절대값 계산
        //    크기 = 0 이면 → 두 면이 같은 평면 (일치)
        //    크기 > 0 이면 → 두 면이 다른 위치에 있음 (평행)
        //    절대값을 사용하는 이유: 법선이 반대 방향일 때 내적이 음수가 되어
        //    일치 판별이 실패하는 것을 방지하기 위함
        crossPosition = Mathf.Abs(Vector3.Dot(Normal, dirToOther));
    }

    /**
     * @brief  두 면이 동일 평면 위에 있는지 판별한다.
     *         법선 벡터가 평행하고, other의 한 점이 이 면 위에 있을 때 true를 반환한다.
     * @param  other    비교할 대상 Plane
     * @return bool     동일 평면이면 true, 아니면 false
     */
    public bool IsCoincident(Plane other)
    {
        // 1. 두 면의 방향 외적과 위치 외적을 계산
        GetCrossVectors(other, out Vector3 crossDirection, out float crossPosition);

        // 2. 반환 조건
        //    crossDirection < Tolerance → 방향이 평행 or 일치
        //    crossPosition < Tolerance → 위치가 동일 (일치만 true)
        //    ex) 두 면이 완전히 겹치는 평면 → true
        //        평행면 및 그 이외 → false
        return crossDirection.magnitude < Tolerance && Mathf.Abs(crossPosition) < Tolerance;
    }

    /**
     * @brief  선이 면 위에 포함되어 있는지 판별한다.
     *         선의 두 끝점이 모두 면 위에 있을 때 true를 반환한다.
     * @param  line     판별할 대상 Line
     * @return bool     면 위에 포함되어 있으면 true, 아니면 false
     */
    public bool Contains(Line line)
    {
        // 1. Contains(Vector3 point) 로 각 끝점이 면 위에 있는지 판별
        //    선 위의 모든 점이 면 위에 있으려면 양 끝점이 면 위에 있어야 함
        // 2. 두 점 모두 Tolerance 이내면 true
        return Contains(line.PointA) && Contains(line.PointB);
    }

    // 점과 면의 관계

    /**
     * @brief  점이 면 위에 있는지 판별한다.
     *         점을 면에 투영한 결과와 원래 점 사이의 거리가 허용 오차 이내이면 true를 반환한다.
     * @param  point    판별할 점의 좌표 (Vector3)
     * @return bool     면 위에 있으면 true, 아니면 false
     */
    public bool Contains(Vector3 point)
    {
        // 1. Dot(Normal, point - _pointA) 로 point와 면 사이의 거리 계산
        //    점이 면 위에 있으면 거리 = 0
        float distance = Mathf.Abs(Vector3.Dot(Normal, point - _pointA));

        // 2. 거리가 Tolerance 이내면 true
        return distance < Tolerance;
    }
}