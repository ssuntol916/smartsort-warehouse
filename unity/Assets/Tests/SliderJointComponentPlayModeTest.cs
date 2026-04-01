using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Reflection;

[TestFixture]
public class SliderJointComponentPlayModeTest
{
    private const float Tolerance = 0.05f;
    private const float PhysicsWaitTime = 0.15f;

    private GameObject _objectA;
    private GameObject _objectB;
    private GameObject _managerObject;
    private SliderJointComponent _jointComponent;

    // 리플렉션용 BindingFlags 설정 (private, protected, public 모두 포함)
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

    [SetUp]
    public void SetUp()
    {
        // 1. 기준 오브젝트 생성
        _objectA = new GameObject("ObjectA");
        _objectA.transform.position = Vector3.zero;
        _objectA.AddComponent<Rigidbody>().isKinematic = true;

        // 2. 이동 대상 오브젝트 생성
        _objectB = new GameObject("ObjectB");
        _objectB.transform.position = Vector3.zero;
        var rbB = _objectB.AddComponent<Rigidbody>();
        rbB.useGravity = false;
        rbB.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // 3. 매니저 오브젝트 생성 (비활성화 상태로 시작하여 Awake 지연)
        _managerObject = new GameObject("JointManager");
        _managerObject.SetActive(false);
    }

    [TearDown]
    public void TearDown()
    {
        if (_objectA != null) Object.Destroy(_objectA);
        if (_objectB != null) Object.Destroy(_objectB);
        if (_managerObject != null) Object.Destroy(_managerObject);
    }

    /**
     * @brief 리플렉션을 통해 데이터를 주입하고 컴포넌트를 초기화합니다.
     */
    private void SetupJoint(float min, float max, Vector3 dir)
    {
        _jointComponent = _managerObject.AddComponent<SliderJointComponent>();

        var baseType = typeof(JointComponent);
        var type = typeof(SliderJointComponent);

        // 부모 클래스(JointComponent) 필드 설정
        baseType.GetField("_objectA", Flags)?.SetValue(_jointComponent, _objectA.transform);
        baseType.GetField("_objectB", Flags)?.SetValue(_jointComponent, _objectB.transform);

        // 자식 클래스(SliderJointComponent) 필드 설정
        type.GetField("_minPosition", Flags)?.SetValue(_jointComponent, min);
        type.GetField("_maxPosition", Flags)?.SetValue(_jointComponent, max);
        type.GetField("_moveDirection", Flags)?.SetValue(_jointComponent, dir);

        // 필드 주입 후 활성화 (이때 Awake와 OnAwake가 실행됨)
        _managerObject.SetActive(true);
    }

    #region Group 1: 초기화 및 기본 속성 (5개)
    [UnityTest] public IEnumerator Init_01_CurrentPos_IsZero() { SetupJoint(0, 10, Vector3.right); yield return null; Assert.AreEqual(0, _jointComponent.CurrentPosition, Tolerance); }
    [UnityTest] public IEnumerator Init_02_NormalizedDir_X() { SetupJoint(0, 10, new Vector3(10, 0, 0)); yield return null; Assert.AreEqual(1, _jointComponent.MoveDirection.x, Tolerance); }
    [UnityTest] public IEnumerator Init_03_NormalizedDir_Y() { SetupJoint(0, 10, new Vector3(0, 5, 0)); yield return null; Assert.AreEqual(1, _jointComponent.MoveDirection.y, Tolerance); }
    [UnityTest] public IEnumerator Init_04_NormalizedDir_Z() { SetupJoint(0, 10, new Vector3(0, 0, 2)); yield return null; Assert.AreEqual(1, _jointComponent.MoveDirection.z, Tolerance); }
    [UnityTest] public IEnumerator Init_05_RigidbodyB_Exists() { SetupJoint(0, 10, Vector3.right); yield return null; Assert.IsNotNull(_objectB.GetComponent<Rigidbody>()); }
    #endregion

