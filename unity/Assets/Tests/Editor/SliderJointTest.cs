// ============================================================
// 파일명  : SliderJointTest.cs
// 역할    : SliderJoint 클래스의 EditMode 단위 테스트
//           Line/Plane 기반 슬라이더 조인트의 유효성 검사, 위치 클램프, 구속 조건을 검증한다.
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

    #region 생성자 테스트

    /**
     * @brief  기본 생성자 테스트 - 유효한 매개변수로 SliderJoint 가 정상 생성되는지 검증한다.
     */
    [Test]
    public void Constructor_ValidParameters_CreatesJoint()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        Plane planeA = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        Plane planeB = new Plane(Vector3.zero, Vector3.right, Vector3.forward);

        // Act
        SliderJoint joint = new SliderJoint(lineA, lineB, planeA, planeB, 0f, 10f);

        // Assert
        Assert.IsNotNull(joint);
        Assert.AreEqual(0f, joint.MinPosition, Tolerance);
        Assert.AreEqual(10f, joint.MaxPosition, Tolerance);
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
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        Plane planeA = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        Plane planeB = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        SliderJoint joint = new SliderJoint(lineA, lineB, planeA, planeB, 0f, 10f);

        // Act
        joint.SetPosition(5f);

        // Assert
        Assert.AreEqual(5f, joint.CurrentPosition, Tolerance);
    }

    /**
     * @brief  최소 위치 미만 설정 테스트 - min 보다 작은 위치가 min 으로 클램프되는지 검증한다.
     */
    [Test]
    public void SetPosition_BelowMin_ClampsToMin()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        Plane planeA = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        Plane planeB = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        SliderJoint joint = new SliderJoint(lineA, lineB, planeA, planeB, 0f, 10f);

        // Act
        joint.SetPosition(-5f);

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
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        Plane planeA = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        Plane planeB = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        SliderJoint joint = new SliderJoint(lineA, lineB, planeA, planeB, 0f, 10f);

        // Act
        joint.SetPosition(20f);

        // Assert
        Assert.AreEqual(10f, joint.CurrentPosition, Tolerance);
    }

    #endregion

    #region 거리 및 클램프 로직 테스트

    /**
     * @brief  투영 거리 계산 테스트 - 축을 기준으로 한 이동 거리가 부호와 함께 정상 반환되는지 검증한다.
     */
    [Test]
    public void GetProjectedDistance_ReturnsSignedDistance()
    {
        // Arrange
        Line line = new Line(Vector3.zero, Vector3.right);
        Vector3 currentB = new Vector3(3f, 2f, 0f);
        Vector3 origin = Vector3.zero;
        Vector3 moveDir = Vector3.right;

        // Act
        float dist = SliderJoint.GetProjectedDistance(line, currentB, origin, moveDir);

        // Assert
        Assert.AreEqual(3f, dist, Tolerance);
    }

    /**
     * @brief  최대값 초과 클램프 계산 테스트 - max 초과 시 최대 위치의 Vector3 좌표를 정상 반환하는지 검증한다.
     */
    [Test]
    public void GetClampedPosition_ExceedsMax_ClampsToMax()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(new Vector3(5f, 0f, 0f), new Vector3(6f, 0f, 0f));
        Plane planeA = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        Plane planeB = new Plane(new Vector3(5f, 0f, 0f), new Vector3(5f, 0f, 1f), new Vector3(6f, 0f, 0f));
        SliderJoint joint = new SliderJoint(lineA, lineB, planeA, planeB, 0f, 1f);

        Vector3 currentB = new Vector3(5f + 2f, 0f, 0f); // projected distance = 2
        Vector3 origin = Vector3.zero;
        Vector3 moveDir = Vector3.right;

        // Act
        Vector3 clamped = joint.GetClampedPosition(currentB, origin, moveDir);

        // Assert - x component should be clamped to max=1 (origin + moveDir * 1)
        Assert.AreEqual(1f, clamped.x, Tolerance);
    }

    #endregion

    #region 유효성 및 구속 검증 테스트

    /**
     * @brief  구속 조건 적용 테스트 - Line 이 평행하고 Plane 이 일치할 때 구속 조건이 적용되는지 검증한다.
     */
    [Test]
    public void ApplyConstraint_LineParallel_PlaneCoincident_ReturnsTrue()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(new Vector3(2f, 0f, 0f), new Vector3(3f, 0f, 0f));
        Plane planeA = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        Plane planeB = new Plane(new Vector3(2f, 0f, 0f), new Vector3(2f, 0f, 1f), new Vector3(3f, 0f, 0f));
        SliderJoint joint = new SliderJoint(lineA, lineB, planeA, planeB, 0f, 10f);

        // Act
        bool constrained = joint.ApplyConstraint();

        // Assert
        Assert.IsTrue(constrained);
    }

    /**
     * @brief  min > max 범위 유효성 테스트 - 잘못된 범위일 때 무효한지 검증한다.
     */
    [Test]
    public void IsValid_MinGreaterThanMax_ReturnsFalse()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        Plane planeA = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        Plane planeB = new Plane(Vector3.zero, Vector3.right, Vector3.forward);
        SliderJoint joint = new SliderJoint(lineA, lineB, planeA, planeB, 10f, 0f);

        // Act
        bool valid = joint.IsValid();

        // Assert
        Assert.IsFalse(valid);
    }

    #endregion
}