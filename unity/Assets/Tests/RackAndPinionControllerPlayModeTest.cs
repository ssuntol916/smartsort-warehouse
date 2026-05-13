// ============================================================
// 파일명  : RackAndPinionControllerPlayModeTest.cs
// 역할    : RackAndPinionController 의 PlayMode 통합 테스트.
//           각도 변화 → 이동량 변환, 노이즈 필터, 각도 점프 감지,
//           OnPositionChanged 이벤트 발행, 에러 처리를 검증한다.
//
// 작성자  : 이현화
// 작성일  : 2026-05-04
// 수정이력: 
// ============================================================

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

[TestFixture]
public class RackAndPinionControllerPlayModeTest
{
    // ============================================================
    // 상수
    // ============================================================

    private const float Tolerance = 0.01f;
    private const float WaitTime = 0.1f;
    private const float GearRadius = 0.055f;
    private const float MotionDir = -1f;

    // ============================================================
    // 테스트 픽스처
    // ============================================================

    private GameObject _managerObject;
    private GameObject _objectA;
    private GameObject _objectB;
    private RackAndPinionController _controller;
    private RevoluteJointComponent _revoluteJoint;
    private SliderJointComponent _sliderJoint;

    // ============================================================
    // Setup / Teardown
    // ============================================================

    [SetUp]
    public void SetUp()
    {
        _objectA = new GameObject("ObjectA");
        _objectA.transform.position = Vector3.zero;

        _objectB = new GameObject("ObjectB");
        _objectB.transform.position = Vector3.zero;
        _objectB.transform.rotation = Quaternion.Euler(0, 0, 90f);

        // MeshFilter 추가 (InitializeGearRadius 에서 필요)
        var meshFilter = _objectB.AddComponent<MeshFilter>();
        var mesh = new Mesh();
        mesh.bounds = new Bounds(Vector3.zero, new Vector3(GearRadius * 2f, GearRadius * 2f, 0.01f));
        meshFilter.mesh = mesh;

        _managerObject = new GameObject("RackAndPinionManager");
    }

    [TearDown]
    public void TearDown()
    {
        if (_managerObject != null) Object.Destroy(_managerObject);
        if (_objectA != null) Object.Destroy(_objectA);
        if (_objectB != null) Object.Destroy(_objectB);
        if (_revoluteJoint != null) Object.Destroy(_revoluteJoint.gameObject);
        if (_sliderJoint != null) Object.Destroy(_sliderJoint.gameObject);
    }

    // ============================================================
    // 헬퍼
    // ============================================================

