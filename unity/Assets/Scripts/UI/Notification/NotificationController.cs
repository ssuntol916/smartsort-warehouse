// ============================================================
// 파일명  : NotificationController.cs
// 역할    : 알림 센터 UI 컨트롤러
//           알림 목록 표시 / 뱃지 카운트 / 열기닫기
// 작성자  : 이현화
// 작성일  : 2026-04-14
// 수정이력: 
// ============================================================

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class NotificationController : MonoBehaviour
{
    // ───────────────────────────── SerializeField ─────────────────────────────
    [SerializeField] private GameObject _notificationPanel;                // 알림 센터 패널
    [SerializeField] private GameObject _badge;                            // 알림 버튼 뱃지 오브젝트
    [SerializeField] private TextMeshProUGUI _badgeText;                   // 뱃지 카운트 텍스트
    [SerializeField] private Button _closeBtn;                             // 닫기 버튼
    [SerializeField] private GameObject _notificationRowPrefab;            // 알림 Row 프리팹
    [SerializeField] private Transform _content;                           // 알림 목록 Content
    [SerializeField] private ErrorPopupController _errorPopupController;   // 에러 팝업 컨트롤러
    // ───────────────────────────── 런타임 데이터 ─────────────────────────────
    // TODO: Supabase Realtime 연동으로 교체
    private List<(string type, string message, string time)> _notifications
        = new List<(string, string, string)>();

    private int _unreadCount = 0;       // 미읽은 알림 수

    // ───────────────────────────── Unity 생명주기 ─────────────────────────────

    /**
     * @brief 초기화 - 알림 패널 숨김 상태로 시작
     */
    void Awake()
    {
        _closeBtn.onClick.AddListener(ClosePanel);
        _notificationPanel.SetActive(false);
        UpdateBadge();
    }

    /**
         * @brief 테스트용 키 입력 처리
         *        N키: 일반 알림 추가 / E키: 에러 알림 추가 / C키: 에러 바 해제
         * //TODO: Supabase 연동 후 제거
         */
    void Update()
    {
        if (Keyboard.current.nKey.wasPressedThisFrame)
            AddNotification("notification", "빈 A-1-1 가득 참 - 확인 필요", "14:30:25");

        if (Keyboard.current.eKey.wasPressedThisFrame)
            AddNotification("error", "셔틀 배터리 10% 이하", "14:35:00");

        if (Keyboard.current.cKey.wasPressedThisFrame)
            _errorPopupController.ClearError();
    }

    // ───────────────────────────── 공개 메서드 ─────────────────────────────

    /**
     * @brief 알림 추가 - 목록 최상단에 삽입, 에러면 팝업도 표시
     * @param type    알림 타입 (error / notification)
     * @param message 알림 내용
     * @param time    발생 시각
     */
    public void AddNotification(string type, string message, string time)
    {
        _notifications.Insert(0, (type, message, time));
        _unreadCount++;
        UpdateBadge();
        RefreshRows();

        if (type == "error" && _errorPopupController != null)
            _errorPopupController.ShowError(message);
    }

    /**
     * @brief 알림 센터 열기/닫기 토글
     *        열 때 미읽은 알림 읽음 처리
     */
    public void TogglePanel()
    {
        bool isActive = _notificationPanel.activeSelf;
        _notificationPanel.SetActive(!isActive);

        if (!isActive)
        {
            _unreadCount = 0;
            UpdateBadge();
        }
    }

    // ───────────────────────────── 내부 메서드 ─────────────────────────────

    /**
     * @brief 알림 센터 닫기
     */
    private void ClosePanel()
    {
        _notificationPanel.SetActive(false);
    }

    /**
     * @brief 뱃지 카운트 업데이트
     *        미읽은 알림 있으면 뱃지 표시, 없으면 숨김
     */
    private void UpdateBadge()
    {
        bool hasUnread = _unreadCount > 0;
        _badge.SetActive(hasUnread);
        if (hasUnread)
            _badgeText.text = _unreadCount.ToString();
    }

    /**
     * @brief 알림 Row 전체 갱신
     */
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
}