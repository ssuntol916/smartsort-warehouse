// ============================================================
// 파일명  : ConnectionPanelView.cs
// 역할    : Supabase 및 Raspberry Pi 연결 상태를 색상으로 표시한다.
//           Connected(초록), Disconnected(빨강), Connecting(노랑)
// 작성자  : 이현화
// 작성일  : 2026-04-08
// 수정이력:
// ============================================================

using UnityEngine;
using UnityEngine.UI;

public class ConnectionPanelView : MonoBehaviour
{
    [Header("Supabase 상태")]
    [SerializeField] private Image _supabaseCircle;   // Supabase 상태 원

    [Header("Raspberry Pi 상태")]
    [SerializeField] private Image _rpiCircle;        // Raspberry Pi 상태 원

    // 상태 색상
    private static readonly Color ColorConnected = new Color(0f, 0.8f, 0f, 1f);  // 초록
    private static readonly Color ColorDisconnected = new Color(0.8f, 0f, 0f, 1f);  // 빨강
    private static readonly Color ColorConnecting = new Color(0.8f, 0.8f, 0f, 1f);  // 노랑

    public enum ConnectionStatus { Connected, Disconnected, Connecting }

    //TODO: Supabase 및 RPi 연결 이벤트로 교체
    /**
     * @brief  초기 상태를 Connecting으로 설정한다.
     */
    void Start()
    {
        SetSupabaseStatus(ConnectionStatus.Connecting);
        SetRpiStatus(ConnectionStatus.Connecting);
    }

    /**
     * @brief  Supabase 연결 상태를 설정한다.
     */
    public void SetSupabaseStatus(ConnectionStatus status)
    {
        _supabaseCircle.color = StatusToColor(status);
    }

    /**
     * @brief  Raspberry Pi 연결 상태를 설정한다.
     */
    public void SetRpiStatus(ConnectionStatus status)
    {
        _rpiCircle.color = StatusToColor(status);
    }

    /**
     * @brief  ConnectionStatus를 색상으로 변환한다.
     */
    private Color StatusToColor(ConnectionStatus status)
    {
        return status switch
        {
            ConnectionStatus.Connected => ColorConnected,
            ConnectionStatus.Disconnected => ColorDisconnected,
            ConnectionStatus.Connecting => ColorConnecting,
            _ => ColorDisconnected,
        };
    }
}
