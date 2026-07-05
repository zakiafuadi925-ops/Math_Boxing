using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

namespace MathBoxing.Backend
{
    public class SupabaseManager : MonoBehaviour
    {
        [Header("Configuration Asset")]
        [SerializeField] private SupabaseConfig config; // Tarik file asset ke sini
        [SerializeField] private string tableName = "live_matches";

        public void UpdateMatchScore(string matchId, bool isPlayer1, int currentScore)
        {
            StartCoroutine(PatchScoreCoroutine(matchId, isPlayer1, currentScore));
        }

        private IEnumerator PatchScoreCoroutine(string matchId, bool isPlayer1, int currentScore)
        {
            if (config == null) { Debug.LogError("SupabaseConfig Asset belum dipasang!"); yield break; }

            string jsonPayload = isPlayer1 ? "{\"p1_score\":" + currentScore + "}" : "{\"p2_score\":" + currentScore + "}";
            string url = $"{config.supabaseURL}/rest/v1/{tableName}?match_id=eq.{matchId}";

            using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", config.supabaseApiKey);
                request.SetRequestHeader("Authorization", $"Bearer {config.supabaseApiKey}");
                request.SetRequestHeader("Prefer", "return=minimal");

                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success) Debug.Log($"<color=green>[Supabase]</color> Skor Match Diperbarui!");
                else Debug.LogError($"[Supabase] Gagal: {request.error}");
            }
        }
    }
}