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
    [SerializeField] private RectTransform _statusPanel;
    [SerializeField] private float _hiddenPosY = -390f;
    [SerializeField] private float _shownPosY = 0f;
    [SerializeField] private float _speed = 5f;
    private bool _isOpen = false;
    private bool _isMoving = false;

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

            if (Mathf.Abs(currentPos.y - targetY) < 0.01f)
            {
                // 목표 위치에 도달 → 고정
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
