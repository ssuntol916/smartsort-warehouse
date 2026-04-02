// ============================================================
// 파일명  : MQTTSubscriber.cs
// 역할    : MQTT 메시지의 Unity 수신을 위한 브릿지
// 작성자  : 송준호
// 작성일  : 2026-03-30
// 수정이력: 2026-04-01 — OnMessage 이벤트 및 연결 실패 시 자동 재시도 로직 추가
// ============================================================

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

public class MqttSubscriber : MonoBehaviour
{
    [Header("MQTT 설정")]
    [Tooltip("브로커 IP 주소 (Windows에서 .local 호스트명 미지원 → IP 직접 입력)")]
    [SerializeField] private string _brokerHost = "192.168.0.100";
    [SerializeField] private int _brokerPort = 1883;
    [Tooltip("구독할 토픽 목록 (여러 개 가능)")]
    [SerializeField] private string[] _topics = {
        "warehouse/status/shuttle",
        "warehouse/cmd/shuttle"
    };

    [Header("재연결 설정")]
    [Tooltip("연결 실패 시 재시도 대기 시간 (초)")]
    [SerializeField] private float _reconnectDelaySec = 5f;

    /// <summary>
    /// 메시지 수신 시 Unity 메인 스레드에서 호출되는 이벤트.
    /// 파라미터: (topic, payload)
    /// 사용 예: _subscriber.OnMessage += (topic, payload) => { ... };
    /// </summary>
    public event Action<string, string> OnMessage;

    private IMqttClient _mqttClient;
    private CancellationTokenSource _cts;

    private async void Start()
    {
        _cts = new CancellationTokenSource();
        await ConnectWithRetry();
    }

    // ─────────────────────────────────────────
    //  연결 실패 시 무한 재시도
    // ─────────────────────────────────────────
    private async Task ConnectWithRetry()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                await ConnectAndSubscribe();
                return;  // 연결 성공 시 루프 종료
            }
            catch (OperationCanceledException)
            {
                // OnDestroy에서 취소된 경우 — 정상 종료
                return;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MQTT] 연결 실패: {e.Message}\n" +
                                 $"→ {_reconnectDelaySec}초 후 재시도 ({_brokerHost}:{_brokerPort})");

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_reconnectDelaySec), _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    private async Task ConnectAndSubscribe()
    {
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        // 메시지 수신 핸들러 등록
        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceived;

        // 연결 옵션 설정
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_brokerHost, _brokerPort)
            .WithClientId($"unity-twin-{Guid.NewGuid():N}")
            .WithCleanSession()
            .Build();

        // 브로커 연결
        await _mqttClient.ConnectAsync(options, _cts.Token);
        Debug.Log($"[MQTT] 브로커 연결 완료: {_brokerHost}:{_brokerPort}");

        // 토픽 구독 (다중 토픽)
        var subscribeBuilder = factory.CreateSubscribeOptionsBuilder();
        foreach (var topic in _topics)
        {
            subscribeBuilder.WithTopicFilter(f => f
                .WithTopic(topic)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce));
        }
        await _mqttClient.SubscribeAsync(subscribeBuilder.Build(), _cts.Token);
        Debug.Log($"[MQTT] 토픽 구독 시작: {string.Join(", ", _topics)}");

        // 연결이 끊어질 때까지 대기
        await _mqttClient.PingAsync(_cts.Token);
        await Task.Delay(Timeout.Infinite, _cts.Token);
    }

    private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic   = e.ApplicationMessage.Topic;
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

        Debug.Log($"[MQTT] 수신 — Topic: {topic}, Payload: {payload}");

        // MQTT 콜백은 별도 스레드이므로 Unity API 호출 시 메인 스레드로 전달
        UnityMainThread.Execute(() =>
        {
            OnMessage?.Invoke(topic, payload);
        });

        return Task.CompletedTask;
    }

    private async void OnDestroy()
    {
        _cts?.Cancel();
        if (_mqttClient?.IsConnected == true)
        {
            await _mqttClient.DisconnectAsync();
            Debug.Log("[MQTT] 연결 해제");
        }
        _mqttClient?.Dispose();
        _cts?.Dispose();
    }
}