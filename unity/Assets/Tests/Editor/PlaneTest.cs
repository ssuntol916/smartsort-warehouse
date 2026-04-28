// ============================================================
// 파일명  : PlaneTest.cs
// 역할    : Plane 클래스 단위 테스트 (EditMode)
// 작성자  : 이현화
// 작성일  : 2026-03-25
// 수정이력: 2026-04-22 - Contains_Point_SmallEpsilonOff_ReturnsFalse 테스트 케이스 수정
//                        (Tolerance 가 0.01f 로 변경됨에 따라 테스트 기준값 조정)
// ============================================================

using NUnit.Framework;
using UnityEngine;

public class PlaneTest
{
    // ============================================================
    // 허용 오차
    // ============================================================
    private const float Delta = 0.0001f;

    // ============================================================
    // 헬퍼 — 자주 쓰는 기준 면 생성
    // ============================================================

    /// XY 평면 (z=0): A(0,0,0) B(1,0,0) C(0,1,0)  → Normal = (0,0,1)
    private Plane XYPlane() => new Plane(
        new Vector3(0, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(0, 1, 0));

    /// XY 평면에서 z=1 만큼 올라간 평행면
    private Plane XYPlaneShiftedZ1() => new Plane(
        new Vector3(0, 0, 1),
        new Vector3(1, 0, 1),
        new Vector3(0, 1, 1));

    /// XZ 평면 (y=0): Normal = (0,1,0) → XY 면과 수직
    private Plane XZPlane() => new Plane(
        new Vector3(0, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(0, 0, 1));

    // ============================================================
    // 1. 속성(Properties) 검증
    // ============================================================

    [Test]
    public void Normal_XYPlane_ReturnsPositiveZAxis()
    {
        // 기본 XY 평면의 법선은 (0,0,1) 이어야 한다
        Plane plane = XYPlane();

        Assert.AreEqual(0f, plane.Normal.x, Delta);
        Assert.AreEqual(0f, plane.Normal.y, Delta);
        Assert.AreEqual(1f, plane.Normal.z, Delta);
    }

    [Test]
    public void Normal_ReversedWindingOrder_ReturnsNegativeNormal()
    {
        // 세 점의 순서를 반대로 하면 법선이 반대 방향이 되어야 한다
        // A(0,0,0) → C(0,1,0) → B(1,0,0) 순서로 뒤집으면 Normal = (0,0,-1)
        Plane plane = new Plane(
            new Vector3(0, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(1, 0, 0));

        Assert.AreEqual( 0f, plane.Normal.x, Delta);
        Assert.AreEqual( 0f, plane.Normal.y, Delta);
        Assert.AreEqual(-1f, plane.Normal.z, Delta);
    }

    [Test]
    public void Normal_IsNormalized_LengthEqualsOne()
    {
        // 법선 벡터는 반드시 단위 벡터(길이 = 1)여야 한다
        // 세 점이 정수가 아닌 비대칭 좌표일 때도 성립해야 함
        Plane plane = new Plane(
            new Vector3(1.5f, 2.3f, 0f),
            new Vector3(3.7f, 2.3f, 0f),
            new Vector3(1.5f, 5.1f, 0f));

        Assert.AreEqual(1f, plane.Normal.magnitude, Delta);
    }

    [Test]
    public void Centroid_BasicTriangle_ReturnsCorrectCentroid()
    {
        // 무게중심 = (A + B + C) / 3
        // A(0,0,0) B(3,0,0) C(0,3,0) → (1, 1, 0)
        Plane plane = new Plane(
            new Vector3(0, 0, 0),
            new Vector3(3, 0, 0),
            new Vector3(0, 3, 0));

        Assert.AreEqual(1f, plane.Centroid.x, Delta);
        Assert.AreEqual(1f, plane.Centroid.y, Delta);
        Assert.AreEqual(0f, plane.Centroid.z, Delta);
    }

    [Test]
    public void Centroid_NegativeCoordinates_ReturnsCorrectCentroid()
    {
        // 음수 좌표가 섞인 경우
        // A(-3,0,0) B(3,0,0) C(0,0,6) → (0, 0, 2)
        Plane plane = new Plane(
            new Vector3(-3, 0, 0),
            new Vector3( 3, 0, 0),
            new Vector3( 0, 0, 6));

        Assert.AreEqual(0f, plane.Centroid.x, Delta);
        Assert.AreEqual(0f, plane.Centroid.y, Delta);
        Assert.AreEqual(2f, plane.Centroid.z, Delta);
    }

    // ============================================================
    // 2. IsParallel
    // ============================================================

    [Test]
    public void IsParallel_ZOffset_ReturnsTrue()
    {
        // 기본 케이스: XY 평면과 z=1 평행면
        Assert.IsTrue(XYPlane().IsParallel(XYPlaneShiftedZ1()));
    }

    [Test]
    public void IsParallel_SamePlane_ReturnsFalse()
    {
        // 동일 평면은 평행이 아니라 일치 → false
        Assert.IsFalse(XYPlane().IsParallel(XYPlane()));
    }

    [Test]
    public void IsParallel_Perpendicular_ReturnsFalse()
    {
        // 수직인 두 면은 평행이 아님
        Assert.IsFalse(XYPlane().IsParallel(XZPlane()));
    }

    [Test]
    public void IsParallel_OppositeNormal_SameOffset_ReturnsTrue()
    {
        // 법선 방향이 반대(권취 순서 반대)여도 실제로 평행한 면이면 true
        // XY 평면 (Normal +Z) vs z=2에서 반대 권취 (Normal -Z)
        Plane planeA = XYPlane();
        Plane planeB = new Plane(
            new Vector3(0, 0, 2),
            new Vector3(0, 1, 2),   // 권취 반대 (B, C 바뀜)
            new Vector3(1, 0, 2));

        Assert.IsTrue(planeA.IsParallel(planeB));
    }

    [Test]
    public void IsParallel_Large3DOffset_ReturnsTrue()
    {
        // 3D 공간에서 비축 방향으로 오프셋된 평행면
        // 기울어진 기준면: A(0,0,0) B(1,1,0) C(0,1,1) → 45° 기울어진 면
        // 같은 면을 (5,5,5) 만큼 평행이동한 면과 비교
        Vector3 offset = new Vector3(5, 5, 5);
        Plane planeA = new Plane(
            new Vector3(0, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 1));
        Plane planeB = new Plane(
            new Vector3(0, 0, 0) + offset,
            new Vector3(1, 1, 0) + offset,
            new Vector3(0, 1, 1) + offset);

        Assert.IsTrue(planeA.IsParallel(planeB));
    }

    [Test]
    public void IsParallel_SlightlyTilted_ReturnsFalse()
    {
        // 거의 평행하지만 살짝 기울어진 면 → false
        // XY 평면 vs z=1이지만 한 꼭짓점이 z=1.1 로 미묘하게 기울어진 경우
        Plane planeA = XYPlane();
        Plane planeB = new Plane(
            new Vector3(0, 0, 1f),
            new Vector3(1, 0, 1f),
            new Vector3(0, 1, 1.1f));  // 살짝 기울임

        Assert.IsFalse(planeA.IsParallel(planeB));
    }

    [Test]
    public void IsParallel_Symmetry_BothDirectionsMatch()
    {
        // 대칭성: A.IsParallel(B) == B.IsParallel(A)
        Plane planeA = XYPlane();
        Plane planeB = XYPlaneShiftedZ1();

        Assert.AreEqual(planeA.IsParallel(planeB), planeB.IsParallel(planeA));
    }

    // ============================================================
    // 3. IsPerpendicular
    // ============================================================

    [Test]
    public void IsPerpendicular_XYandXZ_ReturnsTrue()
    {
        // 기본 케이스: XY 면과 XZ 면은 서로 수직
        Assert.IsTrue(XYPlane().IsPerpendicular(XZPlane()));
    }

    [Test]
    public void IsPerpendicular_SamePlane_ReturnsFalse()
    {
        // 같은 평면은 수직이 아님 (Dot = 1)
        Assert.IsFalse(XYPlane().IsPerpendicular(XYPlane()));
    }

    [Test]
    public void IsPerpendicular_ParallelPlanes_ReturnsFalse()
    {
        // 평행한 두 면은 수직이 아님
        Assert.IsFalse(XYPlane().IsPerpendicular(XYPlaneShiftedZ1()));
    }

    [Test]
    public void IsPerpendicular_YZPlane_WithXYPlane_ReturnsTrue()
    {
        // YZ 평면(Normal = 1,0,0) vs XY 평면(Normal = 0,0,1) → 수직
        Plane yzPlane = new Plane(
            new Vector3(0, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 1));

        Assert.IsTrue(XYPlane().IsPerpendicular(yzPlane));
    }

    [Test]
    public void IsPerpendicular_45DegreePlane_WithXYPlane_ReturnsFalse()
    {
        // XY 평면과 45° 각도인 면은 수직이 아님
        // Normal = (0, sin45, cos45) 방향 평면을 구성
        // A(0,0,0) B(1,0,0) C(0,1,1) → AB=(1,0,0), AC=(0,1,1) → Normal = (0,-1,1).normalized
        Plane plane45 = new Plane(
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 1));

        Assert.IsFalse(XYPlane().IsPerpendicular(plane45));
    }

    [Test]
    public void IsPerpendicular_OppositeNormals_StillReturnsTrue()
    {
        // Abs(Dot) 를 쓰므로 법선 방향이 반대여도 수직이면 true
        // YZ 평면(Normal = +X) 권취 반대 버전 → Normal = -X → Dot(XY, YZ reversed) = 0 이어야 함
        Plane yzReversed = new Plane(
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 1),   // 권취 반대
            new Vector3(0, 1, 0));

        Assert.IsTrue(XYPlane().IsPerpendicular(yzReversed));
    }

    [Test]
    public void IsPerpendicular_Symmetry_BothDirectionsMatch()
    {
        // 대칭성: A.IsPerpendicular(B) == B.IsPerpendicular(A)
        Plane planeA = XYPlane();
        Plane planeB = XZPlane();

        Assert.AreEqual(planeA.IsPerpendicular(planeB), planeB.IsPerpendicular(planeA));
    }

    // ============================================================
    // 4. IsCoincident
    // ============================================================

    [Test]
    public void IsCoincident_SamePlane_ReturnsTrue()
    {
        // 기본 케이스: 동일한 세 점으로 정의한 면
        Assert.IsTrue(XYPlane().IsCoincident(XYPlane()));
    }

    [Test]
    public void IsCoincident_DifferentTriangleSamePlane_ReturnsTrue()
    {
        // 같은 평면(z=0)이지만 완전히 다른 삼각형으로 정의
        Plane planeA = XYPlane();          // A(0,0,0) B(1,0,0) C(0,1,0)
        Plane planeB = new Plane(          // 더 큰 삼각형, 다른 좌표
            new Vector3(2, 3, 0),
            new Vector3(5, 0, 0),
            new Vector3(-1, -4, 0));

        Assert.IsTrue(planeA.IsCoincident(planeB));
    }

    [Test]
    public void IsCoincident_ParallelPlane_ReturnsFalse()
    {
        // 평행하지만 다른 위치면 일치 아님
        Assert.IsFalse(XYPlane().IsCoincident(XYPlaneShiftedZ1()));
    }

    [Test]
    public void IsCoincident_PerpendicularPlane_ReturnsFalse()
    {
        // 수직인 면은 일치가 아님
        Assert.IsFalse(XYPlane().IsCoincident(XZPlane()));
    }

    [Test]
    public void IsCoincident_OppositeWindingOrder_SamePlane_ReturnsTrue()
    {
        // 권취 순서 반대 (Normal 방향 반대)여도 같은 평면이면 true
        Plane planeA = XYPlane();  // Normal = (0,0,+1)
        Plane planeB = new Plane(  // Normal = (0,0,-1) 이지만 z=0 평면
            new Vector3(0, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(1, 0, 0));

        Assert.IsTrue(planeA.IsCoincident(planeB));
    }

    [Test]
    public void IsCoincident_TranslatedInPlane_ReturnsTrue()
    {
        // 동일 평면 위의 점들을 XY 방향으로 이동해도 일치
        Plane planeA = XYPlane();
        Plane planeB = new Plane(
            new Vector3(10, 20, 0),
            new Vector3(11, 20, 0),
            new Vector3(10, 21, 0));

        Assert.IsTrue(planeA.IsCoincident(planeB));
    }

    [Test]
    public void IsCoincident_TiltedPlane_SameLocation_ReturnsTrue()
    {
        // 비축(45°) 평면끼리 동일 위치 비교
        Plane planeA = new Plane(
            new Vector3(0, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 1));
        // 같은 평면을 다른 세 점으로 정의
        // 평면 방정식 ax+by+cz=d 를 만족하는 다른 점 사용
        // planeA.Normal 방향은 Cross(B-A, C-A) = Cross((1,1,0),(0,1,1)) = (1,-1,1).normalized
        // planeA.Normal·A = 0  → d=0 → 평면: x - y + z = 0
        // 검증 점: (1,0,-1) → 1-0-1=0 ✅  (2,1,- 1) → 2-1-1=0 ✅  (0,2,2) → 0-2+2=0 ✅
        Plane planeB = new Plane(
            new Vector3(1,  0, -1),
            new Vector3(2,  1, -1),
            new Vector3(0,  2,  2));

        Assert.IsTrue(planeA.IsCoincident(planeB));
    }

    [Test]
    public void IsCoincident_Symmetry_BothDirectionsMatch()
    {
        // 대칭성
        Plane planeA = XYPlane();
        Plane planeB = new Plane(
            new Vector3(5, 3, 0),
            new Vector3(7, 3, 0),
            new Vector3(5, 9, 0));

        Assert.AreEqual(planeA.IsCoincident(planeB), planeB.IsCoincident(planeA));
    }

    // ============================================================
    // 5. Contains(Vector3 point)
    // ============================================================

    [Test]
    public void Contains_Point_DefinitionVertexOnPlane_ReturnsTrue()
    {
        // 면을 정의한 꼭짓점 자체는 반드시 면 위에 있어야 한다
        Plane plane = XYPlane();

        Assert.IsTrue(plane.Contains(plane.PointA));
        Assert.IsTrue(plane.Contains(plane.PointB));
        Assert.IsTrue(plane.Contains(plane.PointC));
    }

    [Test]
    public void Contains_Point_CentroidOnPlane_ReturnsTrue()
    {
        // 무게중심도 같은 평면 위에 있어야 한다
        Plane plane = XYPlane();

        Assert.IsTrue(plane.Contains(plane.Centroid));
    }

    [Test]
    public void Contains_Point_ArbitraryPointOnPlane_ReturnsTrue()
    {
        // XY 평면 위의 임의 점 (z=0 이면 무조건 포함)
        Plane plane = XYPlane();

        Assert.IsTrue(plane.Contains(new Vector3(42f, -7.3f, 0f)));
    }

    [Test]
    public void Contains_Point_OffPlane_ReturnsFalse()
    {
        // 평면에서 벗어난 점
        Plane plane = XYPlane();

        Assert.IsFalse(plane.Contains(new Vector3(0f, 0f, 0.01f)));
    }

    [Test]
    public void Contains_Point_NegativeCoordOnPlane_ReturnsTrue()
    {
        // 음수 좌표도 평면 위에 있으면 true
        Plane plane = XYPlane();

        Assert.IsTrue(plane.Contains(new Vector3(-100f, -200f, 0f)));
    }

    [Test]
    public void Contains_Point_SmallEpsilonOff_ReturnsFalse()
    {
        // [2026.04.22 수정]Tolerance(0.01f) 바로 밖 → false
        Plane plane = XYPlane();

        Assert.IsFalse(plane.Contains(new Vector3(0f, 0f, 0.02f)));
    }

    [Test]
    public void Contains_Point_TiltedPlane_PointOnIt_ReturnsTrue()
    {
        // 기울어진 평면 위의 점
        // 평면: x - y + z = 0 (위 IsCoincident 테스트와 동일 평면)
        Plane plane = new Plane(
            new Vector3(0, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 1));

        // (2, 1, -1): 2 - 1 + (-1) = 0 ✅
        Assert.IsTrue(plane.Contains(new Vector3(2f, 1f, -1f)));
    }

    [Test]
    public void Contains_Point_TiltedPlane_PointOffIt_ReturnsFalse()
    {
        // 기울어진 평면 밖의 점
        Plane plane = new Plane(
            new Vector3(0, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 1));

        // (1, 0, 0): 1 - 0 + 0 = 1 ≠ 0 → 면 위에 없음
        Assert.IsFalse(plane.Contains(new Vector3(1f, 0f, 0f)));
    }

    // ============================================================
    // 6. Contains(Line line)
    // ============================================================

    [Test]
    public void Contains_Line_BothEndpointsOnPlane_ReturnsTrue()
    {
        // XY 평면 위의 선: 두 끝점 모두 z=0
        Plane plane = XYPlane();
        Line line = new Line(
            new Vector3(1f, 2f, 0f),
            new Vector3(5f, 3f, 0f));

        Assert.IsTrue(plane.Contains(line));
    }

    [Test]
    public void Contains_Line_OneEndpointOff_ReturnsFalse()
    {
        // 한 쪽 끝점만 평면 위에 있는 경우
        Plane plane = XYPlane();
        Line line = new Line(
            new Vector3(0f, 0f, 0f),    // 평면 위
            new Vector3(0f, 0f, 1f));   // 평면 밖

        Assert.IsFalse(plane.Contains(line));
    }

    [Test]
    public void Contains_Line_BothEndpointsOff_ReturnsFalse()
    {
        // 두 끝점 모두 평면 밖인 경우
        Plane plane = XYPlane();
        Line line = new Line(
            new Vector3(0f, 0f, 1f),
            new Vector3(1f, 1f, 1f));

        Assert.IsFalse(plane.Contains(line));
    }

    [Test]
    public void Contains_Line_DiagonalLineOnPlane_ReturnsTrue()
    {
        // 대각선 방향 선이 XY 평면 위에 있는 경우
        Plane plane = XYPlane();
        Line line = new Line(
            new Vector3(-10f, -10f, 0f),
            new Vector3(10f,  10f,  0f));

        Assert.IsTrue(plane.Contains(line));
    }

    [Test]
    public void Contains_Line_TiltedPlane_LineOnIt_ReturnsTrue()
    {
        // 기울어진 평면(x - y + z = 0)에 놓인 선
        Plane plane = new Plane(
            new Vector3(0, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 1));

        // (2,1,-1) 과 (1,0,-1) 모두 x-y+z=0 만족
        Line line = new Line(
            new Vector3(2f,  1f, -1f),
            new Vector3(1f,  0f, -1f));

        Assert.IsTrue(plane.Contains(line));
    }

    [Test]
    public void Contains_Line_NegativeCoordinates_ReturnsTrue()
    {
        // 음수 좌표 영역의 선이 XY 평면 위에 있는 경우
        Plane plane = XYPlane();
        Line line = new Line(
            new Vector3(-5f, -3f, 0f),
            new Vector3(-1f, -8f, 0f));

        Assert.IsTrue(plane.Contains(line));
    }

    // ============================================================
    // 7. 상호 관계 복합 검증
    // ============================================================

    [Test]
    public void Relation_ParallelAndCoincident_NeverBothTrue()
    {
        // IsParallel(true)이면 IsCoincident(false), 반대도 성립
        Plane planeA = XYPlane();
        Plane planeB = XYPlaneShiftedZ1();

        bool parallel    = planeA.IsParallel(planeB);
        bool coincident  = planeA.IsCoincident(planeB);

        // 평행하면 일치가 아니어야 한다
        Assert.IsTrue(parallel);
        Assert.IsFalse(coincident);
        Assert.IsFalse(parallel && coincident, "평행 면은 동시에 일치할 수 없다");
    }

    [Test]
    public void Relation_CoincidentPlane_NotParallel_NotPerpendicular()
    {
        // 일치 면: IsParallel=false, IsPerpendicular=false, IsCoincident=true
        Plane planeA = XYPlane();
        Plane planeB = new Plane(
            new Vector3(3, 4, 0),
            new Vector3(9, 4, 0),
            new Vector3(3, 7, 0));

        Assert.IsTrue (planeA.IsCoincident   (planeB));
        Assert.IsFalse(planeA.IsParallel     (planeB));
        Assert.IsFalse(planeA.IsPerpendicular(planeB));
    }

    [Test]
    public void Relation_PerpendicularButNotParallelOrCoincident()
    {
        // 수직 면: IsPerpendicular=true, IsParallel=false, IsCoincident=false
        Plane planeA = XYPlane();
        Plane planeB = XZPlane();

        Assert.IsTrue (planeA.IsPerpendicular(planeB));
        Assert.IsFalse(planeA.IsParallel     (planeB));
        Assert.IsFalse(planeA.IsCoincident   (planeB));
    }

    [Test]
    public void Relation_ContainedLineCentroidOnPlane()
    {
        // Contains(Line) 가 true 이면 두 끝점의 중점도 Contains(Vector3) = true
        Plane plane = XYPlane();
        Vector3 a = new Vector3(2f, 0f, 0f);
        Vector3 b = new Vector3(0f, 4f, 0f);
        Line line = new Line(a, b);

        Vector3 midpoint = (a + b) / 2f;

        Assert.IsTrue(plane.Contains(line));
        Assert.IsTrue(plane.Contains(midpoint));
    }
}
