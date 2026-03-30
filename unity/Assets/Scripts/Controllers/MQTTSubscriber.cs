// ============================================================
// 파일명  : MQTTSubscriber.cs
// 역할    : MQTT 메시지의 Unity 수신을 위한 브릿지
// 작성자  : 송준호
// 작성일  : 2026-03-30
// 수정이력: 
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
    [SerializeField] private string _brokerHost = "smartsort-broker.local";
    [SerializeField] private int _brokerPort = 1883;
    [SerializeField] private string _topic = "warehouse/cmd/shuttle";

    private IMqttClient _mqttClient;
    private CancellationTokenSource _cts;

    private async void Start()
    {
        _cts = new CancellationTokenSource();
        await ConnectAndSubscribe();
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

        // 토픽 구독
        var subscribeOptions = factory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => f
                .WithTopic(_topic)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
            .Build();

        await _mqttClient.SubscribeAsync(subscribeOptions, _cts.Token);
        Debug.Log($"[MQTT] 토픽 구독 시작: {_topic}");
    }

    private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

        Debug.Log($"[MQTT] 수신 — Topic: {topic}, Payload: {payload}");

        // MQTT 콜백은 별도 스레드이므로 Unity API 호출 시 메인 스레드로 전달
        UnityMainThread.Execute(() =>
        {
            // 여기서 payload를 파싱하여 디지털 트윈에 반영
            // 예: JsonUtility.FromJson<T>(payload)
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