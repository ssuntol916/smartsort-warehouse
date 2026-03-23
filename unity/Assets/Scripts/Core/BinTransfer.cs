// ============================================================
// 파일명  : BinTransfer.cs
// 역할    : Bin 이송 알고리즘 클래스
// 작성자  : 
// 작성일  : 
// 수정이력: 
// ============================================================

using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using static BinTransfer;
/// <summary>
/// 창고에 저장할 Bin 위주로 제어. Unity 좌표계를 따르지 않음.
/// </summary>
public class BinTransfer
{
    // 그리드 크기 상수 (3 × 3 × 3)
    public const int GridSize = 3;

    private CellsInt _fromCell;       // Bin 출발 셀 (X, Y, Z)
    private CellsInt _toCell;         // Bin 목적 셀 (X, Y, Z)
    private bool _isTransferring;       // 이송 중 여부

    public CellsInt FromCell => _fromCell;            // Bin 출발 셀
    public CellsInt ToCell => _toCell;                // Bin 목적 셀
    public bool IsTransferring => _isTransferring;      // Bin 이송 중 여부. 셔틀과 함께 관리 요망.

    public Shuttle? shuttle;    // Bin 을 옮길 셔틀. 메서드에서는 값이 존재해야한다.

    // Bin 1개에 대한 이송만 구현되므로 Bin 클래스 추가 요망.
    public BinTransfer(int initX, int initY)
    {
        _fromCell = new CellsInt(initX, initY, 0);
        _toCell = _fromCell;
        _isTransferring = false;
    }

    /**
     * @brief  출발 셀에서 목적 셀까지 Bin을 이송한다.
     *         셔틀 이동 → Bin 리프팅 → 목적 셀 이동 → Bin 하강 순서로 동작한다.
     *         출발·목적 셀이 그리드 범위를 벗어난 경우 처리하지 않는다.
     * @param  fromCell  Bin 출발 셀 좌표 (X, Y, Z)
     * @param  toCell    Bin 목적 셀 좌표 (X, Y, Z)
     */
    public void Transfer(CellsInt fromCell, CellsInt toCell)
    {
        if(!IsInRange(fromCell))
            throw new IndexOutOfRangeException("출발 셀의 좌표가 그리드 범위를 벗어나므로 Transfer 불가\n출발 셀로 옮길 것.");
        if(!IsInRange(toCell))
            throw new IndexOutOfRangeException("목적 셀의 좌표가 그리드 범위를 벗어나므로 Transfer 불가.");

        _fromCell = fromCell;
        _toCell = toCell;
        AssignShuttle();

        _isTransferring = true;
        MoveShuttleToCell(_fromCell);
        LiftBin();
        MoveShuttleToCell(_toCell);
        LowerBin();
        _isTransferring = false;

        MoveShuttleToHome();
        ReleaseShuttle();
    }

    /**
     * @brief  사용할 수 있는 셔틀을 할당한다. (임시구현)
    */
    private void AssignShuttle()
    {
        if (shuttle != null)
            throw new Exception("셔틀이 이미 할당되어 있음.");
        // => 만약 작업 가능한 셔틀이 없다면 예외를 던진다.

        this.shuttle = new Shuttle();
        shuttle.isTransferring = false;
    }
    /**
     * @brief  셔틀을 반환한다. (임시구현)
     */
    private void ReleaseShuttle()
    {
        if (shuttle == null)
            throw new Exception("셔틀이 할당되어 있지 않음.");
        if (shuttle.isTransferring)
            throw new InvalidOperationException("할당된 로봇이 작업중이므로 ReleaseShuttle 불가");

        this.shuttle = null;
    }

    /**
     * @brief  셔틀을 목적 셀의 X·Y 좌표로 이동시킨다.
     *         X → 방향 전환 → Y 순서로 이동한다.
     * @param  targetCell  목적 셀의 X·Y 좌표
     */
    private void MoveShuttleToCell(CellsInt cell)
    {
        if (!IsInRange(cell))
            throw new IndexOutOfRangeException("창고 셀의 범위를 벗어나므로 MoveShuttleToCell 불가");

        shuttle.isTransferring = true;

        // X 방향 이동
        SwitchDirection(shuttle, false);
        // => X 방향 이동 구현 필요.

        // 방향 전환 후 Y 방향 이동
        SwitchDirection(shuttle, true);
        // => Y 방향 이동 구현 필요.

        shuttle.curCell = cell;
        shuttle.isTransferring = false;
    }