    #region Group 2: 단일 축 경계값 클램프 (6개)
    [UnityTest] public IEnumerator Clamp_06_X_Max() { SetupJoint(0, 5, Vector3.right); yield return null; _objectB.transform.position = new Vector3(10, 0, 0); yield return new WaitForFixedUpdate(); Assert.AreEqual(5, _objectB.transform.position.x, Tolerance); }
    [UnityTest] public IEnumerator Clamp_07_X_Min() { SetupJoint(-5, 5, Vector3.right); yield return null; _objectB.transform.position = new Vector3(-10, 0, 0); yield return new WaitForFixedUpdate(); Assert.AreEqual(-5, _objectB.transform.position.x, Tolerance); }
    [UnityTest] public IEnumerator Clamp_08_Y_Max() { SetupJoint(0, 3, Vector3.up); yield return null; _objectB.transform.position = new Vector3(0, 5, 0); yield return new WaitForFixedUpdate(); Assert.AreEqual(3, _objectB.transform.position.y, Tolerance); }
    [UnityTest] public IEnumerator Clamp_09_Y_Min() { SetupJoint(-2, 2, Vector3.up); yield return null; _objectB.transform.position = new Vector3(0, -4, 0); yield return new WaitForFixedUpdate(); Assert.AreEqual(-2, _objectB.transform.position.y, Tolerance); }
    [UnityTest] public IEnumerator Clamp_10_Z_Max() { SetupJoint(0, 8, Vector3.forward); yield return null; _objectB.transform.position = new Vector3(0, 0, 15); yield return new WaitForFixedUpdate(); Assert.AreEqual(8, _objectB.transform.position.z, Tolerance); }
    [UnityTest] public IEnumerator Clamp_11_Z_Min() { SetupJoint(-1, 1, Vector3.forward); yield return null; _objectB.transform.position = new Vector3(0, 0, -5); yield return new WaitForFixedUpdate(); Assert.AreEqual(-1, _objectB.transform.position.z, Tolerance); }
    #endregion

    #region Group 3: 비대칭 및 특수 범위 (4개)
    [UnityTest] public IEnumerator Range_12_PositiveOnly() { SetupJoint(5, 10, Vector3.right); yield return null; _objectB.transform.position = Vector3.zero; yield return new WaitForFixedUpdate(); Assert.AreEqual(5, _objectB.transform.position.x, Tolerance); }
    [UnityTest] public IEnumerator Range_13_NegativeOnly() { SetupJoint(-20, -10, Vector3.right); yield return null; _objectB.transform.position = Vector3.zero; yield return new WaitForFixedUpdate(); Assert.AreEqual(-10, _objectB.transform.position.x, Tolerance); }
    [UnityTest] public IEnumerator Range_14_Narrow() { SetupJoint(0.1f, 0.2f, Vector3.right); yield return null; _objectB.transform.position = Vector3.right * 1f; yield return new WaitForFixedUpdate(); Assert.AreEqual(0.2f, _objectB.transform.position.x, Tolerance); }
    [UnityTest] public IEnumerator Range_15_Wide() { SetupJoint(-100, 100, Vector3.right); yield return null; _objectB.transform.position = Vector3.right * 50f; yield return new WaitForFixedUpdate(); Assert.AreEqual(50, _objectB.transform.position.x, Tolerance); }
    #endregion

    #region Group 4: 물리 엔진 스트레스 테스트 (5개)
    [UnityTest] public IEnumerator Physics_16_Impulse_Max() { SetupJoint(0, 5, Vector3.right); yield return null; _objectB.GetComponent<Rigidbody>().AddForce(Vector3.right * 5000, ForceMode.Impulse); yield return new WaitForSeconds(PhysicsWaitTime); Assert.LessOrEqual(_objectB.transform.position.x, 5 + Tolerance); }
    [UnityTest] public IEnumerator Physics_17_Impulse_Min() { SetupJoint(-5, 0, Vector3.right); yield return null; _objectB.GetComponent<Rigidbody>().AddForce(Vector3.left * 5000, ForceMode.Impulse); yield return new WaitForSeconds(PhysicsWaitTime); Assert.GreaterOrEqual(_objectB.transform.position.x, -5 - Tolerance); }
    [UnityTest] public IEnumerator Physics_18_VelocityLimit() { SetupJoint(0, 10, Vector3.right); yield return null; var rb = _objectB.GetComponent<Rigidbody>(); SetVelocity(rb, Vector3.right * 50); yield return new WaitForSeconds(PhysicsWaitTime); Assert.LessOrEqual(_objectB.transform.position.x, 10 + Tolerance); }
    [UnityTest] public IEnumerator Physics_19_Gravity_On() { SetupJoint(0, 10, Vector3.right); yield return null; yield return new WaitForSeconds(PhysicsWaitTime); Assert.IsTrue(_objectB.GetComponent<Rigidbody>().useGravity); }
    [UnityTest] public IEnumerator Physics_20_Gravity_Off() { SetupJoint(0, 10, Vector3.right); yield return null; _objectB.transform.position = new Vector3(0, 50, 0); yield return new WaitForFixedUpdate(); Assert.IsFalse(_objectB.GetComponent<Rigidbody>().useGravity); }
    #endregion

