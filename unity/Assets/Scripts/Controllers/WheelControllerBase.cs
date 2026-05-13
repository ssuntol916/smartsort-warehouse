// ============================================================
// 파일명  : WheelControllerBase.cs
// 역할    : XWheelController / YWheelController 의 공통 로직을 제공하는 추상 베이스 클래스.
//           MQTT warehouse/cmd/shuttle 수신 시 바퀴 자전 및 셔틀 이동을 처리한다.
//           축(axis), 이동 방향 벡터, 로그 접두사는 각 파생 클래스에서 구현한다.
//
// 작성자  : 이현화
// 작성일  : 2026-05-04
// 수정이력: —
// ============================================================

using System;
using UnityEngine;

public abstract class WheelControllerBase : MonoBehaviour
{
    // ============================================================
    // Inspector 필드
    // ============================================================

    [Header("필수 오브젝트")]
    [SerializeField] private Transform _shuttle;                    // 셔틀 최상위 Transform (이동 대상)
    [SerializeField] private RevoluteJointComponent _wheel1Joint;   // 바퀴1 조인트
    [SerializeField] private RevoluteJointComponent _wheel2Joint;   // 바퀴2 조인트
    [SerializeField] private RevoluteJointComponent _wheel3Joint;   // 바퀴3 조인트
    [SerializeField] private RevoluteJointComponent _wheel4Joint;   // 바퀴4 조인트

    [Header("MQTT")]
    [SerializeField] private MqttSubscriber _mqttSubscriber;        // MQTT 브릿지 (OnMessage 이벤트 구독 대상)

    // ─── 디버그용 Inspector 노출 (런타임 읽기 전용 — 외부 수정 금지) ─────────
    [Header("파라미터 (읽기 전용 — MQTT 수신 시 자동 갱신)")]
    [SerializeField] private float _duration = 0f;      // 이동 지속 시간 (초)
    [SerializeField] private bool _isSignal = false;    // 동작 신호
    [SerializeField] private bool _isForward = true;    // 이동 방향

    // ============================================================
    // 상수
    // ============================================================

    private const float SecondsPerRotation = 0.874f;                   // 1바퀴 기준 시간 (초). 셔틀 실제 이동량 기준 보정값 (원본 3 s / 보정배율 3.432)

    private const float DegreesPerSecond = 360f / SecondsPerRotation;  //초당 회전 각도 (도)

    private const string TargetTopic = "warehouse/cmd/shuttle";        // MQTT 수신 대상 토픽

    // ============================================================
    // 런타임 상태
    // ============================================================

    private float _wheelRadius;                         // 바퀴 반지름 (wheel1 메쉬 바운딩 박스에서 자동 계산, 단위: m)

    private float _elapsedTime;                         // 현재 동작 경과 시간 (초)

    private bool _initialized;                          // 초기화 완료 여부. false 이면 Update/MQTT 핸들러가 조기 종료한다.

    private Vector3[] _centerOffsets = new Vector3[4];  // LineBCenter 기준 각 바퀴의 위치 오프셋 배열 (자전 피벗 보정용, 매 프레임 갱신)

    // ============================================================
    // 추상 멤버 — 파생 클래스에서 반드시 구현
    // ============================================================

    protected abstract string Axis { get; }             // 처리할 MQTT axis 값 ("x" 또는 "y")

    protected abstract Vector3 MoveDirection { get; }   // 셔틀 이동 방향 단위 벡터 (X축: Vector3.right / Y(Z)축: Vector3.forward)

    protected abstract string LogPrefix { get; }        // Debug.Log 접두사 (예: "[XWheel]")

    // ============================================================
    // MQTT 페이로드 파싱용 클래스
    // ============================================================

    [Serializable]
    private class ShuttleCmd      // warehouse/cmd/shuttle 토픽의 JSON 페이로드 구조체(JsonUtility.FromJson 으로 역직렬화한다.)
    {
        public string action;     // 명령 종류 — "drive_direct" 만 처리
        public string axis;       // 이동 축   — 파생 클래스의 Axis 값과 비교
        public int[] angle;       // 이동 방향 — [0,180]: forward / [180,0]: backward
        public int duration_ms;   // 이동 지속 시간 (밀리초)
    }

    // ============================================================
    // Unity 메시지
    // ============================================================

