// ============================================================
// 파일명  : SliderFreezeTestManager.cs
// 역할    : 구속 조건 테스트용 오브젝트 B 드래그 매니저
//           마우스 드래그로 오브젝트 B 를 이동시켜 구속 조건 및 클램프를 테스트한다.
// 작성자  : 이현화
// 작성일  : 2026-04-01
// 수정이력: 2026-04-22 - FixedUpdate() → Update() 로 변경, Rigidbody 제거
//                        (Transform 직접 제어 방식으로 리팩토링)
// ============================================================

using UnityEngine;

public class SliderFreezeTestManager : MonoBehaviour
{
    [SerializeField] private Transform _objectB;         // 테스트 대상 오브젝트 B 연결
    [SerializeField] private float _sliderSpeed = 10f;  // 마우스 드래그 감도

    private void Start()
    {
        if (_objectB == null)
        {
            Debug.LogWarning("SliderFreezeTestManager: 움직일 오브젝트(Object B)가 연결되지 않았습니다. \nInspector 에서 오브젝트를 연결해주세요.");
        }
    }

    /**
     * @brief  마우스 왼쪽 버튼을 누르고 드래그하면 목표 위치를 계산하여
     *         Transform 직접 제어로 즉시 적용한다.
     */
    private void Update()
    {
        if (_objectB == null) return;

        if (Input.GetMouseButton(0))
        {
            // 마우스 위치를 월드 좌표로 변환
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Camera.main.WorldToScreenPoint(_objectB.position).z;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

            // [2026.04.22 수정] Transform 직접 제어로 즉시 적용
            _objectB.position += (worldPos - _objectB.position) * _sliderSpeed * Time.deltaTime;
        }
    }
}