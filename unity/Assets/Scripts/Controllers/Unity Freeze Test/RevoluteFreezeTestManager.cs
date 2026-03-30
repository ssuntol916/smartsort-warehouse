// ============================================================
// 파일명  : FreezeTestManager.cs
// 역할    : 회전 조인트 테스트용 매니저 (마우스 왼쪽 드래그 상하 회전)
// 작성자  : 이현화
// 작성일  : 2026-03-30
// ============================================================
using UnityEngine;

public class FreezeTestManager : MonoBehaviour
{
    [SerializeField] private Transform _objectB;         // 테스트 대상 오브젝트 B 연결
    [SerializeField] private float _rotateSpeed = 0.5f;   // 마우스 드래그 감도

    private RevoluteJointComponent _jointComponent;      // 조인트 컴포넌트 (내부 연결)
    private float _currentTestAngle = 0f;                // 테스트용 현재 각도 저장

    private void Awake()
    {
        // _objectB가 할당되어 있다면 해당 오브젝트에서 컴포넌트를 가져옴
        if (_objectB != null)
        {
            _jointComponent = _objectB.GetComponent<RevoluteJointComponent>();

            if (_jointComponent == null)
            {
                Debug.LogError($"{_objectB.name}에 RevoluteJointComponent가 없습니다.");
            }
        }
    }
        private void Start()
    {
        if (_jointComponent != null)
            _currentTestAngle = _jointComponent.CurrentAngle;
    }

    void Update()
    {
        // 오브젝트나 조인트 컴포넌트가 없으면 리턴
        if (_objectB == null || _jointComponent == null) return;

        // 마우스 왼쪽 버튼(0)을 누르고 있을 때
        if (Input.GetMouseButton(0))
        {
            // 마우스의 상하 움직임(Y)만 가져옵니다.
            float mouseY = Input.GetAxis("Mouse Y");

            // 마우스 위아래 움직임에 따라 각도를 가감합니다.
            // 위로 올리면(mouseY > 0) 각도 증가, 아래로 내리면 감소
            _currentTestAngle += mouseY * _rotateSpeed * 100f;

            // 조인트 컴포넌트에 계산된 각도를 전달합니다.
            _jointComponent.SetAngle(_currentTestAngle);

            // 실제 조인트에서 제한(Clamp)된 각도를 다시 가져와 동기화합니다.
            _currentTestAngle = _jointComponent.CurrentAngle;
        }
    }
}