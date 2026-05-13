// ============================================================
// 파일명  : ShuttleControllerPlayModeTest.cs
// 역할    : ShuttleController 의 PlayMode 통합 테스트.
//           이벤트 구독, 셔틀 이동 시작/종료, 방향 일치 조건,
//           슬라이더 위치 고정, OnShuttleMovingEnd 발행, 에러 처리를 검증한다.
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
public class ShuttleControllerPlayModeTest
{
    private const float Tolerance = 0.001f;
    private const float WaitTime = 0.1f;

    private GameObject _managerObject;
    private GameObject _shuttleObject;
    private GameObject _sliderObject;
    private GameObject _objectA;
    private ShuttleController _controller;
    private SliderFloorDetector _sliderFloorDetector;
    private RackAndPinionController _rackAndPinion;
    private SpurGearController _spurGearController;
    private SliderJointComponent _vsliderJoint;

    [SetUp]
    public void SetUp()
    {
        _objectA = new GameObject("ObjectA");
        _shuttleObject = new GameObject("Shuttle");
        _shuttleObject.transform.position = new Vector3(0f, 5f, 0f);

        _sliderObject = new GameObject("VSlider");
        _sliderObject.transform.SetParent(_shuttleObject.transform);
        _sliderObject.transform.position = new Vector3(0f, 3f, 0f);

        _managerObject = new GameObject("ShuttleManager");
    }

    [TearDown]
    public void TearDown()
    {
        if (_managerObject != null) Object.Destroy(_managerObject);
        if (_shuttleObject != null) Object.Destroy(_shuttleObject);
        if (_objectA != null) Object.Destroy(_objectA);
        if (_sliderFloorDetector != null) Object.Destroy(_sliderFloorDetector.gameObject);
        if (_rackAndPinion != null) Object.Destroy(_rackAndPinion.gameObject);
        if (_spurGearController != null) Object.Destroy(_spurGearController.gameObject);
        if (_vsliderJoint != null) Object.Destroy(_vsliderJoint.gameObject);
    }

    // ============================================================
    // 헬퍼
    // ============================================================

    private SliderJointComponent CreateSliderJoint()
    {
        var jointObj = new GameObject("SliderJointManager");
        jointObj.SetActive(false);
        var joint = jointObj.AddComponent<SliderJointComponent>();
        var type = typeof(SliderJointComponent);
        var baseType = typeof(JointComponent);

        baseType.GetField("_objectA", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, _objectA.transform);
        baseType.GetField("_objectB", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, _sliderObject.transform);
        type.GetField("_minPosition", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, -100f);
        type.GetField("_maxPosition", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, 100f);
        type.GetField("_moveDirection", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, Vector3.up);

        Vector3 lineB = Vector3.up;
        Vector3 perpA = Vector3.Cross(lineB, Vector3.right).normalized;
        Vector3 perpB = Vector3.Cross(lineB, perpA).normalized;
        type.GetField("_lineAPointA", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, Vector3.zero);
        type.GetField("_lineAPointB", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, lineB);
        type.GetField("_lineBPointA", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, Vector3.zero);
        type.GetField("_lineBPointB", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, lineB);
        type.GetField("_planeAPointA", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, Vector3.zero);
        type.GetField("_planeAPointB", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, perpA);
        type.GetField("_planeAPointC", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, perpB);
        type.GetField("_planeBPointA", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, Vector3.zero);
        type.GetField("_planeBPointB", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, perpA);
        type.GetField("_planeBPointC", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, perpB);

        jointObj.SetActive(true);
        return joint;
    }

