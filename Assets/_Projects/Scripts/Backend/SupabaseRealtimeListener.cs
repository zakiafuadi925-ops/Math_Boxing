using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace MathBoxing.Backend
{
    public class SupabaseRealtimeListener : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MatchmakingManager matchmakingManager;

        [Header("Supabase Credentials")]
        [SerializeField] private string supabaseURL = "https://YOUR_PROJECT_ID.supabase.co";
        [SerializeField] private string supabaseApiKey = "YOUR_ANON_KEY";

        private bool isListening = false;
        public int opponentScore = 0;

        public void StartListening()
        {
            if (matchmakingManager == null) matchmakingManager = FindObjectOfType<MatchmakingManager>();
            isListening = true;
            StartCoroutine(PollMatchStatusCoroutine());
        }

        public void StopListening()
        {
            isListening = false;
        }

        private IEnumerator PollMatchStatusCoroutine()
        {
            while (isListening)
            {
                if (string.IsNullOrEmpty(matchmakingManager.currentMatchId))
                {
                    yield return new WaitForSeconds(1.5f);
                    continue;
                }

                string url = $"{supabaseURL}/rest/v1/live_matches?match_id=eq.{matchmakingManager.currentMatchId}&select=*";

                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    request.SetRequestHeader("apikey", supabaseApiKey);
                    request.SetRequestHeader("Authorization", $"Bearer {supabaseApiKey}");

                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string jsonResponse = request.downloadHandler.text;

                        // 1. Jika kita p1 dan status masih waiting, cek apakah status berubah jadi 'active'
                        if (matchmakingManager.isPlayer1 && !matchmakingManager.isMatchReady)
                        {
                            if (jsonResponse.Contains("\"status\":\"active\""))
                            {
                                matchmakingManager.isMatchReady = true;
                                Debug.Log("<color=cyan>[Listener]</color> Player 2 telah bergabung! Pertandingan Dimulai!");
                            }
                        }

                        // 2. Intip skor musuh secara realtime
                        if (matchmakingManager.isMatchReady)
                        {
                            string scoreKey = matchmakingManager.isPlayer1 ? "p2_score" : "p1_score";
                            string scoreValueStr = ExtractNumericValue(jsonResponse, scoreKey);
                            
                            if (int.TryParse(scoreValueStr, out int parsedScore))
                            {
                                if (parsedScore != opponentScore)
                                {
                                    opponentScore = parsedScore;
                                    Debug.Log($"<color=orange>[Realtime]</color> Skor musuh berubah menjadi: {opponentScore}! Petinju musuh bersiap memukul!");
                                    // TODO: Trigger animasi petinju musuh memukul wajah kita di sini!
                                }
                            }
                        }
                    }
                }

                // Interval polling aman agar tidak membebani PC Ryzen-mu dan rate limit Supabase
                yield return new WaitForSeconds(1.5f);
            }
        }

        private string ExtractNumericValue(string json, string key)
        {
            int keyIndex = json.IndexOf($"\"{key}\":");
            if (keyIndex == -1) return "0";
            int startIndex = keyIndex + key.Length + 3;
            
            // Cari pembatas koma atau kurung kurawal tutup
            int endComma = json.IndexOf(",", startIndex);
            int endBracket = json.IndexOf("}", startIndex);
            int endIndex = (endComma != -1 && endComma < endBracket) ? endComma : endBracket;

            return json.Substring(startIndex, endIndex - startIndex).Trim();
        }
    }
}