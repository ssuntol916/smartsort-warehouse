// ============================================================
// 파일명  : SpurGearControllerPlayModeTest.cs
// 역할    : SpurGearController 의 PlayMode 통합 테스트.
//           SetSignal 동작, duration 경과 후 신호 해제, IsForward/IsSlider2 방향 반전,
//           연속 신호, 에러 처리를 검증한다.
//
// 작성자  : 이현화
// 작성일  : 2026-05-04
// 수정이력: 
// ============================================================

using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

[TestFixture]
public class SpurGearControllerPlayModeTest
{
    // ============================================================
    // 상수
    // ============================================================

    private const float Tolerance = 0.05f;
    private const float WaitTime = 0.1f;

    // ============================================================
    // 테스트 픽스처
    // ============================================================

    private GameObject _managerObject;
    private GameObject _shuttleObject;
    private GameObject _objectA;
    private GameObject _objectB;
    private SpurGearController _controller;
    private RevoluteJointComponent _spurGearJoint;

    // ============================================================
    // Setup / Teardown
    // ============================================================

    [SetUp]
    public void SetUp()
    {
        _objectA = new GameObject("ObjectA");
        _objectA.transform.position = Vector3.zero;

        _objectB = new GameObject("ObjectB");
        _objectB.transform.position = new Vector3(0.1f, 0f, 0f);
        _objectB.transform.rotation = Quaternion.Euler(0f, 0f, 90f);

        _shuttleObject = new GameObject("Shuttle");
        _managerObject = new GameObject("SpurGearManager");
    }

    [TearDown]
    public void TearDown()
    {
        if (_managerObject != null) Object.Destroy(_managerObject);
        if (_shuttleObject != null) Object.Destroy(_shuttleObject);
        if (_objectA != null) Object.Destroy(_objectA);
        if (_objectB != null) Object.Destroy(_objectB);
        if (_spurGearJoint != null) Object.Destroy(_spurGearJoint.gameObject);
    }

    // ============================================================
    // 헬퍼
    // ============================================================

