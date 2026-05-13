// ============================================================
// 파일명  : Slider2LockControllerPlayModeTest.cs
// 역할    : Slider2LockController 의 PlayMode 통합 테스트.
//           잠금/해제 상태 전환, 이벤트 구독/해제, OnDestroy 정리,
//           악조건 처리, 에러 처리를 검증한다.
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
public class Slider2LockControllerPlayModeTest
{
    private const float WaitTime = 0.1f;

    private GameObject _managerObject;
    private GameObject _objectA;
    private GameObject _shuttleObject;
    private GameObject _sliderObject;
    private Slider2LockController _controller;
    private SliderFloorDetector _sliderFloorDetector2;
    private ShuttleController _shuttleController;
    private SliderJointComponent _vsliderJoint;

    [SetUp]
    public void SetUp()
    {
        _objectA = new GameObject("ObjectA");
        _shuttleObject = new GameObject("Shuttle");
        _shuttleObject.transform.position = new Vector3(0f, 5f, 0f);

        _sliderObject = new GameObject("VSlider2");
        _sliderObject.transform.SetParent(_shuttleObject.transform);
        _sliderObject.transform.position = new Vector3(0f, 3f, 0f);

        _managerObject = new GameObject("Slider2LockManager");
    }

    [TearDown]
    public void TearDown()
    {
        if (_managerObject != null) Object.Destroy(_managerObject);
        if (_shuttleObject != null) Object.Destroy(_shuttleObject);
        if (_objectA != null) Object.Destroy(_objectA);
        if (_sliderFloorDetector2 != null) Object.Destroy(_sliderFloorDetector2.gameObject);
        if (_shuttleController != null) Object.Destroy(_shuttleController.gameObject);
        if (_vsliderJoint != null) Object.Destroy(_vsliderJoint.gameObject);
    }

    // ============================================================
    // 헬퍼
    // ============================================================

