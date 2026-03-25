// ============================================================
// 파일명  : BinTransfer.cs
// 역할    : Bin 이송 알고리즘 클래스
// 작성자  : 이건호
// 작성일  : 260325
// 수정이력: 
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
/**
 * @brief  창고에 저장할 Bin 위주로 제어.
 *
 * 구성
 * - gridSize : const int
 * - _binList : List<Bin 클래스>
 * - _shuttleList : List<Shuttle 클래스>
 *
 * 메서드: 등록 및 해제
 * - BinRegister(Bin클래스, int, int) — (X, Z, 맨 위)에 Bin 쌓아서 등록
 * - BinRegister(Bin클래스, Vector3Int) — Y 무시 (X, Z, 맨 위)에 Bin 쌓아서 등록
 * - BinUnregister(string) — id 로 Bin 해제. 윗 Bin도 내린다.
 * - BinUnregister(int, int) — (X, Z, 맨 위) Bin 해제.
 * - BinUnregister(Vector3Int) — (X, Y, Z) Bin 해제. 윗 Bin도 내린다.
 * - ShuttleRegister(Shuttle클래스, int, int) — (X, Z)에 Shuttle 등록. HomeCell좌표가 됨.
 * - ShuttleUnregister(string) — id 로 Shuttle 해제.
 *
 * 메서드: Warehouse 관리
 * - TransferXZ(int, int, int, int) — 출발 좌표 맨위 Bin을 목적 좌표 맨위로 옮김
 *
 * 메서드: Shuttle 제어
 * - MoveShuttleToHome(Shuttle클래스) — 셔틀을 HomeCell 로 옮김
 *
 * 메서드: 유틸리티
 * - FindMaxY(int, int) — (X, Z) 의 맨 위 좌표 반환
 *
 * 외부타입
 * - BinTest — Bin 클래스 임시구현
 * - ShuttleTest — Shuttle 클래스 임시구현
 */
public class BinTransfer
{
    // 그리드 크기 상수 (3 × 3 × 3)
    public const int gridSize = 3;
    
    List<BinTest> _binList;     // Bin 1개만 구현.
    List<ShuttleTest> _shuttleList;    // Bin 을 옮길 셔틀. 메서드에서는 값이 존재해야한다.

    // ============================================================
    // 등록 및 해제
    // ============================================================

    // 생성자
    /**
     * @brief  생성자. Bin과 Shuttle의 등록은 메서드를 통해 이루어진다.
     */
    public BinTransfer() {}

    // Bin 등록/해제
    /**
     * @brief  Bin 등록. 해당 (x, z) 셀의 가장 위쪽에 배치하고, 배치된 좌표를 알린다.
     * @param  bin   등록할 BinTest 인스턴스
     * @param  initX  초기 X 좌표
     * @param  initZ  초기 Z 좌표
     * @return 실제로 배치된 y 좌표
     * @throws InvalidOperationException  해당 셀이 가득 찼을 때
     * @throws IndexOutOfRangeException  좌표가 그리드 범위를 벗어날 때
     */
    public Vector3Int BinRegister(BinTest bin, int initX, int initZ)
    {
        if (!IsInRangeXZ(initX, initZ))
            throw new IndexOutOfRangeException($"셀 ({initX}, {initZ})의 좌표가 그리드 범위를 벗어나므로 BinRegister 불가");

        // y=gridSize-1 이 이미 존재하면 가득 찬 것이므로 등록 불가
        int maxY = FindMaxY(initX, initZ);
        if (maxY >= gridSize - 1)
            throw new InvalidOperationException($"셀 ({initX}, {initZ})에 Bin이 가득 차 있으므로 BinRegister 불가");

        // 가장 위쪽에 배치
        int newY = maxY + 1;
        Vector3Int placedCell = new Vector3Int(initX, newY, initZ);
        bin.FromCell = placedCell;
        bin.ToCell = placedCell;
        _binList.Add(bin);

        Debug.Log($"[BinTransfer] Bin '{bin.Id}' 등록 완료 → 셀 ({initX}, {newY}, {initZ})에 배치됨");
        return placedCell;
    }

