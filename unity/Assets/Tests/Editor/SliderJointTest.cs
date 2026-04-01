// ============================================================
// 파일명  : SliderJointTest.cs
// 역할    : SliderJoint 클래스의 EditMode 단위 테스트
//           Line·Plane 기반 슬라이더 조인트의 유효성 검사, 위치 클램프, 구속 조건을 검증한다.
// 작성자  : 이현화
// 작성일  : 2026-04-01
// 수정이력: 
// ============================================================

using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class SliderJointTest
{
    private const float Tolerance = 0.0001f;

    #region 헬퍼 메서드

    /**
     * @brief  테스트용 기본 SliderJoint 를 생성한다.
     *         일치하는 Line 과 Plane 으로 유효한 조인트를 반환한다.
     */
    private SliderJoint CreateValidSliderJoint(float minPos = 0f, float maxPos = 100f)
    {
        // 일치하는 Line 생성 (X축 방향)
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);

        // 일치하는 Plane 생성 (XZ 평면)
        Plane planeA = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        Plane planeB = new Plane(Vector3.zero, Vector3.right, Vector3.forward);

        return new SliderJoint(lineA, lineB, planeA, planeB, minPos, maxPos);
    }

    /**
     * @brief  일치하지 않는 Line 으로 SliderJoint 를 생성한다.
     */
    private SliderJoint CreateInvalidLineSliderJoint()
    {
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(new Vector3(0, 5, 0), new Vector3(1, 5, 0));  // 평행하지만 일치하지 않음

        Plane planeA = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        Plane planeB = new Plane(Vector3.zero, Vector3.right, Vector3.forward);

        return new SliderJoint(lineA, lineB, planeA, planeB, 0f, 100f);
    }

    /**
     * @brief  일치하지 않는 Plane 으로 SliderJoint 를 생성한다.
     */
    private SliderJoint CreateInvalidPlaneSliderJoint()
    {
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);

        Plane planeA = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        Plane planeB = new Plane(new Vector3(0, 5, 0), new Vector3(1, 5, 0), new Vector3(0, 5, 1));  // 다른 평면

        return new SliderJoint(lineA, lineB, planeA, planeB, 0f, 100f);
    }

    #endregion

    #region 생성자 테스트

    /**
     * @brief  기본 생성자 테스트 - 유효한 매개변수로 SliderJoint 가 정상 생성되는지 검증한다.
     */
    [Test]
    public void Constructor_ValidParameters_CreatesJoint()
    {
        // Arrange & Act
        SliderJoint joint = CreateValidSliderJoint(0f, 100f);

        // Assert
        Assert.IsNotNull(joint);
        Assert.AreEqual(0f, joint.MinPosition, Tolerance);
        Assert.AreEqual(100f, joint.MaxPosition, Tolerance);
        Assert.AreEqual(0f, joint.CurrentPosition, Tolerance);
    }

    /**
     * @brief  음수 범위 생성 테스트 - min/max 가 모두 음수일 때 정상 생성되는지 검증한다.
     */
    [Test]
    public void Constructor_NegativeRange_CreatesJoint()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        Plane planeA = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        Plane planeB = new Plane(Vector3.zero, Vector3.right, Vector3.forward);

        // Act
        SliderJoint joint = new SliderJoint(lineA, lineB, planeA, planeB, -100f, -50f);

        // Assert
        Assert.AreEqual(-100f, joint.MinPosition, Tolerance);
        Assert.AreEqual(-50f, joint.MaxPosition, Tolerance);
    }

    /**
     * @brief  비대칭 범위 생성 테스트 - min 이 음수, max 가 양수일 때 정상 생성되는지 검증한다.
     */
    [Test]
    public void Constructor_AsymmetricRange_CreatesJoint()
    {
        // Arrange & Act
        SliderJoint joint = CreateValidSliderJoint(-50f, 150f);

        // Assert
        Assert.AreEqual(-50f, joint.MinPosition, Tolerance);
        Assert.AreEqual(150f, joint.MaxPosition, Tolerance);
    }

    /**
     * @brief  동일 min/max 생성 테스트 - min == max 일 때 (고정 위치) 정상 생성되는지 검증한다.
     */
    [Test]
    public void Constructor_SameMinMax_CreatesJoint()
    {
        // Arrange & Act
        SliderJoint joint = CreateValidSliderJoint(50f, 50f);

        // Assert
        Assert.AreEqual(50f, joint.MinPosition, Tolerance);
        Assert.AreEqual(50f, joint.MaxPosition, Tolerance);
    }

    /**
     * @brief  Y축 이동 방향 생성 테스트 - Y축 방향 Line 으로 정상 생성되는지 검증한다.
     */
    [Test]
    public void Constructor_YAxisDirection_CreatesJoint()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.up);
        Line lineB = new Line(Vector3.zero, Vector3.up);
        Plane planeA = new Plane(Vector3.zero, Vector3.right, Vector3.up);
        Plane planeB = new Plane(Vector3.zero, Vector3.right, Vector3.up);

        // Act
        SliderJoint joint = new SliderJoint(lineA, lineB, planeA, planeB, 0f, 100f);

        // Assert
        Assert.IsNotNull(joint);
    }

    /**
     * @brief  Z축 이동 방향 생성 테스트 - Z축 방향 Line 으로 정상 생성되는지 검증한다.
     */
    [Test]
    public void Constructor_ZAxisDirection_CreatesJoint()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.forward);
        Line lineB = new Line(Vector3.zero, Vector3.forward);
        Plane planeA = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        Plane planeB = new Plane(Vector3.zero, Vector3.right, Vector3.forward);

        // Act
        SliderJoint joint = new SliderJoint(lineA, lineB, planeA, planeB, 0f, 100f);

        // Assert
        Assert.IsNotNull(joint);
    }

    /**
     * @brief  대각선 이동 방향 생성 테스트 - 대각선 방향 Line 으로 정상 생성되는지 검증한다.
     */
    [Test]
    public void Constructor_DiagonalDirection_CreatesJoint()
    {
        // Arrange
        Vector3 direction = new Vector3(1, 1, 1).normalized;
        Line lineA = new Line(Vector3.zero, direction);
        Line lineB = new Line(Vector3.zero, direction);
        Plane planeA = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        Plane planeB = new Plane(Vector3.zero, Vector3.right, Vector3.forward);

        // Act
        SliderJoint joint = new SliderJoint(lineA, lineB, planeA, planeB, 0f, 100f);

        // Assert
        Assert.IsNotNull(joint);
    }

    #endregion

    #region SetPosition 테스트

    /**
     * @brief  범위 내 위치 설정 테스트 - min/max 범위 내의 위치가 그대로 설정되는지 검증한다.
     */
    [Test]
    public void SetPosition_WithinRange_SetsExactPosition()
    {
        // Arrange
        SliderJoint joint = CreateValidSliderJoint(0f, 100f);

        // Act
        joint.SetPosition(50f);

        // Assert
        Assert.AreEqual(50f, joint.CurrentPosition, Tolerance);
    }

    /**
     * @brief  최소 위치 미만 설정 테스트 - min 보다 작은 위치가 min 으로 클램프되는지 검증한다.
     */
    [Test]
    public void SetPosition_BelowMin_ClampsToMin()
    {
        // Arrange
        SliderJoint joint = CreateValidSliderJoint(0f, 100f);

        // Act
        joint.SetPosition(-50f);

        // Assert
        Assert.AreEqual(0f, joint.CurrentPosition, Tolerance);
    }

    /**
     * @brief  최대 위치 초과 설정 테스트 - max 보다 큰 위치가 max 로 클램프되는지 검증한다.
     */
    [Test]
    public void SetPosition_AboveMax_ClampsToMax()
    {
        // Arrange
        SliderJoint joint = CreateValidSliderJoint(0f, 100f);

        // Act
        joint.SetPosition(150f);

        // Assert
        Assert.AreEqual(100f, joint.CurrentPosition, Tolerance);
    }

    /**
     * @brief  경계값 테스트 - 정확히 min 값이 설정되는지 검증한다.
     */
    [Test]
    public void SetPosition_ExactlyMin_SetsMin()
    {
        // Arrange
        SliderJoint joint = CreateValidSliderJoint(0f, 100f);

        // Act
        joint.SetPosition(0f);

        // Assert
        Assert.AreEqual(0f, joint.CurrentPosition, Tolerance);
    }

    /**
     * @brief  경계값 테스트 - 정확히 max 값이 설정되는지 검증한다.
     */
    [Test]
    public void SetPosition_ExactlyMax_SetsMax()
    {
        // Arrange
        SliderJoint joint = CreateValidSliderJoint(0f, 100f);

        // Act
        joint.SetPosition(100f);

        // Assert
        Assert.AreEqual(100f, joint.CurrentPosition, Tolerance);
    }

    /**
     * @brief  음수 범위에서 설정 테스트 - 음수 범위 내에서 정상 설정되는지 검증한다.
     */
    [Test]
    public void SetPosition_NegativeRange_SetsCorrectly()
    {
        // Arrange
        SliderJoint joint = CreateValidSliderJoint(-100f, -50f);

        // Act
        joint.SetPosition(-75f);

        // Assert
        Assert.AreEqual(-75f, joint.CurrentPosition, Tolerance);
    }

    /**
     * @brief  연속 설정 테스트 - 여러 번 SetPosition 호출 시 마지막 값이 유지되는지 검증한다.
     */
    [Test]
    public void SetPosition_MultipleCalls_LastValuePersists()
    {
        // Arrange
        SliderJoint joint = CreateValidSliderJoint(0f, 100f);

        // Act
        joint.SetPosition(30f);
        joint.SetPosition(70f);
        joint.SetPosition(45f);

        // Assert
        Assert.AreEqual(45f, joint.CurrentPosition, Tolerance);
    }

    /**
     * @brief  음수 범위에서 양수 위치 설정 테스트 - 범위가 음수일 때 양수 위치가 클램프되는지 검증한다.
     */
    [Test]
    public void SetPosition_PositiveInNegativeRange_ClampsToMax()
    {
        // Arrange
        SliderJoint joint = CreateValidSliderJoint(-100f, -50f);

        // Act
        joint.SetPosition(50f);

        // Assert
        Assert.AreEqual(-50f, joint.CurrentPosition, Tolerance);
    }

    /**
     * @brief  매우 작은 값 설정 테스트 - 0.001 단위의 미세 위치가 정확히 처리되는지 검증한다.
     */
    [Test]
    public void SetPosition_VerySmallValue_HandledCorrectly()
    {
        // Arrange
        SliderJoint joint = CreateValidSliderJoint(0f, 1f);

        // Act
        joint.SetPosition(0.001f);

        // Assert
        Assert.AreEqual(0.001f, joint.CurrentPosition, 0.00001f);
    }

    /**
     * @brief  매우 큰 값 설정 테스트 - 큰 범위에서 정상 작동하는지 검증한다.
     */
    [Test]
    public void SetPosition_VeryLargeRange_HandledCorrectly()
    {
        // Arrange
        SliderJoint joint = CreateValidSliderJoint(-10000f, 10000f);

        // Act
        joint.SetPosition(5000f);

        // Assert
        Assert.AreEqual(5000f, joint.CurrentPosition, Tolerance);
    }

    #endregion

    #region GetProjectedDistance 테스트

    /**
     * @brief  X축 이동 투영 거리 테스트 - X축 방향 이동이 정확히 계산되는지 검증한다.
     */
    [Test]
    public void GetProjectedDistance_XAxisMovement_CalculatesCorrectly()
    {
        // Arrange
        Line line = new Line(Vector3.zero, Vector3.right);
        Vector3 currentPos = new Vector3(50f, 0f, 0f);
        Vector3 origin = Vector3.zero;
        Vector3 moveDir = Vector3.right;

        // Act
        float distance = SliderJoint.GetProjectedDistance(line, currentPos, origin, moveDir);

        // Assert
        Assert.AreEqual(50f, distance, Tolerance);
    }

    /**
     * @brief  Y축 이동 투영 거리 테스트 - Y축 방향 이동이 정확히 계산되는지 검증한다.
     */
    [Test]
    public void GetProjectedDistance_YAxisMovement_CalculatesCorrectly()
    {
        // Arrange
        Line line = new Line(Vector3.zero, Vector3.up);
        Vector3 currentPos = new Vector3(0f, 75f, 0f);
        Vector3 origin = Vector3.zero;
        Vector3 moveDir = Vector3.up;

        // Act
        float distance = SliderJoint.GetProjectedDistance(line, currentPos, origin, moveDir);

        // Assert
        Assert.AreEqual(75f, distance, Tolerance);
    }

    /**
     * @brief  Z축 이동 투영 거리 테스트 - Z축 방향 이동이 정확히 계산되는지 검증한다.
     */
    [Test]
    public void GetProjectedDistance_ZAxisMovement_CalculatesCorrectly()
    {
        // Arrange
        Line line = new Line(Vector3.zero, Vector3.forward);
        Vector3 currentPos = new Vector3(0f, 0f, 30f);
        Vector3 origin = Vector3.zero;
        Vector3 moveDir = Vector3.forward;

        // Act
        float distance = SliderJoint.GetProjectedDistance(line, currentPos, origin, moveDir);

        // Assert
        Assert.AreEqual(30f, distance, Tolerance);
    }

    /**
     * @brief  음수 방향 이동 투영 거리 테스트 - 음수 방향 이동이 정확히 계산되는지 검증한다.
     */
    [Test]
    public void GetProjectedDistance_NegativeDirection_CalculatesCorrectly()
    {
        // Arrange
        Line line = new Line(Vector3.zero, Vector3.right);
        Vector3 currentPos = new Vector3(-50f, 0f, 0f);
        Vector3 origin = Vector3.zero;
        Vector3 moveDir = Vector3.right;

        // Act
        float distance = SliderJoint.GetProjectedDistance(line, currentPos, origin, moveDir);

        // Assert
        Assert.AreEqual(-50f, distance, Tolerance);
    }

    /**
     * @brief  원점이 아닌 위치에서 투영 거리 테스트 - 원점이 아닌 시작점에서 정확히 계산되는지 검증한다.
     */
    [Test]
    public void GetProjectedDistance_NonZeroOrigin_CalculatesCorrectly()
    {
        // Arrange
        Line line = new Line(new Vector3(10f, 0f, 0f), new Vector3(11f, 0f, 0f));
        Vector3 currentPos = new Vector3(60f, 5f, 3f);
        Vector3 origin = new Vector3(10f, 0f, 0f);
        Vector3 moveDir = Vector3.right;

        // Act
        float distance = SliderJoint.GetProjectedDistance(line, currentPos, origin, moveDir);

        // Assert
        Assert.AreEqual(50f, distance, Tolerance);
    }

    /**
     * @brief  이동축에서 벗어난 위치의 투영 거리 테스트 - 이동축과 다른 위치가 정확히 투영되는지 검증한다.
     */
    [Test]
    public void GetProjectedDistance_OffAxisPosition_ProjectsCorrectly()
    {
        // Arrange
        Line line = new Line(Vector3.zero, Vector3.right);
        Vector3 currentPos = new Vector3(50f, 20f, 10f);  // Y, Z 가 0 이 아님
        Vector3 origin = Vector3.zero;
        Vector3 moveDir = Vector3.right;

        // Act
        float distance = SliderJoint.GetProjectedDistance(line, currentPos, origin, moveDir);

        // Assert - X 축 성분만 계산되어야 함
        Assert.AreEqual(50f, distance, Tolerance);
    }

    /**
     * @brief  대각선 이동 방향 투영 거리 테스트 - 대각선 방향 이동이 정확히 계산되는지 검증한다.
     */
    [Test]
    public void GetProjectedDistance_DiagonalDirection_CalculatesCorrectly()
    {
        // Arrange
        Vector3 direction = new Vector3(1, 1, 0).normalized;
        Line line = new Line(Vector3.zero, direction);
        Vector3 currentPos = direction * 50f;
        Vector3 origin = Vector3.zero;

        // Act
        float distance = SliderJoint.GetProjectedDistance(line, currentPos, origin, direction);

        // Assert
        Assert.AreEqual(50f, distance, 0.01f);
    }

    /**
     * @brief  동일 위치 투영 거리 테스트 - 시작점과 동일한 위치일 때 0 이 반환되는지 검증한다.
     */
    [Test]
    public void GetProjectedDistance_SamePosition_ReturnsZero()
    {
        // Arrange
        Line line = new Line(Vector3.zero, Vector3.right);
        Vector3 origin = Vector3.zero;
        Vector3 moveDir = Vector3.right;

        // Act
        float distance = SliderJoint.GetProjectedDistance(line, origin, origin, moveDir);

        // Assert
        Assert.AreEqual(0f, distance, Tolerance);
    }

    #endregion

    #region GetClampedPosition 테스트

    /**
     * @brief  범위 내 위치 클램프 테스트 - 범위 내 위치일 때 동일한 위치가 반환되는지 검증한다.
     */
    [Test]
    public void GetClampedPosition_WithinRange_ReturnsSamePosition()
    {
        // Arrange
        SliderJoint joint = CreateValidSliderJoint(0f, 100f);
        Vector3 currentPos = new Vector3(50f, 0f, 0f);
        Vector3 origin = Vector3.zero;
        Vector3 moveDir = Vector3.right;

        // Act
        Vector3 result = joint.GetClampedPosition(currentPos, origin, moveDir);

        // Assert
        Assert.AreEqual(50f, result.x, Tolerance);
        Assert.AreEqual(50f, joint.CurrentPosition, Tolerance);
    }

    /**
     * @brief  최대값 초과 클램프 테스트 - max 초과 시 max 로 클램프되는지 검증한다.
     */
    [Test]
    public void GetClampedPosition_ExceedsMax_ClampsToMax()
    {
        // Arrange
        SliderJoint joint = CreateValidSliderJoint(0f, 100f);
        Vector3 currentPos = new Vector3(150f, 0f, 0f);
        Vector3 origin = Vector3.zero;
        Vector3 moveDir = Vector3.right;

        // Act
        Vector3 result = joint.GetClampedPosition(currentPos, origin, moveDir);

        // Assert
        Assert.AreEqual(100f, result.x, Tolerance);
        Assert.AreEqual(100f, joint.CurrentPosition, Tolerance);
    }

    /**
     * @brief  최소값 미만 클램프 테스트 - min 미만 시 min 으로 클램프되는지 검증한다.
     */
    [Test]
    public void GetClampedPosition_BelowMin_ClampsToMin()
    {
        // Arrange
        SliderJoint joint = CreateValidSliderJoint(0f, 100f);
        Vector3 currentPos = new Vector3(-50f, 0f, 0f);
        Vector3 origin = Vector3.zero;
        Vector3 moveDir = Vector3.right;

        // Act
        Vector3 result = joint.GetClampedPosition(currentPos, origin, moveDir);

        // Assert
        Assert.AreEqual(0f, result.x, Tolerance);
        Assert.AreEqual(0f, joint.CurrentPosition, Tolerance);
    }

    /**
     * @brief  Y, Z 축 유지 테스트 - 이동축 외 축이 유지되는지 검증한다.
     */
    [Test]
    public void GetClampedPosition_PreservesNonMoveAxes()
    {
        // Arrange
        SliderJoint joint = CreateValidSliderJoint(0f, 100f);
        Vector3 currentPos = new Vector3(50f, 20f, 10f);  // Y=20, Z=10
        Vector3 origin = Vector3.zero;
        Vector3 moveDir = Vector3.right;

        // Act
        Vector3 result = joint.GetClampedPosition(currentPos, origin, moveDir);

        // Assert - Y, Z 축은 현재 위치 유지
        Assert.AreEqual(50f, result.x, Tolerance);
        Assert.AreEqual(20f, result.y, Tolerance);
        Assert.AreEqual(10f, result.z, Tolerance);
    }

    /**
     * @brief  Y축 이동 방향 클램프 테스트 - Y축 이동 시 X, Z 가 유지되는지 검증한다.
     */
    [Test]
    public void GetClampedPosition_YAxisMovement_PreservesXZ()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.up);
        Line lineB = new Line(Vector3.zero, Vector3.up);
        Plane planeA = new Plane(Vector3.zero, Vector3.right, Vector3.up);
        Plane planeB = new Plane(Vector3.zero, Vector3.right, Vector3.up);
        SliderJoint joint = new SliderJoint(lineA, lineB, planeA, planeB, 0f, 100f);

        Vector3 currentPos = new Vector3(15f, 50f, 25f);
        Vector3 origin = Vector3.zero;
        Vector3 moveDir = Vector3.up;

        // Act
        Vector3 result = joint.GetClampedPosition(currentPos, origin, moveDir);

        // Assert
        Assert.AreEqual(15f, result.x, Tolerance);
        Assert.AreEqual(50f, result.y, Tolerance);
        Assert.AreEqual(25f, result.z, Tolerance);
    }

    /**
     * @brief  원점이 아닌 위치에서 클램프 테스트 - 원점이 아닌 시작점에서 정확히 클램프되는지 검증한다.
     */
    [Test]
    public void GetClampedPosition_NonZeroOrigin_ClampsCorrectly()
    {
        // Arrange
        Line lineA = new Line(new Vector3(10f, 0f, 0f), new Vector3(11f, 0f, 0f));
        Line lineB = new Line(new Vector3(10f, 0f, 0f), new Vector3(11f, 0f, 0f));
        Plane planeA = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        Plane planeB = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        SliderJoint joint = new SliderJoint(lineA, lineB, planeA, planeB, 0f, 50f);

        Vector3 currentPos = new Vector3(80f, 0f, 0f);  // origin(10) + 70 = 80, max 50 초과
        Vector3 origin = new Vector3(10f, 0f, 0f);
        Vector3 moveDir = Vector3.right;

        // Act
        Vector3 result = joint.GetClampedPosition(currentPos, origin, moveDir);

        // Assert - origin(10) + max(50) = 60
        Assert.AreEqual(60f, result.x, Tolerance);
    }

    #endregion

    #region ApplyConstraint 테스트

    /**
     * @brief  일치하는 Line 과 Plane 구속 테스트 - 일치할 때 구속이 적용되는지 검증한다.
     */
    [Test]
    public void ApplyConstraint_CoincidentLineAndPlane_ReturnsTrue()
    {
        // Arrange
        SliderJoint joint = CreateValidSliderJoint();

        // Act
        bool result = joint.ApplyConstraint();

        // Assert
        Assert.IsTrue(result);
        Assert.IsTrue(joint.IsLineConstrained);
        Assert.IsTrue(joint.IsPlaneConstrained);
    }

    /**
     * @brief  평행하지만 일치하지 않는 Line 구속 테스트 - Line 평행 시 구속되는지 검증한다.
     */
    [Test]
    public void ApplyConstraint_ParallelLines_LineConstrainedTrue()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(new Vector3(0, 5, 0), new Vector3(1, 5, 0));  // 평행
        Plane planeA = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        Plane planeB = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        SliderJoint joint = new SliderJoint(lineA, lineB, planeA, planeB, 0f, 100f);

        // Act
        joint.ApplyConstraint();

        // Assert
        Assert.IsTrue(joint.IsLineConstrained);
    }

    /**
     * @brief  일치하지 않는 Plane 구속 테스트 - Plane 불일치 시 구속이 해제되는지 검증한다.
     */
    [Test]
    public void ApplyConstraint_NonCoincidentPlane_PlaneConstrainedFalse()
    {
        // Arrange
        SliderJoint joint = CreateInvalidPlaneSliderJoint();

        // Act
        bool result = joint.ApplyConstraint();

        // Assert
        Assert.IsFalse(result);
        Assert.IsFalse(joint.IsPlaneConstrained);
    }

    /**
     * @brief  교차하는 Line 구속 테스트 - Line 교차 시 구속이 해제되는지 검증한다.
     */
    [Test]
    public void ApplyConstraint_IntersectingLines_LineConstrainedFalse()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.up);  // 교차
        Plane planeA = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        Plane planeB = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        SliderJoint joint = new SliderJoint(lineA, lineB, planeA, planeB, 0f, 100f);

        // Act
        joint.ApplyConstraint();

        // Assert
        Assert.IsFalse(joint.IsLineConstrained);
    }

    #endregion

    #region IsValid 테스트

    /**
     * @brief  유효한 조인트 테스트 - 모든 조건 만족 시 true 가 반환되는지 검증한다.
     */
    [Test]
    public void IsValid_AllConditionsMet_ReturnsTrue()
    {
        // Arrange
        SliderJoint joint = CreateValidSliderJoint();

        // Act
        bool result = joint.IsValid();

        // Assert
        Assert.IsTrue(result);
    }

    /**
     * @brief  일치하지 않는 Line 유효성 테스트 - Line 불일치 시 false 가 반환되는지 검증한다.
     */
    [Test]
    public void IsValid_NonCoincidentLines_ReturnsFalse()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.up);  // 교차
        Plane planeA = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        Plane planeB = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        SliderJoint joint = new SliderJoint(lineA, lineB, planeA, planeB, 0f, 100f);

        // Act
        bool result = joint.IsValid();

        // Assert
        Assert.IsFalse(result);
    }

    /**
     * @brief  일치하지 않는 Plane 유효성 테스트 - Plane 불일치 시 false 가 반환되는지 검증한다.
     */
    [Test]
    public void IsValid_NonCoincidentPlanes_ReturnsFalse()
    {
        // Arrange
        SliderJoint joint = CreateInvalidPlaneSliderJoint();

        // Act
        bool result = joint.IsValid();

        // Assert
        Assert.IsFalse(result);
    }

    /**
     * @brief  min > max 범위 유효성 테스트 - 잘못된 범위일 때 false 가 반환되는지 검증한다.
     */
    [Test]
    public void IsValid_MinGreaterThanMax_ReturnsFalse()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        Plane planeA = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        Plane planeB = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        SliderJoint joint = new SliderJoint(lineA, lineB, planeA, planeB, 100f, 0f);  // min > max

        // Act
        bool result = joint.IsValid();

        // Assert
        Assert.IsFalse(result);
    }

    /**
     * @brief  평행 Line 유효성 테스트 - 평행선일 때 true 가 반환되는지 검증한다.
     */
    [Test]
    public void IsValid_ParallelLines_ReturnsTrue()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(new Vector3(0, 5, 0), new Vector3(1, 5, 0));  // 평행
        Plane planeA = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        Plane planeB = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        SliderJoint joint = new SliderJoint(lineA, lineB, planeA, planeB, 0f, 100f);

        // Act
        bool result = joint.IsValid();

        // Assert
        Assert.IsTrue(result);
    }

    /**
     * @brief  Line 과 Plane 모두 불일치 유효성 테스트 - 둘 다 불일치 시 false 가 반환되는지 검증한다.
     */
    [Test]
    public void IsValid_BothLineAndPlaneInvalid_ReturnsFalse()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.up);  // 교차
        Plane planeA = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        Plane planeB = new Plane(new Vector3(0, 5, 0), new Vector3(1, 5, 0), new Vector3(0, 5, 1));  // 불일치
        SliderJoint joint = new SliderJoint(lineA, lineB, planeA, planeB, 0f, 100f);

        // Act
        bool result = joint.IsValid();

        // Assert
        Assert.IsFalse(result);
    }

    #endregion

    #region 복합 시나리오 테스트

    /**
     * @brief  연속 이동 시뮬레이션 테스트 - 여러 번 위치 변경 후에도 정확히 작동하는지 검증한다.
     */
    [Test]
    public void ComplexScenario_ContinuousMovement_WorksCorrectly()
    {
        // Arrange
        SliderJoint joint = CreateValidSliderJoint(0f, 100f);
        Vector3 origin = Vector3.zero;
        Vector3 moveDir = Vector3.right;

        // Act - 여러 위치로 연속 이동
        Vector3[] positions = {
            new Vector3(30f, 0f, 0f),
            new Vector3(60f, 0f, 0f),
            new Vector3(90f, 0f, 0f),
            new Vector3(120f, 0f, 0f),  // 초과
            new Vector3(-10f, 0f, 0f)   // 미만
        };

        float lastPosition = 0f;
        foreach (var pos in positions)
        {
            joint.GetClampedPosition(pos, origin, moveDir);
            lastPosition = joint.CurrentPosition;
        }

        // Assert - 마지막 위치가 0 으로 클램프되어야 함
        Assert.AreEqual(0f, lastPosition, Tolerance);
    }

    /**
     * @brief  경계값 진동 테스트 - 경계값 근처에서 반복 이동 시 안정적으로 유지되는지 검증한다.
     */
    [Test]
    public void ComplexScenario_BoundaryOscillation_RemainsStable()
    {
        // Arrange
        SliderJoint joint = CreateValidSliderJoint(0f, 100f);
        Vector3 origin = Vector3.zero;
        Vector3 moveDir = Vector3.right;

        // Act - 경계값 근처에서 여러 번 이동
        float[] testPositions = { 99.9f, 100.1f, 99.99f, 100.01f, 0.1f, -0.1f };
        foreach (float pos in testPositions)
        {
            joint.GetClampedPosition(new Vector3(pos, 0f, 0f), origin, moveDir);
        }

        // Assert - 마지막 위치가 0 으로 클램프되어야 함
        Assert.AreEqual(0f, joint.CurrentPosition, Tolerance);
    }

    /**
     * @brief  3D 공간 이동 테스트 - 3D 공간에서 다양한 위치로 이동이 정확히 계산되는지 검증한다.
     */
    [Test]
    public void ComplexScenario_3DSpaceMovement_CalculatesCorrectly()
    {
        // Arrange
        SliderJoint joint = CreateValidSliderJoint(0f, 100f);
        Vector3 origin = Vector3.zero;
        Vector3 moveDir = Vector3.right;

        // Act - Y, Z 가 다른 3D 위치에서 이동
        Vector3 currentPos = new Vector3(50f, 100f, -50f);
        Vector3 result = joint.GetClampedPosition(currentPos, origin, moveDir);

        // Assert - X 만 클램프, Y/Z 유지
        Assert.AreEqual(50f, result.x, Tolerance);
        Assert.AreEqual(100f, result.y, Tolerance);
        Assert.AreEqual(-50f, result.z, Tolerance);
    }

    /**
     * @brief  음수 좌표 공간 테스트 - 음수 좌표에서도 정상 작동하는지 검증한다.
     */
    [Test]
    public void ComplexScenario_NegativeCoordinates_WorksCorrectly()
    {
        // Arrange
        Line lineA = new Line(new Vector3(-100, -100, -100), new Vector3(-99, -100, -100));
        Line lineB = new Line(new Vector3(-100, -100, -100), new Vector3(-99, -100, -100));
        Plane planeA = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        Plane planeB = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        SliderJoint joint = new SliderJoint(lineA, lineB, planeA, planeB, 0f, 50f);

        // Act
        bool isValid = joint.IsValid();
        joint.SetPosition(25f);

        // Assert
        Assert.IsTrue(isValid);
        Assert.AreEqual(25f, joint.CurrentPosition, Tolerance);
    }

    /**
     * @brief  구속 후 이동 테스트 - 구속 적용 후 이동이 정상 작동하는지 검증한다.
     */
    [Test]
    public void ComplexScenario_ConstraintThenMove_WorksCorrectly()
    {
        // Arrange
        SliderJoint joint = CreateValidSliderJoint(0f, 100f);

        // Act - 구속 적용 후 이동
        bool constrained = joint.ApplyConstraint();
        joint.SetPosition(75f);

        // Assert
        Assert.IsTrue(constrained);
        Assert.AreEqual(75f, joint.CurrentPosition, Tolerance);
    }

    /**
     * @brief  좁은 범위 테스트 - 매우 좁은 범위에서도 정상 작동하는지 검증한다.
     */
    [Test]
    public void ComplexScenario_NarrowRange_WorksCorrectly()
    {
        // Arrange
        SliderJoint joint = CreateValidSliderJoint(49f, 51f);

        // Act & Assert
        joint.SetPosition(50f);
        Assert.AreEqual(50f, joint.CurrentPosition, Tolerance);

        joint.SetPosition(45f);
        Assert.AreEqual(49f, joint.CurrentPosition, Tolerance);

        joint.SetPosition(55f);
        Assert.AreEqual(51f, joint.CurrentPosition, Tolerance);
    }

    /**
     * @brief  넓은 범위 테스트 - 매우 넓은 범위에서도 정상 작동하는지 검증한다.
     */
    [Test]
    public void ComplexScenario_WideRange_WorksCorrectly()
    {
        // Arrange
        SliderJoint joint = CreateValidSliderJoint(-1000f, 1000f);

        // Act & Assert
        joint.SetPosition(500f);
        Assert.AreEqual(500f, joint.CurrentPosition, Tolerance);

        joint.SetPosition(-500f);
        Assert.AreEqual(-500f, joint.CurrentPosition, Tolerance);

        joint.SetPosition(1500f);
        Assert.AreEqual(1000f, joint.CurrentPosition, Tolerance);
    }

    #endregion
}