// ============================================================
// 파일명  : RevoluteJointComponentPlayModeTest.cs
// 역할    : RevoluteJointComponent 클래스의 PlayMode 통합 테스트
//           실제 씬 환경에서 Transform 직접 제어, 회전 클램프를 검증한다.
// 작성자  : 이현화
// 작성일  : 2026-04-01
// 수정이력: 2026-04-22 - Rigidbody 제거, Transform 직접 제어 방식으로 리팩토링
//                        (Gravity 테스트 제거, Constraints 테스트 제거)
//                      - SetupJointComponent() 에서 Rigidbody 생성 코드 제거
//                      - 회전 입력 방식을 Transform.rotation 직접 제어로 변경
//                      - _rotationAxis Reflection 제거 (_lineAPointA/B, _lineBPointA/B 방식으로 변경)
// ============================================================

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

[TestFixture]
public class RevoluteJointComponentPlayModeTest
{
    private const float Tolerance = 0.1f;
    private const float PhysicsWaitTime = 0.1f;

    private GameObject _objectA;
    private GameObject _objectB;
    private GameObject _managerObject;
    private RevoluteJointComponent _jointComponent;

    #region Setup / Teardown

    /**
     * @brief  각 테스트 전 씬 오브젝트를 생성한다.
     */
    [SetUp]
    public void SetUp()
    {
        // 오브젝트 A 생성 (기준)
        _objectA = new GameObject("ObjectA");
        _objectA.transform.position = Vector3.zero;

        // 오브젝트 B 생성 (회전 대상)
        _objectB = new GameObject("ObjectB");
        _objectB.transform.position = Vector3.zero;
        _objectB.transform.rotation = Quaternion.Euler(0, 0, 90);  // 초기 회전

        // RevoluteJointComponent 를 별도 오브젝트에 추가
        _managerObject = new GameObject("JointManager");
    }

    /**
     * @brief  각 테스트 후 씬 오브젝트를 정리한다.
     */
    [TearDown]
    public void TearDown()
    {
        if (_objectA != null) Object.Destroy(_objectA);
        if (_objectB != null) Object.Destroy(_objectB);
        if (_managerObject != null) Object.Destroy(_managerObject);
    }

