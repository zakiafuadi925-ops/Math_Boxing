using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using MathBoxing.UI;

namespace MathBoxing.Backend
{
    public class MatchmakingManager : MonoBehaviour
    {
        [Header("Testing Rules (Untuk 2 Player)")]
        public bool forceAsPlayer1 = true;    // CENTANG INI UNTUK JADI HOST (P1)

        [Header("UI Component")]
        [SerializeField] private TMP_Text matchmakingTimerText; 

        [Header("UI Reference")]
        [SerializeField] private LobbyPanelController lobbyPanelController;

        // Variabel untuk menyimpan kode room yang baru saja dibuat
        public string CurrentRoomCode { get; private set; }

        [Header("Timeout Rules")]
        public float matchmakingTimeout = 30f; 

        [Header("Match Info (Output)")]
        public string currentMatchId = ""; 
        public string currentRoomCode = ""; 
        public bool isPlayer1 = false;
        public bool isMatchReady = false;

        private string myPlayerId;
        private Coroutine createRoomCoroutineInstance;

        private const string SavedMatchIdKey = "TEMP_SIMULATED_MATCH_ID";

        [Header("References")]
        [SerializeField] private SupabaseRealtimeListener realtimeListener; 
        [SerializeField] private SupabaseManager supabaseManager;

        private void Awake()
        {
            myPlayerId = System.Guid.NewGuid().ToString();
            Debug.Log($"[Matchmaking] Player ID dikalibrasi ke UUID Steril via Awake: {myPlayerId}");

            if (supabaseManager == null)
            {
                supabaseManager = FindAnyObjectByType<SupabaseManager>();
            }

            if (supabaseManager != null && supabaseManager.gameObject.activeInHierarchy)
            {
                // Steril
            }
            else
            {
                Debug.LogWarning("<color=yellow>[Matchmaking] SupabaseManager non-aktif.</color>");
            }
        }

        public void FindMatch()
        {
            if (forceAsPlayer1)
            {
                isPlayer1 = true;
                createRoomCoroutineInstance = StartCoroutine(CreateRoomCoroutine());
            }
            else
            {
                isPlayer1 = false;
                string savedId = PlayerPrefs.GetString(SavedMatchIdKey, "");
                StartCoroutine(JoinRoomCoroutine(savedId));
            }
        }

        public void CancelMatchmaking()
        {
            Debug.Log("<color=red>[Matchmaking] Player membatalkan pencarian lawan secara manual!</color>");
            
            if (createRoomCoroutineInstance != null) 
                StopCoroutine(createRoomCoroutineInstance);
            
            if (realtimeListener != null) 
                realtimeListener.StopListening();

            if (isPlayer1 && !string.IsNullOrEmpty(currentMatchId))
            {
                if (supabaseManager != null && supabaseManager.gameObject.activeInHierarchy)
                {
                    StartCoroutine(DeleteRoomFromServerCoroutine(currentMatchId));
                }
            }

            isMatchReady = false;
            currentMatchId = "";
            currentRoomCode = "";
        }

        public string GenerateRoomCode(int length = 4)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            char[] stringChars = new char[length];
            for (int i = 0; i < length; i++)
            {
                stringChars[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
            }
            return new string(stringChars);
        }

        public IEnumerator StartTimeoutCountdown()
        {
            float timer = matchmakingTimeout;
            while (timer > 0 && !isMatchReady)
            {
                timer -= Time.deltaTime;

                if (matchmakingTimerText != null)
                {
                    matchmakingTimerText.text = $"Sisa Waktu: {Mathf.CeilToInt(timer)}s";
                }
                yield return null;
            }

            if (!isMatchReady)
            {
                Debug.LogWarning($"[Matchmaking] Waktu habis ({matchmakingTimeout}s)! Tidak ada lawan ditemukan.");
                CancelMatchmaking();
            }
        }

