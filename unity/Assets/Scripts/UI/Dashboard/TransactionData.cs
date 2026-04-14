// ============================================================
// 파일명  : TransactionData.cs
// 역할    : 입고/출고 내역 테이블의 행 데이터를 담는 데이터 클래스
// 작성자  : 이현화
// 작성일  : 2026-04-14
// 수정이력:
// ============================================================

/// <summary>
/// @brief 입고/출고 내역 한 행을 표현하는 데이터 클래스
/// </summary>
[System.Serializable]
public class TransactionData
{
    public string transactionNo; // 입고/출고 번호
    public string qrCode;        // QR 코드
    public string partName;      // 부품명
    public string category;      // 카테고리
    public int boxQty;           // BOX당 제품 수량
    public string binId;         // Bin ID
    public string status;        // 처리 상태 (완료 / 진행중 / 신규)
    public string dateTime;      // 입고/출고 시각
}