// ============================================================
// 파일명  : TotalStatusView.cs
// 역할    : 전체 재고 현황 패널 제어
//           part별 총 중량과 수량을 Row 프리팹으로 동적 생성하여 표시한다.
// 작성자  : 이현화
// 작성일  : 2026-04-08
// 수정이력:
// ============================================================

using UnityEngine;
using TMPro;

public class TotalStatusView : MonoBehaviour
{
    [Header("Row 프리팹")]
    [SerializeField] private GameObject _rowPrefab;  // Row 프리팹(Resources 폴더에 존재)

    //TODO: Supabase 연동 시 실제 데이터로 교체
    /**
     * @brief  더미 데이터로 Row를 동적 생성하여 재고 현황 패널을 초기화한다.
     */
    void Start()
    {
        // 더미 데이터 (부품명, 총중량(g), 수량)
        var dummyData = new (string partName, int totalWeight, int qty)[]
        {
            ("Bolts",         500, 50),
            ("Nuts",          300, 30),
            ("Heat Sink",     800, 10),
            ("Bearing",       600, 20),
            ("Tack Switch",   100, 40),
            ("Timing Pulley", 200, 15),
        };

        foreach (var data in dummyData)
        {
            GameObject row = Instantiate(_rowPrefab, transform);

            var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
            texts[0].text = data.partName;
            texts[1].text = data.totalWeight + " g";
            texts[2].text = data.qty + " EA";
        }
    }
}