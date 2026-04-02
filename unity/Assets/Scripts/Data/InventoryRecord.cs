// ============================================================
// 파일명  : InventoryRecord.cs
// 역할    : Supabase `inventories` 테이블에 대응하는 C# 데이터 모델
// 작성자  : 송준호
// 작성일  : 2026-03-30
// 수정이력: 
// ============================================================

using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

[Table("inventories")]
public class InventoryRecord : BaseModel
{
    [PrimaryKey("id", false)]
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("bin_id")]
    public string BinId { get; set; }

    [JsonProperty("part_id")]
    public string PartId { get; set; }

    [JsonProperty("qty_count")]
    public long QtyCount { get; set; }

    [JsonProperty("created_at")]
    public string CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public string UpdatedAt { get; set; }
}
