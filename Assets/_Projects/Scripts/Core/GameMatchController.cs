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
        [SerializeField] private TextMeshProUGUI questionTextField;
        [SerializeField] private TextMeshProUGUI timerTextField; 

        [Header("Multiplayer Net & Config")]
        [SerializeField] private MathBoxing.Backend.MatchmakingManager matchmakingManager;
        [SerializeField] private MathBoxing.Backend.SupabaseRealtimeListener realtimeListener;
        [SerializeField] private MathBoxing.Backend.SupabaseManager supabaseManager;

        [Header("Game Over UI Components")]
        [SerializeField] private GameObject gameOverPanel; 
        [SerializeField] private TextMeshProUGUI finalScoreTextField; 

        [Header("UI Panels")]
        [SerializeField] private GameObject matchmakingPanel; // Seret Matchmaking_Panel ke sini!

        [Header("Score System")]
        [SerializeField] private int totalScore = 0; 

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

                // ============================================================
                // MOMEN SINKRONISASI VISUAL & LOGIKA (PERMAINAN DIMULAI)
                // ============================================================
                Debug.Log("<color=green>[Controller] Pertandingan SIAP! Menutup panel penantian...</color>");
                
                if (matchmakingPanel != null) matchmakingPanel.SetActive(false);

                // PERBAIKAN MUTLAK: Panggil StartMatch() agar status game aktif & timer berdetak!
                StartMatch(); 
            }
        }

        private void StartMatch()
        {
            Debug.Log("<color=cyan>[Controller] Memulai inisiasi ring pertarungan matematika!</color>");
            totalScore = 0;
            timeRemaining = 60f; 
            isGameActive = true; // Kunci pengaman numpad terbuka detik ini!
            
            StartNewQuestion(); // Generate pertanyaan pertama lewat generator resmi kamu
            StartCoroutine(MatchTimerCoroutine()); // Hidupkan bom waktu pertandingan
        }

        private IEnumerator MatchTimerCoroutine()
        {
            while (timeRemaining > 0 && isGameActive)
            {
                if (timerTextField != null) 
                    timerTextField.text = $"Time: {Mathf.CeilToInt(timeRemaining)}s";
                
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
                // Menggunakan generator resmi kamu agar sinkron dengan sistem pengecekan
                currentQuestion = mathGenerator.GenerateRandomQuestion();
                if (questionTextField != null) 
                {
                    questionTextField.text = currentQuestion.questionText;
                    Debug.Log($"[Controller] Soal Ditampilkan: {currentQuestion.questionText} | Kunci: {currentQuestion.correctAnswer}");
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
                // 1. Tambahkan skor lokal secara instan
                totalScore += currentQuestion.scoreValue;
                Debug.Log($"<color=green>Jawaban BENAR!</color> +{currentQuestion.scoreValue} Poin. Total: {totalScore}");
                
                // 2. KALIBRASI: Amankan visual terlebih dahulu!
                // Munculkan soal berikutnya DETIK INI JUGA tanpa menunggu jaringan!
                StartNewQuestion();

                // 3. Tembakkan pembaruan skor ke server di latar belakang (Fire-and-Forget)
                if (supabaseManager != null && matchmakingManager != null)
                {
                    supabaseManager.UpdateMatchScore(matchmakingManager.currentMatchId, matchmakingManager.isPlayer1, totalScore);
                }
            }
            else
            {
                Debug.Log($"<color=red>Jawaban SALAH!</color> Input: {playerAnswer} | Kunci Seharusnya: {currentQuestion.correctAnswer}");
                if (numpadController != null) numpadController.TriggerWrongAnswerPenalty();
            }
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
}