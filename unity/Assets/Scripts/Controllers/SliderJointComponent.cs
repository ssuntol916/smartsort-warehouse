// ============================================================
// 파일명  : SliderJointComponent.cs
// 역할    : SliderJoint 를 씬 오브젝트에 연결하는 MonoBehaviour 컴포넌트
// 작성자  : 이현화
// 작성일  : 2026-03-
// 수정이력: 
// ============================================================

using UnityEngine;

public class SliderJointComponent : MonoBehaviour
{
    [SerializeField] private Transform _objectA;           // 오브젝트 A (이동축 기준)
    [SerializeField] private Transform _objectB;           // 오브젝트 B (이동 대상)
    [SerializeField] private float _minPosition = 0f;     // 최소 이동 범위 (mm)
    [SerializeField] private float _maxPosition = 100f;   // 최대 이동 범위 (mm)

    private SliderJoint _joint;   // SliderJoint.cs 인스턴스

    public float CurrentPosition => _joint?.CurrentPosition ?? 0f;   // 현재 슬라이더 위치

    /**
     * @brief  오브젝트 A·B 의 Transform 을 기반으로 이동축 Line 과
     *         기준 Plane 을 생성하고 SliderJoint 를 초기화한다. (이동방향: 위, 아래)
     *         - 이동축 Line: transform.position → transform.position + transform.up
     *         - 기준 Plane: transform.up 을 법선 벡터로 하는 면
     *                       세 점(position, position+right, position+forward) 으로 정의
     */
    private void Awake()
    {
        // 오브젝트 A·B 의 Transform 에서 이동축 Line 생성
        Line lineA = new Line(_objectA.position,
                              _objectA.position + _objectA.up);
        Line lineB = new Line(_objectB.position,
                              _objectB.position + _objectB.up);

        // 오브젝트 A·B 의 Transform 에서 기준 Plane 생성
        // transform.right, transform.forward 로 면의 세 점 정의
        Plane planeA = new Plane(_objectA.position,
                                 _objectA.position + _objectA.right,
                                 _objectA.position + _objectA.forward);
        Plane planeB = new Plane(_objectB.position,
                                 _objectB.position + _objectB.right,
                                 _objectB.position + _objectB.forward);

        // SliderJoint.cs 인스턴스 생성
        _joint = new SliderJoint(lineA, lineB,
                                 planeA, planeB,
                                 _minPosition, _maxPosition);
    }

    /**
     * @brief  매 프레임 SliderJoint.ApplyConstraint() 를 호출하여
     *         구속 조건을 오브젝트 B 의 Transform 에 반영한다.
     */
    private void Update()
    {
        if (_joint == null || !_joint.IsValid()) return;

        _joint.ApplyConstraint();
    }

    /**
     * @brief  외부에서 슬라이더 위치를 설정한다.
     *         ShuttleController.cs 에서 X·Y 바퀴 위치 제어 시 호출한다.
     *         SliderJoint.SetPosition() 에 위임한다.
     * @param  position    설정할 슬라이더 위치 (mm)
     */
    public void SetPosition(float position)
    {
        _joint?.SetPosition(position);
    }

    /**
     * @brief  Inspector 에서 값 변경 시 Line·Plane 을 재생성하고
     *         SliderJoint 를 재초기화한다.
     */
    private void OnValidate()
    {
        if (_objectA == null || _objectB == null) return;

        Line lineA = new Line(_objectA.position,
                              _objectA.position + _objectA.up);
        Line lineB = new Line(_objectB.position,
                              _objectB.position + _objectB.up);

        Plane planeA = new Plane(_objectA.position,
                                 _objectA.position + _objectA.right,
                                 _objectA.position + _objectA.forward);
        Plane planeB = new Plane(_objectB.position,
                                 _objectB.position + _objectB.right,
                                 _objectB.position + _objectB.forward);

        _joint = new SliderJoint(lineA, lineB,
                                 planeA, planeB,
                                 _minPosition, _maxPosition);
    }
}