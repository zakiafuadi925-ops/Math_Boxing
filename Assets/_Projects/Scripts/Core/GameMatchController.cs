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

        private void OnEnable()
        {
            if (numpadController != null) numpadController.OnAnswerSubmitted += HandleAnswerSubmitted;
        }

        private void OnDisable()
        {
            if (numpadController != null) numpadController.OnAnswerSubmitted -= HandleAnswerSubmitted;
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
                    timerTextField.text = $"Sisa Waktu: {Mathf.CeilToInt(timeRemaining)}s"; // Kalibrasi teks agar sesuai estetika barumu
                
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
            if (!isGameActive) return; 

            if (playerAnswer == currentQuestion.correctAnswer)
            {
                totalScore += currentQuestion.scoreValue;
                Debug.Log($"<color=green>Jawaban BENAR!</color> +{currentQuestion.scoreValue} Poin. Total: {totalScore}");
                
                int randomAttack = Random.Range(1, 5); 

                // ELEMEN AKSI: Player 1 (Matikan coroutine lama jika ada, lalu jalankan yang baru)
                if (player1Animator != null)
                {
                    if (player1ResetCoroutine != null) StopCoroutine(player1ResetCoroutine);
                    player1Animator.SetInteger("actionType", randomAttack); 
                    player1ResetCoroutine = StartCoroutine(ResetActionTypeCoroutine(player1Animator, 1));
                }

                // ELEMEN REAKSI: Player 2 
                if (player2Animator != null)
                {
                    if (player2ResetCoroutine != null) StopCoroutine(player2ResetCoroutine);
                    player2Animator.SetInteger("actionType", 6); // Boxer_Hit
                    player2ResetCoroutine = StartCoroutine(ResetActionTypeCoroutine(player2Animator, 2));
                }

                if (supabaseManager != null && matchmakingManager != null)
                {
                    supabaseManager.UpdateMatchScore(matchmakingManager.currentMatchId, matchmakingManager.isPlayer1, totalScore);
                }

                StartNewQuestion();
            }
            else
            {
                Debug.Log("<color=red>Jawaban SALAH!</color>");
                
                int randomEnemyAttack = Random.Range(1, 5);

                if (player1Animator != null)
                {
                    if (player1ResetCoroutine != null) StopCoroutine(player1ResetCoroutine);
                    player1Animator.SetInteger("actionType", 6); // Player 1 kena hit
                    player1ResetCoroutine = StartCoroutine(ResetActionTypeCoroutine(player1Animator, 1));
                }
                
                if (player2Animator != null)
                {
                    if (player2ResetCoroutine != null) StopCoroutine(player2ResetCoroutine);
                    player2Animator.SetInteger("actionType", randomEnemyAttack); // Player 2 memukul
                    player2ResetCoroutine = StartCoroutine(ResetActionTypeCoroutine(player2Animator, 2));
                }
                
                if (numpadController != null) numpadController.TriggerWrongAnswerPenalty();
            }
        }

        // Coroutine pembantu untuk mengembalikan sirkuit ke Boxer_Idle (actionType = 0)
        private IEnumerator ResetActionTypeCoroutine(Animator targetAnimator, int playerIndex)
        {
            // Gunakan jeda waktu yang sangat singkat agar game terasa responsif (cth: 0.2 detik)
            yield return new WaitForSeconds(0.2f); 
            
            if (targetAnimator != null)
            {
                targetAnimator.SetInteger("actionType", 0); 
            }

            // Kosongkan referensi pelacak setelah reset berhasil diselesaikan
            if (playerIndex == 1) player1ResetCoroutine = null;
            if (playerIndex == 2) player2ResetCoroutine = null;
        }

        
        
        private void EndMatch()
        {
            isGameActive = false;
            
            if (timerTextField != null) timerTextField.text = "TIME UP!";
            if (questionTextField != null) questionTextField.text = "FINISHED";

            if (realtimeListener != null) realtimeListener.StopListening();

            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            if (finalScoreTextField != null) finalScoreTextField.text = $"FINAL SCORE: {totalScore}";
        }

        public void RetryGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }
} //Commit 13/07/2-26
