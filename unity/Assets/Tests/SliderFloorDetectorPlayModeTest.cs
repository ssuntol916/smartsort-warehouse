// ============================================================
// 파일명  : SliderFloorDetectorPlayModeTest.cs
// 역할    : SliderFloorDetector 의 PlayMode 통합 테스트.
//           신호 상승 엣지 감지, 기준 Y 캡처, 하강/상승 종점 이벤트 발행,
//           isSlider2 방향 반전, 중복 이벤트 방지, 에러 처리를 검증한다.
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
public class SliderFloorDetectorPlayModeTest
{
    // ============================================================
    // 상수
    // ============================================================

    private const float Tolerance = 0.01f;
    private const float WaitTime = 0.1f;
    private const float WheelRadius = 0.14f;
    private const float ReturnTol = 0.002f;

    // ============================================================
    // 테스트 픽스처
    // ============================================================

    private GameObject _managerObject;
    private GameObject _objectA;
    private GameObject _xwheelObject;
    private GameObject _ywheelObject;
    private SpurGearController _spurGearController;
    private RevoluteJointComponent _xwheel1Joint;
    private RevoluteJointComponent _ywheelJoint;
    private SliderFloorDetector _detector;

    // ============================================================
    // Setup / Teardown
    // ============================================================

    [SetUp]
    public void SetUp()
    {
        _objectA = new GameObject("ObjectA");
        _xwheelObject = new GameObject("XWheelObject");
        _ywheelObject = new GameObject("YWheelObject");
        _managerObject = new GameObject("DetectorManager");
    }

    [TearDown]
    public void TearDown()
    {
        if (_managerObject != null) Object.Destroy(_managerObject);
        if (_objectA != null) Object.Destroy(_objectA);
        if (_xwheelObject != null) Object.Destroy(_xwheelObject);
        if (_ywheelObject != null) Object.Destroy(_ywheelObject);
        if (_spurGearController != null) Object.Destroy(_spurGearController.gameObject);
        if (_xwheel1Joint != null) Object.Destroy(_xwheel1Joint.gameObject);
        if (_ywheelJoint != null) Object.Destroy(_ywheelJoint.gameObject);
    }

    // ============================================================
    // 헬퍼
    // ============================================================

