using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

namespace MathBoxing.Core
{
    public class GameMatchController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MathBoxing.UI.NumpadController numpadController;
        [SerializeField] private MathGenerator mathGenerator; 
        [SerializeField] private TextMeshProUGUI questionTextField; // Ini adalah display_text di dalam UI_Display_Panel
        [SerializeField] private TextMeshProUGUI timerTextField;   // Ini Teks di dalam Timer_Panel

        [Header("Visual & Animations")]
        [SerializeField] private Animator player1Animator;
        [SerializeField] private Animator player2Animator; // PERBAIKAN: Referensi wajib untuk mengeksekusi musuh!

        [Header("Multiplayer Net & Config")]
        [SerializeField] private MathBoxing.Backend.MatchmakingManager matchmakingManager;
        [SerializeField] private MathBoxing.Backend.SupabaseRealtimeListener realtimeListener;
        [SerializeField] private MathBoxing.Backend.SupabaseManager supabaseManager;

        [Header("Game Over UI Components")]
        [SerializeField] private GameObject gameOverPanel; 
        [SerializeField] private TextMeshProUGUI finalScoreTextField; 

        [Header("Score System")]
        [SerializeField] private int totalScore = 0; 

        // --- SUNTIKAN KODE BARU UNTUK PELACAK COROUTINE ---
        private Coroutine player1ResetCoroutine;
        private Coroutine player2ResetCoroutine;

        [Header("UI Panels")]
        [SerializeField] private GameObject matchmakingPanel; // PENTING: Jangan diisi Timer_Panel lagi! Isi dengan UI_Display_Panel atau objek panel matchmaking khusus.


        [Header("Timer Settings")]
        [SerializeField] private float timeRemaining = 60f; 
        private bool isGameActive = false;

        private MathQuestion currentQuestion; 

        [Header("UI Score Elements")]
        [SerializeField] private TMPro.TextMeshProUGUI player1ScoreTextField;
        [SerializeField] private TMPro.TextMeshProUGUI player2ScoreTextField;

        private void OnEnable()
        {
            if (numpadController != null) numpadController.OnAnswerSubmitted += HandleAnswerSubmitted;

            if (realtimeListener != null) realtimeListener.OnOpponentScoreChanged += HandleOpponentAttacked;
        }

        private void OnDisable()
        {
            if (numpadController != null) numpadController.OnAnswerSubmitted -= HandleAnswerSubmitted;

            if (realtimeListener != null) realtimeListener.OnOpponentScoreChanged -= HandleOpponentAttacked;
        }

        private void Start()
        {
            // Validasi pencarian komponen secara otomatis pada PC Ryzen milikmu
            if (mathGenerator == null) 
                mathGenerator = FindAnyObjectByType<MathGenerator>();
                
            if (supabaseManager == null) 
                supabaseManager = FindAnyObjectByType<MathBoxing.Backend.SupabaseManager>();
                
            if (matchmakingManager == null) 
                matchmakingManager = FindAnyObjectByType<MathBoxing.Backend.MatchmakingManager>(); 
                
            if (realtimeListener == null) 
                realtimeListener = FindAnyObjectByType<MathBoxing.Backend.SupabaseRealtimeListener>();
            
            if (gameOverPanel != null) 
                gameOverPanel.SetActive(false);

            StartCoroutine(WaitForMatchmakingCoroutine());
        }

        private IEnumerator WaitForMatchmakingCoroutine()
        {
            if (matchmakingManager != null)
            {
                matchmakingManager.FindMatch();
                
                if (matchmakingManager.forceAsPlayer1)
                {
                    StartCoroutine(matchmakingManager.StartTimeoutCountdown());
                }
                
                // Nyalakan panel pencarian di awal fase steril
                if (matchmakingPanel != null) matchmakingPanel.SetActive(true);
                if (questionTextField != null) questionTextField.text = "Mencari Lawan...";

                while (!matchmakingManager.isMatchReady)
                {
                    if (string.IsNullOrEmpty(matchmakingManager.currentMatchId))
                    {
                        if (matchmakingPanel != null) matchmakingPanel.SetActive(false);
                        yield break; 
                    }
                    yield return null;
                }

                Debug.Log("<color=green>[Controller] Pertandingan ready! Menutup panel matchmaking...</color>");
                
                // Matikan panel matchmaking karena pertandingan segera dimulai
                if (matchmakingPanel != null) matchmakingPanel.SetActive(false);

                StartMatch(); 
            }
        }

        private void StartMatch()
        {
            if (player1Animator != null) player1Animator.speed = 1f;
            if (player2Animator != null) player2Animator.speed = 1f;
            Debug.Log("<color=cyan>[Controller] Memulai inisiasi ring pertarungan matematika!</color>");
            totalScore = 0;
            timeRemaining = 60f; 
            isGameActive = true; 
            
            StartNewQuestion(); 
            StartCoroutine(MatchTimerCoroutine()); 
        }

