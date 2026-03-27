using UnityEngine;

/**
 * @brief  Bin 추상 클래스. id로 생성
 * @param id Bin의 고유식별자
 * @param fromCell Bin 출발 셀 (X, Y, Z)
 * @param toCell Bin 목적 셀 (X, Y, Z)
 * @param onDuty 이송 중 여부
 */
public abstract class Bin
{
    private string _id;             // Bin의 고유식별자
    private Vector3Int _fromCell;   // Bin 출발 셀 (X, Y, Z)
    private Vector3Int _toCell;     // Bin 목적 셀 (X, Y, Z)
    private bool _onDuty;           // 이송 중 여부

    public string Id { get => _id; } // 읽기전용
    public Vector3Int FromCell { get => _fromCell; set => _fromCell = value; }
    public Vector3Int ToCell { get => _toCell; set => _toCell = value; }
    public bool OnDuty { get => _onDuty; set => _onDuty = value; }

    private Bin() { }    // 기본 생성자 숨김
    public Bin(string id)
    {
        _id = id;
        _fromCell = new Vector3Int(-1, -1, -1);
        _toCell = _fromCell;
        _onDuty = false;
    }
}
