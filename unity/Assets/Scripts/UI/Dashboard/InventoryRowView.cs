// ============================================================
// 파일명  : InventoryRowView.cs
// 역할    : 재고현황 테이블의 행 하나를 표시하는 View 컴포넌트
// 작성자  : 이현화
// 작성일  : 2026-04-14
// 수정이력:
// ============================================================

using TMPro;
using UnityEngine;

public class InventoryRowView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _noText;        // 행 번호
    [SerializeField] private TextMeshProUGUI _binIdText;     // Bin ID
    [SerializeField] private TextMeshProUGUI _qrCodeText;    // QR 코드
    [SerializeField] private TextMeshProUGUI _partNameText;  // 부품명
    [SerializeField] private TextMeshProUGUI _categoryText;  // 카테고리
    [SerializeField] private TextMeshProUGUI _boxCountText;  // BOX 수량
    [SerializeField] private TextMeshProUGUI _boxQtyText;    // BOX당 제품 수량
    [SerializeField] private TextMeshProUGUI _totalQtyText;  // 제품 총 수량
    [SerializeField] private TextMeshProUGUI _weightText;    // 총 중량(g)
    [SerializeField] private TextMeshProUGUI _positionText;  // 위치

    /**
     * @brief 재고현황 행 데이터 설정
     * @param no   행 번호 (1-based)
     * @param data 표시할 재고 데이터
     */
    public void SetData(int no, InventoryData data)
    {
        _noText.text = no.ToString();
        _binIdText.text = data.binId;
        _qrCodeText.text = data.qrCode;
        _partNameText.text = data.partName;
        _categoryText.text = data.category;
        _boxCountText.text = data.boxCount.ToString();
        _boxQtyText.text = data.boxQty.ToString();
        _totalQtyText.text = data.totalQty.ToString();
        _weightText.text = data.weight.ToString();
        _positionText.text = data.position;
    }
}