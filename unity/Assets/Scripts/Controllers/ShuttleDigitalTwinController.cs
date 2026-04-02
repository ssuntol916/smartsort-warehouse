// ============================================================
// 파일명  : ShuttleDigitalTwinController.cs
// 역할    : MQTT 상태 메시지를 수신하여 Unity 셔틀 오브젝트 동기화
// 작성자  : 송준호
// 작성일  : 2026-04-01
//
// 구독 토픽 및 처리:
//   "warehouse/status/shuttle"  → publishStatus() 파싱 → 조인트 위치 반영
//   "warehouse/cmd/shuttle"     → servo_direct 액션 파싱 → 핀 번호로 조인트 직접 제어
//
// servo_direct 페이로드: {"action":"servo_direct","pin":13,"angle":90}
// 핀 번호 매핑 (main.cpp 기준):
//   13=DIR_LEFT  → base/spurgear2 X축 회전 (직접 Transform)
//   15=DIR_RIGHT → RevoluteJoint (방향 전환)
//   14=X_LEFT,  27=X_RIGHT   → SliderJoint X
//   26=Y_LEFT,  25=Y_RIGHT   → SliderJoint Y
//   32=Z_LEFT,  33=Z_RIGHT   → SliderJoint Z
// ============================================================

using System;
using UnityEngine;

