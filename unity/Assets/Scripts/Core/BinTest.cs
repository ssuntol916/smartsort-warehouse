using UnityEngine;
using static BinTransfer;

/// <summary>
/// 디버그용 Bin 클래스. 펌웨어가 없기 때문에 편의상 정보를 저장했다.
/// </summary>
/// <remarks>
/// 구성:
/// <list type="bullet">
///     <item><term>Id</term> </item>
///     <item><term>FromCell</term> </item>
///     <item><term>ToCell</term> </item>
///     <item><term>IsTransferring</term> </item>
///     <item><term>binGO</term> </item>
/// </list>
/// </remarks>
public class BinTest : MonoBehaviour
{
    private string _id;             // Bin의 고유식별자
    private CellsInt _fromCell;       // Bin 출발 셀 (X, Y, Z)
    private CellsInt _toCell;         // Bin 목적 셀 (X, Y, Z)
    private bool _isTransferring;       // 이송 중 여부


    public string Id { get => _id; }     // Bin 고유식별자
    public CellsInt FromCell { get => _fromCell; set => _fromCell = value; }            // Bin 출발 셀
    public CellsInt ToCell { get => _toCell; set => _toCell = value; }                 // Bin 목적 셀
    public bool IsTransferring { get => _isTransferring; set => _isTransferring = value; }       // Bin 이송 중 여부. 셔틀과 함께 관리 요망.

    private BinTest() { }    // 기본 생성자 숨김
    public BinTest(string id)
    {
        _id = id;
        _fromCell = CellsInt.na;
        _toCell = _fromCell;
        _isTransferring = false;
    }
}
