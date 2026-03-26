// ============================================================
// 파일명  : RevoluteJointComponent.cs
// 역할    : RevoluteJoint 를 씬 오브젝트에 연결하는 MonoBehaviour 컴포넌트
// 작성자  : 
// 작성일  : 
// 수정이력: 
// ============================================================

using UnityEngine;

public class RevoluteJointComponent : MonoBehaviour
{
    [SerializeField] private Transform _objectA;       // 오브젝트 A (회전축 기준)
    [SerializeField] private Transform _objectB;       // 오브젝트 B (회전 대상)
    [SerializeField] private float _minAngle = -90f;   // 최소 회전 각도 (degree)
    [SerializeField] private float _maxAngle = 90f;    // 최대 회전 각도 (degree)

    private RevoluteJoint _joint;   // RevoluteJoint.cs 인스턴스

    public float CurrentAngle => _joint?.CurrentAngle ?? 0f;   // 현재 회전 각도

    /**
     * @brief  오브젝트 A·B 의 Transform 을 기반으로 회전축 Line 을 생성하고
     *         RevoluteJoint 를 초기화한다.
     *         transform.position 을 선의 시작점,
     *         transform.position + transform.up 을 선의 끝점으로 사용한다.
     */
    private void Awake()
    {
        // 오브젝트 A·B 의 Transform 에서 회전축 Line 생성
        Line lineA = new Line(_objectA.position,
                              _objectA.position + _objectA.up);
        Line lineB = new Line(_objectB.position,
                              _objectB.position + _objectB.up);

        // RevoluteJoint.cs 인스턴스 생성
        _joint = new RevoluteJoint(lineA, lineB, _minAngle, _maxAngle);
    }

    /**
     * @brief  매 프레임 RevoluteJoint.ApplyConstraint() 를 호출하여
     *         구속 조건을 오브젝트 B 의 Transform 에 반영한다.
     */
    private void Update()
    {
        if (_joint == null || !_joint.IsValid()) return;

        _joint.ApplyConstraint();
    }

    /**
     * @brief  외부에서 회전 각도를 설정한다.
     *         ShuttleController.cs 에서 임펠러 각도 제어 시 호출한다.
     *         RevoluteJoint.SetAngle() 에 위임한다.
     * @param  angle    설정할 회전 각도 (degree)
     */
    public void SetAngle(float angle)
    {
        _joint?.SetAngle(angle);
    }

    /**
     * @brief  Inspector 에서 값 변경 시 Line 을 재생성하고
     *         RevoluteJoint 를 재초기화한다.
     */
    private void OnValidate()
    {
        if (_objectA == null || _objectB == null) return;

        Line lineA = new Line(_objectA.position,
                              _objectA.position + _objectA.up);
        Line lineB = new Line(_objectB.position,
                              _objectB.position + _objectB.up);

        _joint = new RevoluteJoint(lineA, lineB, _minAngle, _maxAngle);
    }
}