public class ShuttleDigitalTwinController : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  Inspector 참조
    // ─────────────────────────────────────────
    [Header("MQTT 구독자 (warehouse/status/shuttle 토픽으로 설정)")]
    [SerializeField] private MqttSubscriber _mqttSubscriber;

    [Header("방향 전환 서보 — RevoluteJoint (래크 앤 피니언)")]
    [Tooltip("방향 전환 조인트 컴포넌트 (objectA=축, objectB=회전 대상)")]
    [SerializeField] private RevoluteJointComponent _dirJoint;

    [Header("이동 조인트 — SliderJoint")]
    [Tooltip("X축 슬라이더 조인트")]
    [SerializeField] private SliderJointComponent _jointX;

    [Tooltip("Y축 슬라이더 조인트")]
    [SerializeField] private SliderJointComponent _jointY;

    [Tooltip("Z축 슬라이더 조인트")]
    [SerializeField] private SliderJointComponent _jointZ;

    [Header("그리드 셀 크기 (mm → Unity 단위 변환)")]
    [Tooltip("한 셀 이동 시 SliderJoint에 전달하는 거리 값")]
    [SerializeField] private float _cellSizeX = 100f;
    [SerializeField] private float _cellSizeY = 100f;
    [SerializeField] private float _cellSizeZ = 150f;

    [Header("방향 전환 서보 각도 (main.cpp 상수와 일치)")]
    [Tooltip("X축 모드 (Y축 바퀴 상승, X축 접지)")]
    [SerializeField] private float _dirAngleXMode = 0f;
    [Tooltip("Y축 모드 (Y축 바퀴 하강, Y축 접지)")]
    [SerializeField] private float _dirAngleYMode = 180f;

    [Header("디버그")]
    [SerializeField] private bool _showLog = true;

    // ─────────────────────────────────────────
    //  내부 상태
    // ─────────────────────────────────────────
    private string    _lastState  = "";
    private int       _lastX      = -1;
    private int       _lastY      = -1;
    private int       _lastZ      = -1;

    // pin 13 전용: base/spurgear2 Transform 캐시 및 회전 상태
    private Transform _spurGear2;
    private Renderer  _spurGear2Renderer;             // 메시 센터 계산용
    private float     _spurGear2CurrentAngle = 0f;   // 현재 각도 (매 프레임 갱신)
    private float     _spurGear2TargetAngle  = 0f;   // MQTT 수신 목표 각도

    [Header("pin 13 (spurgear2) 회전 속도")]
    [Tooltip("초당 회전 속도 (도/초)")]
    [SerializeField] private float _spurGear2Speed = 120f;

    // ─────────────────────────────────────────
    //  Unity 생명주기
    // ─────────────────────────────────────────
    private void Start()
    {
        if (_mqttSubscriber == null)
        {
            Debug.LogError("[DigitalTwin] MqttSubscriber가 연결되지 않았습니다.");
            return;
        }

        // base/spurgear2 캐싱
        _spurGear2 = transform.Find("base/spurgear2");
        if (_spurGear2 == null)
        {
            Debug.LogWarning("[DigitalTwin] 자식 오브젝트 'base/spurgear2' 를 찾지 못했습니다.");
        }
        else
        {
            // 메시 센터 계산을 위한 Renderer 캐싱 (자식 포함 검색)
            _spurGear2Renderer = _spurGear2.GetComponentInChildren<Renderer>();
            if (_spurGear2Renderer == null)
                Debug.LogWarning("[DigitalTwin] spurgear2 에서 Renderer 를 찾지 못했습니다.");
            else
                Debug.Log("[DigitalTwin] base/spurgear2 캐싱 완료");
        }

        // MQTTSubscriber 이벤트 구독
        _mqttSubscriber.OnMessage += HandleMessage;
        Debug.Log("[DigitalTwin] MQTT 메시지 수신 대기 시작");
    }

    private void Update()
    {
        // spurgear2: 현재 각도 → 목표 각도로 _spurGear2Speed 속도로 이동
        if (_spurGear2 == null || _spurGear2Renderer == null) return;
        if (Mathf.Approximately(_spurGear2CurrentAngle, _spurGear2TargetAngle)) return;

        float newAngle = Mathf.MoveTowards(
            _spurGear2CurrentAngle,
            _spurGear2TargetAngle,
            _spurGear2Speed * Time.deltaTime
        );

        // 이번 프레임의 델타 각도
        float delta = newAngle - _spurGear2CurrentAngle;

        // Renderer.bounds.center: 오브젝트의 실제 메시 센터 (월드 좌표)
        // _spurGear2.right: spurgear2 의 로컬 X축 방향 (월드 기준)
        // → 메시 센터를 축으로, 로컬 X축 방향으로 delta 도만큼 회전
        Vector3 meshCenter = _spurGear2Renderer.bounds.center;
        _spurGear2.RotateAround(meshCenter, _spurGear2.right, delta);

        _spurGear2CurrentAngle = newAngle;
    }

    private void OnDestroy()
    {
        if (_mqttSubscriber != null)
            _mqttSubscriber.OnMessage -= HandleMessage;
    }

    // ─────────────────────────────────────────
    //  메시지 수신 처리 (토픽별 분기)
    // ─────────────────────────────────────────
    private void HandleMessage(string topic, string payload)
    {
        switch (topic)
        {
            case "warehouse/status/shuttle":
                HandleStatus(payload);
                break;

            case "warehouse/cmd/shuttle":
                HandleCommand(payload);
                break;
        }
    }

    // warehouse/status/shuttle 처리
    private void HandleStatus(string payload)
    {
        StatusPayload status;
        try { status = JsonUtility.FromJson<StatusPayload>(payload); }
        catch (Exception ex)
        {
            Debug.LogWarning($"[DigitalTwin] status 파싱 실패: {ex.Message}");
            return;
        }

        if (_showLog)
            Debug.Log($"[DigitalTwin] 상태 수신 — state={status.state} " +
                      $"x={status.x} y={status.y} z={status.z}");

        ApplyStateToJoints(status);
    }

    // warehouse/cmd/shuttle 처리 — servo_direct 액션만 반응
    private void HandleCommand(string payload)
    {
        CmdPayload cmd;
        try { cmd = JsonUtility.FromJson<CmdPayload>(payload); }
        catch (Exception ex)
        {
            Debug.LogWarning($"[DigitalTwin] cmd 파싱 실패: {ex.Message}");
            return;
        }

        if (cmd.action != "servo_direct") return;
        if (cmd.angle < 0 || cmd.angle > 180) return;

        if (_showLog)
            Debug.Log($"[DigitalTwin] servo_direct 수신 — pin={cmd.pin}, angle={cmd.angle}°");

        ApplyServoDirect(cmd.pin, cmd.angle);
    }

    // ─────────────────────────────────────────
    //  상태 → 조인트 매핑
    // ─────────────────────────────────────────
    private void ApplyStateToJoints(StatusPayload status)
    {
        // ── 1. 방향 전환 서보 제어 ──────────────────────────
        // dir_switch: 방향 전환 시 도달 예정 state에 따라 각도 결정
        // moving_x  → X모드 유지 (0°)
        // moving_y  → Y모드 유지 (180°)
        switch (status.state)
        {
            case "moving_x":
            case "homing" when _lastState == "moving_x":
                SetDirJoint(_dirAngleXMode, "X모드 (0°)");
                break;

            case "moving_y":
            case "homing" when _lastState == "moving_y":
                SetDirJoint(_dirAngleYMode, "Y모드 (180°)");
                break;

            case "dir_switch":
                // 직전 상태가 X→Y 전환인지, Y→X 전환인지 추론
                float nextAngle = (_lastState == "moving_x") ? _dirAngleYMode : _dirAngleXMode;
                string label    = (_lastState == "moving_x") ? "X→Y 전환" : "Y→X 전환";
                SetDirJoint(nextAngle, label);
                break;
        }

        // ── 2. 그리드 위치 → 슬라이더 조인트 ───────────────
        // x/y/z가 변경된 경우에만 업데이트
        if (status.x != _lastX)
        {
            SetSliderJoint(_jointX, status.x * _cellSizeX, $"X 슬라이더 → {status.x * _cellSizeX}mm");
            _lastX = status.x;
        }

        if (status.y != _lastY)
        {
            SetSliderJoint(_jointY, status.y * _cellSizeY, $"Y 슬라이더 → {status.y * _cellSizeY}mm");
            _lastY = status.y;
        }

        if (status.z != _lastZ)
        {
            SetSliderJoint(_jointZ, status.z * _cellSizeZ, $"Z 슬라이더 → {status.z * _cellSizeZ}mm");
            _lastZ = status.z;
        }

        _lastState = status.state;
    }

    // ─────────────────────────────────────────
    //  servo_direct → 핀 번호로 조인트 직접 제어
    //  (main.cpp PIN_* 상수와 1:1 대응)
    // ─────────────────────────────────────────
    private void ApplyServoDirect(int pin, int angle)
    {
        switch (pin)
        {
            // PIN_DIR_LEFT — base/spurgear2 를 로컬 X축 기준으로 부드럽게 회전
            // 즉시 이동하지 않고 목표 각도만 저장 → Update()에서 30도/초로 이동
            case 13:
                if (_spurGear2 != null)
                {
                    _spurGear2TargetAngle = angle;
                    if (_showLog)
                        Debug.Log($"[DigitalTwin] servo_direct pin=13 → 목표각도 {angle}° (현재 {_spurGear2CurrentAngle:F1}°)");
                }
                else
                {
                    Debug.LogWarning("[DigitalTwin] base/spurgear2 를 찾을 수 없어 회전을 적용하지 못했습니다.");
                }
                break;

            // PIN_DIR_RIGHT — RevoluteJoint
            case 15:
                SetDirJoint(angle, $"servo_direct pin=15 → {angle}°");
                break;

            // X축 주행 서보 (MG90S 연속회전) — SliderJoint X
            // 90=정지, 0=CW(전진), 180=CCW(후진) → 속도로 처리하거나 각도를 위치로 변환
            case 14: // PIN_X_LEFT
            case 27: // PIN_X_RIGHT
                SetSliderJoint(_jointX, ServoAngleToPosition(angle, _cellSizeX),
                    $"servo_direct pin={pin}(X) → {angle}°");
                break;

            // Y축 주행 서보 (MG90S 연속회전) — SliderJoint Y
            case 26: // PIN_Y_LEFT
            case 25: // PIN_Y_RIGHT
                SetSliderJoint(_jointY, ServoAngleToPosition(angle, _cellSizeY),
                    $"servo_direct pin={pin}(Y) → {angle}°");
                break;

            // Z축 리프트 서보 (MG996R) — SliderJoint Z
            case 32: // PIN_Z_LEFT
            case 33: // PIN_Z_RIGHT
                SetSliderJoint(_jointZ, ServoAngleToPosition(angle, _cellSizeZ),
                    $"servo_direct pin={pin}(Z) → {angle}°");
                break;

            default:
                Debug.LogWarning($"[DigitalTwin] 알 수 없는 핀 번호: {pin}");
                break;
        }
    }

    /// <summary>
    /// 연속회전 서보 각도(0/90/180)를 슬라이더 위치로 변환.
    /// 90(정지)=현재 유지, 0(전진)=+cellSize, 180(후진)=-cellSize
    /// </summary>
    private float ServoAngleToPosition(int angle, float cellSize)
    {
        if (angle == 90)  return 0f;         // 정지 → 변화 없음
        if (angle < 90)   return cellSize;   // CW(전진) → 한 칸 전진
        return -cellSize;                    // CCW(후진) → 한 칸 후진
    }

    // ─────────────────────────────────────────
    //  조인트 헬퍼
    // ─────────────────────────────────────────
    private void SetDirJoint(float angle, string label)
    {
        if (_dirJoint == null) return;
        _dirJoint.SetAngle(angle);
        if (_showLog)
            Debug.Log($"[DigitalTwin] 방향 서보 {label}");
    }

    private void SetSliderJoint(SliderJointComponent joint, float position, string label)
    {
        if (joint == null) return;
        joint.SetPosition(position);
        if (_showLog)
            Debug.Log($"[DigitalTwin] {label}");
    }

    // ─────────────────────────────────────────
    //  JSON 페이로드 매핑 클래스
    // ─────────────────────────────────────────

    // warehouse/status/shuttle (main.cpp publishStatus)
    [Serializable]
    private class StatusPayload
    {
        public int    x;
        public int    y;
        public int    z;
        public string bin_id;
        public string state;
        public bool   homed;
    }

    // warehouse/cmd/shuttle servo_direct 액션
    [Serializable]
    private class CmdPayload
    {
        public string action;   // "servo_direct"
        public int    pin;      // ESP32 핀 번호 (13, 15, 14, 27, 26, 25, 32, 33)
        public int    angle;    // 0 ~ 180
    }
}
