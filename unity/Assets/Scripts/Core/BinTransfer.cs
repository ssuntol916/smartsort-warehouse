// ============================================================
// 파일명  : BinTransfer.cs
// 역할    : Bin 이송 알고리즘 클래스
// 작성자  : 
// 작성일  : 
// 수정이력: 
// ============================================================

using UnityEngine;

public class BinTransfer
{
    // 그리드 크기 상수 (3 × 3 × 3)
    private const int GridSize = 3;

    private Vector3Int _fromCell;       // Bin 출발 셀 (X, Y, Z)
    private Vector3Int _toCell;         // Bin 목적 셀 (X, Y, Z)
    private bool _isTransferring;       // 이송 중 여부

    public Vector3Int FromCell => _fromCell;            // Bin 출발 셀
    public Vector3Int ToCell => _toCell;                // Bin 목적 셀
    public bool IsTransferring => _isTransferring;      // 이송 중 여부

    /**
     * @brief  홈 포지션(0, 0, 0)에서 셔틀을 초기화한다.
     */
    public BinTransfer()
    {
        _fromCell = Vector3Int.zero;
        _toCell = Vector3Int.zero;
        _isTransferring = false;
    }

    /**
     * @brief  출발 셀에서 목적 셀까지 Bin을 이송한다.
     *         셔틀 이동 → Bin 리프팅 → 목적 셀 이동 → Bin 하강 순서로 동작한다.
     *         출발·목적 셀이 그리드 범위를 벗어난 경우 처리하지 않는다.
     * @param  fromCell  Bin 출발 셀 좌표 (X, Y, Z)
     * @param  toCell    Bin 목적 셀 좌표 (X, Y, Z)
     */
    public void Transfer(Vector3Int fromCell, Vector3Int toCell)
    {

    }

    /**
     * @brief  셔틀을 목적 셀의 X·Y 좌표로 이동시킨다.
     *         X → 방향 전환 → Y 순서로 이동한다.
     * @param  targetCell  목적 셀의 X·Y 좌표
     */
    private void MoveShuttleToCell(Vector2Int targetCell)
    {

    }

    /**
     * @brief  크랭크 임펠러를 회전시켜 X·Y 바퀴 접지를 전환한다.
     */
    private void SwitchDirection()
    {

    }

    /**
     * @brief  스풀/벨트를 감아올려 목적 층의 Bin을 리프팅한다.
     * @param  layer  리프팅할 층 번호 (0 ~ GridSize - 1)
     */
    private void LiftBin(int layer)
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
    public void MoveHome()
    {

    }

    /**
     * @brief  셀 좌표가 그리드 범위(0 ~ GridSize - 1) 이내인지 검증한다.
     * @param  cell  검증할 셀 좌표 (X, Y, Z)
     * @return bool  범위 이내이면 true, 아니면 false
     */
    private bool IsInRange(Vector3Int cell)
    {

    }
}