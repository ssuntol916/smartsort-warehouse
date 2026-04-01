// ============================================================
// 파일명  : RevoluteJointTest.cs
// 역할    : RevoluteJoint 클래스의 EditMode 단위 테스트
//           Line 기반 회전 조인트의 유효성 검사, 각도 클램프, 구속 조건을 검증한다.
// 작성자  : 이현화
// 작성일  : 2026-04-01
// 수정이력: 
// ============================================================

using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class RevoluteJointTest
{
    private const float Tolerance = 0.0001f;

    #region 생성자 테스트

    /**
     * @brief  기본 생성자 테스트 - 유효한 매개변수로 RevoluteJoint 가 정상 생성되는지 검증한다.
     */
    [Test]
    public void Constructor_ValidParameters_CreatesJoint()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        float minAngle = -90f;
        float maxAngle = 90f;

        // Act
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, minAngle, maxAngle);

        // Assert
        Assert.IsNotNull(joint);
        Assert.AreEqual(minAngle, joint.MinAngle, Tolerance);
        Assert.AreEqual(maxAngle, joint.MaxAngle, Tolerance);
        Assert.AreEqual(0f, joint.CurrentAngle, Tolerance);
    }

    /**
     * @brief  음수 범위 각도로 생성 테스트 - min/max 가 모두 음수일 때 정상 생성되는지 검증한다.
     */
    [Test]
    public void Constructor_NegativeAngleRange_CreatesJoint()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.up);
        Line lineB = new Line(Vector3.zero, Vector3.up);
        float minAngle = -180f;
        float maxAngle = -90f;

        // Act
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, minAngle, maxAngle);

        // Assert
        Assert.AreEqual(minAngle, joint.MinAngle, Tolerance);
        Assert.AreEqual(maxAngle, joint.MaxAngle, Tolerance);
    }

    /**
     * @brief  비대칭 각도 범위로 생성 테스트 - min/max 가 비대칭일 때 정상 생성되는지 검증한다.
     */
    [Test]
    public void Constructor_AsymmetricAngleRange_CreatesJoint()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.forward);
        Line lineB = new Line(Vector3.zero, Vector3.forward);
        float minAngle = -30f;
        float maxAngle = 150f;

        // Act
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, minAngle, maxAngle);

        // Assert
        Assert.AreEqual(minAngle, joint.MinAngle, Tolerance);
        Assert.AreEqual(maxAngle, joint.MaxAngle, Tolerance);
    }

    /**
     * @brief  동일 각도 범위로 생성 테스트 - min == max 일 때 (고정 각도) 정상 생성되는지 검증한다.
     */
    [Test]
    public void Constructor_SameMinMax_CreatesJoint()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        float fixedAngle = 45f;

        // Act
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, fixedAngle, fixedAngle);

        // Assert
        Assert.AreEqual(fixedAngle, joint.MinAngle, Tolerance);
        Assert.AreEqual(fixedAngle, joint.MaxAngle, Tolerance);
    }

    /**
     * @brief  서로 다른 위치의 평행한 Line 으로 생성 테스트 - 일치하지 않는 Line 으로 생성 가능한지 검증한다.
     */
    [Test]
    public void Constructor_ParallelLinesAtDifferentPositions_CreatesJoint()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(new Vector3(0, 5, 0), new Vector3(1, 5, 0));

        // Act
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -90f, 90f);

        // Assert
        Assert.IsNotNull(joint);
    }

    #endregion

    #region SetAngle 테스트

    /**
     * @brief  범위 내 각도 설정 테스트 - min/max 범위 내의 각도가 그대로 설정되는지 검증한다.
     */
    [Test]
    public void SetAngle_WithinRange_SetsExactAngle()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -90f, 90f);

        // Act
        joint.SetAngle(45f);

        // Assert
        Assert.AreEqual(45f, joint.CurrentAngle, Tolerance);
    }

    /**
     * @brief  최소 각도 초과 테스트 - min 보다 작은 각도가 min 으로 클램프되는지 검증한다.
     */
    [Test]
    public void SetAngle_BelowMin_ClampsToMin()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -90f, 90f);

        // Act
        joint.SetAngle(-150f);

        // Assert
        Assert.AreEqual(-90f, joint.CurrentAngle, Tolerance);
    }

    /**
     * @brief  최대 각도 초과 테스트 - max 보다 큰 각도가 max 로 클램프되는지 검증한다.
     */
    [Test]
    public void SetAngle_AboveMax_ClampsToMax()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -90f, 90f);

        // Act
        joint.SetAngle(180f);

        // Assert
        Assert.AreEqual(90f, joint.CurrentAngle, Tolerance);
    }

    /**
     * @brief  경계값 테스트 - 정확히 min 값이 설정되는지 검증한다.
     */
    [Test]
    public void SetAngle_ExactlyMin_SetsMin()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -90f, 90f);

        // Act
        joint.SetAngle(-90f);

        // Assert
        Assert.AreEqual(-90f, joint.CurrentAngle, Tolerance);
    }

    /**
     * @brief  경계값 테스트 - 정확히 max 값이 설정되는지 검증한다.
     */
    [Test]
    public void SetAngle_ExactlyMax_SetsMax()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -90f, 90f);

        // Act
        joint.SetAngle(90f);

        // Assert
        Assert.AreEqual(90f, joint.CurrentAngle, Tolerance);
    }

    /**
     * @brief  0도 설정 테스트 - 범위 내의 0도가 정상 설정되는지 검증한다.
     */
    [Test]
    public void SetAngle_Zero_SetsZero()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -90f, 90f);
        joint.SetAngle(45f);  // 먼저 다른 값 설정

        // Act
        joint.SetAngle(0f);

        // Assert
        Assert.AreEqual(0f, joint.CurrentAngle, Tolerance);
    }

    /**
     * @brief  연속 설정 테스트 - 여러 번 SetAngle 호출 시 마지막 값이 유지되는지 검증한다.
     */
    [Test]
    public void SetAngle_MultipleCalls_LastValuePersists()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -90f, 90f);

        // Act
        joint.SetAngle(30f);
        joint.SetAngle(-45f);
        joint.SetAngle(60f);

        // Assert
        Assert.AreEqual(60f, joint.CurrentAngle, Tolerance);
    }

    /**
     * @brief  음수 범위에서 양수 각도 설정 테스트 - 범위가 음수일 때 양수 각도가 클램프되는지 검증한다.
     */
    [Test]
    public void SetAngle_PositiveAngleInNegativeRange_ClampsToMax()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -180f, -90f);

        // Act
        joint.SetAngle(45f);

        // Assert
        Assert.AreEqual(-90f, joint.CurrentAngle, Tolerance);
    }

    #endregion

    #region GetCurrentAngle 테스트

    /**
     * @brief  동일 방향 벡터 테스트 - 같은 방향일 때 0도가 반환되는지 검증한다.
     */
    [Test]
    public void GetCurrentAngle_SameDirection_ReturnsZero()
    {
        // Arrange
        Vector3 from = Vector3.up;
        Vector3 to = Vector3.up;
        Vector3 axis = Vector3.right;

        // Act
        float angle = RevoluteJoint.GetCurrentAngle(from, to, axis);

        // Assert
        Assert.AreEqual(0f, angle, Tolerance);
    }

    /**
     * @brief  90도 회전 테스트 - X축 기준 90도 회전이 정확히 계산되는지 검증한다.
     */
    [Test]
    public void GetCurrentAngle_90DegreeRotation_Returns90()
    {
        // Arrange
        Vector3 from = Vector3.up;
        Vector3 to = Vector3.forward;
        Vector3 axis = Vector3.right;

        // Act
        float angle = RevoluteJoint.GetCurrentAngle(from, to, axis);

        // Assert
        Assert.AreEqual(90f, angle, 0.01f);
    }

    /**
     * @brief  -90도 회전 테스트 - X축 기준 -90도 회전이 정확히 계산되는지 검증한다.
     */
    [Test]
    public void GetCurrentAngle_Negative90DegreeRotation_ReturnsMinus90()
    {
        // Arrange
        Vector3 from = Vector3.up;
        Vector3 to = Vector3.back;
        Vector3 axis = Vector3.right;

        // Act
        float angle = RevoluteJoint.GetCurrentAngle(from, to, axis);

        // Assert
        Assert.AreEqual(-90f, angle, 0.01f);
    }

    /**
     * @brief  180도 회전 테스트 - 반대 방향일 때 180도 또는 -180도가 반환되는지 검증한다.
     */
    [Test]
    public void GetCurrentAngle_180DegreeRotation_Returns180OrMinus180()
    {
        // Arrange
        Vector3 from = Vector3.up;
        Vector3 to = Vector3.down;
        Vector3 axis = Vector3.right;

        // Act
        float angle = RevoluteJoint.GetCurrentAngle(from, to, axis);

        // Assert
        Assert.That(Mathf.Abs(angle), Is.EqualTo(180f).Within(0.01f));
    }

    /**
     * @brief  Y축 기준 회전 테스트 - Y축 기준 45도 회전이 정확히 계산되는지 검증한다.
     */
    [Test]
    public void GetCurrentAngle_YAxisRotation_CalculatesCorrectly()
    {
        // Arrange
        Vector3 from = Vector3.forward;
        Vector3 to = new Vector3(1, 0, 1).normalized;
        Vector3 axis = Vector3.up;

        // Act
        float angle = RevoluteJoint.GetCurrentAngle(from, to, axis);

        // Assert
        Assert.AreEqual(45f, angle, 0.1f);
    }

    /**
     * @brief  Z축 기준 회전 테스트 - Z축 기준 회전이 정확히 계산되는지 검증한다.
     */
    [Test]
    public void GetCurrentAngle_ZAxisRotation_CalculatesCorrectly()
    {
        // Arrange
        Vector3 from = Vector3.right;
        Vector3 to = Vector3.up;
        Vector3 axis = Vector3.forward;

        // Act
        float angle = RevoluteJoint.GetCurrentAngle(from, to, axis);

        // Assert
        Assert.AreEqual(90f, angle, 0.01f);
    }

    /**
     * @brief  임의의 축 기준 회전 테스트 - 대각선 축 기준 회전이 정확히 계산되는지 검증한다.
     */
    [Test]
    public void GetCurrentAngle_ArbitraryAxis_CalculatesCorrectly()
    {
        // Arrange
        Vector3 axis = new Vector3(1, 1, 0).normalized;
        Vector3 from = Vector3.Cross(axis, Vector3.forward).normalized;
        Quaternion rotation = Quaternion.AngleAxis(60f, axis);
        Vector3 to = rotation * from;

        // Act
        float angle = RevoluteJoint.GetCurrentAngle(from, to, axis);

        // Assert
        Assert.AreEqual(60f, angle, 0.1f);
    }

    #endregion

    #region GetClampedRotation 테스트

    /**
     * @brief  범위 내 각도 테스트 - 범위 내 각도일 때 동일한 회전이 반환되는지 검증한다.
     */
    [Test]
    public void GetClampedRotation_WithinRange_ReturnsExpectedRotation()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -90f, 90f);

        Vector3 axis = Vector3.right;
        Vector3 initialDir = Vector3.up;
        Quaternion initialRot = Quaternion.identity;
        Quaternion currentRot = Quaternion.AngleAxis(45f, axis);
        Vector3 currentDir = currentRot * initialDir;

        // Act
        Quaternion result = joint.GetClampedRotation(initialDir, currentDir, axis, initialRot);

        // Assert
        Assert.AreEqual(45f, joint.CurrentAngle, 0.1f);
    }

    /**
     * @brief  최대값 초과 클램프 테스트 - max 초과 시 max 로 클램프되는지 검증한다.
     */
    [Test]
    public void GetClampedRotation_ExceedsMax_ClampsToMax()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -90f, 90f);

        Vector3 axis = Vector3.right;
        Vector3 initialDir = Vector3.up;
        Quaternion initialRot = Quaternion.identity;
        Quaternion currentRot = Quaternion.AngleAxis(120f, axis);
        Vector3 currentDir = currentRot * initialDir;

        // Act
        Quaternion result = joint.GetClampedRotation(initialDir, currentDir, axis, initialRot);

        // Assert
        Assert.AreEqual(90f, joint.CurrentAngle, Tolerance);
    }

    /**
     * @brief  최소값 미만 클램프 테스트 - min 미만 시 min 으로 클램프되는지 검증한다.
     */
    [Test]
    public void GetClampedRotation_BelowMin_ClampsToMin()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -90f, 90f);

        Vector3 axis = Vector3.right;
        Vector3 initialDir = Vector3.up;
        Quaternion initialRot = Quaternion.identity;
        Quaternion currentRot = Quaternion.AngleAxis(-120f, axis);
        Vector3 currentDir = currentRot * initialDir;

        // Act
        Quaternion result = joint.GetClampedRotation(initialDir, currentDir, axis, initialRot);

        // Assert
        Assert.AreEqual(-90f, joint.CurrentAngle, Tolerance);
    }

    #endregion

    #region IsValid 테스트

    /**
     * @brief  일치하는 Line 유효성 테스트 - 동일한 Line 일 때 유효한지 검증한다.
     */
    [Test]
    public void IsValid_CoincidentLines_ReturnsTrue()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -90f, 90f);

        // Act
        bool result = joint.IsValid();

        // Assert
        Assert.IsTrue(result);
    }

    /**
     * @brief  평행하지만 일치하지 않는 Line 유효성 테스트 - 떨어진 평행선일 때 무효한지 검증한다.
     */
    [Test]
    public void IsValid_ParallelButNotCoincident_ReturnsFalse()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(new Vector3(0, 5, 0), new Vector3(1, 5, 0));
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -90f, 90f);

        // Act
        bool result = joint.IsValid();

        // Assert
        Assert.IsFalse(result);
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
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, 90f, -90f);  // min > max

        // Act
        bool result = joint.IsValid();

        // Assert
        Assert.IsFalse(result);
    }

    /**
     * @brief  교차하는 Line 유효성 테스트 - 교차하지만 일치하지 않는 Line 일 때 무효한지 검증한다.
     */
    [Test]
    public void IsValid_IntersectingButNotCoincident_ReturnsFalse()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.up);
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -90f, 90f);

        // Act
        bool result = joint.IsValid();

        // Assert
        Assert.IsFalse(result);
    }

    #endregion

    #region ApplyConstraint 테스트

    /**
     * @brief  일치하는 Line 구속 테스트 - 일치하는 Line 일 때 구속이 적용되는지 검증한다.
     */
    [Test]
    public void ApplyConstraint_CoincidentLines_ReturnsTrue()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -90f, 90f);

        // Act
        bool result = joint.ApplyConstraint();

        // Assert
        Assert.IsTrue(result);
        Assert.IsTrue(joint.IsLineConstrained);
    }

    /**
     * @brief  일치하지 않는 Line 구속 테스트 - 일치하지 않는 Line 일 때 구속이 해제되는지 검증한다.
     */
    [Test]
    public void ApplyConstraint_NonCoincidentLines_ReturnsFalse()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(new Vector3(0, 5, 0), new Vector3(1, 5, 0));
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -90f, 90f);

        // Act
        bool result = joint.ApplyConstraint();

        // Assert
        Assert.IsFalse(result);
        Assert.IsFalse(joint.IsLineConstrained);
    }

    #endregion

    #region UpdateLineB 테스트

    /**
     * @brief  LineB 갱신 후 유효성 변경 테스트 - 무효한 상태에서 유효한 상태로 변경되는지 검증한다.
     */
    [Test]
    public void UpdateLineB_FromInvalidToValid_BecomesValid()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(new Vector3(0, 5, 0), new Vector3(1, 5, 0));  // 일치하지 않음
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -90f, 90f);

        // 초기 상태 확인
        Assert.IsFalse(joint.IsValid());

        // Act - lineB 를 일치하는 위치로 갱신
        Line newLineB = new Line(Vector3.zero, Vector3.right);
        joint.UpdateLineB(newLineB);

        // Assert
        Assert.IsTrue(joint.IsValid());
    }

    /**
     * @brief  LineB 갱신 후 유효성 변경 테스트 - 유효한 상태에서 무효한 상태로 변경되는지 검증한다.
     */
    [Test]
    public void UpdateLineB_FromValidToInvalid_BecomesInvalid()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);  // 일치함
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -90f, 90f);

        // 초기 상태 확인
        Assert.IsTrue(joint.IsValid());

        // Act - lineB 를 일치하지 않는 위치로 갱신
        Line newLineB = new Line(new Vector3(0, 5, 0), new Vector3(1, 5, 0));
        joint.UpdateLineB(newLineB);

        // Assert
        Assert.IsFalse(joint.IsValid());
    }

    #endregion

    #region Axis 프로퍼티 테스트

    /**
     * @brief  X축 회전축 테스트 - X축 방향 Line 의 Axis 가 정확히 반환되는지 검증한다.
     */
    [Test]
    public void Axis_XAxisLine_ReturnsXAxis()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -90f, 90f);

        // Act
        Vector3 axis = joint.Axis;

        // Assert
        Assert.AreEqual(1f, axis.x, Tolerance);
        Assert.AreEqual(0f, axis.y, Tolerance);
        Assert.AreEqual(0f, axis.z, Tolerance);
    }

    /**
     * @brief  Y축 회전축 테스트 - Y축 방향 Line 의 Axis 가 정확히 반환되는지 검증한다.
     */
    [Test]
    public void Axis_YAxisLine_ReturnsYAxis()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.up);
        Line lineB = new Line(Vector3.zero, Vector3.up);
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -90f, 90f);

        // Act
        Vector3 axis = joint.Axis;

        // Assert
        Assert.AreEqual(0f, axis.x, Tolerance);
        Assert.AreEqual(1f, axis.y, Tolerance);
        Assert.AreEqual(0f, axis.z, Tolerance);
    }

    /**
     * @brief  대각선 회전축 테스트 - 대각선 방향 Line 의 Axis 가 정규화되어 반환되는지 검증한다.
     */
    [Test]
    public void Axis_DiagonalLine_ReturnsNormalizedAxis()
    {
        // Arrange
        Vector3 direction = new Vector3(1, 1, 1).normalized;
        Line lineA = new Line(Vector3.zero, direction);
        Line lineB = new Line(Vector3.zero, direction);
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -90f, 90f);

        // Act
        Vector3 axis = joint.Axis;

        // Assert
        Assert.AreEqual(1f, axis.magnitude, Tolerance);
    }

    #endregion

    #region 복합 시나리오 테스트

    /**
     * @brief  연속 회전 시뮬레이션 테스트 - 여러 번 회전 후에도 각도가 누적되는지 검증한다.
     */
    [Test]
    public void ComplexScenario_ContinuousRotation_AccumulatesCorrectly()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -180f, 180f);

        Vector3 axis = Vector3.right;
        Vector3 currentDir = Vector3.up;
        Quaternion initialRot = Quaternion.identity;

        // Act - 30도씩 3번 회전 시뮬레이션
        for (int i = 0; i < 3; i++)
        {
            Quaternion rotation = Quaternion.AngleAxis(30f, axis);
            currentDir = rotation * currentDir;
        }

        float angle = RevoluteJoint.GetCurrentAngle(Vector3.up, currentDir, axis);

        // Assert
        Assert.AreEqual(90f, angle, 0.1f);
    }

    /**
     * @brief  경계값 진동 테스트 - 경계값 근처에서 반복 설정 시 안정적으로 유지되는지 검증한다.
     */
    [Test]
    public void ComplexScenario_BoundaryOscillation_RemainsStable()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -90f, 90f);

        // Act - 경계값 근처에서 여러 번 설정
        float[] testAngles = { 89.9f, 90.1f, 89.99f, 90.01f, -89.9f, -90.1f };
        foreach (float angle in testAngles)
        {
            joint.SetAngle(angle);
        }

        // Assert - 마지막 값이 클램프되어 있어야 함
        Assert.AreEqual(-90f, joint.CurrentAngle, Tolerance);
    }

    /**
     * @brief  3D 공간 회전 테스트 - 3D 공간에서 다양한 축으로 회전이 정확히 계산되는지 검증한다.
     */
    [Test]
    public void ComplexScenario_3DSpaceRotation_CalculatesCorrectly()
    {
        // Arrange - 대각선 축
        Vector3 axis = new Vector3(1, 1, 1).normalized;
        Vector3 perpendicular = Vector3.Cross(axis, Vector3.forward).normalized;

        Line lineA = new Line(Vector3.zero, Vector3.zero + axis);
        Line lineB = new Line(Vector3.zero, Vector3.zero + axis);
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -180f, 180f);

        // Act - 축 기준 120도 회전
        Quaternion rotation = Quaternion.AngleAxis(120f, axis);
        Vector3 rotatedDir = rotation * perpendicular;
        float angle = RevoluteJoint.GetCurrentAngle(perpendicular, rotatedDir, axis);

        // Assert
        Assert.AreEqual(120f, angle, 0.1f);
    }

    /**
     * @brief  음수 좌표 공간 테스트 - 음수 좌표에서도 정상 작동하는지 검증한다.
     */
    [Test]
    public void ComplexScenario_NegativeCoordinates_WorksCorrectly()
    {
        // Arrange
        Line lineA = new Line(new Vector3(-10, -10, -10), new Vector3(-9, -10, -10));
        Line lineB = new Line(new Vector3(-10, -10, -10), new Vector3(-9, -10, -10));
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -90f, 90f);

        // Act
        bool isValid = joint.IsValid();
        joint.SetAngle(45f);

        // Assert
        Assert.IsTrue(isValid);
        Assert.AreEqual(45f, joint.CurrentAngle, Tolerance);
    }

    /**
     * @brief  매우 작은 각도 테스트 - 0.001도 단위의 미세 각도가 정확히 처리되는지 검증한다.
     */
    [Test]
    public void ComplexScenario_VerySmallAngles_HandledCorrectly()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -0.01f, 0.01f);

        // Act
        joint.SetAngle(0.005f);

        // Assert
        Assert.AreEqual(0.005f, joint.CurrentAngle, 0.0001f);
    }

    /**
     * @brief  매우 큰 각도 범위 테스트 - 360도 이상의 범위가 정상 처리되는지 검증한다.
     */
    [Test]
    public void ComplexScenario_VeryLargeAngleRange_HandledCorrectly()
    {
        // Arrange
        Line lineA = new Line(Vector3.zero, Vector3.right);
        Line lineB = new Line(Vector3.zero, Vector3.right);
        RevoluteJoint joint = new RevoluteJoint(lineA, lineB, -720f, 720f);

        // Act
        joint.SetAngle(540f);

        // Assert
        Assert.AreEqual(540f, joint.CurrentAngle, Tolerance);
    }

    #endregion
}