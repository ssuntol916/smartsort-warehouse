// ============================================================
// 파일명  : WheelControllerBasePlayModeTest.cs
// 역할    : WheelControllerBase (XWheelController) 의 PlayMode 통합 테스트.
//           MQTT 메시지 파싱, 신호 적용, duration 경과 후 신호 해제,
//           미초기화 상태 방어, 잘못된 페이로드 처리를 검증한다.
//
// 작성자  : 이현화
// 작성일  : 2026-05-04
// 수정이력: 
// ============================================================

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Reflection;

[TestFixture]
public class WheelControllerBasePlayModeTest
{
    // ============================================================
    // 상수
    // ============================================================

    private const float Tolerance = 0.05f;
    private const float WaitTime = 0.1f;
    private const string TargetTopic = "warehouse/cmd/shuttle";
    private const string OtherTopic = "warehouse/status/shuttle";

    // ============================================================
    // 테스트용 구체 파생 클래스
    // ============================================================

    /// <summary>
    /// WheelControllerBase 는 abstract 이므로 테스트용 최소 구현체를 사용한다.
    /// 실제 바퀴/셔틀 이동은 검증하지 않고 신호 처리 로직만 검증한다.
    /// </summary>
    private class TestWheelController : WheelControllerBase
    {
        protected override string Axis => "x";
        protected override Vector3 MoveDirection => Vector3.right;
        protected override string LogPrefix => "[TestWheel]";
    }

    // ============================================================
    // 테스트 픽스처
    // ============================================================

    private GameObject _managerObject;
    private GameObject _shuttleObject;
    private MqttSubscriber _mqttSubscriber;
    private TestWheelController _controller;

    // ============================================================
    // Setup / Teardown
    // ============================================================

    [SetUp]
    public void SetUp()
    {
        _shuttleObject = new GameObject("Shuttle");

        // MqttSubscriber — 비활성화 후 추가하여 Awake 에러 방지
        var mqttObject = new GameObject("MQTTManager");
        mqttObject.SetActive(false);
        _mqttSubscriber = mqttObject.AddComponent<MqttSubscriber>();
        mqttObject.SetActive(true);

        _managerObject = new GameObject("WheelManager");
        _managerObject.SetActive(false);
    }

    [TearDown]
    public void TearDown()
    {
        if (_managerObject != null) Object.Destroy(_managerObject);
        if (_shuttleObject != null) Object.Destroy(_shuttleObject);
        if (_mqttSubscriber != null) Object.Destroy(_mqttSubscriber.gameObject);
    }

    // ============================================================
    // 헬퍼
    // ============================================================

    /**
     * @brief  Reflection 으로 private 필드 값을 가져온다.
     */
    private object GetField(string fieldName)
    {
        return typeof(WheelControllerBase)
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(_controller);
    }

    /**
     * @brief  MqttSubscriber 를 할당하고 컨트롤러를 초기화한다.
     *         ValidateComponents 는 바퀴 조인트 미할당으로 실패하지만
     *         MQTT 구독은 OnEnable 에서 처리되므로 메시지 수신 테스트는 가능하다.
     *         _initialized 는 SetActive(true) 이후에 강제 설정하여
     *         Start() 에서 덮어쓰이지 않도록 한다.
     */
    private void SetupController()
    {
        _controller = _managerObject.AddComponent<TestWheelController>();

        var baseType = typeof(WheelControllerBase);
        baseType.GetField("_shuttle", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_controller, _shuttleObject.transform);
        baseType.GetField("_mqttSubscriber", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_controller, _mqttSubscriber);

        // Start() 에서 ValidateComponents 실패 → 바퀴 조인트 에러만 발생
        LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex(".*바퀴 조인트.*"));

        _managerObject.SetActive(true);

        // ValidateComponents 실패로 enabled=false 가 되므로 강제로 true 로 복원
        _controller.enabled = true;

