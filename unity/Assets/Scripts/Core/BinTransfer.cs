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
 * - _binList : List<Bin>
 * - _shuttleList : List<Shuttle>
 *
 * 메서드: 등록 및 해제
 * - BinRegister(Bin, int, int) — (X, 맨 위, Z)에 Bin 쌓아서 등록
 * - BinUnregister(string) — id 로 Bin 해제. 윗 Bin도 내림
 * - BinUnregister(int, int) — (X, 맨 위, Z) Bin 해제
 * - BinUnregister(Vector3Int) — (X, Y, Z) Bin 해제. 윗 Bin도 내림
 * - ShuttleRegister(Shuttle, int, int) — (X, gridSize, Z)에 Shuttle 등록. HomeCell좌표가 됨
 * - ShuttleUnregister(string) — id 로 Shuttle 해제
 *
 * 메서드: Warehouse 관리
 * - TransferXZ(int, int, int, int) — 출발 좌표 맨위 Bin을 목적 좌표 맨위로 옮김
 *
 * 메서드: Shuttle 제어
 * - MoveShuttleToCell(Shuttle, int, int (,int)) — Shuttle을 (toX, toZ)로 이동
 * - MoveShuttleToCell(Shuttle, Vector3Int (,int)) — Vector3Int 오버로드
 * - MoveShuttleToHome(Shuttle) — 셔틀을 HomeCell 로 옮김
 *
 * 메서드: 유틸리티
 * - FindBinById(string) — id로 Bin 찾기
 * - FindBinByCell(Vector3Int) — Vector3Int 좌표로 Bin 찾기
 * - FindBinByCell(int, int, int) — (X, Y, Z) 좌표로 Bin 찾기
 * - FindBinByCell(int, int) — (X, Z) 좌표의 가장 위 Bin 찾기
 * - FindShuttleById(string) — id로 Shuttle 찾기
 * - FindShuttleByXZ(int, int (,Shuttle)) — (X, Z) 좌표로 Shuttle 찾기
 * - FindMaxY(int, int) — (X, Z) 의 가장 위 Bin 의 y좌표 반환. 없으면 -1
 *
 * 외부타입
 * - Bin — Bin 추상 클래스
 * - Shuttle — Shuttle 추상 클래스
 */
public class BinTransfer
{
    // 그리드 크기 상수 (3 × 3 × 3)
    public const int gridSize = 3;
    
    List<Bin> _binList;     // Bin 1개만 구현.
    List<Shuttle> _shuttleList;    // Bin 을 옮길 셔틀. 메서드에서는 값이 존재해야한다.

    // ============================================================
    // 등록 및 해제
    // ============================================================

    // 생성자
    /**
     * @brief  생성자. Bin과 Shuttle의 등록은 메서드를 통해 이루어진다.
     */
    public BinTransfer()
    {
        _binList = new List<Bin>();
        _shuttleList = new List<Shuttle>();
    }

