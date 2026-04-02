// ============================================================
// 파일명  : WarehouseManager.cs
// 역할    : Supabase Realtime WebSocket을 통해 `inventories` 테이블의 변경을 실시간 수신
// 작성자  : 송준호
// 작성일  : 2026-03-30
// 수정이력: 
// ============================================================

using System;
using System.Threading.Tasks;
using UnityEngine;
using Supabase;
using Supabase.Realtime;
using Supabase.Realtime.PostgresChanges;
using static Supabase.Realtime.PostgresChanges.PostgresChangesOptions;

public class WarehouseManager : MonoBehaviour
{
    [Header("Supabase 설정")]
    [SerializeField] private SupabaseConfig config;

    private Supabase.Client _supabase;
    private RealtimeChannel _inventoryChannel;

    // === 이벤트: 외부 컴포넌트가 구독하여 씬 반영 ===
    public event Action<InventoryRecord> OnInventoryInserted;
    public event Action<InventoryRecord> OnInventoryUpdated;
    public event Action<InventoryRecord> OnInventoryDeleted;

    private async void Start()
    {
        await InitializeSupabase();
        await SubscribeToInventories();
    }

    private async Task InitializeSupabase()
    {
        var options = new SupabaseOptions
        {
            AutoConnectRealtime = true
        };

        _supabase = new Supabase.Client(config.supabaseUrl, config.anonKey, options);
        await _supabase.InitializeAsync();

        Debug.Log($"[WarehouseManager] Supabase 연결 완료 ({config.supabaseUrl})");
    }

    private async Task SubscribeToInventories()
    {
        _inventoryChannel = _supabase.Realtime.Channel("inventory-changes");

        // postgres_changes 이벤트 등록
        _inventoryChannel.Register(new PostgresChangesOptions(
            "public", "inventories"
        ));

        // INSERT 핸들러
        _inventoryChannel.AddPostgresChangeHandler(
            ListenType.Inserts,
            (sender, change) =>
            {
                var record = change.Model<InventoryRecord>();
                Debug.Log($"[Realtime] INSERT — Bin: {record.BinId}, Part: {record.PartId}, Qty: {record.QtyCount}");

                // Unity 메인 스레드에서 이벤트 발행
                UnityMainThread.Execute(() => OnInventoryInserted?.Invoke(record));
            }
        );

        // UPDATE 핸들러
        _inventoryChannel.AddPostgresChangeHandler(
            ListenType.Updates,
            (sender, change) =>
            {
                var record = change.Model<InventoryRecord>();
                Debug.Log($"[Realtime] UPDATE — Bin: {record.BinId}, Qty: {record.QtyCount}");

                UnityMainThread.Execute(() => OnInventoryUpdated?.Invoke(record));
            }
        );

        // DELETE 핸들러
        _inventoryChannel.AddPostgresChangeHandler(
            ListenType.Deletes,
            (sender, change) =>
            {
                var record = change.Model<InventoryRecord>();
                Debug.Log($"[Realtime] DELETE — Id: {record.Id}");

                UnityMainThread.Execute(() => OnInventoryDeleted?.Invoke(record));
            }
        );

        await _inventoryChannel.Subscribe();
        Debug.Log("[WarehouseManager] inventories Realtime 구독 시작");
    }

    private void OnDestroy()
    {
        if (_inventoryChannel != null)
        {
            _inventoryChannel.Unsubscribe();
            Debug.Log("[WarehouseManager] Realtime 구독 해제");
        }
    }
}