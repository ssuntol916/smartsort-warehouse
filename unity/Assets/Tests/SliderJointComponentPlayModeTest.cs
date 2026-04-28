// ============================================================
// 파일명  : SliderJointComponentPlayModeTest.cs
// 역할    : SliderJointComponent 클래스의 PlayMode 통합 테스트
//           실제 씬 환경에서 Transform 직접 제어, 위치 클램프를 검증한다.
// 작성자  : 이현화
// 작성일  : 2026-04-01
// 수정이력: 2026-04-22 - Rigidbody 제거, Transform 직접 제어 방식으로 리팩토링
//                        (Gravity 테스트 제거, Constraints 테스트 제거)
//                      - SetupJointComponent() 에서 Rigidbody 생성 코드 제거
//                      - 이동 입력 방식을 Transform.position 직접 제어로 변경
//                      - Line, Plane 점 좌표 Reflection 설정 추가
// ============================================================

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

[TestFixture]
public class SliderJointComponentPlayModeTest
{
    private const float Tolerance = 0.1f;
    private const float PhysicsWaitTime = 0.1f;

    private GameObject _objectA;
    private GameObject _objectB;
    private GameObject _managerObject;
    private SliderJointComponent _jointComponent;

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

        // 오브젝트 B 생성 (이동 대상)
        _objectB = new GameObject("ObjectB");
        _objectB.transform.position = Vector3.zero;

