// ============================================================
// 파일명  : YWheelController.cs
// 역할    : ywheel1~4 를 자전시키고 셔틀을 Z축 방향으로 이동시킨다.
//           MQTT axis="y" 메시지를 처리하는 WheelControllerBase 파생 클래스.
//
// 작성자  : 이현화
// 작성일  : 2026-04-27
// 수정이력: 2026-05-01 — MQTT 수신 연동 추가 (axis=y, angle, duration_ms)
//                        MovementScale 보정 추가 (물리 계산 기반 → 실제 이동량 일치)
//                        WheelControllerBase 추출로 리팩토링
// ============================================================

using UnityEngine;

public class YWheelController : WheelControllerBase
{
    /// <inheritdoc/>
    protected override string Axis => "y";

    /// <inheritdoc/>
    protected override Vector3 MoveDirection => Vector3.forward;

    /// <inheritdoc/>
    protected override string LogPrefix => "[YWheel]";
}