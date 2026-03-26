using UnityEngine;

/**
 * @brief  Bin 추상 클래스.
 */
public abstract class Bin
{
    private string _id;             // Bin의 고유식별자
    private Vector3Int _fromCell;       // Bin 출발 셀 (X, Y, Z)
    private Vector3Int _toCell;         // Bin 목적 셀 (X, Y, Z)
    private bool _isTransferring;       // 이송 중 여부

    public string Id { get => _id; }     // Bin 고유식별자
    public Vector3Int FromCell { get => _fromCell; set => _fromCell = value; }            // Bin 출발 셀
    public Vector3Int ToCell { get => _toCell; set => _toCell = value; }                 // Bin 목적 셀
    public bool IsTransferring { get => _isTransferring; set => _isTransferring = value; }       // Bin 이송 중 여부. 셔틀과 함께 관리 요망.

    private Bin() { }    // 기본 생성자 숨김
    public Bin(string id)
    {
        _id = id;
        _fromCell = new Vector3Int(-1, -1, -1);
        _toCell = _fromCell;
        _isTransferring = false;
    }
}