        // SetActive 이후에 설정해야 Start() 가 false 로 덮어쓰지 않음
        baseType.GetField("_initialized", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_controller, true);
    }

    /**
     * @brief  WheelControllerBase.HandleMqttMessage 를 Reflection 으로 직접 호출하여
     *         메시지 수신을 시뮬레이션한다.
     *         MqttSubscriber.OnMessage 는 event 타입이라 외부 Invoke 불가이므로
     *         private 핸들러를 직접 호출한다.
     */
    private void SimulateMqttMessage(string topic, string payload)
    {
        typeof(WheelControllerBase)
            .GetMethod("HandleMqttMessage",
                BindingFlags.NonPublic | BindingFlags.Instance)
            ?.Invoke(_controller, new object[] { topic, payload });
    }

    // ============================================================
    // 기본 동작 테스트
    // ============================================================

    #region 기본 동작 테스트

    /**
     * @brief  정상 페이로드 수신 시 _isSignal=true, duration, isForward 가 올바르게 적용되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ReceiveMessage_ValidPayload_AppliesSignalCorrectly()
    {
        // Arrange
        SetupController();
        yield return null;

        string payload = "{\"action\":\"drive_direct\",\"axis\":\"x\",\"angle\":[0,180],\"duration_ms\":1100}";

        // Act
        SimulateMqttMessage(TargetTopic, payload);
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsTrue((bool)GetField("_isSignal"), "isSignal 이 true 여야 합니다.");
        Assert.AreEqual(1.1f, (float)GetField("_duration"), Tolerance, "duration 이 1.1초여야 합니다.");
        Assert.IsTrue((bool)GetField("_isForward"), "angle[0]<angle[1] 이면 isForward=true 여야 합니다.");
    }

    /**
     * @brief  angle[180,0] 수신 시 isForward=false 가 적용되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ReceiveMessage_ReverseAngle_SetsIsForwardFalse()
    {
        // Arrange
        SetupController();
        yield return null;

        string payload = "{\"action\":\"drive_direct\",\"axis\":\"x\",\"angle\":[180,0],\"duration_ms\":1100}";

        // Act
        SimulateMqttMessage(TargetTopic, payload);
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsFalse((bool)GetField("_isForward"), "angle[0]>angle[1] 이면 isForward=false 여야 합니다.");
    }

    /**
     * @brief  _elapsedTime >= _duration 시 StopMovement 가 호출되어 _isSignal=false 가 되는지 검증한다.
     *         바퀴 조인트 미설정으로 Update 실행이 불가하므로
     *         Reflection 으로 직접 상태를 설정하고 Update 를 우회하여 검증한다.
     */
    [UnityTest]
    public IEnumerator Update_AfterDurationExpired_SignalBecomeFalse()
    {
        // Arrange
        SetupController();
        yield return null;

        var baseType = typeof(WheelControllerBase);

        // _isSignal=true, _duration=0.1, _elapsedTime=0.2 로 직접 설정 (duration 초과 상태)
        baseType.GetField("_isSignal", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, true);
        baseType.GetField("_duration", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, 0.1f);
        baseType.GetField("_elapsedTime", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, 0.2f);

        // Act - StopMovement 를 직접 호출
        typeof(WheelControllerBase)
            .GetMethod("StopMovement", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.Invoke(_controller, null);

        yield return null;

        // Assert
        Assert.IsFalse((bool)GetField("_isSignal"), "duration 경과 후 isSignal 이 false 여야 합니다.");
        Assert.AreEqual(0f, (float)GetField("_elapsedTime"), Tolerance, "StopMovement 후 elapsedTime 이 0 이어야 합니다.");
    }

    /**
     * @brief  새 신호 수신 시 elapsedTime 이 0 으로 리셋되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ReceiveMessage_NewSignal_ResetsElapsedTime()
    {
        // Arrange
        SetupController();
        yield return null;

        string payload = "{\"action\":\"drive_direct\",\"axis\":\"x\",\"angle\":[0,180],\"duration_ms\":2000}";
        SimulateMqttMessage(TargetTopic, payload);
        yield return new WaitForSeconds(0.5f);

        // Act - 새 신호 수신
        SimulateMqttMessage(TargetTopic, payload);
        yield return null;

        // Assert
        Assert.AreEqual(0f, (float)GetField("_elapsedTime"), Tolerance,
            "새 신호 수신 시 elapsedTime 이 0 이어야 합니다.");
    }

    #endregion

    // ============================================================
    // 토픽 필터 테스트
    // ============================================================

    #region 토픽 필터 테스트

    /**
     * @brief  다른 토픽 수신 시 신호가 적용되지 않는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ReceiveMessage_WrongTopic_DoesNotApplySignal()
    {
        // Arrange
        SetupController();
        yield return null;

        string payload = "{\"action\":\"drive_direct\",\"axis\":\"x\",\"angle\":[0,180],\"duration_ms\":1100}";

        // Act
        SimulateMqttMessage(OtherTopic, payload);
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsFalse((bool)GetField("_isSignal"), "다른 토픽이면 신호가 적용되지 않아야 합니다.");
    }

    /**
     * @brief  다른 axis 수신 시 신호가 적용되지 않는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ReceiveMessage_WrongAxis_DoesNotApplySignal()
    {
        // Arrange
        SetupController();
        yield return null;

        string payload = "{\"action\":\"drive_direct\",\"axis\":\"y\",\"angle\":[0,180],\"duration_ms\":1100}";

        // Act
        SimulateMqttMessage(TargetTopic, payload);
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsFalse((bool)GetField("_isSignal"), "다른 axis 이면 신호가 적용되지 않아야 합니다.");
    }

    /**
     * @brief  action 이 drive_direct 가 아닐 때 신호가 적용되지 않는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ReceiveMessage_WrongAction_DoesNotApplySignal()
    {
        // Arrange
        SetupController();
        yield return null;

        string payload = "{\"action\":\"servo_direct\",\"axis\":\"x\",\"angle\":[0,180],\"duration_ms\":1100}";

        // Act
        SimulateMqttMessage(TargetTopic, payload);
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsFalse((bool)GetField("_isSignal"), "action 이 drive_direct 가 아니면 신호가 적용되지 않아야 합니다.");
    }

    #endregion

    // ============================================================
    // 악조건 테스트
    // ============================================================

    #region 악조건 테스트

    /**
     * @brief  빈 페이로드 수신 시 예외 없이 처리되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ReceiveMessage_EmptyPayload_HandledGracefully()
    {
        // Arrange
        SetupController();
        yield return null;

        // Act
        SimulateMqttMessage(TargetTopic, "");
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsFalse((bool)GetField("_isSignal"), "빈 페이로드 수신 시 신호가 적용되지 않아야 합니다.");
    }

    /**
     * @brief  angle 배열이 1개일 때 신호가 적용되지 않는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ReceiveMessage_InsufficientAngleArray_DoesNotApplySignal()
    {
        // Arrange
        SetupController();
        yield return null;

        string payload = "{\"action\":\"drive_direct\",\"axis\":\"x\",\"angle\":[0],\"duration_ms\":1100}";

        // Act
        SimulateMqttMessage(TargetTopic, payload);
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsFalse((bool)GetField("_isSignal"), "angle 배열이 부족하면 신호가 적용되지 않아야 합니다.");
    }

    /**
     * @brief  duration_ms=0 수신 시 _isSignal=true 가 되지만 Update 에서
     *         _duration<=0 조건으로 즉시 return → 셔틀 이동 없음을 검증한다.
     */
    [UnityTest]
    public IEnumerator ReceiveMessage_ZeroDuration_SignalDoesNotActivate()
    {
        // Arrange
        SetupController();
        yield return null;

        string payload = "{\"action\":\"drive_direct\",\"axis\":\"x\",\"angle\":[0,180],\"duration_ms\":0}";
        Vector3 initialPos = _shuttleObject.transform.position;

        // Act
        SimulateMqttMessage(TargetTopic, payload);
        yield return null;
        yield return new WaitForSeconds(WaitTime);

        // Assert - _duration<=0 이면 Update 즉시 return → 위치 변화 없음
        Assert.AreEqual(initialPos, _shuttleObject.transform.position,
            "duration=0 이면 셔틀이 이동하지 않아야 합니다.");
    }

    /**
     * @brief  음수 duration_ms 수신 시 Update 에서 _duration<=0 조건으로
     *         즉시 return → 셔틀 이동 없음을 검증한다.
     */
    [UnityTest]
    public IEnumerator ReceiveMessage_NegativeDuration_SignalDoesNotActivate()
    {
        // Arrange
        SetupController();
        yield return null;

        string payload = "{\"action\":\"drive_direct\",\"axis\":\"x\",\"angle\":[0,180],\"duration_ms\":-500}";
        Vector3 initialPos = _shuttleObject.transform.position;

        // Act
        SimulateMqttMessage(TargetTopic, payload);
        yield return null;
        yield return new WaitForSeconds(WaitTime);

        // Assert - 음수 duration 이면 셔틀 이동 없음
        Assert.AreEqual(initialPos, _shuttleObject.transform.position,
            "음수 duration 이면 셔틀이 이동하지 않아야 합니다.");
    }

    /**
     * @brief  _initialized=false 상태에서 MQTT 메시지 수신 시 신호가 무시되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ReceiveMessage_NotInitialized_MessageIgnored()
    {
        // Arrange
        _controller = _managerObject.AddComponent<TestWheelController>();
        var baseType = typeof(WheelControllerBase);
        baseType.GetField("_mqttSubscriber", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_controller, _mqttSubscriber);
        baseType.GetField("_initialized", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_controller, false);

        // Start() 에서 ValidateComponents 실패 에러 예상 (바퀴 조인트 + _shuttle)
        LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex(".*바퀴 조인트.*"));
        LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex(".*_shuttle.*"));

        _managerObject.SetActive(true);
        yield return null;

        string payload = "{\"action\":\"drive_direct\",\"axis\":\"x\",\"angle\":[0,180],\"duration_ms\":1100}";

        // Act
        SimulateMqttMessage(TargetTopic, payload);
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsFalse((bool)GetField("_isSignal"), "_initialized=false 이면 메시지가 무시되어야 합니다.");
    }

    /**
     * @brief  연속 신호 수신 시 마지막 신호가 올바르게 적용되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ReceiveMessage_MultipleSignals_LastSignalApplied()
    {
        // Arrange
        SetupController();
        yield return null;

        string forwardPayload = "{\"action\":\"drive_direct\",\"axis\":\"x\",\"angle\":[0,180],\"duration_ms\":2000}";
        string backwardPayload = "{\"action\":\"drive_direct\",\"axis\":\"x\",\"angle\":[180,0],\"duration_ms\":1100}";

        // Act
        SimulateMqttMessage(TargetTopic, forwardPayload);
        SimulateMqttMessage(TargetTopic, backwardPayload);
        yield return new WaitForSeconds(WaitTime);

        // Assert - 마지막 신호(역방향) 적용
        Assert.IsFalse((bool)GetField("_isForward"), "마지막 신호의 방향이 적용되어야 합니다.");
        Assert.AreEqual(1.1f, (float)GetField("_duration"), Tolerance,
            "마지막 신호의 duration 이 적용되어야 합니다.");
    }

    /**
     * @brief  매우 큰 duration_ms 수신 시 정상 동작하는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ReceiveMessage_VeryLargeDuration_SignalActivatesNormally()
    {
        // Arrange
        SetupController();
        yield return null;

        string payload = "{\"action\":\"drive_direct\",\"axis\":\"x\",\"angle\":[0,180],\"duration_ms\":1000000}";

        // Act
        SimulateMqttMessage(TargetTopic, payload);
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsTrue((bool)GetField("_isSignal"), "큰 duration 이어도 신호가 활성화되어야 합니다.");
        Assert.AreEqual(1000f, (float)GetField("_duration"), 1f, "duration 이 1000초여야 합니다.");
    }

    /**
     * @brief  angle 이 동일한 값일 때 isForward=false 인지 검증한다.
     */
    [UnityTest]
    public IEnumerator ReceiveMessage_SameAngleValues_IsForwardFalse()
    {
        // Arrange
        SetupController();
        yield return null;

        string payload = "{\"action\":\"drive_direct\",\"axis\":\"x\",\"angle\":[90,90],\"duration_ms\":1100}";

        // Act
        SimulateMqttMessage(TargetTopic, payload);
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsFalse((bool)GetField("_isForward"), "angle 이 동일하면 isForward=false 여야 합니다.");
    }

    #endregion

    // ============================================================
    // 에러 처리 테스트
    // ============================================================

    #region 에러 처리 테스트

    /**
     * @brief  MqttSubscriber 미할당 시 ValidateComponents 에러가 발생하고 비활성화되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ValidateComponents_MissingMqttSubscriber_LogsError()
    {
        // Arrange
        _managerObject.SetActive(false);
        _controller = _managerObject.AddComponent<TestWheelController>();

        typeof(WheelControllerBase)
            .GetField("_shuttle", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_controller, _shuttleObject.transform);

        LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex(".*MqttSubscriber.*"));
        LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex(".*바퀴 조인트.*"));

        // Act
        _managerObject.SetActive(true);
        yield return null;

        // Assert
        Assert.IsFalse(_controller.enabled, "MqttSubscriber 미할당 시 컴포넌트가 비활성화되어야 합니다.");
    }

    /**
     * @brief  shuttle 미할당 시 ValidateComponents 에러가 발생하고 비활성화되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ValidateComponents_MissingShuttle_LogsError()
    {
        // Arrange
        _managerObject.SetActive(false);
        _controller = _managerObject.AddComponent<TestWheelController>();

        typeof(WheelControllerBase)
            .GetField("_mqttSubscriber", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_controller, _mqttSubscriber);

        LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex(".*바퀴 조인트.*"));
        LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex(".*_shuttle.*"));

        // Act
        _managerObject.SetActive(true);
        yield return null;

        // Assert
        Assert.IsFalse(_controller.enabled, "shuttle 미할당 시 컴포넌트가 비활성화되어야 합니다.");
    }

    #endregion
}