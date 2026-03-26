using UnityEngine;

/**
 * @brief  Shuttle 추상 클래스.
 */
public abstract class Shuttle
{
    private string _id;             // 셔틀의 고유식별자
    private Vector3Int _fromCell;    // 셔틀의 현재 셀 좌표 (X, Y, Z)
    private Vector3Int _homeCell;   // 셔틀의 홈 셀 좌표 (X, Y, Z)
    private int _liftLevel;   // 셔틀의 리프트 레벨 (Y 좌표)
    private bool _isTransferring; // 이송 중 여부
    private bool _isHeadingZ;     // x는 0번 인덱스, z는 2번 인덱스임을 따름

    public string Id { get => _id; }                     // 셔틀의 고유식별자
    public Vector3Int FromCell { get => _fromCell; set => _fromCell = value; }            // 셔틀의 현재 셀 좌표
    public Vector3Int HomeCell { get => _homeCell; set => _homeCell = value; }         // 셔틀의 홈 셀 좌표
    public int LiftLevel { get => _liftLevel; set => _liftLevel = value; }           // 셔틀의 리프트 레벨
    public bool IsTransferring { get => _isTransferring; set => _isTransferring = value; } // 이송 중 여부
    public bool IsHeadingZ { get => _isHeadingZ; set => _isHeadingZ = value; }         // x는 0번 인덱스, z는 2번 인덱스임을 따름

    public Shuttle(string id)
    {
        this._id = id;
        this._fromCell = new Vector3Int(-1, -1, -1);
        this._homeCell = _fromCell;
        this._liftLevel = _fromCell.y;
        this._isTransferring = false;
        this._isHeadingZ = false;
    }
}
