// ============================================================
// 파일명  : BinDetailPopup.cs
// 역할    : Bin 상세 정보 팝업 제어
//           버튼 클릭 시 열리며, SetData()로 전달받은 데이터를 표시한다.
//           드래그로 팝업 위치를 이동할 수 있다.
// 작성자  : 이현화
// 작성일  : 2026-04-07
// 수정이력: 
// ============================================================

using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class BinDetailPopup : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    [Header("UI 텍스트")]
    [SerializeField] private TextMeshProUGUI _partNameText;         // 부품명
    [SerializeField] private TextMeshProUGUI _categoryText;         // 카테고리
    [SerializeField] private TextMeshProUGUI _totalQtyText;         // 총 수량
    [SerializeField] private TextMeshProUGUI _totalWeightText;      // 총 중량 (bin 중량 제외)
    [SerializeField] private TextMeshProUGUI _unitWeightText;       // 단위중량
    [SerializeField] private TextMeshProUGUI _binStatusText;        // Bin 상태 (%)
    [SerializeField] private TextMeshProUGUI _binIdText;            // Bin ID
    [SerializeField] private TextMeshProUGUI _binPositionText;      // Bin 위치
    [SerializeField] private TextMeshProUGUI _shuttleIdText;        // 셔틀 ID
    [SerializeField] private TextMeshProUGUI _lastInboundTimeText;  // 최근 입고 시간

    // 드래그 이동
    private Vector2 _dragOffset;           // 드래그 시작 시 마우스와 팝업 간의 오프셋
    private RectTransform _rectTransform;  // 팝업 위치 이동에 사용하는 RectTransform

    /**
     * @brief  드래그 이동에 사용할 RectTransform을 초기화한다.
     */
    void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    /**
     * @brief  팝업을 활성화한다.
     */
    public void Open()
    {
        gameObject.SetActive(true);
    }

    /**
     * @brief  팝업을 비활성화한다.
     */
    public void Close()
    {
        gameObject.SetActive(false);
    }

    /**
     * @brief  StatusView에서 전달받은 데이터를 UI 요소에 표시한다.
     */
    public void SetData(string partName, string category, int totalQty,
                        int totalWeight, int unitWeight, int binStatus,
                        string binId, string binPosition, string shuttleId,
                        string lastInboundTime)
    {
        _partNameText.text = partName;
        _categoryText.text = category;
        _totalQtyText.text = totalQty.ToString();
        _totalWeightText.text = totalWeight.ToString();
        _unitWeightText.text = unitWeight.ToString();
        _binStatusText.text = binStatus.ToString() + "%";  // 퍼센트로 표시
        _binIdText.text = binId;
        _binPositionText.text = binPosition;
        _shuttleIdText.text = shuttleId;
        _lastInboundTimeText.text = lastInboundTime;
    }

    /**
     * @brief  Drag 시작 시, 마우스 위치와 팝업 위치 간의 오프셋을 계산하여 저장한다.
     */
    public void OnBeginDrag(PointerEventData eventData)
    {
        _dragOffset = _rectTransform.anchoredPosition - eventData.position;
    }

    /**
     * @brief  Drag 중에는 마우스 위치에 오프셋을 더한 위치로 팝업을 이동시킨다.
     */
    public void OnDrag(PointerEventData eventData)
    {
        _rectTransform.anchoredPosition = eventData.position + _dragOffset;
    }

}