    /**
     * @brief  RevoluteJointComponent 를 지정 위치에 생성한다.
     *         SetActive(false) 후 필드 설정 → SetActive(true) 패턴 적용.
     */
    private RevoluteJointComponent CreateWheelJoint(GameObject wheelObj, float lineBCenterY)
    {
        wheelObj.transform.position = new Vector3(0f, lineBCenterY, 0f);

        var jointObj = new GameObject($"JointManager_{wheelObj.name}");
        jointObj.SetActive(false);
        var joint = jointObj.AddComponent<RevoluteJointComponent>();

        var revType = typeof(RevoluteJointComponent);
        var baseType = typeof(JointComponent);

        baseType.GetField("_objectA", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, _objectA.transform);
        baseType.GetField("_objectB", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, wheelObj.transform);
        revType.GetField("_minAngle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, -360f);
        revType.GetField("_maxAngle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, 360f);
        revType.GetField("_useClamp", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, false);
        revType.GetField("_lineAPointA", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, Vector3.zero);
        revType.GetField("_lineAPointB", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, Vector3.right);
        revType.GetField("_lineBPointA", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, new Vector3(0f, lineBCenterY, 0f));
        revType.GetField("_lineBPointB", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joint, new Vector3(1f, lineBCenterY, 0f));

        jointObj.SetActive(true);
        return joint;
    }

    /**
     * @brief  SpurGearController 를 생성한다.
     *         SetActive(false) 후 필드 설정 → SetActive(true) 패턴 적용.
     */
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

    /**
     * @brief  SliderFloorDetector 를 설정하고 초기화한다.
     */
    private void SetupDetector(
        float xwheelY = 1f,
        float ywheelY = 1f,
        bool isSlider2 = false)
    {
        _managerObject.SetActive(false);

        _spurGearController = CreateSpurGearController(isSlider2);
        _xwheel1Joint = CreateWheelJoint(_xwheelObject, xwheelY);
        _ywheelJoint = CreateWheelJoint(_ywheelObject, ywheelY);

        _detector = _managerObject.AddComponent<SliderFloorDetector>();
        var type = typeof(SliderFloorDetector);

        type.GetField("_spurGearController", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_detector, _spurGearController);
        type.GetField("_xwheel1Joint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_detector, _xwheel1Joint);
        type.GetField("_ywheelJoint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_detector, _ywheelJoint);
        type.GetField("_wheelRadius", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_detector, WheelRadius);
        type.GetField("_returnTolerance", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_detector, ReturnTol);
        type.GetField("_isSlider2", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_detector, isSlider2);

        _managerObject.SetActive(true);
    }

    private T GetField<T>(string name)
    {
        return (T)typeof(SliderFloorDetector)
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(_detector);
    }

    /**
     * @brief  ywheel 의 LineBCenter Y 를 변경한다.
     *         LineBCenter 는 _localLineBCenter 캐시 기반이므로 Reflection 으로 직접 변경한다.
     *         부모 없는 경우 _localLineBCenter = _lineBPointA 이므로 동일하게 설정한다.
     */
    private void MoveYWheelTo(float newY)
    {
        typeof(RevoluteJointComponent)
            .GetField("_localLineBCenter", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_ywheelJoint, new Vector3(0f, newY, 0f));
    }

    private void MoveXWheelTo(float newY)
    {
        typeof(RevoluteJointComponent)
            .GetField("_localLineBCenter", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_xwheel1Joint, new Vector3(0f, newY, 0f));
    }

    // ============================================================
    // 기본 동작 테스트
    // ============================================================

    #region 기본 동작 테스트

    [UnityTest]
    public IEnumerator Initialization_IsGoingDown_MatchesSpurGearIsForward()
    {
        SetupDetector();
        yield return null;

        _spurGearController.SetSignal(true, 5f);
        yield return new WaitForSeconds(WaitTime);

        Assert.AreEqual(_spurGearController.IsForward, _detector.IsGoingDown,
            "IsGoingDown 이 SpurGear.IsForward 와 일치해야 합니다.");
    }

    [UnityTest]
    public IEnumerator Update_SignalRisingEdge_CapturesTargetFloorY()
    {
        SetupDetector(xwheelY: 1f, ywheelY: 0.5f);
        yield return null;

        _spurGearController.SetSignal(true, 5f);
        yield return null;

        float expected = Mathf.Min(1f - WheelRadius, 0.5f - WheelRadius);
        Assert.AreEqual(expected, GetField<float>("_targetFloorY"), Tolerance,
            "_targetFloorY 가 두 바퀴 Y 최솟값 중 낮은 값이어야 합니다.");
    }

    [UnityTest]
    public IEnumerator Update_NoSignal_EventNotFired()
    {
        SetupDetector(xwheelY: 0f, ywheelY: 0f);
        yield return null;

        bool eventFired = false;
        _detector.OnFloorReached += () => eventFired = true;
        _detector.OnShuttleFloorReached += () => eventFired = true;

        yield return new WaitForSeconds(WaitTime);

        Assert.IsFalse(eventFired, "신호 없을 때 이벤트가 발행되지 않아야 합니다.");
    }

    #endregion

    // ============================================================
    // 하강 종점 감지 테스트
    // ============================================================

    #region 하강 종점 감지 테스트

    [UnityTest]
    public IEnumerator Update_IsGoingDown_YWheelReachesFloor_FiresOnFloorReached()
    {
        SetupDetector(xwheelY: 1f, ywheelY: 1f);
        yield return null;

        bool floorReached = false;
        _detector.OnFloorReached += () => floorReached = true;

        _spurGearController.SetSignal(true, 5f);
        yield return null;

        MoveYWheelTo(0.86f);
        yield return new WaitForSeconds(WaitTime);

        Assert.IsTrue(floorReached, "ywheel 이 targetFloorY 에 도달하면 OnFloorReached 가 발행되어야 합니다.");
    }

    [UnityTest]
    public IEnumerator Update_IsGoingDown_YWheelNotReached_EventNotFired()
    {
        // xwheel=1f, ywheel=1f 로 초기화
        SetupDetector(xwheelY: 1f, ywheelY: 1f);
        yield return null;

        bool floorReached = false;
        _detector.OnFloorReached += () => floorReached = true;

        // 신호 전에 ywheel 을 높게 이동 → targetFloorY 캡처 시 xwheel 기준으로 설정됨
        // xwheel=1f → targetFloorY = Min(0.86, 2.86) = 0.86
        // ywheel YMin = 2.86 > 0.862 → 미도달
        MoveYWheelTo(3f);

        _spurGearController.SetSignal(true, 5f);
        yield return null; // targetFloorY 캡처
        yield return new WaitForSeconds(WaitTime);

        Assert.IsFalse(floorReached, "ywheel 이 아직 도달하지 않으면 이벤트가 발행되지 않아야 합니다.");
    }

    [UnityTest]
    public IEnumerator Update_FloorReached_NoDuplicateEvents()
    {
        SetupDetector(xwheelY: 1f, ywheelY: 1f);
        yield return null;

        int eventCount = 0;
        _detector.OnFloorReached += () => eventCount++;

        _spurGearController.SetSignal(true, 5f);
        yield return null;

        MoveYWheelTo(0.5f);
        yield return new WaitForSeconds(0.3f);

        Assert.AreEqual(1, eventCount, "도달 이벤트는 1번만 발행되어야 합니다.");
    }

    #endregion

    // ============================================================
    // 상승 종점 감지 테스트
    // ============================================================

    #region 상승 종점 감지 테스트

    [UnityTest]
    public IEnumerator Update_NotGoingDown_XWheelReachesFloor_FiresOnShuttleFloorReached()
    {
        SetupDetector(xwheelY: 1f, ywheelY: 0.5f);
        yield return null;

        bool shuttleFloorReached = false;
        _detector.OnShuttleFloorReached += () => shuttleFloorReached = true;

        _spurGearController.SetSignal(false, 5f);
        yield return null; // 엣지 감지 프레임

        // targetFloorY = Min(1-0.14, 0.5-0.14) = 0.36
        // XMin = 0.36 - 0.14 = 0.22 ≤ 0.36 → 도달
        MoveXWheelTo(0.36f);
        yield return new WaitForSeconds(WaitTime);

        Assert.IsTrue(shuttleFloorReached,
            "xwheel 이 targetFloorY 에 도달하면 OnShuttleFloorReached 가 발행되어야 합니다.");
    }

    [UnityTest]
    public IEnumerator Update_NotGoingDown_XWheelNotReached_EventNotFired()
    {
        SetupDetector(xwheelY: 1f, ywheelY: 0.5f);
        yield return null;

        bool shuttleFloorReached = false;
        _detector.OnShuttleFloorReached += () => shuttleFloorReached = true;

        _spurGearController.SetSignal(false, 5f);
        yield return null;

        MoveXWheelTo(0.8f);
        yield return new WaitForSeconds(WaitTime);

        Assert.IsFalse(shuttleFloorReached, "xwheel 미도달 시 이벤트가 발행되지 않아야 합니다.");
    }

    #endregion

    // ============================================================
    // isSlider2 테스트
    // ============================================================

    #region isSlider2 테스트

    [UnityTest]
    public IEnumerator IsGoingDown_IsSlider2True_InvertsDirection()
    {
        // isSlider2=false SpurGear + Detector isSlider2=true → IsGoingDown = !IsForward
        SetupDetector(isSlider2: false);
        yield return null;

        // Detector 의 _isSlider2 만 true 로 재설정
        typeof(SliderFloorDetector)
            .GetField("_isSlider2", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_detector, true);

        _spurGearController.SetSignal(true, 5f); // IsForward=true
        yield return new WaitForSeconds(WaitTime);

        // IsGoingDown = !IsForward = false
        Assert.IsFalse(_detector.IsGoingDown,
            "isSlider2=true 이면 IsGoingDown 이 IsForward 의 반전이어야 합니다.");
    }

    [UnityTest]
    public IEnumerator IsGoingDown_IsSlider2True_IsForwardFalse_ReturnsTrue()
    {
        SetupDetector(isSlider2: false);
        yield return null;

        typeof(SliderFloorDetector)
            .GetField("_isSlider2", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_detector, true);

        _spurGearController.SetSignal(false, 5f); // IsForward=false
        yield return new WaitForSeconds(WaitTime);

        // IsGoingDown = !IsForward = true
        Assert.IsTrue(_detector.IsGoingDown,
            "isSlider2=true + isForward=false 이면 IsGoingDown=true 여야 합니다.");
    }

    #endregion

    // ============================================================
    // 악조건 테스트
    // ============================================================

    #region 악조건 테스트

    [UnityTest]
    public IEnumerator Update_ReSignalAfterFloorReached_ResetsState()
    {
        SetupDetector(xwheelY: 1f, ywheelY: 1f);
        yield return null;

        int eventCount = 0;
        _detector.OnFloorReached += () => eventCount++;

        _spurGearController.SetSignal(true, 0.2f);
        yield return null;
        MoveYWheelTo(0.5f);
        yield return new WaitForSeconds(0.3f);

        MoveYWheelTo(1f);
        _spurGearController.SetSignal(true, 5f);
        yield return null;
        MoveYWheelTo(0.5f);
        yield return new WaitForSeconds(WaitTime);

        Assert.AreEqual(2, eventCount, "재신호 후 도달 이벤트가 다시 발행되어야 합니다.");
    }

    [UnityTest]
    public IEnumerator Update_SameHeightWheels_TargetFloorYSetCorrectly()
    {
        SetupDetector(xwheelY: 0.5f, ywheelY: 0.5f);
        yield return null;

        _spurGearController.SetSignal(true, 5f);
        yield return null;

        float expected = 0.5f - WheelRadius;
        Assert.AreEqual(expected, GetField<float>("_targetFloorY"), Tolerance,
            "같은 높이이면 targetFloorY = height - wheelRadius 여야 합니다.");
    }

    [UnityTest]
    public IEnumerator Update_JustAboveTolerance_EventNotFired()
    {
        // xwheel=1f, ywheel=1f → targetFloorY = 0.86
        SetupDetector(xwheelY: 1f, ywheelY: 1f);
        yield return null;

        bool floorReached = false;
        _detector.OnFloorReached += () => floorReached = true;

        // 신호 전에 ywheel 높게 이동
        MoveYWheelTo(3f);

        _spurGearController.SetSignal(true, 5f);
        yield return null; // targetFloorY = Min(0.86, 2.86) = 0.86 캡처

        // tolerance 바로 위: YMin > 0.86 + 0.002 = 0.862
        // newY - 0.14 = 0.863 → newY = 1.003
        MoveYWheelTo(1.003f + WheelRadius); // YMin = 1.003 > 0.862
        yield return new WaitForSeconds(WaitTime);

        Assert.IsFalse(floorReached, "tolerance 바로 위에서는 이벤트가 발행되지 않아야 합니다.");
    }

    [UnityTest]
    public IEnumerator Update_JustBelowTolerance_EventFired()
    {
        SetupDetector(xwheelY: 1f, ywheelY: 1f);
        yield return null;

        bool floorReached = false;
        _detector.OnFloorReached += () => floorReached = true;

        _spurGearController.SetSignal(true, 5f);
        yield return null;

        MoveYWheelTo(0.86f + ReturnTol + WheelRadius);
        yield return new WaitForSeconds(WaitTime);

        Assert.IsTrue(floorReached, "tolerance 경계값 이하에서는 이벤트가 발행되어야 합니다.");
    }

    #endregion

    // ============================================================
    // 에러 처리 테스트
    // ============================================================

    #region 에러 처리 테스트

    [UnityTest]
    public IEnumerator ValidateComponents_MissingSpurGearController_LogsError()
    {
        // Arrange
        _managerObject.SetActive(false);
        _detector = _managerObject.AddComponent<SliderFloorDetector>();
        _xwheel1Joint = CreateWheelJoint(_xwheelObject, 1f);
        _ywheelJoint = CreateWheelJoint(_ywheelObject, 1f);

        typeof(SliderFloorDetector).GetField("_xwheel1Joint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_detector, _xwheel1Joint);
        typeof(SliderFloorDetector).GetField("_ywheelJoint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_detector, _ywheelJoint);

        LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex(".*SpurGearController.*"));

        _managerObject.SetActive(true);
        yield return null;

        Assert.IsFalse(_detector.enabled, "SpurGearController 미할당 시 컴포넌트가 비활성화되어야 합니다.");
    }

    [UnityTest]
    public IEnumerator ValidateComponents_MissingXWheel1Joint_LogsError()
    {
        // Arrange
        _managerObject.SetActive(false);
        _detector = _managerObject.AddComponent<SliderFloorDetector>();
        _spurGearController = CreateSpurGearController();
        _ywheelJoint = CreateWheelJoint(_ywheelObject, 1f);

        typeof(SliderFloorDetector).GetField("_spurGearController", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_detector, _spurGearController);
        typeof(SliderFloorDetector).GetField("_ywheelJoint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_detector, _ywheelJoint);

        LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex(".*xwheel1Joint.*"));

        _managerObject.SetActive(true);
        yield return null;

        Assert.IsFalse(_detector.enabled, "xwheel1Joint 미할당 시 컴포넌트가 비활성화되어야 합니다.");
    }

    #endregion
}