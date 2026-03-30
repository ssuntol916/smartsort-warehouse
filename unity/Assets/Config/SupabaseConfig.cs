using UnityEngine;

[CreateAssetMenu(fileName = "SupabaseConfig", menuName = "SmartSort/Supabase Config")]
public class SupabaseConfig : ScriptableObject
{
    [Header("Supabase 접속 정보")]
    public string supabaseUrl = "http://127.0.0.1:54321";
    public string anonKey = "";
}