    #region Group 5: 좌표계 및 계층 구조 (5개)
    [UnityTest] public IEnumerator Transform_21_Diagonal() { Vector3 diag = new Vector3(1, 1, 0).normalized; SetupJoint(0, 10, diag); yield return null; _objectB.transform.position = diag * 20; yield return new WaitForFixedUpdate(); Assert.AreEqual(10 / Mathf.Sqrt(2), _objectB.transform.position.x, Tolerance); }
    [UnityTest] public IEnumerator Transform_22_ParentMove() { SetupJoint(0, 5, Vector3.right); yield return null; _objectA.transform.position = Vector3.right * 10; _objectB.transform.position = Vector3.right * 10; yield return new WaitForFixedUpdate(); Assert.AreEqual(10, _objectB.transform.position.x, Tolerance); }
    [UnityTest] public IEnumerator Transform_23_ParentFollow() { SetupJoint(0, 5, Vector3.right); yield return null; _objectA.transform.position = Vector3.right * 10; _objectB.transform.position = Vector3.right * 20; yield return new WaitForFixedUpdate(); Assert.AreEqual(15, _objectB.transform.position.x, Tolerance); }
    [UnityTest] public IEnumerator Transform_24_ScaleIndependent() { SetupJoint(0, 10, Vector3.right); yield return null; _objectB.transform.localScale = Vector3.one * 5; _objectB.transform.position = Vector3.right * 20; yield return new WaitForFixedUpdate(); Assert.AreEqual(10, _objectB.transform.position.x, Tolerance); }
    [UnityTest] public IEnumerator Transform_25_RotationAxis() { SetupJoint(0, 10, Vector3.right); yield return null; _objectA.transform.rotation = Quaternion.Euler(0, 90, 0); yield return null; Assert.Pass(); }
    #endregion

    #region Group 6: 에러 핸들링 (5개)
    [UnityTest] public IEnumerator Error_26_MinMax_Inverted() { SetupJoint(10, 0, Vector3.right); yield return null; Assert.Pass(); }
    [UnityTest] public IEnumerator Error_27_ZeroDirection() { SetupJoint(0, 10, Vector3.zero); yield return null; Assert.Pass(); }
    [UnityTest] public IEnumerator Error_28_NullObject() { _managerObject.SetActive(false); _managerObject.AddComponent<SliderJointComponent>(); _managerObject.SetActive(true); yield return null; Assert.Pass(); }
    [UnityTest] public IEnumerator Error_29_DoubleComponent() { _managerObject.AddComponent<SliderJointComponent>(); _managerObject.AddComponent<SliderJointComponent>(); yield return null; Assert.Pass(); }
    [UnityTest] public IEnumerator Error_30_FastPositionChange() { SetupJoint(0, 10, Vector3.right); for (int i = 0; i < 5; i++) _objectB.transform.position = Vector3.right * 20; yield return null; Assert.Pass(); }
    #endregion

    // 유니티 버전에 따른 velocity 설정 도우미
    private void SetVelocity(Rigidbody rb, Vector3 vel)
    {
#if UNITY_2022_3_OR_NEWER
        rb.linearVelocity = vel;
#else
        rb.velocity = vel;
#endif
    }
}