    private void Start()
    {
        // ValidateComponents 실패 시 초기화를 중단하고 플래그를 false 로 유지
        if (!ValidateComponents()) return;

        InitializeWheelRadius();
        InitializeCenterOffsets();

        _initialized = true; // 모든 초기화 완료 후에만 true 로 설정
        Debug.Log($"{LogPrefix} 초기화 완료 | wheelRadius={_wheelRadius:F4}m");
    }

    /**
     * @brief  컴포넌트 활성화 시 MqttSubscriber.OnMessage 이벤트를 구독한다.
     *         OnDisable 에서 반드시 해제하여 메모리 누수를 방지한다.
     */
    private void OnEnable()
    {
        if (_mqttSubscriber != null)
            _mqttSubscriber.OnMessage += HandleMqttMessage;
    }

    /**
     * @brief  컴포넌트 비활성화 시 이벤트 구독을 해제한다.
     */
    private void OnDisable()
    {
        if (_mqttSubscriber != null)
            _mqttSubscriber.OnMessage -= HandleMqttMessage;
    }

    /**
     * @brief  매 프레임 바퀴 자전 및 셔틀 이동을 처리한다.
     *         _initialized=false, _isSignal=false, _duration<=0 인 경우 조기 종료한다.
     *         _duration 경과 시 자동으로 신호를 해제한다.
     */
    private void Update()
    {
        // 초기화 미완료 또는 신호 없음 → 조기 종료
        if (!_initialized || !_isSignal || _duration <= 0f) return;

        _elapsedTime += Time.deltaTime;

        // 지속 시간 초과 시 동작 종료
        if (_elapsedTime >= _duration)
        {
            StopMovement();
            return;
        }

        // 방향에 따른 이번 프레임 회전 각도 계산
        float direction = _isForward ? 1f : -1f;
        float deltaAngle = DegreesPerSecond * Time.deltaTime * direction;

        // wheel1 의 회전축을 기준으로 Quaternion 생성 (부호 반전: 바퀴 자전 방향 보정)
        Quaternion rotation = Quaternion.AngleAxis(-deltaAngle, _wheel1Joint.ObjectBRotationAxis);

        RotateWheel(_wheel1Joint, rotation, ref _centerOffsets[0]);
        RotateWheel(_wheel2Joint, rotation, ref _centerOffsets[1]);
        RotateWheel(_wheel3Joint, rotation, ref _centerOffsets[2]);
        RotateWheel(_wheel4Joint, rotation, ref _centerOffsets[3]);

        // 바퀴 회전량을 선형 이동 거리로 변환 (호의 길이 = r × θ)
        float moveDistance = _wheelRadius * deltaAngle * Mathf.Deg2Rad;
        _shuttle.position += MoveDirection * moveDistance;
    }

    // ============================================================
    // MQTT 처리
    // ============================================================

    /**
     * @brief  MqttSubscriber.OnMessage 이벤트 핸들러.
     *         토픽 필터 → JSON 파싱 → axis/action 검증 → 동작 파라미터 적용 순으로 처리한다.
     *         _initialized=false 이면 메시지를 무시하여 미초기화 상태 진입을 방지한다.
     * @param  topic    수신된 MQTT 토픽 문자열
     * @param  payload  수신된 JSON 페이로드 문자열
     */
    private void HandleMqttMessage(string topic, string payload)
    {
        // 초기화 미완료 또는 대상 토픽이 아니면 무시
        if (!_initialized) return;
        if (topic != TargetTopic) return;

        ShuttleCmd cmd = JsonUtility.FromJson<ShuttleCmd>(payload);

        if (cmd == null)
        {
            Debug.LogWarning($"{LogPrefix} 페이로드 파싱 실패: {payload}");
            return;
        }

        // drive_direct + 해당 axis 인 경우에만 처리
        if (cmd.action != "drive_direct" || cmd.axis != Axis) return;

        if (cmd.angle == null || cmd.angle.Length < 2)
        {
            Debug.LogWarning($"{LogPrefix} angle 배열이 올바르지 않습니다.");
            return;
        }

        ApplyCommand(cmd);
    }

