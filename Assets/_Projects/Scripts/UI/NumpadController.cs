using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

namespace MathBoxing.UI
{
    public class NumpadController : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI inputDisplayTextField; // Menampilkan angka yang sedang diketik
        [SerializeField] private CanvasGroup numpadCanvasGroup;        // Digunakan untuk mengunci seluruh tombol saat penalti

        [Header("Settings")]
        [SerializeField] private int maxInputLength = 5; // Batas aman digit (cth: untuk jawaban volume/jarak ratusan)

        private string currentInputString = "";
        private bool isLocked = false;

        // Event yang akan didengarkan oleh MathGameManager untuk validasi
        public delegate void SubmitAnswerHandler(int answer);
        public event SubmitAnswerHandler OnAnswerSubmitted;

        private void Start()
        {
            ResetInput();
        }

        /// <summary>
        /// Fungsi ini dipanggil oleh Button 0-9 di Unity Inspectormu
        /// </summary>
        public void PressNumberButton(string number)
        {
            if (isLocked) return;

            if (currentInputString.Length < maxInputLength)
            {
                currentInputString += number;
                UpdateInputUI();
            }
        }

        /// <summary>
        /// Fungsi untuk tombol minus [-] (Penting untuk aljabar)
        /// </summary>
        public void PressMinusButton()
        {
            if (isLocked) return;

            // Tombol minus hanya boleh di depan angka
            if (currentInputString.Length == 0)
            {
                currentInputString = "-";
                UpdateInputUI();
            }
        }

        /// <summary>
        /// Fungsi untuk tombol [CLR] (Clear/Hapus semua)
        /// </summary>
        public void PressClearButton()
        {
            if (isLocked) return;
            ResetInput();
        }

        /// <summary>
        /// Fungsi untuk tombol [ENTER] (Kirim Jawaban)
        /// </summary>
        public void PressEnterButton()
        {
            if (isLocked || string.IsNullOrEmpty(currentInputString) || currentInputString == "-") return;

            // Konversi input string ke Integer secara aman
            if (int.TryParse(currentInputString, out int submmitedAnswer))
            {
                // Lempar data jawaban ke GameManager untuk dicek secara lokal
                OnAnswerSubmitted?.Invoke(submmitedAnswer);
            }
            
            ResetInput();
        }

        /// <summary>
        /// Panggil fungsi ini dari GameManager jika jawaban ANAK SALAH (Penalti Lock 1 Detik)
        /// </summary>
        public void TriggerWrongAnswerPenalty()
        {
            StartCoroutine(PenaltyCooldownCoroutine());
        }

        private IEnumerator PenaltyCooldownCoroutine()
        {
            isLocked = true;
            numpadCanvasGroup.alpha = 0.5f; // Efek visual redup saat terkunci
            inputDisplayTextField.text = "Oops!";
            inputDisplayTextField.color = Color.red;

            yield return new WaitForSeconds(1.0f); // Durasi lock sesuai GDD

            inputDisplayTextField.color = Color.white;
            ResetInput();
            numpadCanvasGroup.alpha = 1.0f;
            isLocked = false;
        }

        public void ResetInput()
        {
            currentInputString = "";
            inputDisplayTextField.text = "?";
        }

        private void UpdateInputUI()
        {
            inputDisplayTextField.text = currentInputString;
        }
    }
}