    private SliderJointComponent CreateSliderJoint()
    {
        var jointObj = new GameObject("SliderJointManager2");
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

    private SpurGearController CreateSpurGearController()
    {
        var spurObj = new GameObject("SpurGearManager2");
        spurObj.SetActive(false);

        var gearObj = new GameObject("GearObj2");
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
        typeof(SpurGearController).GetField("_shuttle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, _objectA.transform);

        spurObj.SetActive(true);
        return controller;
    }

    private SliderFloorDetector CreateSliderFloorDetector(SpurGearController spurGear)
    {
        var detObj = new GameObject("DetectorManager2");
        var xObj = new GameObject("XWheel2");
        var yObj = new GameObject("YWheel2");
        xObj.transform.position = new Vector3(0f, 1f, 0f);
        yObj.transform.position = new Vector3(0f, 1f, 0f);

        RevoluteJointComponent CreateWheelJoint(GameObject obj, float y)
        {
            var jObj = new GameObject($"WheelJoint_{obj.name}");
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

        var xJoint = CreateWheelJoint(xObj, 1f);
        var yJoint = CreateWheelJoint(yObj, 1f);
        var detector = detObj.AddComponent<SliderFloorDetector>();
        var dType = typeof(SliderFloorDetector);

        dType.GetField("_spurGearController", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(detector, spurGear);
        dType.GetField("_xwheel1Joint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(detector, xJoint);
        dType.GetField("_ywheelJoint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(detector, yJoint);
        dType.GetField("_wheelRadius", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(detector, 0.14f);
        dType.GetField("_returnTolerance", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(detector, 0.002f);

        return detector;
    }

    private ShuttleController CreateShuttleController(
        SliderFloorDetector detector,
        SliderJointComponent sliderJoint)
    {
        var shuttleManagerObj = new GameObject("ShuttleManager2");
        shuttleManagerObj.SetActive(false);

        var spurGear = CreateSpurGearController();

        // RackAndPinion 최소 구성
        var rackObj = new GameObject("RackAndPinionManager2");
        rackObj.SetActive(false);

        var gearObj = new GameObject("RackGearObj");
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

        var rackAndPinion = rackObj.AddComponent<RackAndPinionController>();
        var rackType = typeof(RackAndPinionController);
        rackType.GetField("_revoluteJoint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(rackAndPinion, revJoint);
        rackType.GetField("_sliderJoint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(rackAndPinion, sliderJoint);
        rackType.GetField("_motionDirection", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(rackAndPinion, -1f);

        LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(".*gearRadius.*"));
        rackObj.SetActive(true);

        var shuttleCtrl = shuttleManagerObj.AddComponent<ShuttleController>();
        var sType = typeof(ShuttleController);
        sType.GetField("_shuttle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(shuttleCtrl, _shuttleObject.transform);
        sType.GetField("_vsliderJoint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(shuttleCtrl, sliderJoint);
        sType.GetField("_rackAndPinion", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(shuttleCtrl, rackAndPinion);
        sType.GetField("_sliderFloorDetector", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(shuttleCtrl, detector);
        sType.GetField("_spurGearController", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(shuttleCtrl, spurGear);

        shuttleManagerObj.SetActive(true);
        return shuttleCtrl;
    }

    private void SetupController()
    {
        _managerObject.SetActive(false);

        var spurGear = CreateSpurGearController();
        _vsliderJoint = CreateSliderJoint();
        _sliderFloorDetector2 = CreateSliderFloorDetector(spurGear);
        _shuttleController = CreateShuttleController(_sliderFloorDetector2, _vsliderJoint);

        _controller = _managerObject.AddComponent<Slider2LockController>();
        var type = typeof(Slider2LockController);
        type.GetField("_vsliderJoint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _vsliderJoint);
        type.GetField("_sliderFloorDetector2", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _sliderFloorDetector2);
        type.GetField("_shuttleController", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _shuttleController);

        _managerObject.SetActive(true);
    }

    private T GetField<T>(string name)
    {
        return (T)typeof(Slider2LockController)
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(_controller);
    }

    // ============================================================
    // 기본 동작 테스트
    // ============================================================

    #region 기본 동작 테스트

    [UnityTest]
    public IEnumerator Initialization_AfterStart_IsLockedFalse()
    {
        SetupController();
        yield return null;
        yield return new WaitForSeconds(WaitTime);

        Assert.IsFalse(GetField<bool>("_isLocked"), "초기 _isLocked 가 false 여야 합니다.");
    }

    [UnityTest]
    public IEnumerator OnFloorReached_SetsIsLockedTrue()
    {
        SetupController();
        yield return null;

        _sliderFloorDetector2.OnFloorReached?.Invoke();
        yield return new WaitForSeconds(WaitTime);

        Assert.IsTrue(GetField<bool>("_isLocked"),
            "OnFloorReached 수신 시 _isLocked 가 true 여야 합니다.");
    }

    [UnityTest]
    public IEnumerator OnShuttleMovingEnd_SetsIsLockedFalse()
    {
        SetupController();
        yield return null;

        _sliderFloorDetector2.OnFloorReached?.Invoke();
        yield return null;

        _shuttleController.OnShuttleMovingEnd?.Invoke();
        yield return new WaitForSeconds(WaitTime);

        Assert.IsFalse(GetField<bool>("_isLocked"),
            "OnShuttleMovingEnd 수신 시 _isLocked 가 false 여야 합니다.");
    }

    [UnityTest]
    public IEnumerator LockUnlockCycle_WorksCorrectly()
    {
        SetupController();
        yield return null;

        _sliderFloorDetector2.OnFloorReached?.Invoke();
        yield return null;
        Assert.IsTrue(GetField<bool>("_isLocked"), "1차 잠금 후 _isLocked=true 여야 합니다.");

        _shuttleController.OnShuttleMovingEnd?.Invoke();
        yield return null;
        Assert.IsFalse(GetField<bool>("_isLocked"), "1차 해제 후 _isLocked=false 여야 합니다.");

        _sliderFloorDetector2.OnFloorReached?.Invoke();
        yield return null;
        Assert.IsTrue(GetField<bool>("_isLocked"), "2차 잠금 후 _isLocked=true 여야 합니다.");

        _shuttleController.OnShuttleMovingEnd?.Invoke();
        yield return new WaitForSeconds(WaitTime);
        Assert.IsFalse(GetField<bool>("_isLocked"), "2차 해제 후 _isLocked=false 여야 합니다.");
    }

    #endregion

    // ============================================================
    // 이벤트 구독 해제 테스트
    // ============================================================

    #region 이벤트 구독 해제 테스트

    [UnityTest]
    public IEnumerator OnDestroy_EventsUnsubscribed_NoExceptionOnEventFire()
    {
        SetupController();
        yield return null;

        Object.Destroy(_managerObject);
        yield return null;

        Assert.DoesNotThrow(() =>
        {
            _sliderFloorDetector2.OnFloorReached?.Invoke();
            _shuttleController.OnShuttleMovingEnd?.Invoke();
        }, "OnDestroy 후 이벤트 발행 시 예외가 발생하지 않아야 합니다.");

        _managerObject = null;
    }

    #endregion

    // ============================================================
    // 악조건 테스트
    // ============================================================

    #region 악조건 테스트

    [UnityTest]
    public IEnumerator OnFloorReached_CalledTwice_IsLockedRemainsTrue()
    {
        SetupController();
        yield return null;

        _sliderFloorDetector2.OnFloorReached?.Invoke();
        _sliderFloorDetector2.OnFloorReached?.Invoke();
        yield return new WaitForSeconds(WaitTime);

        Assert.IsTrue(GetField<bool>("_isLocked"), "_isLocked 가 true 로 유지되어야 합니다.");
    }

    [UnityTest]
    public IEnumerator OnShuttleMovingEnd_WithoutLock_IsLockedRemainsFalse()
    {
        SetupController();
        yield return null;

        _shuttleController.OnShuttleMovingEnd?.Invoke();
        yield return new WaitForSeconds(WaitTime);

        Assert.IsFalse(GetField<bool>("_isLocked"),
            "잠금 없이 해제 이벤트 수신 시 _isLocked=false 가 유지되어야 합니다.");
    }

    [UnityTest]
    public IEnumerator OnShuttleMovingEnd_CalledTwice_IsLockedRemainsFalse()
    {
        SetupController();
        yield return null;

        _sliderFloorDetector2.OnFloorReached?.Invoke();
        yield return null;

        _shuttleController.OnShuttleMovingEnd?.Invoke();
        _shuttleController.OnShuttleMovingEnd?.Invoke();
        yield return new WaitForSeconds(WaitTime);

        Assert.IsFalse(GetField<bool>("_isLocked"),
            "OnShuttleMovingEnd 두 번 수신 시 _isLocked=false 가 유지되어야 합니다.");
    }

    [UnityTest]
    public IEnumerator MultipleCycles_IsLockedAlwaysCorrect()
    {
        SetupController();
        yield return null;

        for (int i = 0; i < 5; i++)
        {
            _sliderFloorDetector2.OnFloorReached?.Invoke();
            yield return null;
            Assert.IsTrue(GetField<bool>("_isLocked"), $"사이클 {i + 1} 잠금 후 _isLocked=true 여야 합니다.");

            _shuttleController.OnShuttleMovingEnd?.Invoke();
            yield return null;
            Assert.IsFalse(GetField<bool>("_isLocked"), $"사이클 {i + 1} 해제 후 _isLocked=false 여야 합니다.");
        }
    }

    #endregion

    // ============================================================
    // 에러 처리 테스트
    // ============================================================

    #region 에러 처리 테스트

    [UnityTest]
    public IEnumerator ValidateComponents_MissingVSliderJoint_LogsError()
    {
        _managerObject.SetActive(false);
        _controller = _managerObject.AddComponent<Slider2LockController>();
        var spurGear = CreateSpurGearController();
        _sliderFloorDetector2 = CreateSliderFloorDetector(spurGear);
        _vsliderJoint = CreateSliderJoint();

        // CreateShuttleController 내부 에러 전체 무시
        LogAssert.ignoreFailingMessages = true;
        _shuttleController = CreateShuttleController(_sliderFloorDetector2, _vsliderJoint);
        LogAssert.ignoreFailingMessages = false;

        typeof(Slider2LockController).GetField("_sliderFloorDetector2", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _sliderFloorDetector2);
        typeof(Slider2LockController).GetField("_shuttleController", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _shuttleController);
        // _vsliderJoint 미할당

        // SetActive 직전에 Expect 등록
        LogAssert.ignoreFailingMessages = true;
        _managerObject.SetActive(true);
        yield return null;
        LogAssert.ignoreFailingMessages = false;

        Assert.IsFalse(_controller.enabled, "VsliderJoint 미할당 시 컴포넌트가 비활성화되어야 합니다.");
        Assert.IsTrue(
            typeof(Slider2LockController)
                .GetField("_isLocked", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(_controller) is bool isLocked && !isLocked
            || !_controller.enabled,
            "VsliderJoint 미할당 시 컴포넌트가 비활성화되어야 합니다.");
    }

    [UnityTest]
    public IEnumerator ValidateComponents_MissingSliderFloorDetector2_LogsError()
    {
        _managerObject.SetActive(false);
        _controller = _managerObject.AddComponent<Slider2LockController>();
        _vsliderJoint = CreateSliderJoint();
        var spurGear = CreateSpurGearController();
        var detector = CreateSliderFloorDetector(spurGear);

        LogAssert.ignoreFailingMessages = true;
        _shuttleController = CreateShuttleController(detector, _vsliderJoint);
        LogAssert.ignoreFailingMessages = false;

        typeof(Slider2LockController).GetField("_vsliderJoint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _vsliderJoint);
        typeof(Slider2LockController).GetField("_shuttleController", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _shuttleController);
        // _sliderFloorDetector2 미할당

        LogAssert.ignoreFailingMessages = true;
        _managerObject.SetActive(true);
        yield return null;
        LogAssert.ignoreFailingMessages = false;

        Assert.IsFalse(_controller.enabled, "SliderFloorDetector2 미할당 시 컴포넌트가 비활성화되어야 합니다.");
    }

    [UnityTest]
    public IEnumerator ValidateComponents_MissingShuttleController_LogsError()
    {
        _managerObject.SetActive(false);
        _controller = _managerObject.AddComponent<Slider2LockController>();
        _vsliderJoint = CreateSliderJoint();
        var spurGear = CreateSpurGearController();
        _sliderFloorDetector2 = CreateSliderFloorDetector(spurGear);

        typeof(Slider2LockController).GetField("_vsliderJoint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _vsliderJoint);
        typeof(Slider2LockController).GetField("_sliderFloorDetector2", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _sliderFloorDetector2);

        LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex(".*ShuttleController.*"));

        _managerObject.SetActive(true);
        yield return null;

        Assert.IsFalse(_controller.enabled, "ShuttleController 미할당 시 컴포넌트가 비활성화되어야 합니다.");
    }

    #endregion
}