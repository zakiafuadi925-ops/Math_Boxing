using UnityEngine;

namespace MathBoxing.Backend
{
    [CreateAssetMenu(fileName = "SupabaseConfig", menuName = "MathBoxing/Supabase Config")]
    public class SupabaseConfig : ScriptableObject
    {
        public string supabaseURL = "https://YOUR_PROJECT_ID.supabase.co";
        [TextArea(2, 5)] public string supabaseApiKey = "YOUR_ANON_KEY";
    }
}