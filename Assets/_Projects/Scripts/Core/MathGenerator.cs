using UnityEngine;

namespace MathBoxing.Core
{
    // Struktur data untuk membungkus paket soal
    public struct MathQuestion
    {
        public string questionText;
        public int correctAnswer;
        public int scoreValue; // Bobot skor untuk soal ini
    }

    public class MathGenerator : MonoBehaviour
    {
        public MathQuestion GenerateRandomQuestion()
        {
            MathQuestion newQuestion = new MathQuestion();
            
            // Acak tipe soal: 0 = Penjumlahan, 1 = Pengurangan, 2 = Perkalian
            int operationType = Random.Range(0, 3); 

            int num1 = Random.Range(1, 10);
            int num2 = Random.Range(1, 10);

            switch (operationType)
            {
                case 0: // Penjumlahan
                    newQuestion.questionText = $"{num1} + {num2} = ?";
                    newQuestion.correctAnswer = num1 + num2;
                    newQuestion.scoreValue = 2; // Skor penjumlahan = 2
                    break;

                case 1: // Pengurangan
                    // Agar tidak menghasilkan angka negatif di awal, kita pastikan num1 lebih besar
                    if (num1 < num2) { int temp = num1; num1 = num2; num2 = temp; }
                    newQuestion.questionText = $"{num1} - {num2} = ?";
                    newQuestion.correctAnswer = num1 - num2;
                    newQuestion.scoreValue = 2; // Skor pengurangan = 2
                    break;

                case 2: // Perkalian
                    newQuestion.questionText = $"{num1} x {num2} = ?";
                    newQuestion.correctAnswer = num1 * num2;
                    newQuestion.scoreValue = 5; // Skor perkalian = 5
                    break;
            }

            return newQuestion;
        }
    }
}