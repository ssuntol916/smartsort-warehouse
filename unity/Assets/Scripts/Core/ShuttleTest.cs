using UnityEngine;
using static BinTransfer;

/// <summary>
/// 디버그용 셔틀 클래스. 필드를 public 으로 임시구현.
/// </summary>
public class ShuttleTest : MonoBehaviour
{
    private string _id;             // 셔틀의 고유식별자
    private CellsInt _fromCell;    // 셔틀의 현재 셀 좌표 (X, Y, Z). 여기서 Z는 리프트의 위치
    private CellsInt _homeCell;   // 셔틀의 홈 셀 좌표 (X, Y, Z). 여기서 Z는 리프트의 위치
    private bool _isTransferring; // 이송 중 여부. Bin과 함께 관리 요망.
    private bool _isHeadingY;     // x는 0번 인덱스, y는 1번 인덱스임을 따름

    public string Id { get => _id; }                     // 셔틀의 고유식별자
    public CellsInt FromCell { get => _fromCell; set => _fromCell = value; }            // 셔틀의 현재 셀 좌표
    public CellsInt HomeCell { get => _homeCell; set => _homeCell = value; }         // 셔틀의 홈 셀 좌표
    public bool IsTransferring { get => _isTransferring; set => _isTransferring = value; } // 이송 중 여부
    public bool IsHeadingY { get => _isHeadingY; set => _isHeadingY = value; }         // x는 0번 인덱스, y는 1번 인덱스임을 따름

    public ShuttleTest(string id)
    {
        this._id = id;
        this._fromCell = CellsInt.na;
        this._homeCell = _fromCell;
        this._isTransferring = false;
        this._isHeadingY = false;
    }
}