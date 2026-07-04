using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

namespace MathBoxing.Backend
{
    public class MatchmakingManager : MonoBehaviour
    {
        [Header("Supabase Credentials")]
        [SerializeField] private string supabaseURL = "https://YOUR_PROJECT_ID.supabase.co";
        [SerializeField] private string supabaseApiKey = "YOUR_ANON_KEY";

        [Header("Match Info (Output)")]
        public string currentMatchId = "";
        public bool isPlayer1 = false;
        public bool isMatchReady = false;

        // ID unik player (sementara bisa pakai serial hardware atau random uuid)
        private string myPlayerId;

        private void Start()
        {
            myPlayerId = System.Guid.NewGuid().ToString();
        }

        public void FindMatch()
        {
            StartCoroutine(FindOrCreateMatchCoroutine());
        }

        private IEnumerator FindOrCreateMatchCoroutine()
        {
            Debug.Log("[Matchmaking] Mencari pertandingan yang tersedia...");

            // Kueri mencari match yang statusnya masih 'waiting' dan p2_id masih kosong
            string checkUrl = $"{supabaseURL}/rest/v1/live_matches?status=eq.waiting&p2_id=is.null&limit=1";

            using (UnityWebRequest request = UnityWebRequest.Get(checkUrl))
            {
                request.SetRequestHeader("apikey", supabaseApiKey);
                request.SetRequestHeader("Authorization", $"Bearer {supabaseApiKey}");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success && request.downloadHandler.text != "[]")
                {
                    // GAME SANGAT LOGIS: Jika ada room kosong, kita masuk sebagai Player 2!
                    string jsonResponse = request.downloadHandler.text;
                    
                    // Ambil match_id dari string response secara manual (menghindari library json external)
                    currentMatchId = ExtractValueFromJson(jsonResponse, "match_id");
                    isPlayer1 = false;

                    yield return StartCoroutine(JoinAsPlayer2(currentMatchId));
                }
                else
                {
                    // Jika tidak ada room, kita buat room baru dan bertindak sebagai Player 1!
                    isPlayer1 = true;
                    yield return StartCoroutine(CreateNewMatchRoom());
                }
            }
        }

        private IEnumerator CreateNewMatchRoom()
        {
            string url = $"{supabaseURL}/rest/v1/live_matches";
            currentMatchId = System.Guid.NewGuid().ToString();

            // Payload: status waiting, p1_id diisi, p1_score & p2_score mulai dari 0
            string jsonPayload = "{" +
                $"\"match_id\":\"{currentMatchId}\"," +
                $"\"p1_id\":\"{myPlayerId}\"," +
                "\"status\":\"waiting\"," +
                "\"p1_score\":0," +
                "\"p2_score\":0" +
                "}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", supabaseApiKey);
                request.SetRequestHeader("Authorization", $"Bearer {supabaseApiKey}");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[Matchmaking] Room Berhasil Dibuat! ID: {currentMatchId}. Menunggu Player 2...");
                    // Tetap tunggu sampai Player 2 bergabung
                    isMatchReady = false;
                }
            }
        }

        private IEnumerator JoinAsPlayer2(string matchId)
        {
            string url = $"{supabaseURL}/rest/v1/live_matches?match_id=eq.{matchId}";
            
            // Update status menjadi 'active' dan isi p2_id
            string jsonPayload = "{" +
                $"\"p2_id\":\"{myPlayerId}\"," +
                "\"status\":\"active\"" +
                "}";

            using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", supabaseApiKey);
                request.SetRequestHeader("Authorization", $"Bearer {supabaseApiKey}");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[Matchmaking] Sukses Masuk ke Match: {matchId} sebagai Player 2! Game Dimulai!");
                    isMatchReady = true;
                }
            }
        }

        private string ExtractValueFromJson(string json, string key)
        {
            int keyIndex = json.IndexOf($"\"{key}\":\"");
            if (keyIndex == -1) return "";
            int startIndex = keyIndex + key.Length + 4;
            int endIndex = json.IndexOf("\"", startIndex);
            return json.Substring(startIndex, endIndex - startIndex);
        }
    }
}