using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement; // Wajib untuk memuat ulang scene/pindah menu

namespace MathBoxing.Core
{
    public class GameMatchController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MathBoxing.UI.NumpadController numpadController;
        [SerializeField] private MathGenerator mathGenerator; 
        [SerializeField] private TextMeshProUGUI questionTextField;
        [SerializeField] private TextMeshProUGUI timerTextField; 

        [Header("Game Over UI Components")]
        [SerializeField] private GameObject gameOverPanel; // Tarik Game_Over_Panel ke sini
        [SerializeField] private TextMeshProUGUI finalScoreTextField; // Tarik Final_Score_Text ke sini

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
            if (numpadController != null) numpadController.OnAnswerSubmitted -= HandleOriginalAnswer;
        }

        private void HandleOriginalAnswer(int answer) => HandleAnswerSubmitted(answer);

        private void Start()
        {
            if (mathGenerator == null) mathGenerator = FindObjectOfType<MathGenerator>();
            
            // Pastikan panel game over tertutup di awal game
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

            // TAMPILKAN PANEL GAME OVER
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            // TAMPILKAN SKOR AKHIR KE PANEL
            if (finalScoreTextField != null)
            {
                finalScoreTextField.text = $"FINAL SCORE: {totalScore}";
            }

            Debug.Log($"<color=yellow>Pertandingan Selesai!</color> Skor Akhir: {totalScore}");
        }

        // --- FUNGSI PUBLIK UNTUK TOMBOL-TOMBOL UI ---

        public void RetryGame()
        {
            // Memuat ulang scene yang sedang aktif saat ini (Main Lagi)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void QuitGame()
        {
            Debug.Log("Keluar dari Game...");
            Application.Quit(); // Hanya bekerja saat game sudah di-build (Windows 11 .exe)
        }
    }
}