    // Bin 등록/해제
    /**
     * @brief  Bin 등록. 해당 (x, z) 셀의 가장 위쪽에 배치하고, 배치된 좌표를 알린다.
     * @param  bin   등록할 Bin 인스턴스
     * @param  initX  초기 X 좌표
     * @param  initZ  초기 Z 좌표
     * @return 실제로 배치된 좌표
     * @throws InvalidOperationException  해당 셀이 가득 찼을 때
     * @throws IndexOutOfRangeException  좌표가 그리드 범위를 벗어날 때
     */
    public Vector3Int BinRegister(Bin bin, int initX, int initZ)
    {
        // 좌표 검사
        if (!IsInRangeXZ(initX, initZ))
            throw new IndexOutOfRangeException($"셀 ({initX}, {initZ})의 좌표가 그리드 범위를 벗어나므로 BinRegister 불가");
        // 가득 차면 등록 불가
        int newY = FindMaxY(initX, initZ) + 1;
        if (newY >= gridSize)
            throw new InvalidOperationException($"셀 ({initX}, {initZ})에 Bin이 가득 차 있으므로 BinRegister 불가");
        // 동일 id의 Bin이 이미 등록되어 있는지 검사
        if (FindBinById(bin.Id)!=null)
            throw new InvalidOperationException($"id '{bin.Id}'가 중복되므로 BinRegister 불가");

        // 가장 위쪽에 배치
        Vector3Int placedCell = new Vector3Int(initX, newY, initZ);
        bin.FromCell = placedCell;
        bin.ToCell = placedCell;
        _binList.Add(bin);

        Debug.Log($"[BinTransfer] Bin '{bin.Id}' 등록 완료 → 셀 ({initX}, {newY}, {initZ})에 배치됨");
        return placedCell;
    }
    /**
     * @brief  고유식별자 기반 Bin 해제. 해당 id의 Bin을 제거하고, 윗층 Bin을 한 층씩 내린다.
     * @param  id  Bin 고유식별자
     * @throws ArgumentException  해당 id의 Bin이 없을 때
     * @throws InvalidOperationException  Bin이 이송 중일 때
     */
    public void BinUnregister(string id)
    {
        // 해당 id의 Bin 찾기
        Bin bin = FindBinById(id);
        if (bin == null)
            throw new ArgumentException($"[BinTransfer] id '{id}'에 해당하는 Bin이 존재하지 않으므로 BinUnregister 불가");

        // Bin 제거
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
        // 해당 셀의 maxY 찾기
        int maxY = FindMaxY(x, z);
        if (maxY < 0)
            throw new ArgumentException($"[BinTransfer] 셀 ({x}, {z})에 해당하는 Bin이 존재하지 않으므로 BinUnregister 불가");

        // Bin 찾고 제거
        Bin bin = FindBinByCell(x, maxY, z);
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
        // 해당 셀에 Bin 찾기
        Bin bin = FindBinByCell(cell);
        if (bin == null)
            throw new ArgumentException($"[BinTransfer] 셀 ({cell.x}, {cell.y}, {cell.z})에 해당하는 Bin이 존재하지 않으므로 BinUnregister 불가");

        // Bin 제거
        BinUnregisterInternal(bin);
    }
    /**
     * @brief  Bin 해제 공통 로직. 이송 중 여부를 확인하고, 제거 후 윗층 Bin을 한 층씩 내린다.
     * @param  bin  해제할 Bin 인스턴스
     * @throws InvalidOperationException  Bin이 이송 중일 때
     */
    private void BinUnregisterInternal(Bin bin)
    {
        // Bin이 이송 중인지 확인
        if (bin.OnDuty)
            throw new InvalidOperationException("[BinTransfer] Bin이 이송 중이므로 BinUnregister 불가");

        // Bin 제거
        Vector3Int removedCell = bin.FromCell;
        _binList.Remove(bin);
        Debug.Log($"[BinTransfer] Bin 셀 ({bin.FromCell.x}, {bin.FromCell.y}, {bin.FromCell.z})에 배치된 '{bin.Id}' 해제 완료");

        // 윗층 Bin 내리기
        LowerUpperBins(removedCell);

        // FromCell 초기화 (제거 신호)
        bin.FromCell = new Vector3Int(-1, -1, -1);
    }
    // Shuttle 등록/해제
    /**
     * @brief  Shuttle 등록. 해당 (x, z) 셀에 배치하고, 배치된 좌표를 알린다. y는 gridSize로 고정.
     * @param  shuttle  등록할 Shuttle 인스턴스
     * @param  initX    초기 X 좌표
     * @param  initZ    초기 Z 좌표
     * @return 실제로 배치된 셀 좌표
     * @throws InvalidOperationException  동일 id의 셔틀이 이미 등록되어 있거나, 해당 셀에 이미 셔틀이 존재할 때
     * @throws IndexOutOfRangeException  좌표가 그리드 범위를 벗어날 때
     */
    public Vector3Int ShuttleRegister(Shuttle shuttle, int initX, int initZ)
    {
        // 좌표 검사
        if (!IsInRangeXZ(initX, initZ))
            throw new IndexOutOfRangeException($"[BinTransfer] 셀 ({initX}, {initZ})의 좌표가 그리드 범위를 벗어나므로 ShuttleRegister 불가");
        // 동일 id의 셔틀이 이미 등록되어 있는지 검사
        if (FindShuttleById(shuttle.Id) != null)
            throw new InvalidOperationException($"[BinTransfer] id '{shuttle.Id}'가 중복되므로 ShuttleRegister 불가");
        // 해당 셀에 이미 셔틀이 존재하는지 검사
        Vector3Int placedCell = new Vector3Int(initX, gridSize, initZ);
        if (FindShuttleByXZ(placedCell.x, placedCell.z) != null)
            throw new InvalidOperationException($"[BinTransfer] 셀 ({initX}, {initZ})에 이미 셔틀이 존재하므로 ShuttleRegister 불가");

        // 배치
        shuttle.FromCell = placedCell;
        shuttle.HomeCell = placedCell;
        shuttle.LiftLevel = gridSize;
        _shuttleList.Add(shuttle);

        Debug.Log($"[BinTransfer] Shuttle '{shuttle.Id}' 등록 완료 → 셀 ({initX}, {initZ})에 배치됨");
        return placedCell;
    }
    /**
     * @brief  고유식별자 기반 Shuttle 해제. 해당 id의 Shuttle을 _shuttleList에서 제거한다.
     * @param  id  Shuttle 고유식별자
     * @throws ArgumentException  해당 id의 Shuttle이 없을 때
     * @throws InvalidOperationException  Shuttle이 이송 중일 때
     */
    public void ShuttleUnregister(string id)
    {
        // 해당 id의 Shuttle 찾기
        Shuttle shuttle = FindShuttleById(id);
        if (shuttle == null)
            throw new ArgumentException($"[BinTransfer] id '{id}'에 해당하는 Shuttle이 존재하지 않으므로 ShuttleUnregister 불가");
        // Shuttle이 이송 중인지 확인
        if (shuttle.OnDuty)
            throw new InvalidOperationException($"[BinTransfer] Shuttle '{id}'이 이송 중이므로 ShuttleUnregister 불가");

        // Shuttle 제거
        _shuttleList.Remove(shuttle);
        Debug.Log($"[BinTransfer] Shuttle '{id}' 해제 완료");

        // FromCell 초기화 (제거 신호)
        shuttle.FromCell = new Vector3Int(-1, -1, -1);
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
     * @return 실제로 배치된 목적 셀 좌표
     * @throws IndexOutOfRangeException  좌표가 그리드 범위를 벗어날 때
     * @throws ArgumentException  출발 셀에 Bin이 없을 때
     * @throws InvalidOperationException  목적 셀이 가득 찼을 때
     */
    public Vector3Int TransferXZ(int fromX, int fromZ, int toX, int toZ)
    {
        // 좌표 검사
        if (!IsInRangeXZ(fromX, fromZ))
            throw new IndexOutOfRangeException($"[BinTransfer] 출발지 셀 ({fromX}, {fromZ})의 좌표가 그리드 범위를 벗어나므로 TransferXZ 불가");
        if (!IsInRangeXZ(toX, toZ))
            throw new IndexOutOfRangeException($"[BinTransfer] 목적지 셀 ({toX}, {toZ})의 좌표가 그리드 범위를 벗어나므로 TransferXZ 불가");

        // 출발지 Bin 검사
        int fromMaxY = FindMaxY(fromX, fromZ);
        if (fromMaxY < 0)
            throw new ArgumentException($"[BinTransfer] 출발지 셀 ({fromX}, {fromZ})에 Bin이 존재하지 않으므로 TransferXZ 불가");
        // 목적지 Bin 가득 찬지 검사
        int toMaxY = FindMaxY(toX, toZ);
        if (toMaxY >= gridSize - 1)
            throw new InvalidOperationException($"[BinTransfer] 목적지 셀 ({toX}, {toZ})에 Bin이 가득 차 있으므로 TransferXZ 불가");

        // 출발지, 목적지 획득
        Vector3Int fromCell = new Vector3Int(fromX, fromMaxY, fromZ);
        Vector3Int toCell = new Vector3Int(toX, toMaxY + 1, toZ);
        // 출발지 Bin, 목적지 Bin, 가까우며 작업없는 Shuttle, 목적지 위 Shuttle 획득
        Bin bin = FindBinByCell(fromCell);
        Bin binUnder = toMaxY >= 0 ? FindBinByCell(toX, toMaxY, toZ) : null;    // 목적지 maxY가 0 미만이면 null
        Shuttle shuttle = FindNearShuttle(fromCell);
        Shuttle shuttleBlock = (shuttle.FromCell.x == toX && shuttle.FromCell.z == toZ) // 목적지 셔틀이 부를 셔틀과 같으면 null
            ? null
            : FindShuttleByXZ(toX, toZ, shuttle);

        // 출발지 Bin, 목적지 Bin 작업 여부 확인
        // Shuttle 작업 여부는 FindNearShuttle에서 이미 고려됨
        if (bin.OnDuty)
            throw new InvalidOperationException($"[BinTransfer] 출발지 셀 ({fromX}, {fromZ})의 Bin이 작업 중이므로 TransferXZ 불가");
        if (binUnder != null && binUnder.OnDuty)
            throw new InvalidOperationException($"[BinTransfer] 목적지 셀 ({toX}, {toZ})의 Bin이 작업 중이므로 TransferXZ 불가");
        if (shuttleBlock != null)
            throw new InvalidOperationException($"[BinTransfer] 목적지 셀 ({toX}, {toZ})에 위치한 Shuttle '{shuttleBlock.Id}'이 방해하므로 TransferXZ 불가");

        // 작업 시작
        // Bin 작업 상태 설정
        bin.ToCell = toCell;
        bin.OnDuty = true;
        shuttle.OnDuty = true;

        // 동작: 출발지이동 - 리프팅 - 목적지이동 - 하강
        MoveShuttleToCell(shuttle, fromCell);
        LiftBin(shuttle);
        MoveShuttleToCell(shuttle, toCell);
        LowerBin(shuttle);
        
        // Bin 좌표 갱신. Shuttle 해제
        bin.FromCell = toCell;
        bin.OnDuty = false;
        shuttle.OnDuty = false;

        return toCell;
    }

    // ============================================================
    // Shuttle 이동 제어
    // ============================================================

    /**
     * @brief  Shuttle을 해당 셀의 (x, z) 좌표로 이동시킨다. y는 무시된다.
     *         정밀도를 위해 기본 7회까지 반복한다.
     * @param  shuttle  이동시킬 Shuttle 인스턴스
     * @param  toX      목적 X 좌표
     * @param  toZ      목적 Z 좌표
     * @param  rep      재귀 깊이 (기본 7)
     * @throws InvalidOperationException  목적 셀에 다른 셔틀이 이미 존재할 때
     * @throws InvalidOperationException  재귀 횟수 초과 시
     */
    public void MoveShuttleToCell(Shuttle shuttle, int toX, int toZ, int rep = 7)
    {
        // 목적 셀에 다른 셔틀이 이미 존재하는지 검사
        if (FindShuttleByXZ(toX, toZ, shuttle) != null)
            throw new InvalidOperationException($"[BinTransfer] 셀 ({toX}, {toZ})에 다른 셔틀이 이미 존재하므로 MoveShuttleToCell 불가");

        // 도달할 때 까지 rep 반복
        while (true)
        {
            // 반복수 초과 시 예외
            if (rep <= 0)
                throw new InvalidOperationException("[BinTransfer] MoveShuttleToCell: Shuttle이 이동에 방해를 받고 있음");

            // 목적지의 (x, z) 좌표와 현재 셔틀의 (x, z) 좌표 차이 계산. 이미 목적지에 도착한 경우 종료
            Vector3Int targetXZ = new Vector3Int(toX, shuttle.FromCell.y, toZ);
            Vector3Int path = targetXZ - shuttle.FromCell;
            if (path == Vector3Int.zero)
                return;

            if (shuttle.IsHeadingZ && path.z != 0)
            {
                // Z 방향으로 향하면, Z 방향으로 이동
                MoveShuttleOnZDrive(shuttle, path.z);
                shuttle.FromCell = shuttle.FromCell + new Vector3Int(0,0,path.z); // TODO: 좌표 갱신. 위치결정센서 가 있는 경우 수정바람.
            }
            else if (!shuttle.IsHeadingZ && path.x != 0)
            {
                // X 방향으로 향하면, X 방향으로 이동
                MoveShuttleOnXDrive(shuttle, path.x);
                shuttle.FromCell = shuttle.FromCell + new Vector3Int(path.x,0,0); // TODO: 좌표 갱신. 위치결정센서 가 있는 경우 수정바람.
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
     * @brief  Shuttle을 해당 셀의 (x, z) 좌표로 이동시킨다 (Vector3Int 오버로드). y는 무시된다.
     * @param  shuttle  이동시킬 Shuttle 인스턴스
     * @param  toCell   목적 셀 좌표 (y는 무시)
     * @param  rep      재귀 깊이 (기본 7)
     */
    public void MoveShuttleToCell(Shuttle shuttle, Vector3Int toCell, int rep = 7)
    {
        MoveShuttleToCell(shuttle, toCell.x, toCell.z, rep);
    }
    public void MoveShuttleToCell(string id, int toX, int toZ, int rep = 7)
    {
        // id에 해당하는 셔틀 획득
        Shuttle shuttle = FindShuttleById(id);
        if (shuttle == null)
            throw new ArgumentException($"[BinTransfer] id '{id}'에 해당하는 Shuttle이 존재하지 않으므로 MoveShuttleToCell 불가");
        
        MoveShuttleToCell(shuttle, toX, toZ, rep);
    }
    public void MoveShuttleToCell(int fromX, int fromZ, int toX, int toZ, int rep = 7)
    {
        // fromX, fromZ 위치의 셔틀 획득
        Shuttle shuttle = FindShuttleByXZ(fromX, fromZ);
        if (shuttle == null)
            throw new ArgumentException($"[BinTransfer] 출발지 셀 ({fromX}, {fromZ})에 해당하는 Shuttle이 존재하지 않으므로 MoveShuttleToCell 불가");
        
        MoveShuttleToCell(shuttle, toX, toZ, rep);
    }
    /**
     * @brief  셔틀을 홈 포지션으로 복귀시킨다.
     *         엔드스톱 스위치 기준으로 캘리브레이션한다.
     * @param  shuttle  복귀시킬 Shuttle 인스턴스
     */
    public void MoveShuttleToHome(Shuttle shuttle)
    {
        MoveShuttleToCell(shuttle, shuttle.HomeCell);
    }
    /**
     * @brief  X·Z 바퀴 접지를 무조건 전환한다.
     * @param  shuttle  방향 전환할 Shuttle 인스턴스
     */
    private void SwitchDirection(Shuttle shuttle)
    {
        shuttle.IsHeadingZ = !shuttle.IsHeadingZ;
        SwitchDirectionDrive(shuttle);
    }
    // ============================================================
    // Lift 제어
    // ============================================================
    /**
     * @brief  스풀/벨트를 감아올려 목적 층의 Bin을 리프팅한다.
     * @param  shuttle  리프팅을 수행할 Shuttle 인스턴스
     * @param  rep      반복 허용 횟수 (기본 7)
     * @throws InvalidOperationException  Shuttle이 위치한 셀에 Bin이 없거나 데이터 오류일 때
     */
    private void LiftBin(Shuttle shuttle, int rep = 7)
    {
        // Shuttle이 있는 셀에 Bin이 있는지 확인
        int maxY = FindMaxY(shuttle.FromCell.x, shuttle.FromCell.z);
        if (maxY < 0 || maxY >= gridSize)
            throw new InvalidOperationException("[BinTransfer] LiftBin: Shuttle이 위치한 셀에 Bin이 존재하지 않거나, 해당 셀 데이터 오류");

        // 하강
        LiftDescend(shuttle, maxY, rep);
        // Bin 잡기
        GrabDrive(shuttle);
        // 상승
        LiftAscend(shuttle, rep);
    }

    /**
     * @brief  스풀/벨트를 풀어 Bin을 현재 층에 내려놓는다.
     * @param  shuttle  하강을 수행할 Shuttle 인스턴스
     * @param  rep      반복 허용 횟수 (기본 7)
     * @throws InvalidOperationException  Shuttle이 위치한 셀에 Bin이 가득 찼거나 데이터 오류일 때
     */
    private void LowerBin(Shuttle shuttle, int rep = 7)
    {
        // Shuttle이 있는 셀에 Bin을 놓을 수 있는지 확인
        int targetY = FindMaxY(shuttle.FromCell.x, shuttle.FromCell.z) + 1;
        if (targetY >= gridSize)
            throw new InvalidOperationException("[BinTransfer] LowerBin: Shuttle이 위치한 셀에 Bin이 가득 차 있거나, 해당 셀 데이터 오류");

        // 하강
        LiftDescend(shuttle, targetY, rep);
        // Bin 놓기
        ReleaseDrive(shuttle);
        // 상승
        LiftAscend(shuttle, rep);
    }
    /**
     * @brief  리프트를 목표 층까지 하강시킨다.
     * @param  shuttle  하강을 수행할 Shuttle 인스턴스
     * @param  maxY     목표 Y 좌표
     * @param  rep      반복 허용 횟수 (기본 7)
     * @throws InvalidOperationException  반복 횟수 초과 시
     */
    private void LiftDescend(Shuttle shuttle, int maxY, int rep = 7)
    {
        // 하강: Shuttle의 y값을 maxY로 맞출때까지 rep 반복.
        int descendRep = rep;
        while (true)
        {
            // 반복수 초과 시 예외
            if (descendRep <= 0)
                throw new InvalidOperationException("[BinTransfer] LiftBin 하강에 방해를 받고 있음");
            // 하강 완료 시 종료
            if (shuttle.LiftLevel == maxY)
                break;

            // 하강
            int descendDelta = maxY - shuttle.LiftLevel;
            LiftDrive(shuttle, descendDelta);
            shuttle.LiftLevel = maxY; // TODO: 좌표 갱신. 위치결정센서 가 있는 경우 수정바람.
            descendRep--;
        }
    }
    /**
     * @brief  리프트를 제자리(gridSize)까지 상승시킨다.
     * @param  shuttle  상승을 수행할 Shuttle 인스턴스
     * @param  rep      반복 허용 횟수 (기본 7)
     * @throws InvalidOperationException  반복 횟수 초과 시
     */
    private void LiftAscend(Shuttle shuttle, int rep = 7)
    {
        // 상승: Shuttle의 y값을 gridSize로 맞춘다.
        int ascendRep = rep;
        while (true)
        {
            // 반복수 초과 시 예외
            if (ascendRep <= 0)
                throw new InvalidOperationException("[BinTransfer] LiftBin 상승에 방해를 받고 있음");
            // 상승 완료 시 종료
            if (shuttle.LiftLevel == gridSize)
                break;

            // 상승
            int ascendDelta = gridSize - shuttle.LiftLevel;
            LiftDrive(shuttle, ascendDelta);
            shuttle.LiftLevel = gridSize; // TODO: 좌표 갱신. 위치결정센서 가 있는 경우 수정바람.
            ascendRep--;
        }
    }

    // ============================================================
    // 구동 메서드
    // ============================================================

    private void MoveShuttleOnXDrive(Shuttle shuttle, int deltaX)
    {
        // TODO: 모터에 deltaX 값을 전달하여 X축 이동 명령.
        // 입력: 양수. 음수.
    }
    private void MoveShuttleOnZDrive(Shuttle shuttle, int deltaZ)
    {
        // TODO: 모터에 deltaZ 값을 전달하여 Z축 이동 명령
        // 입력: 양수. 음수.
    }
    private void SwitchDirectionDrive(Shuttle shuttle)
    {
        // TODO: 크랭크 축에 회전 명령을 전달하여 X·Z 바퀴 접지 전환
        // 입력: 펄스.
    }
    private void LiftDrive(Shuttle shuttle, int deltaY)
    {
        // TODO: 리프트 모터에 deltaY 값을 전달하여 Bin을 리프팅
        // 입력: 양수(상승). 음수(하강).
    }
    private void GrabDrive(Shuttle shuttle)
    {
        // TODO: 그리퍼에 명령을 전달하여 Bin을 집음
        // 입력: 펄스.
    }
    private void ReleaseDrive(Shuttle shuttle)
    {
        // TODO: 그리퍼에 명령을 전달하여 Bin을 놓음
        // 입력: 펄스.
    }

    // ============================================================
    // Utility 메서드
    // ============================================================

    /**
     * @brief  고유식별자로 Bin을 찾는다.
     * @param  id  Bin 고유식별자
     * @return 해당 Bin 인스턴스. 없으면 null
     */
    public Bin FindBinById(string id)
    {
        return _binList.Find(b => b.Id == id);
    }
    /**
     * @brief  (x, y, z) 좌표로 Bin을 찾는다.
     * @param  x  X 좌표
     * @param  y  Y 좌표
     * @param  z  Z 좌표
     * @return 해당 Bin 인스턴스. 없으면 null
     */
    public Bin FindBinByCell(int x, int y, int z)
    {
        return _binList.Find(b =>
            b.FromCell.x == x &&
            b.FromCell.y == y &&
            b.FromCell.z == z);
    }
    /**
    * @brief  Vector3Int 좌표로 Bin을 찾는다.
    * @param  cell  셀 좌표
    * @return 해당 Bin 인스턴스. 없으면 null
    */
    public Bin FindBinByCell(Vector3Int cell)
    {
        return FindBinByCell(cell.x, cell.y, cell.z);
    }
    /**
    * @brief  (x, z) 좌표의 가장 위 Bin을 찾는다.
    * @param  x  X 좌표
    * @param  z  Z 좌표
    * @return 해당 Bin 인스턴스. 없으면 null
     */
    public Bin FindBinByCell(int x, int z)
    {
        return FindBinByCell(x, FindMaxY(x, z), z);
    }
    /**
     * @brief  고유식별자로 Shuttle을 찾는다.
     * @param  id  Shuttle 고유식별자
     * @return 해당 Shuttle 인스턴스. 없으면 null
     */
    public Shuttle FindShuttleById(string id)
    {
        return _shuttleList.Find(s => s.Id == id);
    }
    /**
     * @brief  (x, z) 좌표로 Shuttle을 찾는다. exclude에 지정된 Shuttle은 제외한다.
     * @param  x        X 좌표
     * @param  z        Z 좌표
     * @param  exclude  제외할 Shuttle 인스턴스 (기본 null)
     * @return 해당 Shuttle 인스턴스. 없으면 null
     */
    public Shuttle FindShuttleByXZ(int x, int z, Shuttle exclude = null)
    {
        return _shuttleList.Find(s =>
            s != exclude &&
            s.FromCell.x == x &&
            s.FromCell.z == z);
    }
    /**
 * @brief  사용할 수 있는 가까운 셔틀을 찾는다.
 * @param  cell  기준 셀 좌표 (맨해튼 거리 계산 기준)
 * @return 가장 가까운 사용 가능한 Shuttle 인스턴스
 * @throws InvalidOperationException  사용 가능한 Shuttle이 없을 때
 */
    private Shuttle FindNearShuttle(Vector3Int cell)
    {
        // 가장 가까운 셔틀 탐색 (맨해튼 거리)
        Shuttle nearest = null;
        int minDist = int.MaxValue;

        // 셔틀이 이송 중이면 건너뛰고, 그렇지 않으면 (맨해튼 거리) 계산하여 가장 가까운 셔틀 찾기
        foreach (Shuttle s in _shuttleList)
        {
            if (s.OnDuty)
                continue;

            int dist = Math.Abs(s.FromCell.x - cell.x) + Math.Abs(s.FromCell.z - cell.z);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = s;
            }
        }
        if (nearest == null)
            throw new InvalidOperationException("[BinTransfer] 사용 가능한 Shuttle이 없음");

        return nearest;
    }
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
        foreach (Bin b in _binList)
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
        // 제거된 셀의 윗층에 있는 Bin 찾고 정렬
        List<Bin> upperBins = _binList.FindAll(b =>
            b.FromCell.x == removedCell.x &&
            b.FromCell.z == removedCell.z &&
            b.FromCell.y > removedCell.y);
        upperBins.Sort((a, b) => a.FromCell.y.CompareTo(b.FromCell.y));

        // 윗층 Bin을 한 층씩 내리기
        foreach (Bin upperBin in upperBins)
        {
            Vector3Int lowered = new Vector3Int(upperBin.FromCell.x, upperBin.FromCell.y - 1, upperBin.FromCell.z);
            upperBin.FromCell = lowered;
            upperBin.ToCell = lowered;
        }
    }
}