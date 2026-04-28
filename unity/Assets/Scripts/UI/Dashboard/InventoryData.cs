// ============================================================
// 파일명  : InventoryData.cs
// 역할    : 재고 현황 테이블의 행 데이터를 담는 데이터 클래스
// 작성자  : 이현화
// 작성일  : 2026-04-14
// 수정이력:
// ============================================================

/// <summary>
/// @brief 재고 현황 한 행을 표현하는 데이터 클래스
/// </summary>
[System.Serializable]
public class InventoryData
{
    public string binId;       // Bin ID
    public string qrCode;      // QR 코드
    public string partName;    // 부품명
    public string category;    // 카테고리
    public int boxCount;       // BOX 수량
    public int boxQty;         // BOX당 제품 수량
    public int totalQty;       // 제품 총 수량
    public float weight;       // 총 중량(g)
    public string position;    // 위치
}