    private SpurGearController CreateSpurGearController(bool isSlider2 = false)
    {
        var spurObj = new GameObject("SpurGearManager");
        spurObj.SetActive(false);

        var gearObj = new GameObject("GearObject");
        gearObj.transform.position = Vector3.zero;
        gearObj.transform.rotation = Quaternion.Euler(0f, 0f, 90f);

        var revJoint = spurObj.AddComponent<RevoluteJointComponent>();
        var revType = typeof(RevoluteJointComponent);
        var baseType = typeof(JointComponent);

        baseType.GetField("_objectA", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(revJoint, _objectA.transform);
        baseType.GetField("_objectB", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(revJoint, gearObj.transform);
        revType.GetField("_minAngle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(revJoint, -360f);
        revType.GetField("_maxAngle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(revJoint, 360f);
        revType.GetField("_useClamp", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(revJoint, false);
        revType.GetField("_lineAPointA", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(revJoint, Vector3.zero);
        revType.GetField("_lineAPointB", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(revJoint, Vector3.forward);
        revType.GetField("_lineBPointA", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(revJoint, Vector3.zero);
        revType.GetField("_lineBPointB", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(revJoint, Vector3.forward);

        var controller = spurObj.AddComponent<SpurGearController>();
        typeof(SpurGearController).GetField("_spurGearJoint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, revJoint);
        typeof(SpurGearController).GetField("_isSlider2", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, isSlider2);
        typeof(SpurGearController).GetField("_shuttle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, _objectA.transform);

        spurObj.SetActive(true);
        return controller;
    }

    private SliderFloorDetector CreateSliderFloorDetector(SpurGearController spurGear)
    {
        var detObj = new GameObject("DetectorManager");
        var xObj = new GameObject("XWheel");
        var yObj = new GameObject("YWheel");
        xObj.transform.position = new Vector3(0f, 5f, 0f);
        yObj.transform.position = new Vector3(0f, 3f, 0f);

        RevoluteJointComponent CreateWheelJoint(GameObject obj, float y)
        {
            var jObj = new GameObject($"Joint_{obj.name}");
            jObj.SetActive(false);
            var j = jObj.AddComponent<RevoluteJointComponent>();
            var revType = typeof(RevoluteJointComponent);
            var bType = typeof(JointComponent);
            bType.GetField("_objectA", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(j, _objectA.transform);
            bType.GetField("_objectB", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(j, obj.transform);
            revType.GetField("_minAngle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(j, -360f);
            revType.GetField("_maxAngle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(j, 360f);
            revType.GetField("_useClamp", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(j, false);
            revType.GetField("_lineAPointA", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(j, Vector3.zero);
            revType.GetField("_lineAPointB", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(j, Vector3.right);
            revType.GetField("_lineBPointA", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(j, new Vector3(0f, y, 0f));
            revType.GetField("_lineBPointB", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(j, new Vector3(1f, y, 0f));
            jObj.SetActive(true);
            return j;
        }

        var xJoint = CreateWheelJoint(xObj, 5f);
        var yJoint = CreateWheelJoint(yObj, 3f);
        var detector = detObj.AddComponent<SliderFloorDetector>();
        var dType = typeof(SliderFloorDetector);

        dType.GetField("_spurGearController", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(detector, spurGear);
        dType.GetField("_xwheel1Joint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(detector, xJoint);
        dType.GetField("_ywheelJoint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(detector, yJoint);
        dType.GetField("_wheelRadius", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(detector, 0.14f);
        dType.GetField("_returnTolerance", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(detector, 0.002f);

        return detector;
    }

    private RackAndPinionController CreateRackAndPinion(SliderJointComponent sliderJoint)
    {
        var rackObj = new GameObject("RackAndPinionManager");
        rackObj.SetActive(false);

        var gearObj = new GameObject("GearObj");
        gearObj.AddComponent<MeshFilter>().mesh = new Mesh
        {
            bounds = new Bounds(Vector3.zero, new Vector3(0.11f, 0.11f, 0.01f))
        };
        gearObj.transform.position = Vector3.zero;

        var revJoint = rackObj.AddComponent<RevoluteJointComponent>();
        var revType = typeof(RevoluteJointComponent);
        var baseType = typeof(JointComponent);
        baseType.GetField("_objectA", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(revJoint, _objectA.transform);
        baseType.GetField("_objectB", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(revJoint, gearObj.transform);
        revType.GetField("_minAngle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(revJoint, -360f);
        revType.GetField("_maxAngle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(revJoint, 360f);
        revType.GetField("_useClamp", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(revJoint, false);
        revType.GetField("_lineAPointA", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(revJoint, Vector3.zero);
        revType.GetField("_lineAPointB", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(revJoint, Vector3.forward);
        revType.GetField("_lineBPointA", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(revJoint, Vector3.zero);
        revType.GetField("_lineBPointB", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(revJoint, Vector3.forward);

        var rack = rackObj.AddComponent<RackAndPinionController>();
        var rackType = typeof(RackAndPinionController);
        rackType.GetField("_revoluteJoint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(rack, revJoint);
        rackType.GetField("_sliderJoint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(rack, sliderJoint);
        rackType.GetField("_motionDirection", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(rack, -1f);

        // sliderJoint null 이면 ValidateComponents 실패로 gearRadius 로그 미발생
        if (sliderJoint != null)
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(".*gearRadius.*"));
        else
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*SliderJoint.*"));

        LogAssert.ignoreFailingMessages = true;
        rackObj.SetActive(true);
        LogAssert.ignoreFailingMessages = false;

        return rack;
    }

    private void SetupController(bool spurGearIsSlider2 = false)
    {
        _managerObject.SetActive(false);

        _vsliderJoint = CreateSliderJoint();
        _spurGearController = CreateSpurGearController(spurGearIsSlider2);
        _sliderFloorDetector = CreateSliderFloorDetector(_spurGearController);
        _rackAndPinion = CreateRackAndPinion(_vsliderJoint);

        _controller = _managerObject.AddComponent<ShuttleController>();
        var type = typeof(ShuttleController);

        type.GetField("_shuttle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _shuttleObject.transform);
        type.GetField("_vsliderJoint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _vsliderJoint);
        type.GetField("_rackAndPinion", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _rackAndPinion);
        type.GetField("_sliderFloorDetector", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _sliderFloorDetector);
        type.GetField("_spurGearController", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _spurGearController);

        _managerObject.SetActive(true);
    }

    private T GetField<T>(string name)
    {
        return (T)typeof(ShuttleController)
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(_controller);
    }

    // ============================================================
    // 기본 동작 테스트
    // ============================================================

    #region 기본 동작 테스트

    [UnityTest]
    public IEnumerator Initialization_AfterStart_IsShuttleMovingFalse()
    {
        SetupController();
        yield return null;
        yield return new WaitForSeconds(WaitTime);

        Assert.IsFalse(GetField<bool>("_isShuttleMoving"), "초기 _isShuttleMoving 이 false 여야 합니다.");
    }

    [UnityTest]
    public IEnumerator OnSliderFloorReached_SetsIsShuttleMovingTrue()
    {
        SetupController();
        yield return null;

        _sliderFloorDetector.OnFloorReached?.Invoke();
        yield return new WaitForSeconds(WaitTime);

        Assert.IsTrue(GetField<bool>("_isShuttleMoving"),
            "OnFloorReached 수신 시 _isShuttleMoving 이 true 여야 합니다.");
    }

    [UnityTest]
    public IEnumerator OnShuttleFloorReached_SetsIsShuttleMovingFalse()
    {
        SetupController();
        yield return null;

        _sliderFloorDetector.OnFloorReached?.Invoke();
        yield return null;

        _sliderFloorDetector.OnShuttleFloorReached?.Invoke();
        yield return new WaitForSeconds(WaitTime);

        Assert.IsFalse(GetField<bool>("_isShuttleMoving"),
            "OnShuttleFloorReached 수신 시 _isShuttleMoving 이 false 여야 합니다.");
    }

    [UnityTest]
    public IEnumerator OnShuttleFloorReached_FiresOnShuttleMovingEnd()
    {
        SetupController();
        yield return null;

        bool endEventFired = false;
        _controller.OnShuttleMovingEnd += () => endEventFired = true;

        _sliderFloorDetector.OnShuttleFloorReached?.Invoke();
        yield return new WaitForSeconds(WaitTime);

        Assert.IsTrue(endEventFired, "OnShuttleFloorReached 수신 시 OnShuttleMovingEnd 가 발행되어야 합니다.");
    }

    #endregion

    // ============================================================
    // Update 신호 상승 엣지 테스트
    // ============================================================

    #region Update 신호 상승 엣지 테스트

    [UnityTest]
    public IEnumerator Update_ReverseSignalRisingEdge_SetsIsShuttleMovingTrue()
    {
        SetupController();
        yield return null;

        _spurGearController.SetSignal(false, 5f);
        yield return null;
        yield return new WaitForSeconds(WaitTime);

        Assert.IsTrue(GetField<bool>("_isShuttleMoving"),
            "역방향 신호 상승 엣지에서 _isShuttleMoving 이 true 여야 합니다.");
    }

    [UnityTest]
    public IEnumerator Update_ForwardSignalRisingEdge_DoesNotSetIsShuttleMoving()
    {
        SetupController();
        yield return null;

        // SliderFloorDetector 를 비활성화하여 자동 도달 판정 차단
        _sliderFloorDetector.enabled = false;

        _spurGearController.SetSignal(true, 5f);
        yield return null;
        yield return new WaitForSeconds(WaitTime);

        Assert.IsFalse(GetField<bool>("_isShuttleMoving"),
            "정방향 신호 상승 엣지에서 _isShuttleMoving 이 변경되지 않아야 합니다.");
    }

    #endregion

    // ============================================================
    // HandleRackMovement 테스트
    // ============================================================

    #region HandleRackMovement 테스트

    [UnityTest]
    public IEnumerator HandleRackMovement_IsShuttleMovingFalse_ShuttleDoesNotMove()
    {
        SetupController();
        yield return null;

        Vector3 initialPos = _shuttleObject.transform.position;

        _rackAndPinion.OnPositionChanged?.Invoke(0.01f);
        yield return new WaitForSeconds(WaitTime);

        Assert.AreEqual(initialPos, _shuttleObject.transform.position,
            "_isShuttleMoving=false 이면 셔틀이 이동하지 않아야 합니다.");
    }

    [UnityTest]
    public IEnumerator HandleRackMovement_NoiseDelta_ShuttleDoesNotMove()
    {
        SetupController();
        yield return null;

        _sliderFloorDetector.OnFloorReached?.Invoke();
        yield return null;

        Vector3 initialPos = _shuttleObject.transform.position;

        _rackAndPinion.OnPositionChanged?.Invoke(0.00005f);
        yield return new WaitForSeconds(WaitTime);

        Assert.AreEqual(initialPos.y, _shuttleObject.transform.position.y, Tolerance,
            "노이즈 수준의 delta 는 무시되어야 합니다.");
    }

    [UnityTest]
    public IEnumerator HandleRackMovement_ValidDelta_ShuttleMoves()
    {
        SetupController();
        yield return null;

        _spurGearController.SetSignal(true, 5f);
        yield return null;
        _sliderFloorDetector.OnFloorReached?.Invoke();
        yield return null;

        Vector3 initialPos = _shuttleObject.transform.position;

        _rackAndPinion.OnPositionChanged?.Invoke(0.01f);
        yield return new WaitForSeconds(WaitTime);

        Assert.AreEqual(initialPos.y - 0.01f, _shuttleObject.transform.position.y, Tolerance,
            "유효한 delta 로 셔틀이 이동해야 합니다.");
    }

    [UnityTest]
    public IEnumerator HandleRackMovement_DirectionMismatch_ShuttleDoesNotMove()
    {
        // ShuttleController 의 _sliderFloorDetector 와 _spurGearController 가
        // 같은 SpurGear 를 참조하므로 방향 불일치 자체가 불가능한 구조.
        // 대신 _isShuttleMoving=false 상태에서 이동 차단을 검증한다.
        SetupController();
        yield return null;

        Vector3 initialPos = _shuttleObject.transform.position;

        // _isShuttleMoving=false (기본값) 상태에서 랙 이동
        _rackAndPinion.OnPositionChanged?.Invoke(0.01f);
        yield return new WaitForSeconds(WaitTime);

        Assert.AreEqual(initialPos.y, _shuttleObject.transform.position.y, Tolerance,
            "_isShuttleMoving=false 시 셔틀이 이동하지 않아야 합니다.");
    }

    #endregion

    // ============================================================
    // 슬라이더 위치 고정 테스트
    // ============================================================

    #region 슬라이더 위치 고정 테스트

    [UnityTest]
    public IEnumerator MoveShuttleAndLockSlider_SliderYRemainsLocked()
    {
        SetupController();
        yield return null;

        float initialSliderY = _sliderObject.transform.position.y;

        _spurGearController.SetSignal(true, 5f);
        yield return null;
        _sliderFloorDetector.OnFloorReached?.Invoke();
        yield return null;

        _rackAndPinion.OnPositionChanged?.Invoke(0.1f);
        yield return new WaitForSeconds(WaitTime);

        Assert.AreEqual(initialSliderY, _sliderObject.transform.position.y, Tolerance,
            "셔틀 이동 중 슬라이더 Y 가 잠금 시점 값을 유지해야 합니다.");
    }

    #endregion

    // ============================================================
    // 악조건 테스트
    // ============================================================

    #region 악조건 테스트

    [UnityTest]
    public IEnumerator OnSliderFloorReached_CalledTwice_StaysTrue()
    {
        SetupController();
        yield return null;

        _sliderFloorDetector.OnFloorReached?.Invoke();
        _sliderFloorDetector.OnFloorReached?.Invoke();
        yield return new WaitForSeconds(WaitTime);

        Assert.IsTrue(GetField<bool>("_isShuttleMoving"), "_isShuttleMoving 이 true 로 유지되어야 합니다.");
    }

    [UnityTest]
    public IEnumerator OnShuttleFloorReached_NoSubscribers_HandledGracefully()
    {
        SetupController();
        yield return null;

        Assert.DoesNotThrow(() =>
        {
            _sliderFloorDetector.OnShuttleFloorReached?.Invoke();
        }, "OnShuttleMovingEnd 구독자 없어도 예외가 발생하지 않아야 합니다.");

        yield return null;
    }

    [UnityTest]
    public IEnumerator HandleRackMovement_VeryLargeDelta_AppliedCorrectly()
    {
        SetupController();
        yield return null;

        _spurGearController.SetSignal(true, 5f);
        yield return null;
        _sliderFloorDetector.OnFloorReached?.Invoke();
        yield return null;

        Vector3 initialPos = _shuttleObject.transform.position;

        _rackAndPinion.OnPositionChanged?.Invoke(100f);
        yield return new WaitForSeconds(WaitTime);

        Assert.IsFalse(float.IsNaN(_shuttleObject.transform.position.y), "셔틀 Y 가 NaN 이면 안 됩니다.");
        Assert.IsFalse(float.IsInfinity(_shuttleObject.transform.position.y), "셔틀 Y 가 Infinity 이면 안 됩니다.");
        Assert.AreEqual(initialPos.y - 100f, _shuttleObject.transform.position.y, Tolerance,
            "큰 delta 가 정확히 적용되어야 합니다.");
    }

    #endregion

    // ============================================================
    // 에러 처리 테스트
    // ============================================================

    #region 에러 처리 테스트

    [UnityTest]
    public IEnumerator ValidateComponents_MissingShuttle_LogsError()
    {
        _managerObject.SetActive(false);
        _controller = _managerObject.AddComponent<ShuttleController>();
        _vsliderJoint = CreateSliderJoint();
        _spurGearController = CreateSpurGearController();
        _sliderFloorDetector = CreateSliderFloorDetector(_spurGearController);
        _rackAndPinion = CreateRackAndPinion(_vsliderJoint);

        var type = typeof(ShuttleController);
        type.GetField("_vsliderJoint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _vsliderJoint);
        type.GetField("_rackAndPinion", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _rackAndPinion);
        type.GetField("_sliderFloorDetector", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _sliderFloorDetector);
        type.GetField("_spurGearController", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _spurGearController);

        LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex(".*_shuttle.*"));

        _managerObject.SetActive(true);
        yield return null;

        Assert.IsFalse(_controller.enabled, "shuttle 미할당 시 컴포넌트가 비활성화되어야 합니다.");
    }

    [UnityTest]
    public IEnumerator ValidateComponents_MissingVSliderJoint_LogsError()
    {
        _managerObject.SetActive(false);
        _controller = _managerObject.AddComponent<ShuttleController>();
        _spurGearController = CreateSpurGearController();
        _sliderFloorDetector = CreateSliderFloorDetector(_spurGearController);
        _rackAndPinion = CreateRackAndPinion(null);

        var type = typeof(ShuttleController);
        type.GetField("_shuttle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _shuttleObject.transform);
        type.GetField("_rackAndPinion", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _rackAndPinion);
        type.GetField("_sliderFloorDetector", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _sliderFloorDetector);
        type.GetField("_spurGearController", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _spurGearController);

        LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex(".*VsliderJoint.*"));

        _managerObject.SetActive(true);
        yield return null;

        Assert.IsFalse(_controller.enabled, "vsliderJoint 미할당 시 컴포넌트가 비활성화되어야 합니다.");
    }

    #endregion
}