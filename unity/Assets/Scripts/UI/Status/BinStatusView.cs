// ============================================================
// 파일명  : BinStatusView.cs
// 역할    : Bin 상태 표시 및 상세 팝업 제어
//           Bin이 없으면 Empty 이미지, Bin이 있으면 Basic+Filled 이미지와
//           부품명 텍스트를 표시한다.(Filled는 수량에 따라 fillAmount와 색상이 변함)
//           버튼 클릭 시 BinDetailPopup을 열어서 상세 정보를 표시한다.
// 작성자  : 이현화
// 작성일  : 2026-04-07
// 수정이력: 
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BinStatusView : MonoBehaviour
{
    [Header("UI 이미지")]
    [SerializeField] private Image _emptyImage;              // Bin 없을 때 표시
    [SerializeField] private Image _basicImage;              // Bin 있을 때 배경 이미지
    [SerializeField] private Image _filledImage;             // 수량에 따라 fillAmount·색상 변경
    [SerializeField] private TextMeshProUGUI _partNameText;  // 부품명 텍스트

    [Header("Bin 데이터")]
    [SerializeField] private bool _hasBin;                   // Bin 존재 여부
    [SerializeField] private string _partName;               // 부품명
    [SerializeField] private int _qty;                       // 수량
    [SerializeField] private int _unitWeight;                // 개당 중량 (g)
    [SerializeField] private int _maxWeight = 2000;          // 최대 총중량 (g)

    [Header("셀 좌표")]
    [SerializeField] private int _posX;
    [SerializeField] private int _posY;
    [SerializeField] private int _posZ;

    [Header("팝업")]
    [SerializeField] private BinDetailPopup _popup;  // 팝업 참조

    private int TotalWeight => _qty * _unitWeight;                  // 현재 총중량 (수량 × 개당중량, g)
    private float FillRatio => (float)TotalWeight / _maxWeight;  // 현재 중량 비율 (0~1)

    //TODO: Supabase Realtime 구독 시 데이터 변경 이벤트로 UpdateView() 호출로 교체
    /**
     * @brief  Bin 상태에 따라 초기 UI 요소를 설정한다.
     */
    void Start()
    {
        UpdateView();
    }

#if UNITY_EDITOR
    /**
     * @brief  에디터에서 Inspector 값 변경 시 UI를 즉시 갱신한다. (에디터 전용)
     */
    private void OnValidate()
    {
        UpdateView();
    }
#endif

    /**
     * @brief  Bin 상태에 따라 UI 요소를 업데이트한다.
     */
    public void UpdateView()
    {
        if (!_hasBin)
        {
            // Bin 없음
            _emptyImage.gameObject.SetActive(true);
            _basicImage.gameObject.SetActive(false);
            _filledImage.gameObject.SetActive(false);
            _partNameText.gameObject.SetActive(false);
        }
        else
        {
            // Bin 있음 — Basic이 Empty를 가려주니까 Empty는 그냥 둬도 됨
            _basicImage.gameObject.SetActive(true);
            _filledImage.gameObject.SetActive(true);
            _partNameText.gameObject.SetActive(true);

            // 부품명 텍스트 설정
            _partNameText.text = _partName;

            // fillAmount: 중량 비율에 따라 0.1~0.8 범위로 설정
            _filledImage.fillAmount = Mathf.Lerp(0.1f, 0.8f, FillRatio);

            // color: 중량 비율에 따라 파랑→빨강으로 Lerp
            _filledImage.color = Color.Lerp(new Color(70 / 255f, 120 / 255f, 190 / 255f, 230 / 255f),   // 수량 0일 때 색상
                                            new Color(200 / 255f, 80 / 255f, 80 / 255f, 220 / 255f),    // 수량 MAX일 때 색상
                                            FillRatio);  
        }
    }

    //TODO: 버튼 클릭 시 Supabase Realtime 구독으로 받은 데이터로 SetData() 호출로 교체
    /**
     * @brief  버튼 클릭 시 팝업에 데이터를 전달하고 열린다.
     */
    public void OnClick()
    {
        _popup.SetData(
            _partName, 
            "bolt",
            _qty,                    // 실제 수량
            TotalWeight,             // 현재 총중량 (g) (bin 중량 제외)
            _unitWeight,             // 개당 중량 (g)
            (int)(FillRatio * 100),  // Bin 상태 (%)
            "BIN-001",
            "X:0, Y:0, Z:0",
            "SHUTTLE-01",
            "2026-04-06 10:30"
        );
        _popup.Open();
    }
}