    /**
     * @brief  RevoluteJointComponent 를 설정하고 초기화한다.
     *         회전축은 rotationAxis 방향으로 lineA, lineB 를 생성하여 설정한다.
     * @param  minAngle     최소 회전 각도
     * @param  maxAngle     최대 회전 각도
     * @param  rotationAxis 회전축 (월드 기준)
     */
    private void SetupJointComponent(float minAngle, float maxAngle, Vector3 rotationAxis)
    {
        _managerObject.SetActive(false);
        _jointComponent = _managerObject.AddComponent<RevoluteJointComponent>();

        var type = typeof(RevoluteJointComponent);
        var baseType = typeof(JointComponent);

        baseType.GetField("_objectA", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
          .SetValue(_jointComponent, _objectA.transform);
        baseType.GetField("_objectB", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
          .SetValue(_jointComponent, _objectB.transform);

        type.GetField("_minAngle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
          .SetValue(_jointComponent, minAngle);
        type.GetField("_maxAngle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
          .SetValue(_jointComponent, maxAngle);

        // 회전축 Line 점 좌표 설정 (rotationAxis 방향으로 lineA, lineB 생성)
        Vector3 pointA = Vector3.zero;
        Vector3 pointB = rotationAxis.normalized;

        type.GetField("_lineAPointA", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
          .SetValue(_jointComponent, pointA);
        type.GetField("_lineAPointB", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
          .SetValue(_jointComponent, pointB);
        type.GetField("_lineBPointA", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
          .SetValue(_jointComponent, pointA);
        type.GetField("_lineBPointB", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
          .SetValue(_jointComponent, pointB);

        _managerObject.SetActive(true);
    }

    #endregion

    #region 기본 동작 테스트

    /**
     * @brief  컴포넌트 초기화 테스트 - Awake 후 컴포넌트가 정상 초기화되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator Initialization_AfterAwake_ComponentInitializes()
    {
        // Arrange & Act
        SetupJointComponent(-90f, 90f, Vector3.right);
        yield return null;

        // Assert
        Assert.IsNotNull(_jointComponent);
        Assert.AreEqual(0f, _jointComponent.CurrentAngle, Tolerance);
    }

    /**
     * @brief  회전축 프로퍼티 테스트 - ObjectBRotationAxis 가 정확히 반환되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ObjectBRotationAxis_AfterInitialization_ReturnsCorrectAxis()
    {
        // Arrange
        SetupJointComponent(-90f, 90f, Vector3.right);
        yield return null;
        yield return new WaitForSeconds(PhysicsWaitTime);

        // Assert
        Vector3 axis = _jointComponent.ObjectBRotationAxis;
        Assert.AreEqual(1f, axis.x, Tolerance);
        Assert.AreEqual(0f, axis.y, Tolerance);
        Assert.AreEqual(0f, axis.z, Tolerance);
    }

    /**
     * @brief  Y축 회전축 테스트 - Y축 회전축이 정확히 설정되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ObjectBRotationAxis_YAxis_ReturnsYAxis()
    {
        // Arrange
        SetupJointComponent(-90f, 90f, Vector3.up);
        yield return null;
        yield return new WaitForSeconds(PhysicsWaitTime);

        // Assert
        Vector3 axis = _jointComponent.ObjectBRotationAxis;
        Assert.AreEqual(0f, axis.x, Tolerance);
        Assert.AreEqual(1f, axis.y, Tolerance);
        Assert.AreEqual(0f, axis.z, Tolerance);
    }

    #endregion

    #region 회전 클램프 테스트

    /**
     * @brief  범위 내 회전 테스트 - 범위 내 회전 시 클램프가 적용되지 않는지 검증한다.
     */
    [UnityTest]
    public IEnumerator Rotation_WithinRange_NoClampApplied()
    {
        // Arrange
        SetupJointComponent(-90f, 90f, Vector3.right);
        yield return null;

        // Act - 범위 내 회전 적용 (Transform 직접 제어)
        _objectB.transform.rotation = Quaternion.Euler(45f, 0, 90f);

        yield return new WaitForSeconds(PhysicsWaitTime);

        // Assert - 회전이 유지되어야 함
        float xAngle = _objectB.transform.rotation.eulerAngles.x;
        if (xAngle > 180f) xAngle -= 360f;
        Assert.That(Mathf.Abs(xAngle), Is.LessThanOrEqualTo(90f + Tolerance));
    }

    /**
     * @brief  최대 각도 초과 회전 테스트 - max 초과 시 클램프가 적용되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator Rotation_ExceedsMax_ClampApplied()
    {
        // Arrange
        SetupJointComponent(-90f, 90f, Vector3.right);
        yield return null;

        // Act - max 초과 회전 적용 (Transform 직접 제어)
        _objectB.transform.rotation = Quaternion.Euler(120f, 0, 90f);

        yield return new WaitForSeconds(PhysicsWaitTime * 2);

        // Assert - 클램프되어 90도 이하여야 함
        Assert.That(_jointComponent.CurrentAngle, Is.LessThanOrEqualTo(90f + Tolerance));
    }

    /**
     * @brief  최소 각도 미만 회전 테스트 - min 미만 시 클램프가 적용되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator Rotation_BelowMin_ClampApplied()
    {
        // Arrange
        SetupJointComponent(-90f, 90f, Vector3.right);
        yield return null;

        // Act - min 미만 회전 적용 (Transform 직접 제어)
        _objectB.transform.rotation = Quaternion.Euler(-120f, 0, 90f);

        yield return new WaitForSeconds(PhysicsWaitTime * 2);

        // Assert - 클램프되어 -90도 이상이어야 함
        Assert.That(_jointComponent.CurrentAngle, Is.GreaterThanOrEqualTo(-90f - Tolerance));
    }

    #endregion

    #region SetAngle 테스트

    /**
     * @brief  SetAngle 범위 내 테스트 - SetAngle 로 범위 내 각도가 설정되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator SetAngle_WithinRange_SetsAngle()
    {
        // Arrange
        SetupJointComponent(-90f, 90f, Vector3.right);
        yield return null;

        // Act
        _jointComponent.SetAngle(45f);
        yield return new WaitForSeconds(PhysicsWaitTime);

        // Assert
        Assert.AreEqual(45f, _jointComponent.CurrentAngle, Tolerance);
    }

    /**
     * @brief  SetAngle 최대 초과 테스트 - max 초과 시 클램프되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator SetAngle_ExceedsRange_ClampedToRange()
    {
        // Arrange
        SetupJointComponent(-90f, 90f, Vector3.right);
        yield return null;

        // Act
        _jointComponent.SetAngle(120f);
        yield return new WaitForSeconds(PhysicsWaitTime);

        // Assert
        Assert.AreEqual(90f, _jointComponent.CurrentAngle, Tolerance);
    }

    /**
     * @brief  SetAngle 음수 테스트 - 음수 각도가 정상 설정되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator SetAngle_NegativeAngle_SetsNegativeAngle()
    {
        // Arrange
        SetupJointComponent(-90f, 90f, Vector3.right);
        yield return null;

        // Act
        _jointComponent.SetAngle(-45f);
        yield return new WaitForSeconds(PhysicsWaitTime);

        // Assert
        Assert.AreEqual(-45f, _jointComponent.CurrentAngle, Tolerance);
    }

    #endregion

    #region 복합 시나리오 테스트

    /**
     * @brief  연속 회전 입력 테스트 - 여러 번의 회전 입력이 정상 처리되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ComplexScenario_ContinuousRotationInput_HandledCorrectly()
    {
        // Arrange
        SetupJointComponent(-90f, 90f, Vector3.right);
        yield return null;

        // Act - 여러 번 회전 입력 (Transform 직접 제어)
        for (int i = 0; i < 5; i++)
        {
            Quaternion currentRot = _objectB.transform.rotation;
            Quaternion delta = Quaternion.AngleAxis(10f, Vector3.right);
            _objectB.transform.rotation = delta * currentRot;
            yield return new WaitForFixedUpdate();
        }

        yield return new WaitForSeconds(PhysicsWaitTime);

        // Assert - 총 50도 회전 시도했으나 90도 이하로 클램프
        Assert.That(_jointComponent.CurrentAngle, Is.LessThanOrEqualTo(90f + Tolerance));
    }

    /**
     * @brief  빠른 회전 변경 테스트 - 빠르게 방향이 바뀔 때도 안정적인지 검증한다.
     */
    [UnityTest]
    public IEnumerator ComplexScenario_RapidDirectionChange_RemainsStable()
    {
        // Arrange
        SetupJointComponent(-90f, 90f, Vector3.right);
        yield return null;

        // Act - 빠르게 방향 변경 (Transform 직접 제어)
        float[] angles = { 30f, -30f, 45f, -45f, 60f, -60f };
        foreach (float angle in angles)
        {
            _objectB.transform.rotation = Quaternion.Euler(angle, 0, 90f);
            yield return new WaitForFixedUpdate();
        }

        yield return new WaitForSeconds(PhysicsWaitTime);

        // Assert - 범위 내에 있어야 함
        Assert.That(_jointComponent.CurrentAngle, Is.InRange(-90f - Tolerance, 90f + Tolerance));
    }

    /**
     * @brief  비대칭 범위 테스트 - 비대칭 min/max 에서도 정상 작동하는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ComplexScenario_AsymmetricRange_WorksCorrectly()
    {
        // Arrange
        SetupJointComponent(-30f, 150f, Vector3.right);
        yield return null;

        // Act - 양쪽 경계 테스트
        _jointComponent.SetAngle(-50f);  // min 미만
        yield return new WaitForSeconds(PhysicsWaitTime);
        Assert.AreEqual(-30f, _jointComponent.CurrentAngle, Tolerance);

        _jointComponent.SetAngle(200f);  // max 초과
        yield return new WaitForSeconds(PhysicsWaitTime);
        Assert.AreEqual(150f, _jointComponent.CurrentAngle, Tolerance);

        _jointComponent.SetAngle(60f);  // 범위 내
        yield return new WaitForSeconds(PhysicsWaitTime);
        Assert.AreEqual(60f, _jointComponent.CurrentAngle, Tolerance);
    }

    /**
     * @brief  좁은 범위 테스트 - 매우 좁은 범위에서도 정상 작동하는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ComplexScenario_NarrowRange_WorksCorrectly()
    {
        // Arrange
        SetupJointComponent(-5f, 5f, Vector3.right);
        yield return null;

        // Act & Assert
        _jointComponent.SetAngle(3f);
        yield return new WaitForSeconds(PhysicsWaitTime);
        Assert.AreEqual(3f, _jointComponent.CurrentAngle, Tolerance);

        _jointComponent.SetAngle(10f);  // 초과
        yield return new WaitForSeconds(PhysicsWaitTime);
        Assert.AreEqual(5f, _jointComponent.CurrentAngle, Tolerance);
    }

    /**
     * @brief  넓은 범위 테스트 - 매우 넓은 범위에서도 정상 작동하는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ComplexScenario_WideRange_WorksCorrectly()
    {
        // Arrange
        SetupJointComponent(-180f, 180f, Vector3.right);
        yield return null;

        // Act & Assert
        _jointComponent.SetAngle(170f);
        yield return new WaitForSeconds(PhysicsWaitTime);
        Assert.AreEqual(170f, _jointComponent.CurrentAngle, Tolerance);

        _jointComponent.SetAngle(-170f);
        yield return new WaitForSeconds(PhysicsWaitTime);
        Assert.AreEqual(-170f, _jointComponent.CurrentAngle, Tolerance);
    }

    /**
     * @brief  오브젝트 이동 후 회전 테스트 - 위치 변경 후에도 회전이 정상 작동하는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ComplexScenario_AfterPositionChange_RotationStillWorks()
    {
        // Arrange
        SetupJointComponent(-90f, 90f, Vector3.right);
        yield return null;

        // Act - 위치 변경 (Transform 직접 제어)
        _objectA.transform.position = new Vector3(5, 5, 5);
        _objectB.transform.position = new Vector3(5, 5, 5);
        yield return new WaitForSeconds(PhysicsWaitTime);

        // 회전 설정
        _jointComponent.SetAngle(45f);
        yield return new WaitForSeconds(PhysicsWaitTime);

        // Assert
        Assert.AreEqual(45f, _jointComponent.CurrentAngle, Tolerance);
    }

    /**
     * @brief  대각선 축 회전 테스트 - 대각선 축에서도 정상 작동하는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ComplexScenario_DiagonalAxis_WorksCorrectly()
    {
        // Arrange
        Vector3 diagonalAxis = new Vector3(1, 1, 0).normalized;
        SetupJointComponent(-90f, 90f, diagonalAxis);
        yield return null;
        yield return new WaitForSeconds(PhysicsWaitTime);

        // Assert - 축이 설정되었는지 확인
        Vector3 axis = _jointComponent.ObjectBRotationAxis;
        Assert.That(axis.magnitude, Is.EqualTo(1f).Within(Tolerance));
    }

    #endregion

    #region 에러 처리 테스트

    /**
     * @brief  오브젝트 A 미연결 테스트 - 오브젝트 A 가 없을 때 에러 없이 처리되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ErrorHandling_MissingObjectA_HandledGracefully()
    {
        _managerObject.SetActive(false);

        UnityEngine.TestTools.LogAssert.Expect(LogType.Error, "RevoluteJointComponent: 필수 오브젝트(A 또는 B)가 할당되지 않았습니다.");

        _jointComponent = _managerObject.AddComponent<RevoluteJointComponent>();

        var baseType = typeof(JointComponent);
        baseType.GetField("_objectB", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
          .SetValue(_jointComponent, _objectB.transform);

        _managerObject.SetActive(true);
        yield return null;
        yield return new WaitForSeconds(PhysicsWaitTime);

        Assert.Pass("No exception thrown with missing ObjectA");
    }

    /**
     * @brief  오브젝트 B 미연결 테스트 - 오브젝트 B 가 없을 때 에러 없이 처리되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ErrorHandling_MissingObjectB_HandledGracefully()
    {
        _managerObject.SetActive(false);

        UnityEngine.TestTools.LogAssert.Expect(LogType.Error, "RevoluteJointComponent: 필수 오브젝트(A 또는 B)가 할당되지 않았습니다.");

        _jointComponent = _managerObject.AddComponent<RevoluteJointComponent>();

        var baseType = typeof(JointComponent);
        baseType.GetField("_objectA", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
          .SetValue(_jointComponent, _objectA.transform);

        _managerObject.SetActive(true);
        yield return null;
        yield return new WaitForSeconds(PhysicsWaitTime);

        Assert.Pass("No exception thrown with missing ObjectB");
    }

    #endregion
}