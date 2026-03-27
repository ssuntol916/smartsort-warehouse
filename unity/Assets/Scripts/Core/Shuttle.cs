using UnityEngine;

/**
 * @brief  Shuttle 추상 클래스.
 * @param id 셔틀의 고유식별자
 * @param fromCell 셔틀의 현재 셀 좌표 (X, Y, Z)
 * @param homeCell 셔틀의 홈 셀 좌표 (X, Y, Z)
 * @param liftLevel 셔틀의 리프트 레벨 (Y 좌표)
 * @param onDuty 이송 중 여부
 * @param isHeadingZ false 는 x, true는 z축 접지
 */
public abstract class Shuttle
{
    private string _id;             // 셔틀의 고유식별자
    private Vector3Int _fromCell;   // 셔틀의 현재 셀 좌표 (X, Y, Z)
    private Vector3Int _homeCell;   // 셔틀의 홈 셀 좌표 (X, Y, Z)
    private int _liftLevel;         // 셔틀의 리프트 레벨 (Y 좌표)
    private bool _onDuty;           // 이송 중 여부
    private bool _isHeadingZ;       // false 는 x, true는 z축 접지

    public string Id { get => _id; } // 읽기전용
    public Vector3Int FromCell { get => _fromCell; set => _fromCell = value; }
    public Vector3Int HomeCell { get => _homeCell; set => _homeCell = value; }
    public int LiftLevel { get => _liftLevel; set => _liftLevel = value; }
    public bool OnDuty { get => _onDuty; set => _onDuty = value; }
    public bool IsHeadingZ { get => _isHeadingZ; set => _isHeadingZ = value; }

    private Shuttle() { }    // 기본 생성자 숨김
    public Shuttle(string id)
    {
        this._id = id;
        this._fromCell = new Vector3Int(-1, -1, -1);
        this._homeCell = _fromCell;
        this._liftLevel = _fromCell.y;
        this._onDuty = false;
        this._isHeadingZ = false;
    }
}
