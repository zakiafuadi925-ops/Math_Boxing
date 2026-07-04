using UnityEngine;
using TMPro;

namespace MathBoxing.Core
{
    public class GameMatchController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MathBoxing.UI.NumpadController numpadController;
        [SerializeField] private TextMeshProUGUI questionTextField; // UI tempat soal muncul

        private int currentCorrectAnswer;

        private void OnEnable()
        {
            // Berlangganan ke event OnAnswerSubmitted milik Numpad
            if (numpadController != null)
            {
                numpadController.OnAnswerSubmitted += HandleAnswerSubmitted;
            }
        }

        private void OnDisable()
        {
            // Memutus hubungan agar tidak terjadi memory leak
            if (numpadController != null)
            {
                numpadController.OnAnswerSubmitted -= HandleAnswerSubmitted;
            }
        }

        private void Start()
        {
            StartNewQuestion();
        }

        private void StartNewQuestion()
        {
            // Membuat soal acak sederhana
            int num1 = Random.Range(1, 10);
            int num2 = Random.Range(1, 10);
            currentCorrectAnswer = num1 + num2;

            if (questionTextField != null)
            {
                questionTextField.text = $"{num1} + {num2} = ?";
            }
        }

        private void HandleAnswerSubmitted(int playerAnswer)
        {
            // Pengecekan jawaban yang dikirim dari tombol ENTER Numpad
            if (playerAnswer == currentCorrectAnswer)
            {
                Debug.Log("<color=green>GameMatchController: Jawaban BENAR!</color>");
                StartNewQuestion();
            }
            else
            {
                Debug.Log("<color=red>GameMatchController: Jawaban SALAH!</color>");
            }
        }
    }
}