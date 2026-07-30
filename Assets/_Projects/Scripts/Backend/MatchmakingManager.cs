using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using TMPro; // Tambahkan ini di baris atas bersama UnityEngine

namespace MathBoxing.Backend
{
    public class MatchmakingManager : MonoBehaviour
    {
        

        [Header("Testing Rules (Untuk 2 Player)")]
        public bool forceAsPlayer1 = true;    // CENTANG INI UNTUK JADI HOST (P1)

        [Header("UI Component")]
        [SerializeField] private TMP_Text matchmakingTimerText; // Slot untuk menyeret teks timer

        [Header("Timeout Rules")]
        public float matchmakingTimeout = 30f; // Batas waktu menunggu dalam detik

        [Header("Match Info (Output)")]
        public string currentMatchId = ""; 
        public bool isPlayer1 = false;
        public bool isMatchReady = false;

        private string myPlayerId;
        private Coroutine createRoomCoroutineInstance;

        private const string SavedMatchIdKey = "TEMP_SIMULATED_MATCH_ID";

        [Header("Configuration Asset")]
        [SerializeField] private SupabaseConfig config;
        [SerializeField] private SupabaseRealtimeListener realtimeListener; 
        [SerializeField] private SupabaseManager supabaseManager; // <--- TAMBAHKAN BARIS INI!

        private void Awake()
        {
            // Dijamin berjalan paling awal sebelum GameMatchController memanggil FindMatch!
            myPlayerId = System.Guid.NewGuid().ToString();
            Debug.Log($"[Matchmaking] Player ID dikalibrasi ke UUID Steril via Awake: {myPlayerId}");

            // Auto-search SupabaseManager jika slot Inspector lupa diisi
            if (supabaseManager == null)
            {
                supabaseManager = FindAnyObjectByType<SupabaseManager>();
            }

            // CEK PERSYARATAN: Hanya jalankan jika SupabaseManager TIDAK NULL DAN GAMEOBJECT-NYA AKTIF!
            if (supabaseManager != null && supabaseManager.gameObject.activeInHierarchy)
            {
                // CancelMatchmaking();
            }
            else
            {
                Debug.LogWarning("<color=yellow>[Matchmaking] SupabaseManager non-aktif. Mengabaikan pembatalan HTTP request.</color>");
            }
        }

        public void FindMatch()
        {
            if (forceAsPlayer1)
            {
                isPlayer1 = true;
                // Simpan instans coroutine agar bisa dihentikan paksa saat batal
                createRoomCoroutineInstance = StartCoroutine(CreateRoomCoroutine());
            }
            else
            {
                isPlayer1 = false;
                string savedId = PlayerPrefs.GetString(SavedMatchIdKey, "");
                StartCoroutine(JoinRoomCoroutine(savedId));
            }
        }

        // Fungsi yang akan dipanggil oleh Tombol Cancel di UI kamu!
        public void CancelMatchmaking()
        {
            Debug.Log("<color=red>[Matchmaking] Player membatalkan pencarian lawan secara manual!</color>");
            
            // 1. Hentikan coroutine pembuatan kamar jika masih berjalan
            if (createRoomCoroutineInstance != null) 
                StopCoroutine(createRoomCoroutineInstance);
            
            // 2. Matikan pendengar realtime agar tidak bocor memori
            if (realtimeListener != null) 
                realtimeListener.StopListening();

            // 3. Jika kita adalah Host (P1) dan kamar sudah telanjur dibuat:
            if (isPlayer1 && !string.IsNullOrEmpty(currentMatchId))
            {
                // PROTEKSI STERIL: Hanya hapus dari server JIKA SupabaseManager ada DAN GameObject-nya aktif!
                if (supabaseManager != null && supabaseManager.gameObject.activeInHierarchy)
                {
                    StartCoroutine(DeleteRoomFromServerCoroutine(currentMatchId));
                }
                else
                {
                    Debug.LogWarning("<color=yellow>[Matchmaking] SupabaseManager non-aktif. Mengabaikan penghapusan kamar dari server.</color>");
                }
            }

            // 4. Reset status internal
            isMatchReady = false;
            currentMatchId = "";
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

        // Coroutine Timeout Otomatis (Dipanggil dari GameMatchController)
        public IEnumerator StartTimeoutCountdown()
        {
            float timer = matchmakingTimeout;
            while (timer > 0 && !isMatchReady)
            {
                timer -= Time.deltaTime;

                // Perbarui angka di UI kamu setiap frame secara matematis!
                if (matchmakingTimerText != null)
                {
                    // Menyformat angka desimal menjadi hitungan detik bulat (misal: "Sisa Waktu: 175s")
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
            if (config == null) { Debug.LogError("[Fatal] SupabaseConfig belum dipasang!"); yield break; }
            
            currentMatchId = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString(SavedMatchIdKey, currentMatchId);
            PlayerPrefs.Save();

            Debug.Log($"<color=yellow>[P1-Host]</color> Menembak Kamar Baru Berdasarkan ERD: {currentMatchId}");

            string url = $"{config.supabaseURL}/rest/v1/live_matches";

            // Payload presisi memenuhi seluruh kolom NOT NULL (♦) di ERD kamu
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
                request.SetRequestHeader("apikey", config.supabaseApiKey);
                request.SetRequestHeader("Authorization", $"Bearer {config.supabaseApiKey}");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success || request.responseCode == 201)
                {
                    Debug.Log($"<color=green>[Matchmaking] LUAR BIASA! Room P1 lolos sensor ERD dan tercetak di Supabase!</color>");
                    isMatchReady = false;

                    // PICU LISTENER DETIK INI JUGA!
                    if (realtimeListener != null)
                    {
                        realtimeListener.StartListening();
                        Debug.Log("<color=yellow>[Matchmaking]</color> Memulai pipa pengawasan realtime untuk Player 1...");
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
            if (config == null) yield break;
            if (string.IsNullOrEmpty(targetMatchId))
            {
                Debug.LogError("[Matchmaking] P2 GAGAL: Tidak menemukan data Room lama di memori! Jalankan P1 dulu!");
                yield break;
            }

            currentMatchId = targetMatchId;

            Debug.Log($"<color=cyan>[P2-Client]</color> Mencoba melakukan PATCH ke Match ID: {targetMatchId}");
            string url = $"{config.supabaseURL}/rest/v1/live_matches?match_id=eq.{targetMatchId}";
            
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
                request.SetRequestHeader("apikey", config.supabaseApiKey);
                request.SetRequestHeader("Authorization", $"Bearer {config.supabaseApiKey}");
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
                    Debug.LogError($"[Matchmaking] P2 GAGAL! Kode HTTP: {request.responseCode} | Error: {request.error} | Respon: {request.downloadHandler.text}");
                }
            }
        }

        private IEnumerator DeleteRoomFromServerCoroutine(string matchId)
        {
            string url = $"{config.supabaseURL}/rest/v1/live_matches?match_id=eq.{matchId}";

            using (UnityWebRequest request = new UnityWebRequest(url, "DELETE"))
            {
                request.SetRequestHeader("apikey", config.supabaseApiKey);
                request.SetRequestHeader("Authorization", $"Bearer {config.supabaseApiKey}");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"<color=gray>[Matchmaking] Kamar {matchId} berhasil dibersihkan dari server Supabase.</color>");
                }
                else
                {
                    Debug.LogError($"[Matchmaking] Gagal menghapus kamar dari server: {request.error}");
                }
            }
        }

