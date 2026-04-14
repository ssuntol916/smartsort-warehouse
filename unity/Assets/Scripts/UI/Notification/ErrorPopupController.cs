using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/** 
 * @file    ErrorPopupController.cs
 * @brief   에러 팝업 UI 컨트롤러
 *          에러 발생 시 팝업 표시 및 확인 버튼 처리
 * @author  LHH-eng
 * @date    2026-04-13
 * @history 2026-04-13 최초 작성
 */
public class ErrorPopupController : MonoBehaviour
{
    /** @brief 에러 팝업 오브젝트 */
    [SerializeField] private GameObject _errorPopup;

    /** @brief 에러 메시지 텍스트 */
    [SerializeField] private TextMeshProUGUI _errorMessageText;

    /** @brief 확인 버튼 */
    [SerializeField] private Button _confirmBtn;

    /** @brief 상단 에러 바 Image */
    [SerializeField] private Image _errorBarImage;

    /** @brief 상단 에러 바 텍스트 */
    [SerializeField] private TextMeshProUGUI _errorBarText;

    /** @brief 초기화 */
    void Awake()
    {
        _confirmBtn.onClick.AddListener(OnConfirmBtnClicked);

        // 시작 시 팝업 숨기기
        _errorPopup.SetActive(false);

        // 시작 시 에러 바 투명하게
        _errorBarImage.color = new Color32(0, 0, 0, 0);
        _errorBarText.text = "";
    }

    /** @brief 에러 팝업 표시
     * @param message 에러 메시지 내용 */
    public void ShowError(string message)
    {
        _errorMessageText.text = message;
        _errorBarText.text = "⚠ " + message;
        _errorPopup.SetActive(true);
        _errorBarImage.color = new Color32(231, 76, 60, 255); // E74C3C
        _errorBarText.color = new Color32(255, 255, 255, 255); // FFFFFF
    }

    /** @brief 확인 버튼 클릭 - 팝업만 닫기, 에러 바 유지 */
    private void OnConfirmBtnClicked()
    {
        _errorPopup.SetActive(false);
    }

    /** @brief 에러 해결 시 에러 바 숨기기
     * TODO: MQTT / Supabase Realtime 연동으로 에러 해결 시 호출 */
    public void ClearError()
    {
        _errorBarImage.color = new Color32(0, 0, 0, 0);
        _errorBarText.text = "";
    }

    /** @brief 테스트용 - T키: 에러 팝업, C키: 에러 해제 */
    void Update()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame)
            ShowError("셔틀 이상 감지 - 즉시 확인 필요");

        if (Keyboard.current.cKey.wasPressedThisFrame)
            ClearError();
    }
}