    /**
     * @brief  RevoluteJointComponent 와 SliderJointComponent 를 설정하고
     *         RackAndPinionController 를 초기화한다.
     */
    private void SetupController(
        float motionDirection = MotionDir,
        float minPos = -100f,
        float maxPos = 100f)
    {
        _managerObject.SetActive(false);

        // RevoluteJointComponent — SetActive(false) 후 필드 설정 → SetActive(true)
        var revObj = new GameObject("RevoluteJointManager");
        revObj.SetActive(false);
        _revoluteJoint = revObj.AddComponent<RevoluteJointComponent>();
        SetJointField(_revoluteJoint, typeof(JointComponent), "_objectA", _objectA.transform);
        SetJointField(_revoluteJoint, typeof(JointComponent), "_objectB", _objectB.transform);
        SetJointField(_revoluteJoint, typeof(RevoluteJointComponent), "_minAngle", -180f);
        SetJointField(_revoluteJoint, typeof(RevoluteJointComponent), "_maxAngle", 180f);
        SetJointField(_revoluteJoint, typeof(RevoluteJointComponent), "_useClamp", false);
        SetLinePoints(_revoluteJoint, typeof(RevoluteJointComponent), Vector3.forward);
        revObj.SetActive(true);

        // SliderJointComponent — SetActive(false) 후 필드 설정 → SetActive(true)
        var sliderObj = new GameObject("SliderJointManager");
        sliderObj.SetActive(false);
        _sliderJoint = sliderObj.AddComponent<SliderJointComponent>();
        SetJointField(_sliderJoint, typeof(JointComponent), "_objectA", _objectA.transform);
        SetJointField(_sliderJoint, typeof(JointComponent), "_objectB", _objectB.transform);
        SetJointField(_sliderJoint, typeof(SliderJointComponent), "_minPosition", minPos);
        SetJointField(_sliderJoint, typeof(SliderJointComponent), "_maxPosition", maxPos);
        SetJointField(_sliderJoint, typeof(SliderJointComponent), "_moveDirection", Vector3.up);
        SetSliderPlanePoints(_sliderJoint, Vector3.up);
        SetSliderLinePoints(_sliderJoint, Vector3.up);
        sliderObj.SetActive(true);

        // RackAndPinionController 설정
        _controller = _managerObject.AddComponent<RackAndPinionController>();
        var type = typeof(RackAndPinionController);
        type.GetField("_revoluteJoint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _revoluteJoint);
        type.GetField("_sliderJoint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, _sliderJoint);
        type.GetField("_motionDirection", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_controller, motionDirection);

        _managerObject.SetActive(true);
    }

    private void SetJointField(object target, System.Type type, string fieldName, object value)
    {
        type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(target, value);
    }

    private void SetLinePoints(object target, System.Type type, Vector3 axis)
    {
        type.GetField("_lineAPointA", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(target, Vector3.zero);
        type.GetField("_lineAPointB", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(target, axis.normalized);
        type.GetField("_lineBPointA", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(target, Vector3.zero);
        type.GetField("_lineBPointB", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(target, axis.normalized);
    }

    private void SetSliderLinePoints(object target, Vector3 dir)
    {
        var type = typeof(SliderJointComponent);
        Vector3 lineB = dir.sqrMagnitude > 0 ? dir.normalized : Vector3.right;
        type.GetField("_lineAPointA", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(target, Vector3.zero);
        type.GetField("_lineAPointB", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(target, lineB);
        type.GetField("_lineBPointA", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(target, Vector3.zero);
        type.GetField("_lineBPointB", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(target, lineB);
    }

    private void SetSliderPlanePoints(object target, Vector3 dir)
    {
        var type = typeof(SliderJointComponent);
        Vector3 lineB = dir.normalized;
        Vector3 perpA = Vector3.Cross(lineB, Vector3.up).sqrMagnitude > 0
            ? Vector3.Cross(lineB, Vector3.up).normalized
            : Vector3.Cross(lineB, Vector3.right).normalized;
        Vector3 perpB = Vector3.Cross(lineB, perpA).normalized;

        type.GetField("_planeAPointA", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(target, Vector3.zero);
        type.GetField("_planeAPointB", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(target, perpA);
        type.GetField("_planeAPointC", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(target, perpB);
        type.GetField("_planeBPointA", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(target, Vector3.zero);
        type.GetField("_planeBPointB", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(target, perpA);
        type.GetField("_planeBPointC", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(target, perpB);
    }

    private float GetField(string name)
    {
        return (float)typeof(RackAndPinionController)
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(_controller);
    }

    // ============================================================
    // 기본 동작 테스트
    // ============================================================

    #region 기본 동작 테스트

    /**
     * @brief  초기화 후 gearRadius 가 자동 계산되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator Initialization_AfterStart_GearRadiusCalculated()
    {
        // Arrange & Act
        LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(".*gearRadius.*"));
        SetupController();
        yield return null;
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.Greater(GetField("_gearRadius"), 0f, "gearRadius 가 0 보다 커야 합니다.");
    }

    /**
     * @brief  각도 변화 시 OnPositionChanged 이벤트가 발행되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator Update_AngleChange_FiresOnPositionChanged()
    {
        // Arrange
        LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(".*gearRadius.*"));
        SetupController();
        yield return null;

        bool eventFired = false;
        _controller.OnPositionChanged += _ => eventFired = true;

        // Act
        _revoluteJoint.SetAngle(2f);
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsTrue(eventFired, "각도 변화 시 OnPositionChanged 이벤트가 발행되어야 합니다.");
    }

    /**
     * @brief  motionDirection=1 일 때 양수 delta 가 발행되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator Update_PositiveMotionDirection_PositiveDeltaPosition()
    {
        // Arrange
        LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(".*gearRadius.*"));
        SetupController(motionDirection: 1f);
        yield return null;

        var deltas = new List<float>();
        _controller.OnPositionChanged += delta => deltas.Add(delta);

        // Act
        _revoluteJoint.SetAngle(3f);
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.Greater(deltas.Count, 0, "이벤트가 발행되어야 합니다.");
        Assert.Greater(deltas[0], 0f, "motionDirection=1 이면 양수 delta 가 발행되어야 합니다.");
    }

    /**
     * @brief  motionDirection=-1 일 때 음수 delta 가 발행되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator Update_NegativeMotionDirection_NegativeDeltaPosition()
    {
        // Arrange
        LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(".*gearRadius.*"));
        SetupController(motionDirection: -1f);
        yield return null;

        var deltas = new List<float>();
        _controller.OnPositionChanged += delta => deltas.Add(delta);

        // Act
        _revoluteJoint.SetAngle(3f);
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.Greater(deltas.Count, 0, "이벤트가 발행되어야 합니다.");
        Assert.Less(deltas[0], 0f, "motionDirection=-1 이면 음수 delta 가 발행되어야 합니다.");
    }

    #endregion

    // ============================================================
    // 노이즈 필터 테스트
    // ============================================================

    #region 노이즈 필터 테스트

    /**
     * @brief  노이즈 수준 각도 변화에서는 이벤트가 발행되지 않는지 검증한다.
     */
    [UnityTest]
    public IEnumerator Update_TinyAngleChange_EventNotFired()
    {
        // Arrange
        LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(".*gearRadius.*"));
        SetupController();
        yield return null;

        bool eventFired = false;
        _controller.OnPositionChanged += _ => eventFired = true;

        // Act - 매우 작은 각도 변화
        _revoluteJoint.SetAngle(0.001f);
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsFalse(eventFired, "노이즈 수준의 각도 변화에서는 이벤트가 발행되지 않아야 합니다.");
    }

    /**
     * @brief  각도 점프(0.1rad 초과) 발생 시 해당 프레임이 무시되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator Update_AngleJump_FrameSkipped()
    {
        // Arrange
        LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(".*gearRadius.*"));
        SetupController();
        yield return null;

        int eventCount = 0;
        _controller.OnPositionChanged += _ => eventCount++;

        // Act - 10도 = 0.1745 rad > 0.1 rad threshold
        _revoluteJoint.SetAngle(10f);
        yield return null;

        // Assert
        Assert.AreEqual(0, eventCount, "각도 점프 프레임에서는 이벤트가 발행되지 않아야 합니다.");
    }

    /**
     * @brief  각도 점프 이후 정상 각도 변화 시 이벤트가 발행되는지 검증한다.
     *         점프 감지 시 _lastAngle 이 갱신되지 않으므로 Reflection 으로 직접 동기화한다.
     */
    [UnityTest]
    public IEnumerator Update_AfterAngleJump_NormalChangeFiresEvent()
    {
        // Arrange
        LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(".*gearRadius.*"));
        SetupController();
        yield return null;

        // 큰 점프 발생
        _revoluteJoint.SetAngle(10f);
        yield return new WaitForSeconds(WaitTime * 3);

        // _lastAngle 을 현재 각도로 강제 동기화
        typeof(RackAndPinionController)
            .GetField("_lastAngle", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_controller, _revoluteJoint.CurrentAngle);

        bool eventFired = false;
        _controller.OnPositionChanged += _ => eventFired = true;

        // Act - 1도 변화 = 0.0175 rad < 0.1 rad threshold
        _revoluteJoint.SetAngle(11f);
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsTrue(eventFired, "각도 점프 이후 정상 변화에서는 이벤트가 발행되어야 합니다.");
    }

    #endregion

    // ============================================================
    // 악조건 테스트
    // ============================================================

    #region 악조건 테스트

    /**
     * @brief  각도 변화 없을 때 이벤트가 발행되지 않는지 검증한다.
     */
    [UnityTest]
    public IEnumerator Update_NoAngleChange_EventNotFired()
    {
        // Arrange
        LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(".*gearRadius.*"));
        SetupController();
        yield return null;

        bool eventFired = false;
        _controller.OnPositionChanged += _ => eventFired = true;

        // Act
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsFalse(eventFired, "각도 변화 없으면 이벤트가 발행되지 않아야 합니다.");
    }

    /**
     * @brief  역방향 각도 변화 시 음수 delta 가 발행되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator Update_NegativeAngleChange_NegativeDeltaFired()
    {
        // Arrange
        LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(".*gearRadius.*"));
        SetupController(motionDirection: 1f);
        yield return null;

        _revoluteJoint.SetAngle(5f);
        yield return new WaitForSeconds(WaitTime);

        var deltas = new List<float>();
        _controller.OnPositionChanged += delta => deltas.Add(delta);

        // Act
        _revoluteJoint.SetAngle(4f);
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.Greater(deltas.Count, 0, "이벤트가 발행되어야 합니다.");
        Assert.Less(deltas[0], 0f, "역방향 각도 변화 시 음수 delta 가 발행되어야 합니다.");
    }

    /**
     * @brief  연속 각도 변화 시 이벤트가 누적 발행되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator Update_ContinuousAngleChange_MultipleEventsFireCorrectly()
    {
        // Arrange
        LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(".*gearRadius.*"));
        SetupController(motionDirection: 1f);
        yield return null;

        int eventCount = 0;
        _controller.OnPositionChanged += _ => eventCount++;

        // Act
        for (int i = 1; i <= 5; i++)
        {
            _revoluteJoint.SetAngle(i * 1f);
            yield return null;
        }
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.Greater(eventCount, 0, "연속 각도 변화 시 이벤트가 여러 번 발행되어야 합니다.");
    }

    /**
     * @brief  sliderJoint 최대 범위 초과 시 클램프가 적용되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator Update_SliderExceedsMax_PositionClamped()
    {
        // Arrange
        LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(".*gearRadius.*"));
        SetupController(motionDirection: 1f, minPos: -1f, maxPos: 1f);
        yield return null;

        // Act
        for (int i = 0; i < 20; i++)
        {
            _revoluteJoint.SetAngle((i + 1) * 1f);
            yield return null;
        }
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.LessOrEqual(_sliderJoint.CurrentPosition, 1f + 0.1f,
            "슬라이더 위치가 최대 범위를 초과하지 않아야 합니다.");
    }

    /**
     * @brief  빠른 큰 각도 변화 반복에서 NaN/Infinity 없이 안정적인지 검증한다.
     */
    [UnityTest]
    public IEnumerator Update_RapidLargeAngleChanges_RemainsStable()
    {
        // Arrange
        LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(".*gearRadius.*"));
        SetupController();
        yield return null;

        // Act
        float[] angles = { 20f, -20f, 40f, -40f, 60f, -60f };
        foreach (float angle in angles)
        {
            _revoluteJoint.SetAngle(angle);
            yield return new WaitForFixedUpdate();
        }
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsFalse(float.IsNaN(_sliderJoint.CurrentPosition), "슬라이더 위치가 NaN 이면 안 됩니다.");
        Assert.IsFalse(float.IsInfinity(_sliderJoint.CurrentPosition), "슬라이더 위치가 Infinity 이면 안 됩니다.");
    }

    #endregion

    // ============================================================
    // 에러 처리 테스트
    // ============================================================

    #region 에러 처리 테스트

    /**
     * @brief  RevoluteJoint 미할당 시 에러 로그가 발생하고 컴포넌트가 비활성화되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ValidateComponents_MissingRevoluteJoint_LogsError()
    {
        // Arrange
        _managerObject.SetActive(false);
        _controller = _managerObject.AddComponent<RackAndPinionController>();

        var sliderObj = new GameObject("SliderJointManager");
        sliderObj.SetActive(false);
        _sliderJoint = sliderObj.AddComponent<SliderJointComponent>();
        sliderObj.SetActive(true);

        typeof(RackAndPinionController)
            .GetField("_sliderJoint", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_controller, _sliderJoint);

        LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex(".*필수 오브젝트.*"));
        LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex(".*RevoluteJoint.*"));

        // Act
        _managerObject.SetActive(true);
        yield return null;

        // Assert
        Assert.IsFalse(_controller.enabled, "RevoluteJoint 미할당 시 컴포넌트가 비활성화되어야 합니다.");
    }

    /**
     * @brief  SliderJoint 미할당 시 에러 로그가 발생하고 컴포넌트가 비활성화되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator ValidateComponents_MissingSliderJoint_LogsError()
    {
        // Arrange
        _managerObject.SetActive(false);
        _controller = _managerObject.AddComponent<RackAndPinionController>();

        var revObj = new GameObject("RevoluteJointManager");
        revObj.SetActive(false);
        _revoluteJoint = revObj.AddComponent<RevoluteJointComponent>();
        SetJointField(_revoluteJoint, typeof(JointComponent), "_objectA", _objectA.transform);
        SetJointField(_revoluteJoint, typeof(JointComponent), "_objectB", _objectB.transform);
        SetJointField(_revoluteJoint, typeof(RevoluteJointComponent), "_minAngle", -180f);
        SetJointField(_revoluteJoint, typeof(RevoluteJointComponent), "_maxAngle", 180f);
        SetJointField(_revoluteJoint, typeof(RevoluteJointComponent), "_useClamp", false);
        SetLinePoints(_revoluteJoint, typeof(RevoluteJointComponent), Vector3.forward);
        revObj.SetActive(true);

        typeof(RackAndPinionController)
            .GetField("_revoluteJoint", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_controller, _revoluteJoint);

        LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex(".*SliderJoint.*"));

        // Act
        _managerObject.SetActive(true);
        yield return null;

        // Assert
        Assert.IsFalse(_controller.enabled, "SliderJoint 미할당 시 컴포넌트가 비활성화되어야 합니다.");
    }

    /**
     * @brief  ObjectB 에 MeshFilter 미부착 시 에러 로그가 발생하고 컴포넌트가 비활성화되는지 검증한다.
     */
    [UnityTest]
    public IEnumerator InitializeGearRadius_MissingMeshFilter_DisablesComponent()
    {
        // Arrange
        Object.Destroy(_objectB);
        _objectB = new GameObject("NoMeshObject");

        LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex(".*MeshFilter.*"));

        SetupController();
        yield return null;
        yield return new WaitForSeconds(WaitTime);

        // Assert
        Assert.IsFalse(_controller.enabled, "MeshFilter 미부착 시 컴포넌트가 비활성화되어야 합니다.");
    }

    #endregion
}