    /**
     * @brief  Bin 등록 (Vector3Int 오버로드). 해당 (x, z) 셀의 가장 위쪽에 배치하고, 배치된 좌표를 알린다.
     * @param  bin   등록할 BinTest 인스턴스
     * @param  cell  초기 셀 좌표 (y는 무시되고 가장 위쪽에 배치)
     * @return 실제로 배치된 셀 좌표
     * @throws InvalidOperationException  해당 셀이 가득 찼을 때
     * @throws IndexOutOfRangeException  좌표가 그리드 범위를 벗어날 때
     */
    public Vector3Int BinRegister(BinTest bin, Vector3Int cell)
    {
        return BinRegister(bin, cell.x, cell.z);
    }

    /**
     * @brief  고유식별자 기반 Bin 해제. 해당 id의 Bin을 제거하고, 윗층 Bin을 한 층씩 내린다.
     * @param  id  Bin 고유식별자
     * @throws ArgumentException  해당 id의 Bin이 없을 때
     * @throws InvalidOperationException  Bin이 이송 중일 때
     */
    public void BinUnregister(string id)
    {
        BinTest bin = _binList.Find(b => b.Id == id);
        if (bin == null)
            throw new ArgumentException($"id '{id}'에 해당하는 Bin이 존재하지 않으므로 BinUnregister 불가");
        BinUnregisterInternal(bin);
    }

    /**
     * @brief  (x, z) 셀의 가장 위쪽 Bin 해제. 제거 후 윗층 Bin을 한 층씩 내린다.
     * @param  x  X 좌표
     * @param  z  Z 좌표
     * @throws ArgumentException  해당 셀에 Bin이 없을 때
     * @throws InvalidOperationException  Bin이 이송 중일 때
     */
    public void BinUnregister(int x, int z)
    {
        // 해당 셀의 가장 위쪽 Bin 찾기
        int maxY = FindMaxY(x, z);
        if (maxY < 0)
            throw new ArgumentException($"셀 ({x}, {z})에 해당하는 Bin이 존재하지 않으므로 BinUnregister 불가");

        BinTest bin = _binList.Find(b =>
            b.FromCell.x == x &&
            b.FromCell.z == z &&
            b.FromCell.y == maxY);
        BinUnregisterInternal(bin);
    }

    /**
     * @brief  좌표 기반 Bin 해제. 해당 셀에 존재하는 Bin을 제거하고, 윗층 Bin을 한 층씩 내린다.
     * @param  cell  해제할 Bin의 좌표 (x, y, z)
     * @throws ArgumentException  해당 좌표에 Bin이 없을 때
     * @throws InvalidOperationException  Bin이 이송 중일 때
     */
    public void BinUnregister(Vector3Int cell)
    {
        BinTest bin = _binList.Find(b =>
            b.FromCell.x == cell.x &&
            b.FromCell.y == cell.y &&
            b.FromCell.z == cell.z);
        if (bin == null)
            throw new ArgumentException($"셀 ({cell.x}, {cell.y}, {cell.z})에 해당하는 Bin이 존재하지 않으므로 BinUnregister 불가");
        BinUnregisterInternal(bin);
    }

    /**
     * @brief  Bin 해제 공통 로직. 이송 중 여부를 확인하고, 제거 후 윗층 Bin을 한 층씩 내린다.
     * @param  bin  해제할 BinTest 인스턴스
     * @throws InvalidOperationException  Bin이 이송 중일 때
     */
    private void BinUnregisterInternal(BinTest bin)
    {
        if (bin.IsTransferring)
            throw new InvalidOperationException("Bin이 이송 중이므로 BinUnregister 불가");
        Vector3Int removedCell = bin.FromCell;
        _binList.Remove(bin);
        LowerUpperBins(removedCell);
    }