                // 1. BUAT PRIVATE ROOM (HOST)
        public async void CreatePrivateRoom()
        {
            string privateCode = GenerateRoomCode(4); // Contoh hasil: "A8K9"
            
            // Simpan ke Supabase dengan status waiting & is_private = true
            var matchData = new Dictionary<string, object>
            {
                { "player_1_id", localPlayerId },
                { "status", "waiting" },
                { "is_private", true },
                { "room_code", privateCode }
            };

            // Panggil API Supabase Insert ke tabel 'matches'
            // ... (sesuaikan dengan syntax insert Supabase-mu) ...

            Debug.Log($"<color=cyan>[Private Match] Room dibuat dengan Kode: {privateCode}</color>");
            
            // Tampilkan kode ini ke UI Lobby agar pemain bisa memberitahu temannya
            if (lobbyPanelController != null)
            {
                lobbyPanelController.DisplayPrivateRoomCode(privateCode);
            }

            // Mulai listening / polling untuk menunggu Player 2 masuk ke room_code ini
            StartCoroutine(ListenForFriendToJoin(privateCode));
        }

        // 2. JOIN PRIVATE ROOM (GUEST / TEMAN)
        public async void JoinPrivateRoom(string inputCode)
        {
            inputCode = inputCode.ToUpper().Trim(); // Pastikan huruf kapital semua

            if (string.IsNullOrEmpty(inputCode))
            {
                Debug.LogWarning("[Private Match] Kode room tidak boleh kosong!");
                return;
            }

            // Cari di Supabase match dengan room_code = inputCode DAN status = "waiting"
            // Jika Ditemukan: Update player_2_id = localPlayerId & status = "in_progress"
            
            bool isSuccess = await SupabaseJoinMatchByCode(inputCode, localPlayerId);

            if (isSuccess)
            {
                Debug.Log("<color=green>[Private Match] Berhasil masuk ke room teman!</color>");
                // Beritahu Controller untuk memulai match!
            }
            else
            {
                Debug.LogError("[Private Match] Kode room tidak ditemukan atau room sudah penuh!");
                // Tampilkan teks error di UI: "Kode Tidak Valid / Room Penuh!"
            }
        }

        private void OnDisable()
        {
            CancelMatchmaking();
            StopAllCoroutines();
            Debug.Log("<color=gray>[MatchmakingManager]</color> Coroutine jaringan dihentikan dengan aman.");
        }

        // ========================================================================
        // SUNTIKAN KODE: JEMBATAN REALTIME UNTUK PLAYER 1
        // ========================================================================
        public void OnOpponentJoined()
        {
            if (isPlayer1)
            {
                Debug.Log("<color=green>[Matchmaking] Sinyal Realtime Diterima! Player 2 telah menginvasi kamar. Pertandingan SIAP!</color>");
                isMatchReady = true; // <── Ini yang akan memicu GameMatchController untuk START GAME!
            }
        }
        
    }
}