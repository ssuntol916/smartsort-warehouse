// ============================================================
// 파일명  : ErrorPopupController.cs
// 역할    : 에러 팝업 UI 컨트롤러
//           에러 발생 시 팝업 표시 및 확인 버튼 처리
// 작성자  : 이현화
// 작성일  : 2026-04-14
// 수정이력: 
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ErrorPopupController : MonoBehaviour
{
    // ───────────────────────────── 색상 상수 ─────────────────────────────
    private static readonly Color32 ColorErrorBar = new Color32(231, 76, 60, 255); // 에러 바 배경 (#E74C3C)
    private static readonly Color32 ColorErrorBarText = new Color32(255, 255, 255, 255); // 에러 바 텍스트
    private static readonly Color32 ColorTransparent = new Color32(0, 0, 0, 0); // 투명

    // ───────────────────────────── SerializeField ─────────────────────────────
    [SerializeField] private GameObject _errorPopup;        // 에러 팝업 오브젝트
    [SerializeField] private TextMeshProUGUI _errorMessageText;  // 에러 메시지 텍스트
    [SerializeField] private Button _confirmBtn;        // 확인 버튼
    [SerializeField] private Image _errorBarImage;     // 상단 에러 바 Image
    [SerializeField] private TextMeshProUGUI _errorBarText;      // 상단 에러 바 텍스트

    // ───────────────────────────── Unity 생명주기 ─────────────────────────────

    /**
     * @brief 초기화 - 팝업 및 에러 바 숨김 상태로 시작
     */
    void Awake()
    {
        _confirmBtn.onClick.AddListener(OnConfirmBtnClicked);
        _errorPopup.SetActive(false);
        _errorBarImage.color = ColorTransparent;
        _errorBarText.text = "";
    }

    // ───────────────────────────── 공개 메서드 ─────────────────────────────

    /**
     * @brief 에러 팝업 및 에러 바 표시
     * @param message 에러 메시지 내용
     */
    public void ShowError(string message)
    {
        _errorMessageText.text = message;
        _errorBarText.text = "⚠ " + message;
        _errorBarText.color = ColorErrorBarText;
        _errorBarImage.color = ColorErrorBar;
        _errorPopup.SetActive(true);
    }

    /**
     * @brief 에러 해결 시 에러 바 숨기기
     * //TODO: MQTT / Supabase Realtime 연동으로 에러 해결 시 호출
     */
    public void ClearError()
    {
        _errorBarImage.color = ColorTransparent;
        _errorBarText.text = "";
    }

    // ───────────────────────────── 이벤트 핸들러 ─────────────────────────────

    /**
     * @brief 확인 버튼 클릭 - 팝업만 닫기, 에러 바 유지
     */
    private void OnConfirmBtnClicked()
    {
        _errorPopup.SetActive(false);
    }
}