    // Shuttle 등록/해제
    /**
     * @brief  Shuttle 등록. 해당 (x, z) 셀에 배치하고, 배치된 좌표를 알린다. y는 gridSize로 고정.
     * @param  shuttle  등록할 ShuttleTest 인스턴스
     * @param  initX    초기 X 좌표
     * @param  initZ    초기 Z 좌표
     * @return 실제로 배치된 셀 좌표
     * @throws InvalidOperationException  동일 id의 셔틀이 이미 등록되어 있거나, 해당 셀에 이미 셔틀이 존재할 때
     * @throws IndexOutOfRangeException  좌표가 그리드 범위를 벗어날 때
     */
    public Vector3Int ShuttleRegister(ShuttleTest shuttle, int initX, int initZ)
    {
        if (!IsInRangeXZ(initX, initZ))
            throw new IndexOutOfRangeException($"셀 ({initX}, {initZ})의 좌표가 그리드 범위를 벗어나므로 ShuttleRegister 불가");
        if (_shuttleList.Exists(s => s.Id == shuttle.Id))
            throw new InvalidOperationException($"id '{shuttle.Id}'가 중복되므로 ShuttleRegister 불가");
        Vector3Int placedCell = new Vector3Int(initX, gridSize, initZ);
        if (_shuttleList.Exists(s =>
            s.FromCell.x == placedCell.x &&
            s.FromCell.z == placedCell.z))
            throw new InvalidOperationException($"셀 ({initX}, {initZ})에 이미 셔틀이 존재하므로 ShuttleRegister 불가");

        shuttle.FromCell = placedCell;
        shuttle.HomeCell = placedCell;
        _shuttleList.Add(shuttle);

        Debug.Log($"[BinTransfer] Shuttle '{shuttle.Id}' 등록 완료 → 셀 ({initX}, {initZ})에 배치됨");
        return placedCell;
    }
    