        private IEnumerator MatchTimerCoroutine()
        {
            while (timeRemaining > 0 && isGameActive)
            {
                if (timerTextField != null) 
                    timerTextField.text = $"Timer: {Mathf.CeilToInt(timeRemaining)}s"; // Kalibrasi teks agar sesuai estetika barumu
                
                yield return new WaitForSeconds(1f);
                timeRemaining--;
            }
            EndMatch();
        }

        private void StartNewQuestion()
        {
            if (!isGameActive) return; 

            if (mathGenerator != null)
            {
                currentQuestion = mathGenerator.GenerateRandomQuestion();
                if (questionTextField != null) 
                {
                    questionTextField.text = currentQuestion.questionText;
                }
            }
            else
            {
                Debug.LogError("[Controller] MathGenerator tidak ditemukan di Scene!");
            }
        }

        private void HandleAnswerSubmitted(int playerAnswer)
        {
            if (!isGameActive) return; //

            if (playerAnswer == currentQuestion.correctAnswer)
            {
                totalScore += currentQuestion.scoreValue; //
                Debug.Log($"<color=green>Jawaban BENAR!</color> +{currentQuestion.scoreValue} Poin. Total: {totalScore}"); //[cite: 2]
                
                int randomAttack = Random.Range(1, 5); // Pukulan P1 (1-4)[cite: 2]

                // ELEMEN AKSI: Player 1 Memukul (1-4)[cite: 2]
                if (player1Animator != null)
                {
                    if (player1ResetCoroutine != null) StopCoroutine(player1ResetCoroutine); //[cite: 2]
                    player1Animator.SetInteger("actionType", randomAttack); //[cite: 2]
                    player1ResetCoroutine = StartCoroutine(ResetActionTypeCoroutine(player1Animator, 1)); //[cite: 2]
                }

                // ELEMEN REAKSI MULTIPLAYER: Player 2 (50% Peluang Block / 50% Kena Pukul)
                if (player2Animator != null)
                {
                    if (player2ResetCoroutine != null) StopCoroutine(player2ResetCoroutine); //[cite: 2]
                    
                    bool isEnemyBlocking = Random.value > 0.5f; // Acak status pertahanan musuh
                    if (isEnemyBlocking)
                    {
                        player2Animator.SetBool("isBlocking", true);
                        player2ResetCoroutine = StartCoroutine(ResetBlockStatusCoroutine(player2Animator, 2));
                        Debug.Log("<color=yellow>[Mekanik] Player 2 berhasil melakukan BLOCK!</color>");
                    }
                    else
                    {
                        player2Animator.SetInteger("actionType", 6); // Terkena Hit[cite: 2]
                        player2ResetCoroutine = StartCoroutine(ResetActionTypeCoroutine(player2Animator, 2)); //[cite: 2]
                    }
                }

                if (supabaseManager != null && matchmakingManager != null)
                {
                    supabaseManager.UpdateMatchScore(matchmakingManager.currentMatchId, matchmakingManager.isPlayer1, totalScore); //[cite: 2]
                }

                StartNewQuestion(); //[cite: 2]
            }
            else
            {
                Debug.Log("<color=red>Jawaban SALAH!</color>"); //[cite: 2]
                int randomEnemyAttack = Random.Range(1, 5); //[cite: 2]

                // ELEMEN REAKSI LOKAL: Player 1 (50% Peluang Block / 50% Kena Pukul)
                if (player1Animator != null)
                {
                    if (player1ResetCoroutine != null) StopCoroutine(player1ResetCoroutine); //[cite: 2]
                    
                    bool isPlayer1Blocking = Random.value > 0.5f;
                    if (isPlayer1Blocking)
                    {
                        player1Animator.SetBool("isBlocking", true);
                        player1ResetCoroutine = StartCoroutine(ResetBlockStatusCoroutine(player1Animator, 1));
                        Debug.Log("<color=yellow>[Mekanik] Kamu (Player 1) berhasil BLOCK serangan!</color>");
                    }
                    else
                    {
                        player1Animator.SetInteger("actionType", 6); // Terkena Hit[cite: 2]
                        player1ResetCoroutine = StartCoroutine(ResetActionTypeCoroutine(player1Animator, 1)); //[cite: 2]
                    }
                }
                
                // Player 2 (Musuh) Memukul[cite: 2]
                if (player2Animator != null)
                {
                    if (player2ResetCoroutine != null) StopCoroutine(player2ResetCoroutine); //[cite: 2]
                    player2Animator.SetInteger("actionType", randomEnemyAttack); //[cite: 2]
                    player2ResetCoroutine = StartCoroutine(ResetActionTypeCoroutine(player2Animator, 2)); //[cite: 2]
                }
                
                if (numpadController != null) numpadController.TriggerWrongAnswerPenalty(); //[cite: 2]
            }
        }

