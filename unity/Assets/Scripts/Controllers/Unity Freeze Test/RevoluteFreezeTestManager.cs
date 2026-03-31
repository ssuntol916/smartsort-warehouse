// ============================================================
// 파일명  : RevoluteFreezeTestManager.cs
// 역할    : 구속 조건 테스트용 오브젝트 B 드래그 매니저
//           마우스 드래그로 오브젝트 B에 회전을 주어 구속 조건 및 클램프를 테스트한다.
// 작성자  : 이현화
// 작성일  : 2026-03-31
// 수정이력: 
// ============================================================

using UnityEngine;

public class RevoluteFreezeTestManager : MonoBehaviour
{
    [SerializeField] private Transform _objectB;                        // 테스트 대상 오브젝트 B 연결
    [SerializeField] private RevoluteJointComponent _jointComponent;    // 누적 각도 관리 컴포넌트
    [SerializeField] private float _rotateSpeed = 5f;                   // 마우스 드래그 감도
    [SerializeField] private bool _useMouseY = true;                    // 마우스 입력 방향 (true: 위아래, false: 좌우)

    private Rigidbody _rigidbodyB;  // 오브젝트 B 리지드바디

    private void Start()
    {
        // 오브젝트 B 의 Rigidbody 컴포넌트를 가져온다.
        _rigidbodyB = _objectB.GetComponent<Rigidbody>();

        // Rigidbody 가 없으면 경고 출력 후 종료
        if (_rigidbodyB == null)
        {
            Debug.LogWarning("RevoluteFreezeTestManager: 움직일 오브젝트(Object B)가 연결되지 않았습니다. \nInspector 에서 오브젝트를 연결해주세요.");
            return;
        }

    }

    /**
     * @brief  마우스 오른쪽 버튼을 누르고 드래그하면 오브젝트 B 를 회전시킨다.
     *         마우스 입력 방향(Mouse X 또는 Mouse Y)에 감도를 곱해 회전 각도 변화량을 구하고
     *         Rigidbody.MoveRotation() 으로 오브젝트 B 를 직접 회전시킨다.
     */
    private void Update()
    {
        // Rigidbody 가 없으면 이하 코드 실행하지 않는다.
        if (_rigidbodyB == null) return;

        // JointComponent 가 없으면 이하 코드 실행하지 않는다.
        if (_jointComponent == null)
        {
            Debug.LogWarning("RevoluteFreezeTestManager: JointComponent 가 연결되지 않았습니다. \nInspector 에서 오브젝트를 연결해주세요.");
            return;
        }

        if (Input.GetMouseButton(1))
        {
            float delta = Input.GetAxis(_useMouseY ? "Mouse Y" : "Mouse X") * _rotateSpeed;  // 마우스 입력 방향에 따라 회전 변화량 계산

            // 회전축 가져오기
            Vector3 rotationAxis = _jointComponent.RotationAxis;

            // delta만큼 회전시키기
            Quaternion rotation = Quaternion.AngleAxis(delta, rotationAxis);
            _rigidbodyB.MoveRotation(_rigidbodyB.rotation * rotation);
        }
    }
}
