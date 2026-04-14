using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/** 
 * @file    NotificationController.cs
 * @brief   알림 센터 UI 컨트롤러
 *          알림 목록 표시 / 뱃지 카운트 / 열기닫기
 * @author  LHH-eng
 * @date    2026-04-14
 * @history 2026-04-14 최초 작성
 */
public class NotificationController : MonoBehaviour
{
    /** @brief 알림 센터 패널 */
    [SerializeField] private GameObject _notificationPanel;

    /** @brief 알림 버튼 뱃지 */
    [SerializeField] private GameObject _badge;
    [SerializeField] private TextMeshProUGUI _badgeText;

    /** @brief 닫기 버튼 */
    [SerializeField] private UnityEngine.UI.Button _closeBtn;

    /** @brief Row 프리팹 / Content */
    [SerializeField] private GameObject _notificationRowPrefab;
    [SerializeField] private Transform _content;

    /** @brief 에러 팝업 컨트롤러 */
    [SerializeField] private ErrorPopupController _errorPopupController;

    /** @brief 알림 데이터 */
    private List<(string type, string message, string time)> _notifications
        = new List<(string, string, string)>();

    /** @brief 미읽은 알림 수 */
    private int _unreadCount = 0;

    /** @brief 초기화 */
    void Awake()
    {
        _closeBtn.onClick.AddListener(ClosePanel);
        _notificationPanel.SetActive(false);
        UpdateBadge();
    }

    /** @brief 알림 추가
     * @param type 알림 타입 (error/notification)
     * @param message 알림 내용
     * @param time 발생 시각 */
    public void AddNotification(string type, string message, string time)
    {
        _notifications.Insert(0, (type, message, time));
        _unreadCount++;
        UpdateBadge();
        RefreshRows();

        // 에러면 팝업도 표시
        if (type == "error")
            _errorPopupController.ShowError(message);
    }

    /** @brief 알림 센터 열기/닫기 토글 */
    public void TogglePanel()
    {
        bool isActive = _notificationPanel.activeSelf;
        _notificationPanel.SetActive(!isActive);

        if (!isActive)
        {
            // 열면 읽음 처리
            _unreadCount = 0;
            UpdateBadge();
        }
    }

    /** @brief 알림 센터 닫기 */
    private void ClosePanel()
    {
        _notificationPanel.SetActive(false);
    }

    /** @brief 뱃지 카운트 업데이트 */
    private void UpdateBadge()
    {
        if (_unreadCount > 0)
        {
            _badge.SetActive(true);
            _badgeText.text = _unreadCount.ToString();
        }
        else
        {
            _badge.SetActive(false);
        }
    }

    /** @brief Row 갱신 */
    private void RefreshRows()
    {
        foreach (Transform child in _content)
            Destroy(child.gameObject);

        foreach (var notification in _notifications)
        {
            GameObject row = Instantiate(_notificationRowPrefab, _content);
            row.GetComponent<NotificationRowView>().SetData(
                notification.type,
                notification.message,
                notification.time);
        }
    }

    /** @brief 테스트용 - N키: 알림 추가, E키: 에러 알림 추가 */
    void Update()
    {
        if (Keyboard.current.nKey.wasPressedThisFrame)
            AddNotification("notification", "빈 A-1-1 가득 참 - 확인 필요", "14:30:25");

        if (Keyboard.current.eKey.wasPressedThisFrame)
            AddNotification("error", "셔틀 배터리 10% 이하", "14:35:00");
    }
}