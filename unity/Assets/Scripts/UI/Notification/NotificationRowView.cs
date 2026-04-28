// ============================================================
// 파일명  : NotificationRowView.cs
// 역할    : 알림 센터 Row UI 뷰
//           알림 타입 / 메시지 / 시각 표시
// 작성자  : 이현화
// 작성일  : 2026-04-14
// 수정이력: 
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NotificationRowView : MonoBehaviour
{
    // ───────────────────────────── 색상 상수 ─────────────────────────────
    private static readonly Color32 ColorError = new Color32(231, 76, 60, 255);           // 에러 태그 (#E74C3C)
    private static readonly Color32 ColorNotification = new Color32(243, 156, 18, 255);   // 알림 태그 (#F39C12)

    // ───────────────────────────── SerializeField ─────────────────────────────
    [SerializeField] private Image _typeTagImage;           // 타입 태그 배경
    [SerializeField] private TextMeshProUGUI _typeText;     // 타입 텍스트 (에러 / 알림)
    [SerializeField] private TextMeshProUGUI _messageText;  // 알림 메시지
    [SerializeField] private TextMeshProUGUI _timeText;     // 알림 시각

    // ───────────────────────────── 공개 메서드 ─────────────────────────────

    /**
     * @brief Row 데이터 설정
     * @param type    알림 타입 (error / notification)
     * @param message 알림 내용
     * @param time    발생 시각
     */
    public void SetData(string type, string message, string time)
    {
        _messageText.text = message;
        _timeText.text = time;

        if (type == "error")
        {
            _typeText.text = "에러";
            _typeTagImage.color = ColorError;
        }
        else
        {
            _typeText.text = "알림";
            _typeTagImage.color = ColorNotification;
        }
    }
}