    /**
     * @brief  고유식별자 기반 Shuttle 해제. 해당 id의 Shuttle을 _shuttleList에서 제거한다.
     * @param  id  Shuttle 고유식별자
     * @throws InvalidOperationException  _shuttleList가 비어 있을 때
     * @throws ArgumentException  해당 id의 Shuttle이 없을 때
     * @throws InvalidOperationException  Shuttle이 이송 중일 때
     */
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
    /**
     * @brief  (fromX, fromZ)의 가장 위쪽 Bin을 (toX, toZ)의 가장 위쪽으로 이송한다.
     * @param  fromX  출발 X 좌표
     * @param  fromZ  출발 Z 좌표
     * @param  toX    목적 X 좌표
     * @param  toZ    목적 Z 좌표
     * @throws IndexOutOfRangeException  좌표가 그리드 범위를 벗어날 때
     * @throws ArgumentException  출발 셀에 Bin이 없을 때
     * @throws InvalidOperationException  목적 셀이 가득 찼을 때
     */
    public Vector3Int TransferXZ(int fromX, int fromZ, int toX, int toZ)
    {
        // 좌표 검사
        if (!IsInRangeXZ(fromX, fromZ))
            throw new IndexOutOfRangeException($"출발 셀 ({fromX}, {fromZ})의 좌표가 그리드 범위를 벗어나므로 TransferXZ 불가");
        if (!IsInRangeXZ(toX, toZ))
            throw new IndexOutOfRangeException($"목적 셀 ({toX}, {toZ})의 좌표가 그리드 범위를 벗어나므로 TransferXZ 불가");

        // 출발지 Bin 검사
        int fromMaxY = FindMaxY(fromX, fromZ);
        if (fromMaxY < 0)
            throw new ArgumentException($"출발 셀 ({fromX}, {fromZ})에 Bin이 존재하지 않으므로 TransferXZ 불가");

        // 목적지 가득 찬지 확인
        int toMaxY = FindMaxY(toX, toZ);
        if (toMaxY >= gridSize - 1)
            throw new InvalidOperationException($"목적 셀 ({toX}, {toZ})에 Bin이 가득 차 있으므로 TransferXZ 불가");

        // 출발지, 출발지 Bin, 목적지 확인
        Vector3Int fromCell = new Vector3Int(fromX, fromMaxY, fromZ);
        Vector3Int toCell = new Vector3Int(toX, toMaxY + 1, toZ);

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
    private ShuttleTest FindNearShuttle(Vector3Int cell)
    {
        ShuttleTest nearest = null;
        int minDist = int.MaxValue;

        foreach (ShuttleTest s in _shuttleList)
        {
            if (s.IsTransferring)
                continue;

            int dist = Math.Abs(s.FromCell.x - cell.x) + Math.Abs(s.FromCell.z - cell.z);
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
    /**
     * @brief  Shuttle을 해당 셀의 (x, z) 좌표로 이동시킨다. y는 무시된다.
     *         정밀도를 위해 기본 7회까지 반복한다.
     * @param  shuttle  이동시킬 ShuttleTest 인스턴스
     * @param  cell     목적 셀 좌표
     * @param  rep      재귀 깊이 (기본 7)
     * @throws InvalidOperationException  재귀 횟수 초과 시
     */
    private void MoveShuttleToCell(ShuttleTest shuttle, Vector3Int cell, int rep = 7)
    {
        while (true)
        {
            if (rep <= 0)
                throw new InvalidOperationException("MoveShuttleToCell: Shuttle이 이동에 방해를 받고 있음");

            Vector3Int targetXZ = new Vector3Int(cell.x, shuttle.FromCell.y, cell.z);
            Vector3Int path = targetXZ - shuttle.FromCell;

            // 이미 목적지에 도착한 경우
            if (path == Vector3Int.zero)
                return;

            Vector3Int arrivedCell = new Vector3Int(cell.x, gridSize, cell.z);
            if (shuttle.IsHeadingZ && path.z != 0)
            {
                MoveShuttleOnZDrive(shuttle, path.z);
                MoveShuttleOnXDrive(shuttle, path.x);
                shuttle.FromCell = arrivedCell; // 좌표 갱신. 위치센서 가 있는 경우 수정바람.
                return;
            }
            else if (!shuttle.IsHeadingZ && path.x != 0)
            {
                MoveShuttleOnXDrive(shuttle, path.x);
                MoveShuttleOnZDrive(shuttle, path.z);
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
     * @brief  X·Z 바퀴 접지를 무조건 전환한다.
     */
    private void SwitchDirection(ShuttleTest shuttle)
    {
        shuttle.IsHeadingZ = !shuttle.IsHeadingZ;
        SwitchDirectionDrive(shuttle);
    }

    /**
     * @brief  스풀/벨트를 감아올려 목적 층의 Bin을 리프팅한다.
     * @param  shuttle  리프팅을 수행할 ShuttleTest 인스턴스
     */
    private void LiftBin(ShuttleTest shuttle, int rep = 7)
    {
        // Shuttle의 리프트가 제자리에 있는지 확인
        if (shuttle.FromCell.y != gridSize)
            throw new InvalidOperationException("Shuttle의 리프트가 제자리에 있지 않음");

        int maxY = FindMaxY(shuttle.FromCell.x, shuttle.FromCell.z);
        if (maxY < 0 ||  maxY >= gridSize)
            throw new InvalidOperationException("LiftBin: Shuttle이 위치한 셀에 Bin이 존재하지 않거나, 해당 셀 데이터 오류");

        // 하강: Shuttle의 y값을 maxY로 맞춘다.
        int descendRep = rep;
        while (true)
        {
            if (descendRep <= 0)
                throw new InvalidOperationException("LiftBin 하강에 방해를 받고 있음");
            // 하강완료.
            if (shuttle.FromCell.y == maxY)
                break;

            int descendDelta = maxY - shuttle.FromCell.y;
            LiftDrive(shuttle, descendDelta);
            shuttle.FromCell = new Vector3Int(shuttle.FromCell.x, maxY, shuttle.FromCell.z);
            descendRep--;
        }

        // Bin 잡기
        GrabDrive(shuttle);

        // 상승: Shuttle의 y값을 gridSize로 맞춘다.
        int ascendRep = rep;
        while (true)
        {
            if (ascendRep <= 0)
                throw new InvalidOperationException("LiftBin 상승에 방해를 받고 있음");
            // 상승완료.
            if (shuttle.FromCell.y == gridSize)
                break;

            int ascendDelta = gridSize - shuttle.FromCell.y;
            LiftDrive(shuttle, ascendDelta);
            shuttle.FromCell = new Vector3Int(shuttle.FromCell.x, gridSize, shuttle.FromCell.z);
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
        if (shuttle.FromCell.y != gridSize)
            throw new InvalidOperationException("Shuttle의 리프트가 제자리에 있지 않음");

        int maxY = FindMaxY(shuttle.FromCell.x, shuttle.FromCell.z);
        int targetY = maxY + 1;
        if (targetY >= gridSize)
            throw new InvalidOperationException("LowerBin: Shuttle이 위치한 셀에 Bin이 가득 차 있거나, 해당 셀 데이터 오류");

        // 하강: Shuttle의 y값을 targetY로 맞춘다.
        int descendRep = rep;
        while (true)
        {
            if (shuttle.FromCell.y == targetY)
                break;
            if (descendRep <= 0)
                throw new InvalidOperationException("LowerBin 하강에 방해를 받고 있음");

            int descendDelta = targetY - shuttle.FromCell.y;
            LiftDrive(shuttle, descendDelta);
            shuttle.FromCell = new Vector3Int(shuttle.FromCell.x, targetY, shuttle.FromCell.z); // 좌표 갱신. 위치센서 가 있는 경우 수정바람.
            descendRep--;
        }

        // Bin 놓기
        ReleaseDrive(shuttle);

        // 상승: Shuttle의 y값을 gridSize로 맞춘다.
        int ascendRep = rep;
        while (true)
        {
            if (shuttle.FromCell.y == gridSize)
                break;
            if (ascendRep <= 0)
                throw new InvalidOperationException("LowerBin 상승에 방해를 받고 있음");

            int ascendDelta = gridSize - shuttle.FromCell.y;
            LiftDrive(shuttle, ascendDelta);
            shuttle.FromCell = new Vector3Int(shuttle.FromCell.x, gridSize, shuttle.FromCell.z); // 좌표 갱신. 위치센서 가 있는 경우 수정바람.
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
    // X바퀴, Z바퀴, 크랭크축 회전, 리프트, 그리퍼
    // ============================================================

    private void MoveShuttleOnXDrive(ShuttleTest shuttle, int deltaX)
    {
        // TODO: 모터에 deltaX 값을 전달하여 X축 이동 명령.
        // 입력: 양수. 음수.
    }
    private void MoveShuttleOnZDrive(ShuttleTest shuttle, int deltaZ)
    {
        // TODO: 모터에 deltaZ 값을 전달하여 Z축 이동 명령
        // 입력: 양수. 음수.
    }
    private void SwitchDirectionDrive(ShuttleTest shuttle)
    {
        // TODO: 크랭크 축에 회전 명령을 전달하여 X·Z 바퀴 접지 전환
        // 입력: 펄스.
    }
    private void LiftDrive(ShuttleTest shuttle, int deltaY)
    {
        // TODO: 리프트 모터에 deltaY 값을 전달하여 Bin을 리프팅
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
    /**
     * @brief  해당 (x, z) 좌표가 그리드 범위 내에 있는지 확인한다. (0 ≤ x, z ≤ gridSize - 1) y는 무시된다.
     * @param  x  X 좌표
     * @param  z  Z 좌표
     * @return 범위 내이면 true
     */
    private bool IsInRangeXZ(int x, int z)
    {
        return x >= 0 && x <= gridSize - 1
            && z >= 0 && z <= gridSize - 1;
    }

    /**
     * @brief  해당 셀(X, Z)에 존재하는 Bin 중 가장 위쪽(Y가 가장 큰) Bin의 Y 좌표를 반환한다. 해당 셀에 Bin이 없으면 -1 반환.
     * @param  initX  X 좌표
     * @param  initZ  Z 좌표
     * @return 가장 위쪽 Bin의 Y 좌표. 없으면 -1
     */
    public int FindMaxY(int initX, int initZ)
    {
        int maxY = -1;
        foreach (BinTest b in _binList)
        {
            if (b.FromCell.x == initX && b.FromCell.z == initZ && b.FromCell.y > maxY)
                maxY = b.FromCell.y;
        }

        return maxY;
    }
    /**
     * @brief  제거된 셀의 윗층에 있는 Bin을 한 층씩 내린다. (y=0이 가장 밑층)
     * @param  removedCell  제거된 Bin이 있던 좌표
     */
    private void LowerUpperBins(Vector3Int removedCell)
    {
        List<BinTest> upperBins = _binList.FindAll(b =>
            b.FromCell.x == removedCell.x &&
            b.FromCell.z == removedCell.z &&
            b.FromCell.y > removedCell.y);
        upperBins.Sort((a, b) => a.FromCell.y.CompareTo(b.FromCell.y));
        foreach (BinTest upperBin in upperBins)
        {
            Vector3Int lowered = new Vector3Int(upperBin.FromCell.x, upperBin.FromCell.y - 1, upperBin.FromCell.z);
            upperBin.FromCell = lowered;
            upperBin.ToCell = lowered;
        }
    }
}