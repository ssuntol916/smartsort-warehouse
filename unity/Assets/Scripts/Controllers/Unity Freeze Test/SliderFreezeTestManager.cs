// ============================================================
// 파일명  : SliderFreezeTestManager.cs
// 역할    : 구속 조건 테스트용 오브젝트 B 드래그 매니저
//           마우스 드래그로 오브젝트 B 를 이동시켜 구속 조건 및 클램프를 테스트한다.
// 작성자  : 이현화
// 작성일  : 2026-04-01
// 수정이력: 
// ============================================================

using UnityEngine;

public class SliderFreezeTestManager : MonoBehaviour
{
    [SerializeField] private Transform _objectB;         // 테스트 대상 오브젝트 B 연결
    [SerializeField] private float _sliderSpeed = 10f;  // 마우스 드래그 감도

    private Rigidbody _rigidbodyB;      // 오브젝트 B 리지드바디
    private Vector3 _pendingPosition;   // 목표 위치 임시 저장 (Update → FixedUpdate 전달용)
    private bool _hasPending;           // 이번 프레임에 입력이 있었는지 여부

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
     * @brief  마우스 왼쪽 버튼을 누르고 드래그하면 목표 위치를 _pendingPosition 에 저장한다.
     *         마우스 입력은 Update 에서만 읽어야 하므로 물리 적용은 FixedUpdate 에 위임한다.
     */
    private void Update()
    {
        // Rigidbody 가 없으면 이하 코드 실행하지 않는다.
        if (_rigidbodyB == null) return;

        if (Input.GetMouseButton(0))
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Camera.main.WorldToScreenPoint(_rigidbodyB.transform.position).z;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

            // 목표 위치 계산 후 저장 (물리 적용은 FixedUpdate 에 위임)
            _pendingPosition = _rigidbodyB.position + (worldPos - _rigidbodyB.position) * _sliderSpeed * Time.deltaTime;
            _hasPending = true;
        }
        else
        {
            _hasPending = false;  // 버튼 안 누르면 초기화
        }

    }

    /**
    * @brief  매 물리 프레임 _pendingPosition 을 읽어 오브젝트 B 를 이동시킨다.
    *         Rigidbody.MovePosition() 은 물리 연산이므로 FixedUpdate 에서 호출한다.
    */
    private void FixedUpdate()
    {
        // 입력값이 없으면 실행하지 않는다.
        if (_rigidbodyB == null || !_hasPending) return;

        _rigidbodyB.angularVelocity = Vector3.zero;  // 회전 속도 초기화
        _rigidbodyB.MovePosition(_pendingPosition);
    }
}
