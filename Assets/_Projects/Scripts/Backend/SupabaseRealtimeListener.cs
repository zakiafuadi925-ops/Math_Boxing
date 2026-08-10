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
        [SerializeField] private float pollingInterval = 1.2f;

        private bool isListening = false;
        public int opponentScore = 0;

        public delegate void OpponentScoreChangedHandler(int newScore);
        public event OpponentScoreChangedHandler OnOpponentScoreChanged;

        public void StartListening()
        {
            if (matchmakingManager == null) 
                matchmakingManager = GetComponent<MatchmakingManager>();
            
            if (matchmakingManager == null) 
                matchmakingManager = Object.FindAnyObjectByType<MatchmakingManager>();

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
                if (ConfigManager.Instance == null || !ConfigManager.Instance.IsLoaded || matchmakingManager == null || string.IsNullOrEmpty(matchmakingManager.currentMatchId))
                {
                    yield return new WaitForSeconds(1.0f);
                    continue;
                }

                var configData = ConfigManager.Instance.Config;
                string url = $"{configData.supabaseURL}/rest/v1/{tableName}?match_id=eq.{matchmakingManager.currentMatchId}";

                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    request.SetRequestHeader("apikey", configData.supabaseApiKey);
                    request.SetRequestHeader("Authorization", $"Bearer {configData.supabaseApiKey}");

                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string jsonResponse = request.downloadHandler.text;

                        if (matchmakingManager.isPlayer1 && !matchmakingManager.isMatchReady)
                        {
                            if (jsonResponse.Contains("\"status\":\"active\"") || jsonResponse.Contains("\"status\": \"active\""))
                            {
                                Debug.Log("<color=cyan>[Listener]</color> Player 2 telah bergabung! Memicu OnOpponentJoined()...");
                                matchmakingManager.OnOpponentJoined();
                            }
                        }

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

                yield return new WaitForSeconds(pollingInterval);
            }
        }

        private string ExtractNumericValue(string json, string key)
        {
            string searchPattern = $"\"{key}\":";
            int keyIndex = json.IndexOf(searchPattern);
            if (keyIndex == -1) return "0";

            int startIndex = keyIndex + searchPattern.Length;

            while (startIndex < json.Length && (json[startIndex] == ' ' || json[startIndex] == '\"'))
            {
                startIndex++;
            }

            int endIndex = startIndex;
            while (endIndex < json.Length && char.IsDigit(json[endIndex]))
            {
                endIndex++;
            }

            if (startIndex == endIndex) return "0";

            return json.Substring(startIndex, endIndex - startIndex);
        }
    }
}