    /**
     * @brief  크랭크 임펠러를 회전시켜 X·Y 바퀴 접지를 전환한다.
     */
    private void SwitchDirection(Shuttle shuttle, bool headingTo)
    {
        if (shuttle.isHeadingY == headingTo)
        {
            return;
        }
        else
        {
            SwitchDirectionImplement(shuttle);
            shuttle.isHeadingY = !shuttle.isHeadingY;
        }
    }
    private void SwitchDirection(Shuttle shuttle)
    {
        shuttle.isHeadingY = !shuttle.isHeadingY;
        SwitchDirectionImplement(shuttle);
    }
    /**
     * 셔틀이 방향전환하는 동작. Unity 상 움직임만 구현. 펌웨어의 명령전달 제외.
     */
    private void SwitchDirectionImplement(Shuttle shuttle)
    {

    }

    /**
     * @brief  스풀/벨트를 감아올려 목적 층의 Bin을 리프팅한다.
     * @param  layer  리프팅할 층 번호 (0 ~ GridSize - 1)
     */
    private void LiftBin()
    {

    }

    /**
     * @brief  스풀/벨트를 풀어 Bin을 현재 층에 내려놓는다.
     */
    private void LowerBin()
    {

    }

    /**
     * @brief  셔틀을 홈 포지션(0, 0, 0)으로 복귀시킨다.
     *         엔드스톱 스위치 기준으로 캘리브레이션한다.
     */
    public void MoveShuttleToHome()
    {
        if (shuttle == null)
            throw new Exception("셔틀이 할당되어 있지 않음. MoveShuttleToHome 불가.");
        if (_isTransferring)
            throw new InvalidOperationException("Bin 이송 중이므로 MoveShuttleToHome 불가");
        MoveShuttleToCell(shuttle.homeCell);
    }

    /**
     * @brief  셀 좌표가 그리드 범위(0 ~ GridSize - 1) 이내인지 검증한다. 층고는 생각하지 않는다.
     * @param  cell  검증할 셀 좌표 (X, Y, Z)
     * @return bool  범위 이내이면 true, 아니면 false
     */
    private bool IsInRange(CellsInt cell)
    {
        for (int idx = 0; idx < 2; idx++)
        {
            if (cell[idx] < 0 || cell[idx] > GridSize-1)
                return false;
        }
        return true;
    }
    /// <summary>
    /// 창고 내부에 사용할 좌표체계. (x: 행, y: 열, z: 층)
    /// </summary>
    public struct CellsInt
    {
        public int x;
        public int y;
        public int z;

        public CellsInt(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public static CellsInt operator +(CellsInt a, CellsInt b)
        {
            return new CellsInt(a.x + b.x, a.y + b.y, a.z + b.z);
        }
        public static CellsInt operator -(CellsInt a, CellsInt b)
        {
            return new CellsInt(a.x - b.x, a.y - b.y, a.z - b.z);
        }
        public static CellsInt operator *(int r, CellsInt a)
        {
            return new CellsInt(r * a.x, r * a.y, r * a.z);
        }
        public static CellsInt zero => new CellsInt(0, 0, 0);
        public int this[int idx]
        {
            get
            {
                return idx switch
                {
                    0 => x,
                    1 => y,
                    2 => z,
                    _ => throw new IndexOutOfRangeException("CellsInt 인덱스는 0, 1, 2만 허용")
                };
            }
        }
    }

    /// <summary>
    /// 디버그용 셔틀 클래스. 필드를 public 으로 임시구현.
    /// </summary>
    public class Shuttle
    {
        public CellsInt curCell;    // 셔틀의 현재 셀 좌표 (X, Y, Z). 여기서 Z는 의미없으나 셀좌표와 연산을 위해 남겨둔다.
        public CellsInt homeCell;   // 셔틀의 홈 셀 좌표 (X, Y, Z). 여기서 Z는 의미없으나 셀좌표와 연산을 위해 남겨둔다.
        public bool isTransferring; // 이송 중 여부. Bin과 함께 관리 요망.
        public bool isHeadingY;     // x는 0번 인덱스, y는 1번 인덱스임을 따름
        public GameObject shuttleGO;

        public Shuttle()
        {
            curCell = CellsInt.zero;
            homeCell = curCell;
            isTransferring = false;
            isHeadingY = false;
        }
    }
}