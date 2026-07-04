using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

namespace MathBoxing.Backend
{
    public class SupabaseManager : MonoBehaviour
    {
        [Header("Supabase Credentials")]
        [SerializeField] private string supabaseURL = "https://YOUR_PROJECT_ID.supabase.co";
        [SerializeField] private string supabaseApiKey = "YOUR_ANON_KEY";
        [SerializeField] private string tableName = "live_matches"; // Gunakan tabel ini!

        /// <summary>
        /// Mengupdate skor ke Supabase menggunakan metode PATCH berdasarkan ID Match aktif
        /// </summary>
        public void UpdateMatchScore(string matchId, bool isPlayer1, int currentScore)
        {
            StartCoroutine(PatchScoreCoroutine(matchId, isPlayer1, currentScore));
        }

        private IEnumerator PatchScoreCoroutine(string matchId, bool isPlayer1, int currentScore)
        {
            // 1. Tentukan kolom mana yang akan diupdate berdasarkan peran pemain
            MatchScorePayload payload = new MatchScorePayload();
            if (isPlayer1) payload.p1_score = currentScore;
            else payload.p2_score = currentScore;

            string jsonPayload = JsonUtility.ToJson(payload);
            
            // Jika hanya ingin mengupdate satu kolom tanpa menimpa kolom lain dengan angka 0,
            // kita bersihkan string JSON-nya secara manual agar hanya berisi kolom yang kita tuju
            if (isPlayer1)
                jsonPayload = "{\"p1_score\":" + currentScore + "}";
            else
                jsonPayload = "{\"p2_score\":" + currentScore + "}";

            // 2. Gunakan query filter untuk mencari match_id yang sesuai
            // Contoh URL: https://id.supabase.co/rest/v1/live_matches?match_id=eq.uuid-match
            string url = $"{supabaseURL}/rest/v1/{tableName}?match_id=eq.{matchId}";

            // 3. Buat UnityWebRequest dengan metode PATCH
            using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                // 4. Set Header wajib
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", supabaseApiKey);
                request.SetRequestHeader("Authorization", $"Bearer {supabaseApiKey}");
                request.SetRequestHeader("Prefer", "return=minimal"); // Menghemat bandwidth server

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"<color=green>[Supabase]</color> Skor Match {matchId} Berhasil Diperbarui di Server!");
                }
                else
                {
                    Debug.LogError($"<color=red>[Supabase] Gagal Update:</color> {request.error} | {request.downloadHandler.text}");
                }
            }
        }
    }
}