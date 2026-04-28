// ============================================================
// 파일명  : LineTest.cs
// 역할    : Line 클래스 단위 테스트 (엣지 케이스 중심)
// 작성자  : 이현화
// 작성일  : 2026-03-24
// 사용법  : Unity Test Runner (EditMode) 에서 실행
//           Window > General > Test Runner > EditMode > Run All
// 수정이력: 2026-04-24 - Contains_OffLine_ReturnsFalse 테스트 수정
//                        Y 오프셋 0.001f → 0.03f 로 변경
//                        (Tolerance 가 0.02f 로 변경됨에 따라 0.001f 는
//                        허용 범위 내로 들어가 테스트가 실패하는 문제 수정)
// ============================================================

using NUnit.Framework;
using UnityEngine;

public class LineTest
{
    // ──────────────────────────────────────────────
    // 공용 Tolerance (Line.cs 와 동일값)
    // ──────────────────────────────────────────────
    private const float Tolerance = 0.02f; // [2026.04.24 수정] Line.cs 의 Tolerance 와 동일값으로 설정

    // ============================================================
    // [1] IsCoincident — 동일선 판별
    // ============================================================

    /// <summary>
    /// 가장 기본: 같은 두 점으로 만든 선 → 당연히 일치
    /// (실패하면 Direction 계산 자체가 잘못된 것)
    /// </summary>
    [Test]
    public void IsCoincident_IdenticalLine_ReturnsTrue()
    {
        var a = new Line(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        var b = new Line(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        Assert.IsTrue(a.IsCoincident(b));
    }

    /// <summary>
    /// 함정: 방향이 반대인 선도 같은 선 위에 있으면 일치여야 한다.
    /// 방향벡터 외적 크기는 0 이지만 부호가 다를 수 있음 → normalized 로 처리했는지 확인
    /// ex) (0,0,0)→(1,0,0) vs (5,0,0)→(-3,0,0) → 같은 X축 선
    /// </summary>
    [Test]
    public void IsCoincident_OppositeDirection_SameLine_ReturnsTrue()
    {
        var a = new Line(new Vector3(0, 0, 0), new Vector3(4, 0, 0));
        var b = new Line(new Vector3(5, 0, 0), new Vector3(-3, 0, 0)); // 반대 방향, 같은 X축 위
        Assert.IsTrue(a.IsCoincident(b));
    }

    /// <summary>
    /// 함정: 방향은 평행하지만 Y축으로 1 이동한 선 → 일치 아님 (평행)
    /// crossDirection ≈ 0 이지만 crossPosition > 0 이어야 false
    /// </summary>
    [Test]
    public void IsCoincident_ParallelNotCoincident_ReturnsFalse()
    {
        var a = new Line(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        var b = new Line(new Vector3(0, 1, 0), new Vector3(1, 1, 0)); // Y=1 평행선
        Assert.IsFalse(a.IsCoincident(b));
    }

    /// <summary>
    /// 함정: 3D 공간에서 같은 직선 위 다른 두 점으로 만든 선
    /// (1,1,1)→(3,3,3) 과 (-1,-1,-1)→(5,5,5) 는 동일 대각선
    /// </summary>
    [Test]
    public void IsCoincident_3D_DiagonalSameLine_ReturnsTrue()
    {
        var a = new Line(new Vector3(1, 1, 1), new Vector3(3, 3, 3));
        var b = new Line(new Vector3(-1, -1, -1), new Vector3(5, 5, 5));
        Assert.IsTrue(a.IsCoincident(b));
    }

    // ============================================================
    // [2] IsParallel — 평행 판별
    // ============================================================

    /// <summary>
    /// 기본 평행: X축 방향 두 선, Y로 1칸 떨어짐
    /// </summary>
    [Test]
    public void IsParallel_BasicParallel_ReturnsTrue()
    {
        var a = new Line(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        var b = new Line(new Vector3(0, 1, 0), new Vector3(2, 1, 0));
        Assert.IsTrue(a.IsParallel(b));
    }

    /// <summary>
    /// 함정: 일치선은 IsParallel 에서 false 여야 한다.
    /// IsParallel 조건: crossDirection≈0 AND crossPosition>Tolerance
    /// 일치선이면 crossPosition≈0 이므로 false 가 맞음
    /// </summary>
    [Test]
    public void IsParallel_CoincidentLine_ReturnsFalse()
    {
        var a = new Line(new Vector3(0, 0, 0), new Vector3(3, 0, 0));
        var b = new Line(new Vector3(1, 0, 0), new Vector3(5, 0, 0)); // 같은 X축 선
        Assert.IsFalse(a.IsParallel(b));
    }

    /// <summary>
    /// 함정: 반대 방향이지만 평행 → true 이어야 한다.
    /// 방향이 반대여도 외적 크기는 0 이므로 평행 판별은 방향 부호에 무관해야 함
    /// </summary>
    [Test]
    public void IsParallel_OppositeDirection_ReturnsTrue()
    {
        var a = new Line(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        var b = new Line(new Vector3(0, 2, 0), new Vector3(-1, 2, 0)); // 반대 방향, Y=2
        Assert.IsTrue(a.IsParallel(b));
    }

    /// <summary>
    /// 3D 사선 평행: (1,1,0)→(2,2,0) 과 (0,0,1)→(1,1,1) 는 XY평면 45도 사선 평행
    /// </summary>
    [Test]
    public void IsParallel_3D_DiagonalParallel_ReturnsTrue()
    {
        var a = new Line(new Vector3(1, 1, 0), new Vector3(3, 3, 0));
        var b = new Line(new Vector3(0, 0, 5), new Vector3(2, 2, 5)); // Z=5 평면의 같은 사선
        Assert.IsTrue(a.IsParallel(b));
    }

    // ============================================================
    // [3] IsPerpendicular — 수직 판별
    // ============================================================

    /// <summary>
    /// 기본 수직: X축 vs Y축
    /// </summary>
    [Test]
    public void IsPerpendicular_XaxisVsYaxis_ReturnsTrue()
    {
        var a = new Line(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        var b = new Line(new Vector3(0, 0, 0), new Vector3(0, 1, 0));
        Assert.IsTrue(a.IsPerpendicular(b));
    }

    /// <summary>
    /// 함정: 반대 방향이어도 수직이면 true 이어야 한다.
    /// Dot(u, -v) = -Dot(u, v) → Abs 로 처리했는지 확인
    /// Direction=(1,0,0) vs Direction=(0,-1,0) → Dot=0 → 수직
    /// </summary>
    [Test]
    public void IsPerpendicular_OppositeDirectionStillPerpendicular_ReturnsTrue()
    {
        var a = new Line(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        var b = new Line(new Vector3(0, 5, 0), new Vector3(0, -3, 0)); // 반대 방향 Y축
        Assert.IsTrue(a.IsPerpendicular(b));
    }

    /// <summary>
    /// 함정: 3D에서 공간상 교차하지 않아도 방향이 수직이면 true
    /// X축 방향 선 vs Z축 방향 선 → 교차점 없지만 방향은 수직
    /// </summary>
    [Test]
    public void IsPerpendicular_3D_SkewButPerpendicularDirection_ReturnsTrue()
    {
        var a = new Line(new Vector3(0, 0, 0), new Vector3(1, 0, 0)); // X축
        var b = new Line(new Vector3(0, 5, 0), new Vector3(0, 5, 1)); // Y=5 위의 Z축 방향
        Assert.IsTrue(a.IsPerpendicular(b));
    }

    /// <summary>
    /// 45도 기울어진 선 → 수직 아님
    /// </summary>
    [Test]
    public void IsPerpendicular_DiagonalLine_ReturnsFalse()
    {
        var a = new Line(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        var b = new Line(new Vector3(0, 0, 0), new Vector3(1, 1, 0)); // 45도
        Assert.IsFalse(a.IsPerpendicular(b));
    }

    // ============================================================
    // [4] Intersect — 교점 계산
    // ============================================================

    /// <summary>
    /// 기본 교점: XY 평면에서 + 모양으로 교차
    /// (0,1,0)→(2,1,0) 과 (1,0,0)→(1,2,0) → 교점 (1,1,0)
    /// </summary>
    [Test]
    public void Intersect_BasicCross_ReturnsCorrectPoint()
    {
        var a = new Line(new Vector3(0, 1, 0), new Vector3(2, 1, 0)); // Y=1 수평선
        var b = new Line(new Vector3(1, 0, 0), new Vector3(1, 2, 0)); // X=1 수직선
        Vector3? result = a.Intersect(b);

        Assert.IsNotNull(result);
        Assert.AreEqual(1f, result.Value.x, Tolerance);
        Assert.AreEqual(1f, result.Value.y, Tolerance);
        Assert.AreEqual(0f, result.Value.z, Tolerance);
    }

    /// <summary>
    /// 함정: 평행선 → null 이어야 한다
    /// </summary>
    [Test]
    public void Intersect_ParallelLines_ReturnsNull()
    {
        var a = new Line(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        var b = new Line(new Vector3(0, 1, 0), new Vector3(1, 1, 0));
        Assert.IsNull(a.Intersect(b));
    }

    /// <summary>
    /// 함정: 일치선도 null 이어야 한다 (무한히 많은 교점 → 정의 불가)
    /// </summary>
    [Test]
    public void Intersect_CoincidentLines_ReturnsNull()
    {
        var a = new Line(new Vector3(0, 0, 0), new Vector3(3, 0, 0));
        var b = new Line(new Vector3(1, 0, 0), new Vector3(5, 0, 0));
        Assert.IsNull(a.Intersect(b));
    }

    /// <summary>
    /// 함정 (핵심): 3D 꼬인 관계(skew lines) → null 이어야 한다
    /// X축 방향 선 vs Y=0,Z=1 위의 Y축 방향 선 → 절대 만나지 않음
    /// directionCross ≠ 0 이므로 t 계산은 되지만 Contains 에서 걸러져야 함
    /// </summary>
    [Test]
    public void Intersect_SkewLines_ReturnsNull()
    {
        var a = new Line(new Vector3(0, 0, 0), new Vector3(10, 0, 0)); // X축
        var b = new Line(new Vector3(0, 0, 1), new Vector3(0, 10, 1)); // Z=1 위의 Y축
        Assert.IsNull(a.Intersect(b));
    }

    /// <summary>
    /// 교환 법칙 확인: a.Intersect(b) 와 b.Intersect(a) 는 같은 점이어야 한다
    /// </summary>
    [Test]
    public void Intersect_Commutative_SameResult()
    {
        var a = new Line(new Vector3(0, 2, 0), new Vector3(4, 2, 0));
        var b = new Line(new Vector3(2, 0, 0), new Vector3(2, 4, 0));

        Vector3? fromA = a.Intersect(b);
        Vector3? fromB = b.Intersect(a);

        Assert.IsNotNull(fromA);
        Assert.IsNotNull(fromB);
        Assert.AreEqual(fromA.Value.x, fromB.Value.x, Tolerance);
        Assert.AreEqual(fromA.Value.y, fromB.Value.y, Tolerance);
        Assert.AreEqual(fromA.Value.z, fromB.Value.z, Tolerance);
    }

    // ============================================================
    // [5] Contains — 점이 선 위에 있는지
    // ============================================================

    /// <summary>
    /// 시작점 자체는 반드시 선 위에 있다
    /// </summary>
    [Test]
    public void Contains_StartPoint_ReturnsTrue()
    {
        var line = new Line(new Vector3(1, 2, 3), new Vector3(4, 5, 6));
        Assert.IsTrue(line.Contains(new Vector3(1, 2, 3)));
    }

    /// <summary>
    /// 끝점도 선 위에 있다
    /// </summary>
    [Test]
    public void Contains_EndPoint_ReturnsTrue()
    {
        var line = new Line(new Vector3(0, 0, 0), new Vector3(3, 4, 0));
        Assert.IsTrue(line.Contains(new Vector3(3, 4, 0)));
    }

    /// <summary>
    /// 함정: 중간점도 선 위에 있어야 한다
    /// (0,0,0)→(4,0,0) 의 중간 (2,0,0)
    /// </summary>
    [Test]
    public void Contains_Midpoint_ReturnsTrue()
    {
        var line = new Line(new Vector3(0, 0, 0), new Vector3(4, 0, 0));
        Assert.IsTrue(line.Contains(new Vector3(2, 0, 0)));
    }

    /// <summary>
    /// 함정: 선분(segment)이 아닌 무한 직선 기준 → 연장선 위의 점도 true
    /// (0,0,0)→(1,0,0) 의 연장선 위 (100,0,0) → Line 은 무한선이므로 true
    /// </summary>
    [Test]
    public void Contains_ExtendedLine_BeyondEndPoint_ReturnsTrue()
    {
        var line = new Line(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        Assert.IsTrue(line.Contains(new Vector3(100, 0, 0))); // 연장선
    }

    /// <summary>
    /// 선에서 Tolerance(0.02f) 바깥으로 벗어난 점 → false
    /// [2026.04.24 수정] Y 오프셋 0.001f → 0.03f 로 변경
    ///                  Tolerance 가 0.02f 로 변경됨에 따라 0.001f 는
    ///                  허용 범위 내로 들어가 테스트가 실패하는 문제 수정
    /// </summary>
    [Test]
    public void Contains_OffLine_ReturnsFalse()
    {
        var line = new Line(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        Assert.IsFalse(line.Contains(new Vector3(0.5f, 0.03f, 0))); // [2026.04.24 수정] 0.001f → 0.03f
    }

    // ============================================================
    // [6] Project — 점을 선에 투영
    // ============================================================

    /// <summary>
    /// 기본 투영: (3,5,0) → X축 선 → 투영점 (3,0,0)
    /// </summary>
    [Test]
    public void Project_PointAboveLine_ReturnsFootOfPerpendicular()
    {
        var line = new Line(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        Vector3 result = line.Project(new Vector3(3, 5, 0));

        Assert.AreEqual(3f, result.x, Tolerance);
        Assert.AreEqual(0f, result.y, Tolerance);
        Assert.AreEqual(0f, result.z, Tolerance);
    }

    /// <summary>
    /// 함정: 이미 선 위에 있는 점을 투영하면 → 자기 자신이 나와야 한다
    /// </summary>
    [Test]
    public void Project_PointOnLine_ReturnsSamePoint()
    {
        var line = new Line(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        Vector3 point = new Vector3(2.5f, 0, 0);
        Vector3 result = line.Project(point);

        Assert.AreEqual(point.x, result.x, Tolerance);
        Assert.AreEqual(point.y, result.y, Tolerance);
        Assert.AreEqual(point.z, result.z, Tolerance);
    }

    /// <summary>
    /// 함정: 시작점보다 뒤쪽(-t 방향)에 있는 점도 올바르게 투영되어야 한다
    /// Line 은 무한선이므로 t가 음수여도 정상 동작해야 함
    /// (-5, 3, 0) → X축 선 → 투영점 (-5, 0, 0)
    /// </summary>
    [Test]
    public void Project_PointBehindStartPoint_ReturnsCorrectProjection()
    {
        var line = new Line(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        Vector3 result = line.Project(new Vector3(-5, 3, 0));

        Assert.AreEqual(-5f, result.x, Tolerance);
        Assert.AreEqual(0f, result.y, Tolerance);
        Assert.AreEqual(0f, result.z, Tolerance);
    }

    /// <summary>
    /// 3D 사선에 투영: (0,0,0)→(1,1,0) 선에 (2,0,0) 투영
    /// 방향벡터 = (1/√2, 1/√2, 0)
    /// t = Dot((2,0,0), (1/√2, 1/√2, 0)) = 2/√2 = √2
    /// Q = (0,0,0) + (1/√2, 1/√2, 0) * √2 = (1,1,0)
    /// </summary>
    [Test]
    public void Project_3D_DiagonalLine_ReturnsCorrectProjection()
    {
        var line = new Line(new Vector3(0, 0, 0), new Vector3(2, 2, 0));
        Vector3 result = line.Project(new Vector3(2, 0, 0));

        Assert.AreEqual(1f, result.x, 0.0001f);
        Assert.AreEqual(1f, result.y, 0.0001f);
        Assert.AreEqual(0f, result.z, 0.0001f);
    }

    // ============================================================
    // [7] Distance — 점과 선 사이 최단 거리
    // ============================================================

    /// <summary>
    /// 기본 거리: (0,3,0) 에서 X축 선까지 거리 = 3
    /// </summary>
    [Test]
    public void Distance_PointAboveLine_ReturnsCorrectDistance()
    {
        var line = new Line(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        float dist = line.Distance(new Vector3(0, 3, 0));
        Assert.AreEqual(3f, dist, 0.0001f);
    }

    /// <summary>
    /// 선 위의 점은 거리 = 0
    /// </summary>
    [Test]
    public void Distance_PointOnLine_ReturnsZero()
    {
        var line = new Line(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        float dist = line.Distance(new Vector3(7, 0, 0));
        Assert.AreEqual(0f, dist, Tolerance);
    }

    /// <summary>
    /// 함정 (수치 검증): 3D 점-선 거리 수식 직접 검증
    /// 선: (0,0,0)→(1,0,0), 점: (3,4,0)
    /// 투영점: (3,0,0), 거리: |(3,4,0)-(3,0,0)| = 4
    /// </summary>
    [Test]
    public void Distance_3D_KnownValue_ReturnsCorrectDistance()
    {
        var line = new Line(new Vector3(0, 0, 0), new Vector3(5, 0, 0));
        float dist = line.Distance(new Vector3(3, 4, 0));
        Assert.AreEqual(4f, dist, 0.0001f);
    }

    /// <summary>
    /// 함정 (3D): Z 성분이 있는 점의 거리
    /// 선: X축 (0,0,0)→(1,0,0), 점: (2,3,4)
    /// 투영점: (2,0,0), 거리: |(0,3,4)| = √(9+16) = 5
    /// </summary>
    [Test]
    public void Distance_3D_WithZComponent_ReturnsCorrectDistance()
    {
        var line = new Line(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        float dist = line.Distance(new Vector3(2, 3, 4));
        Assert.AreEqual(5f, dist, 0.0001f);
    }

    // ============================================================
    // [8] 복합 트랩 케이스 — 여러 메서드 연쇄 검증
    // ============================================================

    /// <summary>
    /// 복합 트랩 1: IsCoincident 이면 IsParallel 은 false 이어야 한다 (상호 배타)
    /// 두 조건은 동시에 true 가 될 수 없음
    /// </summary>
    [Test]
    public void Trap_CoincidentAndParallel_AreMutuallyExclusive()
    {
        var a = new Line(new Vector3(0, 0, 0), new Vector3(3, 0, 0));
        var b = new Line(new Vector3(-1, 0, 0), new Vector3(5, 0, 0)); // 동일 X축

        bool coincident = a.IsCoincident(b);
        bool parallel = a.IsParallel(b);

        Assert.IsTrue(coincident != parallel,
            $"IsCoincident={coincident}, IsParallel={parallel} — 둘 다 같은 값이면 안 됨");
    }

    /// <summary>
    /// 복합 트랩 2: 교점이 존재하면 그 점은 양쪽 선 위에 있어야 한다 (Contains 연계)
    /// </summary>
    [Test]
    public void Trap_IntersectPoint_MustBeContainedInBothLines()
    {
        var a = new Line(new Vector3(0, 3, 0), new Vector3(6, 3, 0)); // Y=3 수평선
        var b = new Line(new Vector3(3, 0, 0), new Vector3(3, 6, 0)); // X=3 수직선

        Vector3? pt = a.Intersect(b);
        Assert.IsNotNull(pt, "교점이 null — Intersect 오류");

        Assert.IsTrue(a.Contains(pt.Value), "교점이 선 A 위에 없음");
        Assert.IsTrue(b.Contains(pt.Value), "교점이 선 B 위에 없음");
    }

    /// <summary>
    /// 복합 트랩 3: 투영점은 반드시 선 위에 있어야 한다 (Project + Contains 연계)
    /// 임의의 점 어디서 투영해도 결과는 항상 Contains = true
    /// </summary>
    [Test]
    public void Trap_ProjectedPoint_AlwaysContainedInLine()
    {
        var line = new Line(new Vector3(1, 2, 3), new Vector3(7, 5, 9));

        Vector3[] testPoints = {
            new Vector3(0, 0, 0),
            new Vector3(100, -50, 30),
            new Vector3(1, 2, 3),   // 시작점 자체
            new Vector3(7, 5, 9),   // 끝점 자체
            new Vector3(-10, 20, -5)
        };

        foreach (var pt in testPoints)
        {
            Vector3 projected = line.Project(pt);
            Assert.That(line.Distance(projected), Is.LessThan(0.001f),
                $"투영점 {projected} 가 선 위에 없음 (원점: {pt})");
        }
    }

    /// <summary>
    /// 복합 트랩 4: Distance == Vector3.Distance(point, Project(point)) 가 항상 같아야 한다
    /// Distance 내부 로직이 Project 와 일관성을 유지하는지 교차 검증
    /// </summary>
    [Test]
    public void Trap_Distance_EqualToManualProjectionDistance()
    {
        var line = new Line(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
        Vector3 point = new Vector3(3, -2, 5);

        float distFromMethod = line.Distance(point);
        float distManual = Vector3.Distance(point, line.Project(point));

        Assert.AreEqual(distManual, distFromMethod, Tolerance,
            "Distance() 와 Vector3.Distance(point, Project()) 결과가 다름");
    }

    /// <summary>
    /// 복합 트랩 5: Tolerance(0.02f) 이내의 노이즈를 가진 점은 Contains = true 이어야 한다
    /// [2026.04.24 수정] 노이즈값 1e-7 → 0.01f 로 변경 (Tolerance 0.02f 이내)
    /// </summary>
    [Test]
    public void Trap_FloatingPoint_SmallNoise_StillContained()
    {
        var line = new Line(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        Vector3 noisyPoint = new Vector3(2f, 0.01f, 0f); // [2026.04.24 수정] Tolerance(0.02f) 이내
        Assert.IsTrue(line.Contains(noisyPoint),
            "Tolerance 이내 노이즈를 선 위 점으로 인식 못함");
    }

    /// <summary>
    /// 복합 트랩 6: Tolerance(0.02f) 경계 바로 바깥 → Contains = false 이어야 한다
    /// [2026.04.24 수정] 기준값 수정 (Tolerance 0.02f 바깥인 0.03f 사용)
    /// </summary>
    [Test]
    public void Trap_FloatingPoint_JustOutsideTolerance_NotContained()
    {
        var line = new Line(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        Vector3 point = new Vector3(2f, 0.03f, 0f); // [2026.04.24 수정] Tolerance(0.02f) 바깥
        Assert.IsFalse(line.Contains(point),
            "Tolerance 바깥 점이 선 위로 인식됨");
    }
}