        // SliderJointComponent 를 별도 오브젝트에 추가
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
     * @brief  SliderJointComponent 를 설정하고 초기화한다.
     *         이동축 Line 과 기준 Plane 을 moveDirection 방향으로 생성하여 설정한다.
     * @param  minPosition     최소 이동 범위
     * @param  maxPosition     최대 이동 범위
     * @param  moveDirection   이동 방향 (월드 기준)
     */
    private void SetupJointComponent(float minPosition, float maxPosition, Vector3 moveDirection)
    {
        _managerObject.SetActive(false);
        _jointComponent = _managerObject.AddComponent<SliderJointComponent>();

        var type = typeof(SliderJointComponent);
        var baseType = typeof(JointComponent);

        baseType.GetField("_objectA", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_jointComponent, _objectA.transform);
        baseType.GetField("_objectB", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_jointComponent, _objectB.transform);

        type.GetField("_minPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_jointComponent, minPosition);
        type.GetField("_maxPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_jointComponent, maxPosition);
        type.GetField("_moveDirection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_jointComponent, moveDirection);

        // 이동축 Line 점 좌표 설정 (moveDirection 방향으로 lineA, lineB 생성)
        Vector3 linePointA = Vector3.zero;
        Vector3 linePointB = moveDirection.sqrMagnitude > 0 ? moveDirection.normalized : Vector3.right;

        type.GetField("_lineAPointA", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_jointComponent, linePointA);
        type.GetField("_lineAPointB", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_jointComponent, linePointB);
        type.GetField("_lineBPointA", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_jointComponent, linePointA);
        type.GetField("_lineBPointB", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_jointComponent, linePointB);

        // 기준 Plane 점 좌표 설정 (moveDirection 과 수직인 평면 생성)
        Vector3 perpA = Vector3.Cross(linePointB, Vector3.up).sqrMagnitude > 0
            ? Vector3.Cross(linePointB, Vector3.up).normalized
            : Vector3.Cross(linePointB, Vector3.right).normalized;
        Vector3 perpB = Vector3.Cross(linePointB, perpA).normalized;

        type.GetField("_planeAPointA", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_jointComponent, Vector3.zero);
        type.GetField("_planeAPointB", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_jointComponent, perpA);
        type.GetField("_planeAPointC", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_jointComponent, perpB);
        type.GetField("_planeBPointA", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_jointComponent, Vector3.zero);
        type.GetField("_planeBPointB", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_jointComponent, perpA);
        type.GetField("_planeBPointC", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_jointComponent, perpB);

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
        SetupJointComponent(0f, 100f, Vector3.right);
        yield return null;

        // Assert
        Assert.IsNotNull(_jointComponent);
        Assert.AreEqual(0f, _jointComponent.CurrentPosition, Tolerance);
    }

    /**
     * @brief  X축 이동 방향 테스트 - X축 방향이 정확히 설정되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator MoveDirection_XAxis_SetsCorrectly()
    {
        // Arrange
        SetupJointComponent(0f, 100f, Vector3.right);
        yield return null;
        yield return new WaitForSeconds(PhysicsWaitTime);

        // Assert
        Assert.AreEqual(0f, _jointComponent.CurrentPosition, Tolerance);
    }

    /**
     * @brief  Y축 이동 방향 테스트 - Y축 방향이 정확히 설정되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator MoveDirection_YAxis_SetsCorrectly()
    {
        // Arrange
        SetupJointComponent(0f, 100f, Vector3.up);
        yield return null;
        yield return new WaitForSeconds(PhysicsWaitTime);

        // Assert
        Assert.AreEqual(0f, _jointComponent.CurrentPosition, Tolerance);
    }

    /**
     * @brief  Z축 이동 방향 테스트 - Z축 방향이 정확히 설정되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator MoveDirection_ZAxis_SetsCorrectly()
    {
        // Arrange
        SetupJointComponent(0f, 100f, Vector3.forward);
        yield return null;
        yield return new WaitForSeconds(PhysicsWaitTime);

        // Assert
        Assert.AreEqual(0f, _jointComponent.CurrentPosition, Tolerance);
    }

    #endregion

    #region 위치 클램프 테스트

    /**
     * @brief  범위 내 이동 테스트 - 범위 내 이동 시 클램프가 적용되지 않는지 검증한다.
     */
    [UnityTest]
    public IEnumerator Movement_WithinRange_NoClampApplied()
    {
        // Arrange
        SetupJointComponent(0f, 100f, Vector3.right);
        yield return null;

        // Act - 범위 내 위치로 이동 (Transform 직접 제어)
        _objectB.transform.position = new Vector3(50f, 0f, 0f);

        yield return new WaitForSeconds(PhysicsWaitTime);

        // Assert - 위치가 유지되어야 함
        Assert.That(_objectB.transform.position.x, Is.InRange(0f, 100f + Tolerance));
    }

    /**
     * @brief  최대 위치 초과 이동 테스트 - max 초과 시 클램프가 적용되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator Movement_ExceedsMax_ClampApplied()
    {
        // Arrange
        SetupJointComponent(0f, 100f, Vector3.right);
        yield return null;

        // Act - max 초과 위치로 이동 (Transform 직접 제어)
        _objectB.transform.position = new Vector3(150f, 0f, 0f);

        yield return new WaitForSeconds(PhysicsWaitTime * 2);

        // Assert - 클램프되어 100 이하여야 함
        Assert.That(_jointComponent.CurrentPosition, Is.LessThanOrEqualTo(100f + Tolerance));
    }

    /**
     * @brief  최소 위치 미만 이동 테스트 - min 미만 시 클램프가 적용되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator Movement_BelowMin_ClampApplied()
    {
        // Arrange
        SetupJointComponent(0f, 100f, Vector3.right);
        yield return null;

        // Act - min 미만 위치로 이동 (Transform 직접 제어)
        _objectB.transform.position = new Vector3(-50f, 0f, 0f);

        yield return new WaitForSeconds(PhysicsWaitTime * 2);

        // Assert - 클램프되어 0 이상이어야 함
        Assert.That(_jointComponent.CurrentPosition, Is.GreaterThanOrEqualTo(0f - Tolerance));
    }

    #endregion

    #region SetPosition 테스트

    /**
     * @brief  SetPosition 범위 내 테스트 - SetPosition 으로 범위 내 위치가 설정되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator SetPosition_WithinRange_SetsPosition()
    {
        // Arrange
        SetupJointComponent(0f, 100f, Vector3.right);
        yield return null;

        // Act
        _jointComponent.SetPosition(50f);
        yield return new WaitForSeconds(PhysicsWaitTime);

        // Assert
        Assert.AreEqual(50f, _jointComponent.CurrentPosition, Tolerance);
    }

    /**
     * @brief  SetPosition 최대 초과 테스트 - max 초과 시 클램프되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator SetPosition_ExceedsMax_ClampedToMax()
    {
        // Arrange
        SetupJointComponent(0f, 100f, Vector3.right);
        yield return null;

        // Act
        _jointComponent.SetPosition(150f);
        yield return new WaitForSeconds(PhysicsWaitTime);

        // Assert
        Assert.AreEqual(100f, _jointComponent.CurrentPosition, Tolerance);
    }

    /**
     * @brief  SetPosition 최소 미만 테스트 - min 미만 시 클램프되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator SetPosition_BelowMin_ClampedToMin()
    {
        // Arrange
        SetupJointComponent(0f, 100f, Vector3.right);
        yield return null;

        // Act
        _jointComponent.SetPosition(-50f);
        yield return new WaitForSeconds(PhysicsWaitTime);

        // Assert
        Assert.AreEqual(0f, _jointComponent.CurrentPosition, Tolerance);
    }

    #endregion

    #region 복합 시나리오 테스트

    /**
     * @brief  연속 이동 입력 테스트 - 여러 번의 이동 입력이 정상 처리되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ComplexScenario_ContinuousMovementInput_HandledCorrectly()
    {
        // Arrange
        SetupJointComponent(0f, 100f, Vector3.right);
        yield return null;

        // Act - 여러 번 이동 입력 (Transform 직접 제어)
        for (int i = 0; i < 5; i++)
        {
            Vector3 currentPos = _objectB.transform.position;
            _objectB.transform.position = currentPos + Vector3.right * 10f;
            yield return new WaitForFixedUpdate();
        }

        yield return new WaitForSeconds(PhysicsWaitTime);

        // Assert - 총 50 이동 시도했으나 100 이하로 클램프
        Assert.That(_jointComponent.CurrentPosition, Is.LessThanOrEqualTo(100f + Tolerance));
    }

    /**
     * @brief  빠른 방향 변경 테스트 - 빠르게 방향이 바뀔 때도 안정적인지 검증한다.
     */
    [UnityTest]
    public IEnumerator ComplexScenario_RapidDirectionChange_RemainsStable()
    {
        // Arrange
        SetupJointComponent(0f, 100f, Vector3.right);
        yield return null;

        // Act - 빠르게 위치 변경 (Transform 직접 제어)
        float[] positions = { 30f, -10f, 50f, 120f, 80f, -20f };
        foreach (float pos in positions)
        {
            _objectB.transform.position = new Vector3(pos, 0f, 0f);
            yield return new WaitForFixedUpdate();
        }

        yield return new WaitForSeconds(PhysicsWaitTime);

        // Assert - 범위 내에 있어야 함
        Assert.That(_jointComponent.CurrentPosition, Is.InRange(0f - Tolerance, 100f + Tolerance));
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
        _jointComponent.SetPosition(-50f);  // min 미만
        yield return new WaitForSeconds(PhysicsWaitTime);
        Assert.AreEqual(-30f, _jointComponent.CurrentPosition, Tolerance);

        _jointComponent.SetPosition(200f);  // max 초과
        yield return new WaitForSeconds(PhysicsWaitTime);
        Assert.AreEqual(150f, _jointComponent.CurrentPosition, Tolerance);

        _jointComponent.SetPosition(60f);  // 범위 내
        yield return new WaitForSeconds(PhysicsWaitTime);
        Assert.AreEqual(60f, _jointComponent.CurrentPosition, Tolerance);
    }

    /**
     * @brief  좁은 범위 테스트 - 매우 좁은 범위에서도 정상 작동하는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ComplexScenario_NarrowRange_WorksCorrectly()
    {
        // Arrange
        SetupJointComponent(48f, 52f, Vector3.right);
        yield return null;

        // Act & Assert
        _jointComponent.SetPosition(50f);
        yield return new WaitForSeconds(PhysicsWaitTime);
        Assert.AreEqual(50f, _jointComponent.CurrentPosition, Tolerance);

        _jointComponent.SetPosition(40f);  // 미만
        yield return new WaitForSeconds(PhysicsWaitTime);
        Assert.AreEqual(48f, _jointComponent.CurrentPosition, Tolerance);

        _jointComponent.SetPosition(60f);  // 초과
        yield return new WaitForSeconds(PhysicsWaitTime);
        Assert.AreEqual(52f, _jointComponent.CurrentPosition, Tolerance);
    }

    /**
     * @brief  넓은 범위 테스트 - 매우 넓은 범위에서도 정상 작동하는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ComplexScenario_WideRange_WorksCorrectly()
    {
        // Arrange
        SetupJointComponent(-500f, 500f, Vector3.right);
        yield return null;

        // Act & Assert
        _jointComponent.SetPosition(250f);
        yield return new WaitForSeconds(PhysicsWaitTime);
        Assert.AreEqual(250f, _jointComponent.CurrentPosition, Tolerance);

        _jointComponent.SetPosition(-250f);
        yield return new WaitForSeconds(PhysicsWaitTime);
        Assert.AreEqual(-250f, _jointComponent.CurrentPosition, Tolerance);
    }

    /**
     * @brief  오브젝트 A 위치 변경 후 이동 테스트 - A 위치 변경 후에도 정상 작동하는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ComplexScenario_AfterObjectAPositionChange_MovementStillWorks()
    {
        // Arrange
        SetupJointComponent(0f, 100f, Vector3.right);
        yield return null;

        // Act - 오브젝트 A 위치 변경 (Transform 직접 제어)
        _objectA.transform.position = new Vector3(50f, 50f, 50f);
        _objectB.transform.position = new Vector3(50f, 50f, 50f);
        yield return new WaitForSeconds(PhysicsWaitTime);

        // 위치 설정
        _jointComponent.SetPosition(30f);
        yield return new WaitForSeconds(PhysicsWaitTime);

        // Assert
        Assert.AreEqual(30f, _jointComponent.CurrentPosition, Tolerance);
    }

    /**
     * @brief  대각선 이동 방향 테스트 - 대각선 방향에서도 정상 작동하는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ComplexScenario_DiagonalMoveDirection_WorksCorrectly()
    {
        // Arrange
        Vector3 diagonalDir = new Vector3(1, 1, 0).normalized;
        SetupJointComponent(0f, 100f, diagonalDir);
        yield return null;
        yield return new WaitForSeconds(PhysicsWaitTime);

        // Assert - 초기화가 정상적으로 되었는지 확인
        Assert.AreEqual(0f, _jointComponent.CurrentPosition, Tolerance);
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

        UnityEngine.TestTools.LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex("SliderJointComponent: 필수 오브젝트.*"));

        _jointComponent = _managerObject.AddComponent<SliderJointComponent>();

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

        UnityEngine.TestTools.LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex("SliderJointComponent: 필수 오브젝트.*"));

        _jointComponent = _managerObject.AddComponent<SliderJointComponent>();

        var baseType = typeof(JointComponent);
        baseType.GetField("_objectA", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_jointComponent, _objectA.transform);

        _managerObject.SetActive(true);
        yield return null;
        yield return new WaitForSeconds(PhysicsWaitTime);

        Assert.Pass("No exception thrown with missing ObjectB");
    }

    /**
     * @brief  영벡터 이동 방향 테스트 - 이동 방향이 영벡터일 때 에러 없이 처리되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ErrorHandling_ZeroMoveDirection_HandledGracefully()
    {
        // Arrange
        SetupJointComponent(0f, 100f, Vector3.zero);
        yield return null;
        yield return new WaitForSeconds(PhysicsWaitTime);

        // Assert - 예외 없이 실행되어야 함
        Assert.Pass("No exception thrown with zero move direction");
    }

    #endregion
}