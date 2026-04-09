// ============================================================
// 파일명  : StatusPanelToggle.cs
// 역할    : Status Panel 표시/숨김 토글 제어
//           Toggle() 호출 시 패널 Y위치를 Lerp로 슬라이드한다.
// 작성자  : 이현화
// 작성일  : 2026-04-07
// 수정이력: 
// ============================================================

using UnityEngine;

public class StatusPanelToggle : MonoBehaviour
{
    [SerializeField] private RectTransform _statusPanel;    // 슬라이드할 패널
    [SerializeField] private float _hiddenPosY = -390f;     // 숨김 상태 Y 위치
    [SerializeField] private float _shownPosY = 0f;         // 표시 상태 Y 위치
    [SerializeField] private float _speed = 5f;             // Lerp 이동 속도

    private bool _isOpen = false;                           // 패널 열림 여부
    private bool _isMoving = false;                         // 패널 이동 중 여부

    /**
     * @brief  매 프레임 패널 Y위치를 목표값으로 Lerp 이동한다.
     */
    void Update()
    {
        if (_isMoving)
        {
            float targetY = _isOpen ? _shownPosY : _hiddenPosY;

            Vector2 currentPos = _statusPanel.anchoredPosition;

            float newY = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * _speed);
            _statusPanel.anchoredPosition = new Vector2(currentPos.x, newY);

            // 목표 위치에 충분히 가까워지면 딱 고정하고 이동 중지
            // Lerp는 목표값에 완전히 도달하지 못하므로 임계값(0.01f) 이내면 강제 고정
            // anchoredPosition: 부모 오브젝트 기준 상대 좌표 (Canvas UI 위치 제어에 사용)
            if (Mathf.Abs(currentPos.y - targetY) < 0.01f)
            {
                _isMoving = false;
                _statusPanel.anchoredPosition = new Vector2(currentPos.x, targetY);
            }

        }
    }

    /**
     * @brief  이동 중일 때만 패널 Y위치를 목표값으로 Lerp 이동한다.
     *         목표 위치 도달 시 위치를 고정하고 이동을 멈춘다.
     */
    public void Toggle()
    {
        _isOpen = !_isOpen;  // true ↔ false 전환
        _isMoving = true;
    }
}
