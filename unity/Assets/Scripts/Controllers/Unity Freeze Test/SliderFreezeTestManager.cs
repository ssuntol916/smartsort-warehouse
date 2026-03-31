// ============================================================
// 파일명  : SliderFreezeTestManager.cs
// 역할    : 구속 조건 테스트용 오브젝트 B 드래그 매니저
//           마우스 드래그로 오브젝트 B 를 이동시켜 구속 조건 및 클램프를 테스트한다.
// 작성자  : 이현화
// 작성일  : 2026-03-30
// 수정이력: 
// ============================================================

using UnityEngine;

public class SliderFreezeTestManager : MonoBehaviour
{
    [SerializeField] private Transform _objectB;         // 테스트 대상 오브젝트 B 연결
    [SerializeField] private float _sliderSpeed = 0.5f;  // 마우스 드래그 감도

    private Rigidbody _rigidbodyB;  // 오브젝트 B 리지드바디

    private void Start()
    {
        // 오브젝트 B 의 Rigidbody 컴포넌트를 가져온다.
        _rigidbodyB = _objectB.GetComponent<Rigidbody>();

        // Rigidbody 가 없으면 경고 출력 후 종료
        if (_rigidbodyB == null)
        {
            Debug.LogWarning("SliderFreezeTestManager: 움직일 오브젝트(Object B)가 연결되지 않았습니다. \nInspector 에서 오브젝트를 연결해주세요.");
            return;
        }
        
    }

    /**
     * @brief  마우스 왼쪽 버튼을 누르고 드래그하면 오브젝트 B 를 이동시킨다.
     *         마우스 스크린 좌표를 가져와 오브젝트 B 의 깊이값(z)을 적용한 후
     *         3D 월드 좌표로 변환하여 Rigidbody.MovePosition() 으로 이동시킨다.
     */
    private void Update()
    {
        // Rigidbody 가 없으면 이하 코드 실행하지 않는다.
        if (_rigidbodyB == null) return;

        if (Input.GetMouseButton(0))
        {
            _rigidbodyB.angularVelocity = Vector3.zero;  // 회전 속도 초기화
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Camera.main.WorldToScreenPoint(_rigidbodyB.transform.position).z;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

            Vector3 delta = (worldPos - _rigidbodyB.position) * _sliderSpeed;
            _rigidbodyB.MovePosition(_rigidbodyB.position + delta);
        }

    }
}