        private IEnumerator CreateRoomCoroutine()
        {
            while (ConfigManager.Instance == null || !ConfigManager.Instance.IsLoaded)
            {
                yield return null;
            }

            var configData = ConfigManager.Instance.Config;
            if (configData == null)
            {
                Debug.LogError("[Matchmaking] Gagal: Data Config kosong!");
                yield break;
            }

            currentMatchId = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString(SavedMatchIdKey, currentMatchId);
            PlayerPrefs.Save();

            Debug.Log($"<color=yellow>[P1-Host]</color> Menembak Kamar Baru Berdasarkan ERD: {currentMatchId}");

            string url = $"{configData.supabaseURL}/rest/v1/live_matches";

            string jsonPayload = "{" +
                $"\"match_id\":\"{currentMatchId}\"," +
                $"\"p1_id\":\"{myPlayerId}\"," +
                "\"status\":\"waiting\"," +
                "\"current_question\":\"0+0\"," + 
                "\"current_answer\":0," +          
                "\"question_version\":1," +        
                "\"p1_score\":0," +
                "\"p2_score\":0," +
                "\"time_remaining\":60" +
                "}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", configData.supabaseApiKey);
                request.SetRequestHeader("Authorization", $"Bearer {configData.supabaseApiKey}");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success || request.responseCode == 201)
                {
                    Debug.Log($"<color=green>[Matchmaking] Room P1 tercetak di Supabase!</color>");
                    isMatchReady = false;

                    if (realtimeListener != null)
                    {
                        realtimeListener.StartListening();
                    }
                }
                else
                {
                    Debug.LogError($"[Matchmaking] P1 GAGAL! Respon Aturan Database: {request.downloadHandler.text}");
                }
            }
        }

        private IEnumerator JoinRoomCoroutine(string targetMatchId)
        {
            while (ConfigManager.Instance == null || !ConfigManager.Instance.IsLoaded)
            {
                yield return null;
            }

            var configData = ConfigManager.Instance.Config;
            if (configData == null) yield break;

            if (string.IsNullOrEmpty(targetMatchId))
            {
                Debug.LogError("[Matchmaking] P2 GAGAL: Tidak menemukan data Room lama!");
                yield break;
            }

            currentMatchId = targetMatchId;

            string url = $"{configData.supabaseURL}/rest/v1/live_matches?match_id=eq.{targetMatchId}";
            
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
                request.SetRequestHeader("apikey", configData.supabaseApiKey);
                request.SetRequestHeader("Authorization", $"Bearer {configData.supabaseApiKey}");
                request.SetRequestHeader("Prefer", "return=representation");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success || request.responseCode == 200)
                {
                    currentMatchId = targetMatchId;
                    Debug.Log($"<color=green>[Matchmaking] SUKSES! Kamu masuk sebagai Player 2. Pertandingan AKTIF!</color>");
                    isMatchReady = true;
                }
                else
                {
                    Debug.LogError($"[Matchmaking] P2 GAGAL! Respon: {request.downloadHandler.text}");
                }
            }
        }

        private IEnumerator DeleteRoomFromServerCoroutine(string matchId)
        {
            while (ConfigManager.Instance == null || !ConfigManager.Instance.IsLoaded)
            {
                yield return null;
            }

            var configData = ConfigManager.Instance.Config;
            if (configData == null) yield break;

            string url = $"{configData.supabaseURL}/rest/v1/live_matches?match_id=eq.{matchId}";

            using (UnityWebRequest request = new UnityWebRequest(url, "DELETE"))
            {
                request.SetRequestHeader("apikey", configData.supabaseApiKey);
                request.SetRequestHeader("Authorization", $"Bearer {configData.supabaseApiKey}");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"<color=gray>[Matchmaking] Kamar {matchId} dibersihkan.</color>");
                }
            }
        }

        // ========================================================================
        // INTEGRASI PRIVATE ROOM (MAIN BERSAMA TEMAN)
        // ========================================================================

        public void CreatePrivateRoom()
        {
            isPlayer1 = true;
            currentRoomCode = GenerateRoomCode(4);
            Debug.Log($"<color=cyan>[Private Room] Membuat Room dengan Kode: {currentRoomCode}</color>");
            
            if (lobbyPanelController != null)
            {
                lobbyPanelController.DisplayCreatedRoomCode(currentRoomCode);
            }
            createRoomCoroutineInstance = StartCoroutine(CreatePrivateRoomCoroutine(currentRoomCode));
        }

        private IEnumerator CreatePrivateRoomCoroutine(string roomCode)
        {
            while (ConfigManager.Instance == null || !ConfigManager.Instance.IsLoaded)
            {
                yield return null;
            }

            var configData = ConfigManager.Instance.Config;
            if (configData == null)
            {
                Debug.LogError("[Matchmaking] Gagal: Data Config kosong!");
                yield break;
            }

            currentMatchId = System.Guid.NewGuid().ToString();
            string url = $"{configData.supabaseURL}/rest/v1/live_matches";

            string jsonPayload = "{" +
                $"\"match_id\":\"{currentMatchId}\"," +
                $"\"p1_id\":\"{myPlayerId}\"," +
                $"\"room_code\":\"{roomCode}\"," +
                "\"status\":\"waiting\"," +
                "\"current_question\":\"0+0\"," + 
                "\"current_answer\":0," +          
                "\"question_version\":1," +        
                "\"p1_score\":0," +
                "\"p2_score\":0," +
                "\"time_remaining\":60" +
                "}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", configData.supabaseApiKey);
                request.SetRequestHeader("Authorization", $"Bearer {configData.supabaseApiKey}");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success || request.responseCode == 201)
                {
                    Debug.Log($"<color=green>[Private Room] Berhasil dibuat! Kode: {roomCode}</color>");
                    isMatchReady = false;

                    if (realtimeListener != null)
                    {
                        realtimeListener.StartListening();
                    }
                }
                else
                {
                    Debug.LogError($"[Private Room] Gagal buat room: {request.downloadHandler.text}");
                }
            }
        }

        public void JoinPrivateRoom(string inputCode)
        {
            inputCode = inputCode.ToUpper().Trim();

            if (string.IsNullOrEmpty(inputCode))
            {
                Debug.LogWarning("[Private Room] Kode room tidak boleh kosong!");
                return;
            }

            isPlayer1 = false;
            StartCoroutine(JoinPrivateRoomCoroutine(inputCode));
        }

        private IEnumerator JoinPrivateRoomCoroutine(string roomCode)
        {
            while (ConfigManager.Instance == null || !ConfigManager.Instance.IsLoaded)
            {
                yield return null;
            }

            var configData = ConfigManager.Instance.Config;
            if (configData == null) yield break;

            Debug.Log($"<color=cyan>[Private Room]</color> Mencarikan Room dengan Kode: {roomCode}");

            string url = $"{configData.supabaseURL}/rest/v1/live_matches?room_code=eq.{roomCode}&status=eq.waiting";

            using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
            {
                string jsonPayload = "{" +
                    $"\"p2_id\":\"{myPlayerId}\"," +
                    "\"status\":\"active\"" +
                    "}";

                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", configData.supabaseApiKey);
                request.SetRequestHeader("Authorization", $"Bearer {configData.supabaseApiKey}");
                request.SetRequestHeader("Prefer", "return=representation");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success || request.responseCode == 200)
                {
                    Debug.Log($"<color=green>[Private Room] Berhasil join ke kode {roomCode}! Game Siap!</color>");
                    isMatchReady = true;
                }
                else
                {
                    Debug.LogError($"[Private Room] Gagal Join! Kode salah atau room penuh. Error: {request.downloadHandler.text}");
                }
            }
        }

        private void OnDisable()
        {
            CancelMatchmaking();
            StopAllCoroutines();
            Debug.Log("<color=gray>[MatchmakingManager]</color> Coroutine jaringan dihentikan dengan aman.");
        }

        public void OnOpponentJoined()
        {
            if (isPlayer1)
            {
                Debug.Log("<color=green>[Matchmaking] Sinyal Realtime Diterima! Lawan telah masuk. Pertandingan SIAP!</color>");
                isMatchReady = true; 
            }
        }
    }
}