using UnityEngine;

namespace MathBoxing.Core
{
    public struct MathQuestion
    {
        public string questionText;
        public int correctAnswer;
        public int scoreValue; 
    }

    public class MathGenerator : MonoBehaviour
    {
        // Fungsi generik biasa
        public MathQuestion GenerateRandomQuestion()
        {
            return GenerateQuestionInternal();
        }

        // Fungsi dengan Seed khusus agar P1 dan P2 mendapat soal yang sama persis
        public MathQuestion GenerateSeededQuestion(int seed)
        {
            Random.InitState(seed);
            return GenerateQuestionInternal();
        }

        private MathQuestion GenerateQuestionInternal()
        {
            MathQuestion newQuestion = new MathQuestion();
            
            int operationType = Random.Range(0, 3); 
            int num1 = Random.Range(1, 10);
            int num2 = Random.Range(1, 10);

            switch (operationType)
            {
                case 0: // Penjumlahan
                    newQuestion.questionText = $"{num1} + {num2} = ?";
                    newQuestion.correctAnswer = num1 + num2;
                    newQuestion.scoreValue = 2;
                    break;

                case 1: // Pengurangan
                    if (num1 < num2) { int temp = num1; num1 = num2; num2 = temp; }
                    newQuestion.questionText = $"{num1} - {num2} = ?";
                    newQuestion.correctAnswer = num1 - num2;
                    newQuestion.scoreValue = 2;
                    break;

                case 2: // Perkalian
                    newQuestion.questionText = $"{num1} x {num2} = ?";
                    newQuestion.correctAnswer = num1 * num2;
                    newQuestion.scoreValue = 5;
                    break;
            }

            return newQuestion;
        }
    }
}