        // --- JALUR PIPA MULTIPLAYER: SAAT LAWAN BERHASIL MENJAWAB BENAR DI LAYARNYA ---
        private void HandleOpponentAttacked(int newOpponentScore)
        {
            Debug.Log($"<color=magenta>[Realtime] Lawan menyerang! Skor mereka: {newOpponentScore}</color>");

            // Pilihan serangan musuh yang tampak di layar kita: 1 s/d 4
            int randomEnemyAttack = Random.Range(1, 5);

            // Player 2 (Musuh) memukul visual
            if (player2Animator != null)
            {
                if (player2ResetCoroutine != null) StopCoroutine(player2ResetCoroutine);
                player2Animator.SetInteger("actionType", randomEnemyAttack);
                player2ResetCoroutine = StartCoroutine(ResetActionTypeCoroutine(player2Animator, 2));
            }

            // Player 1 (Karakter kita) dipaksa memutar animasi Terkena Hit (6)
            if (player1Animator != null)
            {
                if (player1ResetCoroutine != null) StopCoroutine(player1ResetCoroutine);
                player1Animator.SetInteger("actionType", 6); 
                player1ResetCoroutine = StartCoroutine(ResetActionTypeCoroutine(player1Animator, 1));
            }
        }

        // KALIBRASI 3: Berikan waktu bernapas pada klip animasi agar tidak terpotong instan
        private IEnumerator ResetActionTypeCoroutine(Animator targetAnimator, int playerIndex)
        {
            // Naikkan waktu tunggu dari 0.2s ke 0.4s agar transisi gerakan selesai dengan mulus
            yield return new WaitForSeconds(0.4f); 
            
            if (targetAnimator != null)
            {
                targetAnimator.SetInteger("actionType", 0); // Kembali ke Idle[cite: 2]
            }

            if (playerIndex == 1) player1ResetCoroutine = null; //[cite: 2]
            if (playerIndex == 2) player2ResetCoroutine = null; //[cite: 2]
        }

        private IEnumerator ResetBlockStatusCoroutine(Animator targetAnimator, int playerIndex)
        {
            yield return new WaitForSeconds(0.3f); // Durasi menahan serangan sebelum kembali normal
            
            if (targetAnimator != null)
            {
                targetAnimator.SetBool("isBlocking", false); // Matikan pertahanan
            }

            if (playerIndex == 1) player1ResetCoroutine = null;
            if (playerIndex == 2) player2ResetCoroutine = null;
        }        
        
        // Panggil fungsi ini di Start() untuk menginisialisasi skor ke 0
        private void InitializeScoreUI()
        {
            if (player1ScoreTextField != null) player1ScoreTextField.text = "0";
            if (player2ScoreTextField != null) player2ScoreTextField.text = "0";
        }

        // Fungsi steril untuk memperbarui UI Skor lokal saat kamu mencetak poin
        private void UpdateLocalScoreUI(int newScore)
        {
            if (player1ScoreTextField != null)
            {
                player1ScoreTextField.text = newScore.ToString();
            }
        }

        // Fungsi steril untuk memperbarui UI Skor lawan saat Realtime Listener mendeteksi perubahan di database
        public void UpdateOpponentScoreUI(int opponentScore)
        {
            if (player2ScoreTextField != null)
            {
                player2ScoreTextField.text = opponentScore.ToString();
            }
        }

        private void EndMatch()
        {
            isGameActive = false; //[cite: 2]
            
            if (timerTextField != null) timerTextField.text = "TIME UP!"; //[cite: 2]
            if (questionTextField != null) questionTextField.text = "FINISHED"; //[cite: 2]

            if (realtimeListener != null)
            {
                realtimeListener.StopListening(); //[cite: 2]
                
                // EVALUASI LOGIKA PEMENANG: Bandingkan skormu dengan skor musuh di listener
                int finalOpponentScore = realtimeListener.opponentScore; //
                
                if (totalScore < finalOpponentScore)
                {
                    // Kamu kalah, Player 1 Knockdown!
                    if (player1Animator != null) player1Animator.SetBool("isDead", true);
                    Debug.Log("<color=red>[Match Over] Kamu KO!</color>");
                }
                else if (totalScore > finalOpponentScore)
                {
                    // Kamu menang, Player 2 Knockdown!
                    if (player2Animator != null) player2Animator.SetBool("isDead", true);
                    Debug.Log("<color=green>[Match Over] Lawan KO!</color>");
                }
                // Jika seri, kedua karakter dibiarkan tetap berdiri idle
            }

            if (gameOverPanel != null) gameOverPanel.SetActive(true); //[cite: 2]
            if (finalScoreTextField != null) finalScoreTextField.text = $"FINAL SCORE: {totalScore}"; //[cite: 2]
        }

        public void RetryGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        // --- FUNGSI BARU: DIKIRIM LANGSUNG DARI JALUR PIPA SUPABASE REALTIME ---
        
    }

} //Commit 13/07/2-26
