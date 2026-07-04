using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

namespace MathBoxing.Core
{
    public class GameMatchController : MonoBehaviour
    {
        [Header("Multiplayer Net")]
        [SerializeField] private MathBoxing.Backend.MatchmakingManager matchmakingManager;
        [SerializeField] private MathBoxing.Backend.SupabaseRealtimeListener realtimeListener;
        [SerializeField] private MathBoxing.Backend.SupabaseManager supabaseManager;

        [Header("References")]
        [SerializeField] private MathBoxing.UI.NumpadController numpadController;
        [SerializeField] private MathGenerator mathGenerator; 
        [SerializeField] private TextMeshProUGUI questionTextField;
        [SerializeField] private TextMeshProUGUI timerTextField; 

        // Hubungkan ke skrip SupabaseManager baru kita
        [Header("Backend Integration")]
        [SerializeField] private MathBoxing.Backend.SupabaseManager supabaseManager;
        [SerializeField] private string currentPlayerName = "Player_Ryzen"; // Nama sementara sebelum sistem Auth aktif

        [Header("Game Over UI Components")]
        [SerializeField] private GameObject gameOverPanel; 
        [SerializeField] private TextMeshProUGUI finalScoreTextField; 

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
            if (mathGenerator == null) mathGenerator = FindObjectOfType<MathGenerator>();
            if (supabaseManager == null) supabaseManager = FindObjectOfType<MathBoxing.Backend.SupabaseManager>();
            
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            StartMatch();
        }

        private void StartMatch()
        {
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
                if (timerTextField != null) timerTextField.text = $"Time: {Mathf.CeilToInt(timeRemaining)}s";
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
                if (questionTextField != null) questionTextField.text = currentQuestion.questionText;
            }
        }

        private void HandleAnswerSubmitted(int playerAnswer)
        {
            if (!isGameActive) return; 

            if (playerAnswer == currentQuestion.correctAnswer)
            {
                totalScore += currentQuestion.scoreValue;
                Debug.Log($"<color=green>Jawaban BENAR!</color> +{currentQuestion.scoreValue} Poin. Total: {totalScore}");
                
                // TEMBAKKAN SKOR SECARA REALTIME KE TABEL LIVE_MATCHES SESUAI ROLE!
                if (supabaseManager != null && matchmakingManager != null)
                {
                    supabaseManager.UpdateMatchScore(matchmakingManager.currentMatchId, matchmakingManager.isPlayer1, totalScore);
                }

                StartNewQuestion();
            }
            else
            {
                Debug.Log("<color=red>Jawaban SALAH!</color>");
                if (numpadController != null) numpadController.TriggerWrongAnswerPenalty();
            }
        }

        private void EndMatch()
        {
            isGameActive = false;
            
            if (timerTextField != null) timerTextField.text = "TIME UP!";
            if (questionTextField != null) questionTextField.text = "FINISHED";

            if (gameOverPanel != null) gameOverPanel.SetActive(true);

            if (finalScoreTextField != null) finalScoreTextField.text = $"FINAL SCORE: {totalScore}";

            Debug.Log($"<color=yellow>Pertandingan Selesai!</color> Skor Akhir: {totalScore}");

            // DI SINI TEMBAKAN API DIKIRIMKAN!
            if (supabaseManager != null)
            {
                supabaseManager.SaveScore(currentPlayerName, totalScore);
            }
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