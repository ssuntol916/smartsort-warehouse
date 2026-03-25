// ============================================================
// 파일명  : BinTransfer.cs
// 역할    : Bin 이송 알고리즘 클래스
// 작성자  : 이건호
// 작성일  : 260325
// 수정이력: 
// ============================================================

using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using static BinTransfer;
/// <summary>
/// 창고에 저장할 Bin 위주로 제어.
/// </summary>
/// <remarks>
/// 사용자 타입
/// <list type="bullet">
///     <item><term>CellsInt</term> 창고 전용 3차원 정수 좌표</item>
/// 구성
/// <list type="bullet">
///     <item><term> gridSize </term> const int</item>
///     <item><term> _binList </term> List&lt;<strong>Bin 클래스</strong>&gt;</item>
///     <item><term> _shuttleList </term> List&lt;<strong>Shuttle 클래스</strong>&gt;</item>
/// </list>
/// 메서드: 등록 및 해제
/// <list type="bullet">
///     <item><term>BinRegister(Bin클래스, int, int)</term> (X, Y, 맨 위)에 Bin 쌓아서 등록</item>
///     <item><term>BinRegister(Bin클래스, CellsInt)</term> Z 무시 (X, Y, 맨 위)에 Bin 쌓아서 등록</item>
///     <item><term>BinUnregister(string)</term> id 로 Bin 해제. 윗 Bin도 내린다.</item>
///     <item><term>BinUnregister(int, int)</term> (X, Y, 맨 위) Bin 해제.</item>
///     <item><term>BinUnregister(CellsInt)</term> (X, Y, Z) Bin 해제. 윗 Bin도 내린다.</item>
///     <item><term>ShuttleRegister(Shuttle클래스, int, int)</term> (X, Y)에 Shuttle 등록. HomeCell좌표가 됨.</item>
///     <item><term>ShuttleUnregister(string)</term> id 로 Shuttle 해제.</item>
/// </list>
/// 메서드: Warehouse 관리
/// <list type="bullet">
///     <item><term>TransferXY(int, int, int, int)</term> 출발 좌표 맨위 Bin을 목적 좌표 맨위로 옮김</item>
/// </list>
/// 메서드: Shuttle 제어
/// <list type="bullet">
///     <item><term>MoveShuttleToHome(Shuttle클래스)</term> 셔틀을 HomeCell 로 옮김</item>
/// </list>
/// 메서드: 유틸리티
/// <list type="bullet">
///     <item><term>FindMaxZ(int, int)</term> (X, Y) 의 맨 위 좌표 반환</item>
/// </list>
/// </list>
/// 외부타입
/// <list type="bullet">
///     <item><term>BinTest</term> Bin 클래스 임시구현</item>
///     <item><term>ShuttleTest</term> Shuttle 클래스 임시구현</item>
/// </list>
/// </remarks>
public class BinTransfer
{
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
        public static bool operator ==(CellsInt a, CellsInt b)
        {
            foreach (int idx in new int[] { 0, 1, 2 })
            {
                if (a[idx] != b[idx])
                    return false;
            }
            return true;
        }
        public static bool operator !=(CellsInt a, CellsInt b)
        {
            return !(a == b);
        }
        public static CellsInt zero => new CellsInt(0, 0, 0);
        public static CellsInt na => new CellsInt(-1, -1, -1);
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
        public static Vector3Int ConvertToVector3Int(CellsInt cells)
        {
            return new Vector3Int(cells.x, cells.z, cells.y);
        }
        public static CellsInt ConvertFromVector3Int(Vector3Int vec)
        {
            return new CellsInt(vec.x, vec.z, vec.y);
        }
    }
    // 그리드 크기 상수 (3 × 3 × 3)
    public const int gridSize = 3;
    
    List<BinTest> _binList;     // Bin 1개만 구현.
    List<ShuttleTest> _shuttleList;    // Bin 을 옮길 셔틀. 메서드에서는 값이 존재해야한다.

    // ============================================================
    // 등록 및 해제
    // ============================================================

    // 생성자
    /// <summary>
    /// 생성자. Bin과 Shuttle의 등록은 메서드를 통해 이루어진다.
    /// </summary>
    public BinTransfer() {}

    // Bin 등록/해제
    /// <summary>
    /// Bin 등록. 해당 (x, y) 셀의 가장 위쪽에 배치하고, 배치된 좌표를 알린다.
    /// </summary>
    /// <param name="bin">등록할 BinTest 인스턴스</param>
    /// <param name="initX">초기 X 좌표</param>
    /// <param name="initY">초기 Y 좌표</param>
    /// <returns>실제로 배치된 z 좌표</returns>
    /// <exception cref="InvalidOperationException">해당 셀이 가득 찼을 때</exception>
    /// <exception cref="IndexOutOfRangeException">좌표가 그리드 범위를 벗어날 때</exception>
    public CellsInt BinRegister(BinTest bin, int initX, int initY)
    {
        if (!IsInRangeXY(initX, initY))
            throw new IndexOutOfRangeException($"셀 ({initX}, {initY})의 좌표가 그리드 범위를 벗어나므로 BinRegister 불가");

        // z=gridSize-1 이 이미 존재하면 가득 찬 것이므로 등록 불가
        int maxZ = FindMaxZ(initX, initY);
        if (maxZ >= gridSize - 1)
            throw new InvalidOperationException($"셀 ({initX}, {initY})에 Bin이 가득 차 있으므로 BinRegister 불가");

        // 가장 위쪽에 배치
        int newZ = maxZ + 1;
        CellsInt placedCell = new CellsInt(initX, initY, newZ);
        bin.FromCell = placedCell;
        bin.ToCell = placedCell;
        _binList.Add(bin);

        Debug.Log($"[BinTransfer] Bin '{bin.Id}' 등록 완료 → 셀 ({initX}, {initY}, {newZ})에 배치됨");
        return placedCell;
    }

    /// <summary>
    /// Bin 등록 (CellsInt 오버로드). 해당 (x, y) 셀의 가장 위쪽에 배치하고, 배치된 좌표를 알린다.
    /// </summary>
    /// <param name="bin">등록할 BinTest 인스턴스</param>
    /// <param name="cell">초기 셀 좌표 (z는 무시되고 가장 위쪽에 배치)</param>
    /// <returns>실제로 배치된 셀 좌표</returns>
    /// <exception cref="InvalidOperationException">해당 셀이 가득 찼을 때</exception>
    /// <exception cref="IndexOutOfRangeException">좌표가 그리드 범위를 벗어날 때</exception>
    public CellsInt BinRegister(BinTest bin, CellsInt cell)
    {
        return BinRegister(bin, cell.x, cell.y);
    }

    /// <summary>
    /// 고유식별자 기반 Bin 해제. 해당 id의 Bin을 제거하고, 윗층 Bin을 한 층씩 내린다.
    /// </summary>
    /// <param name="id">Bin 고유식별자</param>
    /// <exception cref="ArgumentException">해당 id의 Bin이 없을 때</exception>
    /// <exception cref="InvalidOperationException">Bin이 이송 중일 때</exception>
    public void BinUnregister(string id)
    {
        BinTest bin = _binList.Find(b => b.Id == id);
        if (bin == null)
            throw new ArgumentException($"id '{id}'에 해당하는 Bin이 존재하지 않으므로 BinUnregister 불가");
        BinUnregisterInternal(bin);
    }

    /// <summary>
    /// (x, y) 셀의 가장 위쪽 Bin 해제. 제거 후 윗층 Bin을 한 층씩 내린다.
    /// </summary>
    /// <param name="x">X 좌표</param>
    /// <param name="y">Y 좌표</param>
    /// <exception cref="ArgumentException">해당 셀에 Bin이 없을 때</exception>
    /// <exception cref="InvalidOperationException">Bin이 이송 중일 때</exception>
    public void BinUnregister(int x, int y)
    {
        // 해당 셀의 가장 위쪽 Bin 찾기
        int maxZ = FindMaxZ(x, y);
        if (maxZ < 0)
            throw new ArgumentException($"셀 ({x}, {y})에 해당하는 Bin이 존재하지 않으므로 BinUnregister 불가");

        BinTest bin = _binList.Find(b =>
            b.FromCell.x == x &&
            b.FromCell.y == y &&
            b.FromCell.z == maxZ);
        BinUnregisterInternal(bin);
    }

    /// <summary>
    /// 좌표 기반 Bin 해제. 해당 셀에 존재하는 Bin을 제거하고, 윗층 Bin을 한 층씩 내린다.
    /// </summary>
    /// <param name="cell">해제할 Bin의 좌표 (x, y, z)</param>
    /// <exception cref="ArgumentException">해당 좌표에 Bin이 없을 때</exception>
    /// <exception cref="InvalidOperationException">Bin이 이송 중일 때</exception>
    public void BinUnregister(CellsInt cell)
    {
        BinTest bin = _binList.Find(b =>
            b.FromCell.x == cell.x &&
            b.FromCell.y == cell.y &&
            b.FromCell.z == cell.z);
        if (bin == null)
            throw new ArgumentException($"셀 ({cell.x}, {cell.y}, {cell.z})에 해당하는 Bin이 존재하지 않으므로 BinUnregister 불가");
        BinUnregisterInternal(bin);
    }

    /// <summary>
    /// Bin 해제 공통 로직. 이송 중 여부를 확인하고, 제거 후 윗층 Bin을 한 층씩 내린다.
    /// </summary>
    /// <param name="bin">해제할 BinTest 인스턴스</param>
    /// <exception cref="InvalidOperationException">Bin이 이송 중일 때</exception>
    private void BinUnregisterInternal(BinTest bin)
    {
        if (bin.IsTransferring)
            throw new InvalidOperationException("Bin이 이송 중이므로 BinUnregister 불가");
        CellsInt removedCell = bin.FromCell;
        _binList.Remove(bin);
        LowerUpperBins(removedCell);
    }

    // Shuttle 등록/해제
    /// <summary>
    /// Shuttle 등록. 해당 (x, y) 셀에 배치하고, 배치된 좌표를 알린다. z는 gridSize로 고정.
    /// </summary>
    /// <param name="shuttle">등록할 ShuttleTest 인스턴스</param>
    /// <param name="initX">초기 X 좌표</param>
    /// <param name="initY">초기 Y 좌표</param>
    /// <returns>실제로 배치된 셀 좌표</returns>
    /// <exception cref="InvalidOperationException">동일 id의 셔틀이 이미 등록되어 있거나, 해당 셀에 이미 셔틀이 존재할 때</exception>
    /// <exception cref="IndexOutOfRangeException">좌표가 그리드 범위를 벗어날 때</exception>
    public CellsInt ShuttleRegister(ShuttleTest shuttle, int initX, int initY)
    {
        if (!IsInRangeXY(initX, initY))
            throw new IndexOutOfRangeException($"셀 ({initX}, {initY})의 좌표가 그리드 범위를 벗어나므로 ShuttleRegister 불가");
        if (_shuttleList.Exists(s => s.Id == shuttle.Id))
            throw new InvalidOperationException($"id '{shuttle.Id}'가 중복되므로 ShuttleRegister 불가");
        CellsInt placedCell = new CellsInt(initX, initY, gridSize);
        if (_shuttleList.Exists(s =>
            s.FromCell.x == placedCell.x &&
            s.FromCell.y == placedCell.y))
            throw new InvalidOperationException($"셀 ({initX}, {initY})에 이미 셔틀이 존재하므로 ShuttleRegister 불가");

        shuttle.FromCell = placedCell;
        shuttle.HomeCell = placedCell;
        _shuttleList.Add(shuttle);

        Debug.Log($"[BinTransfer] Shuttle '{shuttle.Id}' 등록 완료 → 셀 ({initX}, {initY})에 배치됨");
        return placedCell;
    }
    
    /// <summary>
    /// 고유식별자 기반 Shuttle 해제. 해당 id의 Shuttle을 _shuttleList에서 제거한다.
    /// </summary>
    /// <param name="id">Shuttle 고유식별자</param>
    /// <exception cref="InvalidOperationException">_shuttleList가 비어 있을 때</exception>
    /// <exception cref="ArgumentException">해당 id의 Shuttle이 없을 때</exception>
    /// <exception cref="InvalidOperationException">Shuttle이 이송 중일 때</exception>
    public void ShuttleUnregister(string id)
    {
        ShuttleTest shuttle = _shuttleList.Find(s => s.Id == id);
        if (shuttle == null)
            throw new ArgumentException($"id '{id}'에 해당하는 Shuttle이 존재하지 않으므로 ShuttleUnregister 불가");
        if (shuttle.IsTransferring)
            throw new InvalidOperationException($"Shuttle '{id}'이 이송 중이므로 ShuttleUnregister 불가");

        _shuttleList.Remove(shuttle);
        Debug.Log($"[BinTransfer] Shuttle '{id}' 해제 완료");
    }

    // ============================================================
    // Warehouse 관리
    // ============================================================
    /// <summary>
    /// (fromX, fromY)의 가장 위쪽 Bin을 (toX, toY)의 가장 위쪽으로 이송한다.
    /// </summary>
    /// <param name="fromX">출발 X 좌표</param>
    /// <param name="fromY">출발 Y 좌표</param>
    /// <param name="toX">목적 X 좌표</param>
    /// <param name="toY">목적 Y 좌표</param>
    /// <exception cref="IndexOutOfRangeException">좌표가 그리드 범위를 벗어날 때</exception>
    /// <exception cref="ArgumentException">출발 셀에 Bin이 없을 때</exception>
    /// <exception cref="InvalidOperationException">목적 셀이 가득 찼을 때</exception>
    public CellsInt TransferXY(int fromX, int fromY, int toX, int toY)
    {
        // 좌표 검사
        if (!IsInRangeXY(fromX, fromY))
            throw new IndexOutOfRangeException($"출발 셀 ({fromX}, {fromY})의 좌표가 그리드 범위를 벗어나므로 TransferXY 불가");
        if (!IsInRangeXY(toX, toY))
            throw new IndexOutOfRangeException($"목적 셀 ({toX}, {toY})의 좌표가 그리드 범위를 벗어나므로 TransferXY 불가");

        // 출발지 Bin 검사
        int fromMaxZ = FindMaxZ(fromX, fromY);
        if (fromMaxZ < 0)
            throw new ArgumentException($"출발 셀 ({fromX}, {fromY})에 Bin이 존재하지 않으므로 TransferXY 불가");

        // 목적지 가득 찬지 확인
        int toMaxZ = FindMaxZ(toX, toY);
        if (toMaxZ >= gridSize - 1)
            throw new InvalidOperationException($"목적 셀 ({toX}, {toY})에 Bin이 가득 차 있으므로 TransferXY 불가");

        // 출발지, 출발지 Bin, 목적지 확인
        CellsInt fromCell = new CellsInt(fromX, fromY, fromMaxZ);
        CellsInt toCell = new CellsInt(toX, toY, toMaxZ + 1);

        // Bin, Shuttle 획득. 작업중 체크.
        BinTest bin = _binList.Find(b =>
            b.FromCell.x == fromCell.x &&
            b.FromCell.y == fromCell.y &&
            b.FromCell.z == fromCell.z);

        bin.ToCell = toCell;
        bin.IsTransferring = true;

        ShuttleTest shuttle = FindNearShuttle(fromCell);

        // 동작: 출발지이동 → 리프팅 → 목적지이동 → 하강
        MoveShuttleToCell(shuttle, fromCell);
        LiftBin(shuttle);
        MoveShuttleToCell(shuttle, toCell);
        LowerBin(shuttle);
        
        // Bin 좌표 갱신. Shuttle 해제
        bin.FromCell = toCell;
        bin.IsTransferring = false;

        MoveShuttleToHome(shuttle);
        
        shuttle.IsTransferring = false;

        return toCell;
    }

    /**
     * @brief  사용할 수 있는 가까운 셔틀을 할당한다.
    */
    private ShuttleTest FindNearShuttle(CellsInt cell)
    {
        ShuttleTest nearest = null;
        int minDist = int.MaxValue;

        foreach (ShuttleTest s in _shuttleList)
        {
            if (s.IsTransferring)
                continue;

            int dist = Math.Abs(s.FromCell.x - cell.x) + Math.Abs(s.FromCell.y - cell.y);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = s;
            }
        }

        if (nearest == null)
            throw new InvalidOperationException("사용 가능한 Shuttle이 없으므로 FindNearShuttle 불가");

        nearest.IsTransferring = true;
        return nearest;
    }

    // ============================================================
    // Shuttle 제어
    // ============================================================
    // 좌표이동, 방향전환, 픽업, 적치
    // ============================================================
    /// <summary>
    /// Shuttle을 해당 셀의 (x, y) 좌표로 이동시킨다. z는 무시된다.
    /// 정밀도를 위해 기본 7회까지 반복한다.
    /// </summary>
    /// <param name="shuttle">이동시킬 ShuttleTest 인스턴스</param>
    /// <param name="cell">목적 셀 좌표</param>
    /// <param name="rep">재귀 깊이 (기본 7)</param>
    /// <exception cref="InvalidOperationException">재귀 횟수 초과 시</exception>
    private void MoveShuttleToCell(ShuttleTest shuttle, CellsInt cell, int rep = 7)
    {
        while (true)
        {
            if (rep <= 0)
                throw new InvalidOperationException("MoveShuttleToCell: Shuttle이 이동에 방해를 받고 있음");

            CellsInt targetXY = new CellsInt(cell.x, cell.y, shuttle.FromCell.z);
            CellsInt path = targetXY - shuttle.FromCell;

            // 이미 목적지에 도착한 경우
            if (path == CellsInt.zero)
                return;

            CellsInt arrivedCell = new CellsInt(cell.x, cell.y, gridSize);
            if (shuttle.IsHeadingY && path.y != 0)
            {
                MoveShuttleOnYDrive(shuttle, path.y);
                MoveShuttleOnXDrive(shuttle, path.x);
                shuttle.FromCell = arrivedCell; // 좌표 갱신. 위치센서 가 있는 경우 수정바람.
                return;
            }
            else if (!shuttle.IsHeadingY && path.x != 0)
            {
                MoveShuttleOnXDrive(shuttle, path.x);
                MoveShuttleOnYDrive(shuttle, path.y);
                shuttle.FromCell = arrivedCell; // 좌표 갱신. 위치센서 가 있는 경우 수정바람.
                return;
            }
            else
            {
                // 현재 heading 방향의 경로가 0이므로 방향 전환 후 재시도
                SwitchDirection(shuttle);
                rep--;
            }
        }
    }
    /**
     * @brief  X·Y 바퀴 접지를 무조건 전환한다.
     */
    private void SwitchDirection(ShuttleTest shuttle)
    {
        shuttle.IsHeadingY = !shuttle.IsHeadingY;
        SwitchDirectionDrive(shuttle);
    }

    /**
     * @brief  스풀/벨트를 감아올려 목적 층의 Bin을 리프팅한다.
     * @param  shuttle  리프팅을 수행할 ShuttleTest 인스턴스
     */
    private void LiftBin(ShuttleTest shuttle, int rep = 7)
    {
        // Shuttle의 리프트가 제자리에 있는지 확인
        if (shuttle.FromCell.z != gridSize)
            throw new InvalidOperationException("Shuttle의 리프트가 제자리에 있지 않음");

        int maxZ = FindMaxZ(shuttle.FromCell.x, shuttle.FromCell.y);
        if (maxZ < 0 ||  maxZ >= gridSize)
            throw new InvalidOperationException("LiftBin: Shuttle이 위치한 셀에 Bin이 존재하지 않거나, 해당 셀 데이터 오류");

        // 하강: Shuttle의 z값을 maxZ로 맞춘다.
        int descendRep = rep;
        while (true)
        {
            if (descendRep <= 0)
                throw new InvalidOperationException("LiftBin 하강에 방해를 받고 있음");
            // 하강완료.
            if (shuttle.FromCell.z == maxZ)
                break;

            int descendDelta = maxZ - shuttle.FromCell.z;
            LiftDrive(shuttle, descendDelta);
            shuttle.FromCell = new CellsInt(shuttle.FromCell.x, shuttle.FromCell.y, maxZ);
            descendRep--;
        }

        // Bin 잡기
        GrabDrive(shuttle);

        // 상승: Shuttle의 z값을 gridSize로 맞춘다.
        int ascendRep = rep;
        while (true)
        {
            if (ascendRep <= 0)
                throw new InvalidOperationException("LiftBin 상승에 방해를 받고 있음");
            // 상승완료.
            if (shuttle.FromCell.z == gridSize)
                break;

            int ascendDelta = gridSize - shuttle.FromCell.z;
            LiftDrive(shuttle, ascendDelta);
            shuttle.FromCell = new CellsInt(shuttle.FromCell.x, shuttle.FromCell.y, gridSize);
            ascendRep--;
        }
    }
    /**
     * @brief  스풀/벨트를 풀어 Bin을 현재 층에 내려놓는다.
     * @param  shuttle  하강을 수행할 ShuttleTest 인스턴스
     */
    private void LowerBin(ShuttleTest shuttle, int rep = 7)
    {
        // Shuttle의 리프트가 제자리에 있는지 확인
        if (shuttle.FromCell.z != gridSize)
            throw new InvalidOperationException("Shuttle의 리프트가 제자리에 있지 않음");

        int maxZ = FindMaxZ(shuttle.FromCell.x, shuttle.FromCell.y);
        int targetZ = maxZ + 1;
        if (targetZ >= gridSize)
            throw new InvalidOperationException("LowerBin: Shuttle이 위치한 셀에 Bin이 가득 차 있거나, 해당 셀 데이터 오류");

        // 하강: Shuttle의 z값을 targetZ로 맞춘다.
        int descendRep = rep;
        while (true)
        {
            if (shuttle.FromCell.z == targetZ)
                break;
            if (descendRep <= 0)
                throw new InvalidOperationException("LowerBin 하강에 방해를 받고 있음");

            int descendDelta = targetZ - shuttle.FromCell.z;
            LiftDrive(shuttle, descendDelta);
            shuttle.FromCell = new CellsInt(shuttle.FromCell.x, shuttle.FromCell.y, targetZ); // 좌표 갱신. 위치센서 가 있는 경우 수정바람.
            descendRep--;
        }

        // Bin 놓기
        ReleaseDrive(shuttle);

        // 상승: Shuttle의 z값을 gridSize로 맞춘다.
        int ascendRep = rep;
        while (true)
        {
            if (shuttle.FromCell.z == gridSize)
                break;
            if (ascendRep <= 0)
                throw new InvalidOperationException("LowerBin 상승에 방해를 받고 있음");

            int ascendDelta = gridSize - shuttle.FromCell.z;
            LiftDrive(shuttle, ascendDelta);
            shuttle.FromCell = new CellsInt(shuttle.FromCell.x, shuttle.FromCell.y, gridSize); // 좌표 갱신. 위치센서 가 있는 경우 수정바람.
            ascendRep--;
        }
    }

    /**
     * @brief  셔틀을 홈 포지션으로 복귀시킨다.
     *         엔드스톱 스위치 기준으로 캘리브레이션한다.
     */
    public void MoveShuttleToHome(ShuttleTest shuttle)
    {
        MoveShuttleToCell(shuttle, shuttle.HomeCell);
    }

    // ============================================================
    // 구동 메서드
    // ============================================================
    // X바퀴, Y바퀴, 크랭크축 회전, 리프트, 그리퍼
    // ============================================================

    private void MoveShuttleOnXDrive(ShuttleTest shuttle, int deltaX)
    {
        // TODO: 모터에 deltaX 값을 전달하여 X축 이동 명령.
        // 입력: 양수. 음수.
    }
    private void MoveShuttleOnYDrive(ShuttleTest shuttle, int deltaY)
    {
        // TODO: 모터에 deltaY 값을 전달하여 Y축 이동 명령
        // 입력: 양수. 음수.
    }
    private void SwitchDirectionDrive(ShuttleTest shuttle)
    {
        // TODO: 크랭크 축에 회전 명령을 전달하여 X·Y 바퀴 접지 전환
        // 입력: 펄스.
    }
    private void LiftDrive(ShuttleTest shuttle, int deltaZ)
    {
        // TODO: 리프트 모터에 deltaZ 값을 전달하여 Bin을 리프팅
        // 입력: 양수(상승). 음수(하강).
    }
    private void GrabDrive(ShuttleTest shuttle)
    {
        // TODO: 그리퍼에 명령을 전달하여 Bin을 집음
        // 입력: 펄스.
    }
    private void ReleaseDrive(ShuttleTest shuttle)
    {
        // TODO: 그리퍼에 명령을 전달하여 Bin을 놓음
        // 입력: 펄스.
    }
    // ============================================================
    // Utility 메서드
    // ============================================================
    /// <summary>
    /// 해당 (x, y) 좌표가 그리드 범위 내에 있는지 확인한다. (0 ≤ x, y ≤ gridSize - 1) z는 무시된다.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    private bool IsInRangeXY(int x, int y)
    {
        return x >= 0 && x <= gridSize - 1
            && y >= 0 && y <= gridSize - 1;
    }

    /// <summary>
    /// 해당 셀(X, Y)에 존재하는 Bin 중 가장 위쪽(Z가 가장 큰) Bin의 Z 좌표를 반환한다. 해당 셀에 Bin이 없으면 -1 반환.
    /// </summary>
    /// <param name="initX"></param>
    /// <param name="initY"></param>
    /// <returns></returns>
    public int FindMaxZ(int initX, int initY)
    {
        int maxZ = -1;
        foreach (BinTest b in _binList)
        {
            if (b.FromCell.x == initX && b.FromCell.y == initY && b.FromCell.z > maxZ)
                maxZ = b.FromCell.z;
        }

        return maxZ;
    }
    /// <summary>
    /// 제거된 셀의 윗층에 있는 Bin을 한 층씩 내린다. (z=0이 가장 밑층)
    /// </summary>
    /// <param name="removedCell">제거된 Bin이 있던 좌표</param>
    private void LowerUpperBins(CellsInt removedCell)
    {
        List<BinTest> upperBins = _binList.FindAll(b =>
            b.FromCell.x == removedCell.x &&
            b.FromCell.y == removedCell.y &&
            b.FromCell.z > removedCell.z);
        upperBins.Sort((a, b) => a.FromCell.z.CompareTo(b.FromCell.z));
        foreach (BinTest upperBin in upperBins)
        {
            CellsInt lowered = new CellsInt(upperBin.FromCell.x, upperBin.FromCell.y, upperBin.FromCell.z - 1);
            upperBin.FromCell = lowered;
            upperBin.ToCell = lowered;
        }
    }
}