    /**
     * @brief  파싱된 명령을 런타임 상태에 적용하고 동작을 시작한다.
     * @param  cmd  파싱된 ShuttleCmd 인스턴스
     */
    private void ApplyCommand(ShuttleCmd cmd)
    {
        _duration = cmd.duration_ms / 1000f;
        _isForward = cmd.angle[0] < cmd.angle[1]; // [0,180] → forward / [180,0] → backward
        _elapsedTime = 0f;
        _isSignal = true;

        Debug.Log($"{LogPrefix} 명령 수신 | isForward={_isForward}, duration={_duration:F2}s");
    }

    // ============================================================
    // 이동 종료
    // ============================================================

    /**
     * @brief  동작을 중지하고 상태를 초기화한다.
     */
    private void StopMovement()
    {
        _isSignal = false;
        _elapsedTime = 0f;
        Debug.Log($"{LogPrefix} 동작 완료");
    }

    // ============================================================
    // 초기화
    // ============================================================

    /**
     * @brief  wheel1 메쉬 바운딩 박스에서 바퀴 반지름을 자동 계산한다.
     *         extents.y = Y축 절반 크기 = 반지름 (로컬 좌표)
     *         lossyScale.y 를 곱해 월드 기준 실제 반지름으로 변환한다.
     * @throws MissingComponentException  wheel1 오브젝트에 MeshFilter 미부착 시
     */
    private void InitializeWheelRadius()
    {
        MeshFilter meshFilter = _wheel1Joint.ObjectB.GetComponent<MeshFilter>();

        if (meshFilter == null)
        {
            Debug.LogError($"{LogPrefix} wheel1 오브젝트에 MeshFilter 가 없습니다.");
            enabled = false;
            return;
        }

        Bounds bounds = meshFilter.mesh.bounds;
        _wheelRadius = bounds.extents.y * _wheel1Joint.ObjectB.lossyScale.y;
    }

    /**
     * @brief  LineBCenter 기준 각 바퀴의 초기 위치 오프셋을 _centerOffsets 배열에 저장한다.
     *         RotateWheel 에서 피벗 보정에 사용된다.
     */
    private void InitializeCenterOffsets()
    {
        _centerOffsets[0] = _wheel1Joint.ObjectB.position - _wheel1Joint.LineBCenter;
        _centerOffsets[1] = _wheel2Joint.ObjectB.position - _wheel2Joint.LineBCenter;
        _centerOffsets[2] = _wheel3Joint.ObjectB.position - _wheel3Joint.LineBCenter;
        _centerOffsets[3] = _wheel4Joint.ObjectB.position - _wheel4Joint.LineBCenter;
    }

    // ============================================================
    // 회전 처리
    // ============================================================

    /**
     * @brief  바퀴 하나를 LineBCenter 기준으로 자전시킨다.
     *         오프셋을 회전시켜 피벗을 보정한 뒤 rotation 을 적용한다.
     * @param  joint     회전 대상 RevoluteJointComponent
     * @param  rotation  적용할 회전 Quaternion
     * @param  offset    LineBCenter 기준 바퀴 위치 오프셋 (ref 로 매 프레임 갱신됨)
     */
    private static void RotateWheel(
        RevoluteJointComponent joint,
        Quaternion rotation,
        ref Vector3 offset)
    {
        offset = rotation * offset;
        joint.ObjectB.position = joint.LineBCenter + offset;
        joint.ObjectB.rotation = rotation * joint.ObjectB.rotation;
    }

    // ============================================================
    // 유효성 검사
    // ============================================================

    /**
     * @brief  Inspector 할당 필수 오브젝트의 유효성을 검사한다.
     *         하나라도 미할당 시 컴포넌트를 비활성화하고 false 를 반환한다.
     * @return bool  모두 유효하면 true
     */
    private bool ValidateComponents()
    {
        bool isValid = true;

        if (_mqttSubscriber == null)
        {
            Debug.LogError($"{LogPrefix} MqttSubscriber 가 할당되지 않았습니다.");
            isValid = false;
        }

        if (_wheel1Joint == null || _wheel2Joint == null ||
            _wheel3Joint == null || _wheel4Joint == null)
        {
            Debug.LogError($"{LogPrefix} 바퀴 조인트가 할당되지 않았습니다.");
            isValid = false;
        }

        if (_shuttle == null)
        {
            Debug.LogError($"{LogPrefix} _shuttle 이 할당되지 않았습니다.");
            isValid = false;
        }

        if (!isValid) enabled = false;
        return isValid;
    }
}