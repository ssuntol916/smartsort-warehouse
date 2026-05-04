// ============================================================
// 파일명  : SliderSignalController.cs
// 역할    : MQTT warehouse/cmd/shuttle 토픽의 servo_direct 명령을 수신하여
//           SpurGearController 1/2 에 동작 신호를 전달한다.
//           Inspector 의 _debugSignal 플래그로 에디터 내 수동 테스트를 지원한다.
//
// 작성자  : 이현화
// 작성일  : 2026-04-27
// 수정이력: 2026-05-04 — pin null 체크, 핀 번호 상수화, ValidateComponents 추가
// ============================================================

using System;
using System.Linq;
using UnityEngine;

public class SliderSignalController : MonoBehaviour
{
    // ============================================================
    // Inspector 필드
    // ============================================================

    [Header("필수 오브젝트")]
    [SerializeField] private MqttSubscriber _mqttSubscriber;            // MQTT 브릿지
    [SerializeField] private SpurGearController _spurGearController1;   // 스퍼 기어 1
    [SerializeField] private SpurGearController _spurGearController2;   // 스퍼 기어 2

    [Header("파라미터")]
    [SerializeField] private float _duration = 3f;                      // SpurGear 동작 지속 시간 (초)

    [Header("디버그")]
    [SerializeField] private bool _debugSignal = false;                 // true 로 설정 시 다음 프레임에 신호 발생
    [SerializeField] private bool _debugIsForward = true;               // 디버그 신호 방향

    // ============================================================
    // 상수
    // ============================================================

    private const string TargetTopic = "warehouse/cmd/shuttle";     // 반응할 MQTT 토픽
    private const string TargetAction = "servo_direct";             // 처리할 action 값
    private const int TargetPin1 = 13;                              // 반응할 핀 번호 1
    private const int TargetPin2 = 15;                              // 반응할 핀 번호 2
    private const int AngleForward = 180;                           // isForward=true 에 대응하는 angle 값

    // ============================================================
    // MQTT 페이로드 파싱용 클래스
    // ============================================================

    [Serializable]
    private class ServoDirectPayload  // warehouse/cmd/shuttle 토픽의 servo_direct JSON 페이로드 구조체.(JsonUtility.FromJson 으로 역직렬화)
    {
        public string action; // 명령 종류 — "servo_direct" 만 처리
        public int[] pin;    // 대상 핀 번호 배열
        public int angle;  // 서보 각도 (180 = forward)
    }

    // ============================================================
    // Unity 메시지
    // ============================================================

    private void Start()
    {
        ValidateComponents();
    }

    /**
     * @brief  컴포넌트 활성화 시 MQTT 이벤트를 구독한다.
     */
    private void OnEnable()
    {
        if (_mqttSubscriber != null)
            _mqttSubscriber.OnMessage += HandleMqttMessage;
    }

    /**
     * @brief  컴포넌트 비활성화 시 MQTT 이벤트 구독을 해제한다.
     */
    private void OnDisable()
    {
        if (_mqttSubscriber != null)
            _mqttSubscriber.OnMessage -= HandleMqttMessage;
    }

    /**
     * @brief  _debugSignal 플래그를 감지하여 수동 테스트 신호를 발생시킨다.
     *         플래그는 1프레임 후 자동으로 false 로 초기화된다.
     */
    private void Update()
    {
        if (!_debugSignal) return;

        _debugSignal = false;
        SendSignalToBothGears(_debugIsForward);
    }

    // ============================================================
    // MQTT 처리
    // ============================================================

    /**
     * @brief  MQTT 메시지 수신 핸들러.
     *         토픽 필터 → 파싱 → action 검증 → pin 검증 → 신호 전달 순으로 처리한다.
     * @param  topic    수신된 MQTT 토픽 문자열
     * @param  payload  수신된 JSON 페이로드 문자열
     */
    private void HandleMqttMessage(string topic, string payload)
    {
        if (topic != TargetTopic) return;

        ServoDirectPayload cmd = JsonUtility.FromJson<ServoDirectPayload>(payload);

        if (cmd == null || cmd.action != TargetAction) return;

        // pin 배열 null 체크 — 미포함 페이로드에서 NullReferenceException 방지
        if (cmd.pin == null || cmd.pin.Length == 0)
        {
            Debug.LogWarning("[SliderSignal] pin 배열이 비어 있습니다.");
            return;
        }

        if (!cmd.pin.Contains(TargetPin1) && !cmd.pin.Contains(TargetPin2)) return;

        bool isForward = cmd.angle == AngleForward;
        SendSignalToBothGears(isForward);
    }

    // ============================================================
    // 신호 전달
    // ============================================================

    /**
     * @brief  두 SpurGearController 에 동일한 신호를 전달한다.
     * @param  isForward  회전 방향
     */
    private void SendSignalToBothGears(bool isForward)
    {
        _spurGearController1?.SetSignal(isForward, _duration);
        _spurGearController2?.SetSignal(isForward, _duration);
        Debug.Log($"[SliderSignal] 신호 전달 | isForward={isForward}, duration={_duration}s");
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
            Debug.LogError("[SliderSignal] MqttSubscriber 가 할당되지 않았습니다.");
            isValid = false;
        }

        if (_spurGearController1 == null)
        {
            Debug.LogError("[SliderSignal] SpurGearController1 이 할당되지 않았습니다.");
            isValid = false;
        }

        if (_spurGearController2 == null)
        {
            Debug.LogError("[SliderSignal] SpurGearController2 가 할당되지 않았습니다.");
            isValid = false;
        }

        if (!isValid) enabled = false;
        return isValid;
    }
}