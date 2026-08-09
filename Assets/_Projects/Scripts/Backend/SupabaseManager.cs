using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

namespace MathBoxing.Backend
{
    public class SupabaseManager : MonoBehaviour
    {
        [Header("Database Settings")]
        [SerializeField] private string tableName = "live_matches";

        public void UpdateMatchScore(string matchId, bool isPlayer1, int currentScore)
        {
            StartCoroutine(PatchScoreCoroutine(matchId, isPlayer1, currentScore));
        }

        private IEnumerator PatchScoreCoroutine(string matchId, bool isPlayer1, int currentScore)
        {
            // Tunggu ConfigManager siap
            while (ConfigManager.Instance == null || !ConfigManager.Instance.IsLoaded)
            {
                yield return null;
            }

            var configData = ConfigManager.Instance.Config;
            if (configData == null)
            {
                Debug.LogError("[SupabaseManager] Config Data belum dimuat!");
                yield break;
            }

            string jsonPayload = isPlayer1 ? "{\"p1_score\":" + currentScore + "}" : "{\"p2_score\":" + currentScore + "}";
            string url = $"{configData.supabaseURL}/rest/v1/{tableName}?match_id=eq.{matchId}";

            using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", configData.supabaseApiKey);
                request.SetRequestHeader("Authorization", $"Bearer {configData.supabaseApiKey}");
                request.SetRequestHeader("Prefer", "return=minimal");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success) 
                    Debug.Log($"<color=green>[Supabase]</color> Skor Match Diperbarui!");
                else 
                    Debug.LogError($"[Supabase] Gagal Patch Skor: {request.error}");
            }
        }

        public string GetUrl()
        {
            return ConfigManager.Instance != null && ConfigManager.Instance.IsLoaded 
                ? ConfigManager.Instance.Config.supabaseURL 
                : "";
        }

        public string GetApiKey()
        {
            return ConfigManager.Instance != null && ConfigManager.Instance.IsLoaded 
                ? ConfigManager.Instance.Config.supabaseApiKey 
                : "";
        }
    }
}