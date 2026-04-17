// ============================================================
// 파일명  : TransactionRowView.cs
// 역할    : 입고/출고 내역 테이블의 행 하나를 표시하는 View 컴포넌트
// 작성자  : 이현화
// 작성일  : 2026-04-14
// 수정이력:
// ============================================================

using TMPro;
using UnityEngine;

public class TransactionRowView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _noText;               // 행 번호
    [SerializeField] private TextMeshProUGUI _transactionNoText;    // 입고/출고 번호
    [SerializeField] private TextMeshProUGUI _qrCodeText;           // QR 코드
    [SerializeField] private TextMeshProUGUI _partNameText;         // 부품명
    [SerializeField] private TextMeshProUGUI _categoryText;         // 카테고리
    [SerializeField] private TextMeshProUGUI _boxQtyText;           // BOX당 제품 수량
    [SerializeField] private TextMeshProUGUI _binIdText;            // Bin ID
    [SerializeField] private TextMeshProUGUI _statusText;           // 처리 상태 (완료 / 진행중 / 신규)
    [SerializeField] private TextMeshProUGUI _dateTimeText;         // 입고/출고 시각

    /**
     * @brief 입고/출고 행 데이터 설정
     * @param no   행 번호 (1-based)
     * @param data 표시할 입고/출고 데이터
     */
    public void SetData(int no, TransactionData data)
    {
        _noText.text = no.ToString();
        _transactionNoText.text = data.transactionNo;
        _qrCodeText.text = data.qrCode;
        _partNameText.text = data.partName;
        _categoryText.text = data.category;
        _boxQtyText.text = data.boxQty.ToString();
        _binIdText.text = data.binId;
        _statusText.text = data.status;
        _dateTimeText.text = data.dateTime;
    }
}