    /**
     * @brief  RevoluteJointComponent 와 SpurGearController 를 설정하고 초기화한다.
     */
    private void SetupController(bool isSlider2 = false, bool useClamp = false)
    {
        _managerObject.SetActive(false);

        // RevoluteJointComponent — SetActive(false) 후 필드 설정 → SetActive(true)
        var revObj = new GameObject("RevoluteJointManager");
        revObj.SetActive(false);
        _spurGearJoint = revObj.AddComponent<RevoluteJointComponent>();

        var revType = typeof(RevoluteJointComponent);
        var baseType = typeof(JointComponent);

        baseType.GetField("_objectA", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_spurGearJoint, _objectA.transform);
        baseType.GetField("_objectB", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_spurGearJoint, _objectB.transform);
        revType.GetField("_minAngle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_spurGearJoint, -360f);
        revType.GetField("_maxAngle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_spurGearJoint, 360f);
        revType.GetField("_useClamp", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_spurGearJoint, useClamp);
        revType.GetField("_lineAPointA", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_spurGearJoint, Vector3.zero);
        revType.GetField("_lineAPointB", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_spurGearJoint, Vector3.forward);
        revType.GetField("_lineBPointA", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_spurGearJoint, Vector3.zero);
        revType.GetField("_lineBPointB", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_spurGearJoint, Vector3.forward);

        revObj.SetActive(true);

        // SpurGearController 설정
        _controller = _managerObject.AddComponent<SpurGearController>();
        var type = typeof(SpurGearController);
        type.GetField("_shuttle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _shuttleObject.transform);
        type.GetField("_spurGearJoint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _spurGearJoint);
        type.GetField("_isSlider2", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, isSlider2);

        _managerObject.SetActive(true);
    }

    private T GetField<T>(string name)
    {
        return (T)typeof(SpurGearController)
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(_controller);
    }

    // ============================================================
    // 기본 동작 테스트
    // ============================================================

    #region 기본 동작 테스트

    /**
     * @brief  초기화 후 _isInitialized=true 이고 IsSignal=false 인지 검증한다.
     */
    [UnityTest]
    public IEnumerator Initialization_AfterStart_IsInitializedAndNotSignal()
    {
        // Arrange & Act
        SetupController();
        yield return null;
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsTrue(GetField<bool>("_isInitialized"), "_isInitialized 가 true 여야 합니다.");
        Assert.IsFalse(_controller.IsSignal, "초기 IsSignal 이 false 여야 합니다.");
    }

    /**
     * @brief  SetSignal 호출 시 IsSignal=true, duration, isForward 가 올바르게 적용되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator SetSignal_ValidParams_AppliesCorrectly()
    {
        // Arrange
        SetupController();
        yield return null;

        // Act
        _controller.SetSignal(true, 2f);
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsTrue(_controller.IsSignal, "SetSignal 후 IsSignal 이 true 여야 합니다.");
        Assert.IsTrue(_controller.IsForward, "isForward=true 로 설정 후 IsForward 가 true 여야 합니다.");
        Assert.AreEqual(2f, GetField<float>("_duration"), Tolerance, "duration 이 2초여야 합니다.");
    }

    /**
     * @brief  duration 경과 후 IsSignal 이 자동으로 false 가 되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator Update_AfterDurationExpired_SignalBecomeFalse()
    {
        // Arrange
        SetupController();
        yield return null;

        // Act
        _controller.SetSignal(true, 0.1f);
        yield return new WaitForSeconds(0.3f);

        // Assert
        Assert.IsFalse(_controller.IsSignal, "duration 경과 후 IsSignal 이 false 여야 합니다.");
    }

    /**
     * @brief  동작 중 기어 각도가 변화하는지 검증한다.
     */
    [UnityTest]
    public IEnumerator Update_DuringSignal_GearAngleChanges()
    {
        // Arrange
        SetupController();
        yield return null;

        float initialAngle = _spurGearJoint.CurrentAngle;

        // Act
        _controller.SetSignal(true, 2f);
        yield return new WaitForSeconds(0.5f);

        // Assert
        Assert.AreNotEqual(initialAngle, _spurGearJoint.CurrentAngle, "동작 중 기어 각도가 변화해야 합니다.");
    }

    #endregion

    // ============================================================
    // IsForward / IsSlider2 테스트
    // ============================================================

    #region IsForward / IsSlider2 테스트

    /**
     * @brief  isSlider2=false 일 때 IsForward 가 _isForward 와 동일한지 검증한다.
     */
    [UnityTest]
    public IEnumerator IsForward_IsSlider2False_ReturnsSameAsIsForward()
    {
        // Arrange
        SetupController(isSlider2: false);
        yield return null;

        _controller.SetSignal(true, 1f);
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsTrue(_controller.IsForward, "isSlider2=false 이면 IsForward=true 여야 합니다.");
    }

    /**
     * @brief  isSlider2=true 일 때 IsForward 가 _isForward 의 반전인지 검증한다.
     */
    [UnityTest]
    public IEnumerator IsForward_IsSlider2True_ReturnsInvertedIsForward()
    {
        // Arrange
        SetupController(isSlider2: true);
        yield return null;

        _controller.SetSignal(true, 1f);
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsFalse(_controller.IsForward, "isSlider2=true 이면 IsForward 가 반전되어야 합니다.");
    }

    /**
     * @brief  isSlider2=true + isForward=false 일 때 IsForward=true 인지 검증한다.
     */
    [UnityTest]
    public IEnumerator IsForward_IsSlider2True_IsForwardFalse_ReturnsTrue()
    {
        // Arrange
        SetupController(isSlider2: true);
        yield return null;

        _controller.SetSignal(false, 1f);
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsTrue(_controller.IsForward, "isSlider2=true + isForward=false 이면 IsForward=true 여야 합니다.");
    }

    #endregion

    // ============================================================
    // 악조건 테스트
    // ============================================================

    #region 악조건 테스트

    /**
     * @brief  duration=0 으로 SetSignal 시 _isSignal=true 지만 기어가 회전하지 않는지 검증한다.
     *         Update 에서 _duration<=0 조건으로 즉시 return 하므로 각도 변화 없음.
     */
    [UnityTest]
    public IEnumerator SetSignal_ZeroDuration_SignalDoesNotActivate()
    {
        // Arrange
        SetupController();
        yield return null;

        float initialAngle = _spurGearJoint.CurrentAngle;

        // Act
        _controller.SetSignal(true, 0f);
        yield return null;
        yield return new WaitForSeconds(WaitTime);

        // Assert - 기어 각도 변화 없음
        Assert.AreEqual(initialAngle, _spurGearJoint.CurrentAngle,
            "duration=0 이면 기어가 회전하지 않아야 합니다.");
    }

    /**
     * @brief  음수 duration 으로 SetSignal 시 기어가 회전하지 않는지 검증한다.
     */
    [UnityTest]
    public IEnumerator SetSignal_NegativeDuration_SignalDoesNotActivate()
    {
        // Arrange
        SetupController();
        yield return null;

        float initialAngle = _spurGearJoint.CurrentAngle;

        // Act
        _controller.SetSignal(true, -1f);
        yield return null;
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.AreEqual(initialAngle, _spurGearJoint.CurrentAngle,
            "음수 duration 이면 기어가 회전하지 않아야 합니다.");
    }

    /**
     * @brief  새 신호 수신 시 elapsedTime 이 0 으로 리셋되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator SetSignal_NewSignal_ResetsElapsedTime()
    {
        // Arrange
        SetupController();
        yield return null;

        _controller.SetSignal(true, 5f);
        yield return new WaitForSeconds(0.3f);

        // Act
        _controller.SetSignal(true, 5f);
        yield return null;

        // Assert
        Assert.AreEqual(0f, GetField<float>("_elapsedTime"), Tolerance,
            "새 신호 수신 시 elapsedTime 이 0 이어야 합니다.");
    }

    /**
     * @brief  연속 방향 전환 시 마지막 방향이 적용되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator SetSignal_MultipleDirectionChanges_LastDirectionApplied()
    {
        // Arrange
        SetupController();
        yield return null;

        // Act
        _controller.SetSignal(true, 1f);
        _controller.SetSignal(false, 1f);
        _controller.SetSignal(true, 1f);
        _controller.SetSignal(false, 2f);
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsFalse(_controller.IsForward, "마지막 방향이 적용되어야 합니다.");
        Assert.AreEqual(2f, GetField<float>("_duration"), Tolerance, "마지막 duration 이 적용되어야 합니다.");
    }

    /**
     * @brief  Start 이전에 SetSignal 호출 시 EnsureInitialized 가 안전하게 처리되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator SetSignal_BeforeStart_EnsureInitializedHandledSafely()
    {
        // Arrange
        _managerObject.SetActive(false);

        var revObj = new GameObject("RevoluteJointManager");
        revObj.SetActive(false);
        _spurGearJoint = revObj.AddComponent<RevoluteJointComponent>();

        var revType = typeof(RevoluteJointComponent);
        var baseType = typeof(JointComponent);
        baseType.GetField("_objectA", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_spurGearJoint, _objectA.transform);
        baseType.GetField("_objectB", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_spurGearJoint, _objectB.transform);
        revType.GetField("_minAngle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_spurGearJoint, -360f);
        revType.GetField("_maxAngle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_spurGearJoint, 360f);
        revType.GetField("_useClamp", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_spurGearJoint, false);
        revType.GetField("_lineAPointA", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_spurGearJoint, Vector3.zero);
        revType.GetField("_lineAPointB", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_spurGearJoint, Vector3.forward);
        revType.GetField("_lineBPointA", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_spurGearJoint, Vector3.zero);
        revType.GetField("_lineBPointB", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_spurGearJoint, Vector3.forward);
        revObj.SetActive(true);

        _controller = _managerObject.AddComponent<SpurGearController>();
        typeof(SpurGearController)
            .GetField("_spurGearJoint", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_controller, _spurGearJoint);
        typeof(SpurGearController)
            .GetField("_shuttle", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_controller, _shuttleObject.transform);

        // Start 이전에 SetSignal 호출
        _controller.SetSignal(true, 1f);

        // Act
        _managerObject.SetActive(true);
        yield return null;
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsTrue(_controller.IsSignal, "Start 이전 SetSignal 후 신호가 활성화되어야 합니다.");
        Assert.IsTrue(GetField<bool>("_isInitialized"), "_isInitialized 가 true 여야 합니다.");
    }

    /**
     * @brief  매우 큰 duration 으로 SetSignal 시 정상 동작하는지 검증한다.
     */
    [UnityTest]
    public IEnumerator SetSignal_VeryLargeDuration_SignalActivatesNormally()
    {
        // Arrange
        SetupController();
        yield return null;

        // Act
        _controller.SetSignal(true, 10000f);
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsTrue(_controller.IsSignal, "큰 duration 이어도 신호가 활성화되어야 합니다.");
    }

    /**
     * @brief  동작 중 재신호 시 duration 이 리셋되어 계속 동작하는지 검증한다.
     */
    [UnityTest]
    public IEnumerator SetSignal_ResignalDuringOperation_DurationResets()
    {
        // Arrange
        SetupController();
        yield return null;

        _controller.SetSignal(true, 0.2f);
        yield return new WaitForSeconds(0.15f);

        // Act
        _controller.SetSignal(true, 0.2f);
        yield return new WaitForSeconds(0.15f);

        // Assert
        Assert.IsTrue(_controller.IsSignal, "재신호 후 duration 이 리셋되어 계속 동작 중이어야 합니다.");
    }

    #endregion

    // ============================================================
    // 에러 처리 테스트
    // ============================================================

    #region 에러 처리 테스트

    /**
     * @brief  SpurGearJoint 미할당 시 에러 로그가 발생하고 컴포넌트가 비활성화되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ValidateComponents_MissingSpurGearJoint_LogsError()
    {
        // Arrange
        _managerObject.SetActive(false);
        _controller = _managerObject.AddComponent<SpurGearController>();

        LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex(".*SpurGearJoint.*"));

        // Act
        _managerObject.SetActive(true);
        yield return null;

        // Assert
        Assert.IsFalse(_controller.enabled, "SpurGearJoint 미할당 시 컴포넌트가 비활성화되어야 합니다.");
    }

    #endregion
}