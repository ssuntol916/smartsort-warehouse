using TMPro;
using UnityEngine;
using UnityEngine.UI;

/** 
 * @file    NotificationRowView.cs
 * @brief   알림 센터 Row UI 뷰
 *          알림 타입 / 메시지 / 시각 표시
 * @author  LHH-eng
 * @date    2026-04-14
 * @history 2026-04-14 최초 작성
 */
public class NotificationRowView : MonoBehaviour
{
    /** @brief 타입 태그 배경 */
    [SerializeField] private Image _typeTagImage;

    /** @brief 타입 텍스트 (에러/알림) */
    [SerializeField] private TextMeshProUGUI _typeText;

    /** @brief 알림 메시지 */
    [SerializeField] private TextMeshProUGUI _messageText;

    /** @brief 알림 시각 */
    [SerializeField] private TextMeshProUGUI _timeText;

    /** @brief Row 데이터 설정
     * @param type 알림 타입 (error/notification)
     * @param message 알림 내용
     * @param time 발생 시각 */
    public void SetData(string type, string message, string time)
    {
        _messageText.text = message;
        _timeText.text = time;

        if (type == "error")
        {
            _typeText.text = "에러";
            _typeTagImage.color = new Color32(231, 76, 60, 255);   // 빨강
        }
        else
        {
            _typeText.text = "알림";
            _typeTagImage.color = new Color32(243, 156, 18, 255);  // 노랑
        }
    }
}