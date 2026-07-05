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
            // Menggunakan FindAnyObjectByType yang modern untuk menghindari warning depresi Unity
            if (mathGenerator == null) 
                mathGenerator = FindAnyObjectByType<MathGenerator>();
                
            if (supabaseManager == null) 
                supabaseManager = FindAnyObjectByType<MathBoxing.Backend.SupabaseManager>();
                
            if (matchmakingManager == null) 
                matchmakingManager = FindAnyObjectByType<MathBoxing.Backend.MatchmakingManager>(); // Perbaikan Namespace murni
                
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
                
                // Tunggu sampai room penuh / ready
                while (!matchmakingManager.isMatchReady)
                {
                    if (questionTextField != null) questionTextField.text = "Mencari Lawan...";
                    yield return null;
                }
            }

            // Setelah match ready, aktifkan listener realtime dan mulai game!
            if (realtimeListener != null) realtimeListener.StartListening();
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
                
                // Tembakkan pembaruan skor ke server secara realtime berdasarkan role kita (p1 atau p2)
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