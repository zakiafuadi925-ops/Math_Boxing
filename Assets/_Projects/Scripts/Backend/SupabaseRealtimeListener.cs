using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace MathBoxing.Backend
{
    public class SupabaseRealtimeListener : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MatchmakingManager matchmakingManager;

        [Header("Database Settings")]
        [SerializeField] private string tableName = "live_matches";

        private bool isListening = false;
        public int opponentScore = 0;

        // --- DELEGATE / EVENT ---
        public delegate void OpponentScoreChangedHandler(int newScore);
        public event OpponentScoreChangedHandler OnOpponentScoreChanged;

        public void StartListening()
        {
            if (matchmakingManager == null) matchmakingManager = GetComponent<MatchmakingManager>();
            if (matchmakingManager == null) matchmakingManager = Object.FindAnyObjectByType<MatchmakingManager>();

            if (isListening) return; 

            isListening = true;
            StartCoroutine(PollMatchStatusCoroutine());
        }

        public void StopListening()
        {
            isListening = false;
        }

        private void OnDisable()
        {
            StopListening();
            StopAllCoroutines();
            Debug.Log("<color=gray>[Listener]</color> Pipa pengawasan dimatikan dengan aman.");
        }

        private IEnumerator PollMatchStatusCoroutine()
        {
            while (isListening)
            {
                // Tunggu ConfigManager dan matchId tersedia
                if (ConfigManager.Instance == null || !ConfigManager.Instance.IsLoaded || matchmakingManager == null || string.IsNullOrEmpty(matchmakingManager.currentMatchId))
                {
                    yield return new WaitForSeconds(1.5f);
                    continue;
                }

                var configData = ConfigManager.Instance.Config;
                // RUTE QUERY REST API SUPABASE DENGAN PARAMETER MATCH_ID
                string url = $"{configData.supabaseURL}/rest/v1/{tableName}?match_id=eq.{matchmakingManager.currentMatchId}";

                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    request.SetRequestHeader("apikey", configData.supabaseApiKey);
                    request.SetRequestHeader("Authorization", $"Bearer {configData.supabaseApiKey}");

                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string jsonResponse = request.downloadHandler.text;

                        // 1. Jika Player 1 dan status di database sudah berubah jadi 'active'
                        if (matchmakingManager.isPlayer1 && !matchmakingManager.isMatchReady)
                        {
                            if (jsonResponse.Contains("\"status\":\"active\""))
                            {
                                matchmakingManager.OnOpponentJoined(); // Memanggil penanda resmi bahwa lawan sudah masuk
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
                                    Debug.Log($"<color=orange>[Realtime]</color> Skor musuh berubah menjadi: {opponentScore}!");
                                    OnOpponentScoreChanged?.Invoke(opponentScore);
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[Listener] Gagal Polling: {request.error}");
                    }
                }

                // Interval polling aman 1.5 detik
                yield return new WaitForSeconds(1.5f);
            }
        }

        private string ExtractNumericValue(string json, string key)
        {
            int keyIndex = json.IndexOf($"\"{key}\":");
            if (keyIndex == -1) return "0";
            int startIndex = keyIndex + key.Length + 3;
            
            int endComma = json.IndexOf(",", startIndex);
            int endBracket = json.IndexOf("}", startIndex);
            int endIndex = (endComma != -1 && endComma < endBracket) ? endComma : endBracket;

            if (endIndex == -1) return "0";

            return json.Substring(startIndex, endIndex - startIndex).Trim();
        }
    }
}