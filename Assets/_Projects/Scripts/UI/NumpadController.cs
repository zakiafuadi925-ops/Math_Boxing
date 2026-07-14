using UnityEngine;
using TMPro;
using System.Collections;

namespace MathBoxing.UI
{
    [RequireComponent(typeof(CanvasGroup))] // Memastikan CanvasGroup selalu ada secara aman
    public class NumpadController : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI inputDisplayTextField; // Menampilkan angka yang sedang diketik
        [SerializeField] private CanvasGroup numpadCanvasGroup;         // Mengunci seluruh tombol saat penalti

        [Header("Settings")]
        [SerializeField] private int maxInputLength = 5; // Batas aman digit

        private string currentInputString = "";
        private bool isLocked = false;
        private Color originalTextColor;

        // Event yang akan didengarkan oleh MathGameManager untuk validasi
        public delegate void SubmitAnswerHandler(int answer);
        public event SubmitAnswerHandler OnAnswerSubmitted;

        private void Start()
        {
            // Validasi komponen dasar agar tidak menghasilkan NullReferenceException yang memalukan
            if (numpadCanvasGroup == null)
            {
                numpadCanvasGroup = GetComponent<CanvasGroup>();
            }

            if (inputDisplayTextField != null)
            {
                originalTextColor = inputDisplayTextField.color;
            }
            else
            {
                Debug.LogError($"[{nameof(NumpadController)}] inputDisplayTextField belum direferensikan di Inspector!");
            }

            ResetInput();
        }

        /// <summary>
        /// Dipanggil oleh Button 0-9 di Unity Inspector.
        /// </summary>
        public void PressNumberButton(string number)
        {
            if (isLocked) return;

            // Cegah input angka nol berlebih di depan (e.g., "0005" menjadi "5")
            if (currentInputString == "0")
            {
                currentInputString = number;
                UpdateInputUI();
                return;
            }
            
            if (currentInputString == "-0")
            {
                currentInputString = "-" + number;
                UpdateInputUI();
                return;
            }

            if (currentInputString.Length < maxInputLength)
            {
                currentInputString += number;
                UpdateInputUI();
            }
        }

        /// <summary>
        /// Dipanggil oleh tombol minus [-] untuk angka negatif.
        /// </summary>
        public void PressMinusButton()
        {
            if (isLocked) return;

            // Logika toggle: Jika kosong, pasang "-". Jika sudah ada "-", hapus "-".
            if (currentInputString.Length == 0)
            {
                currentInputString = "-";
            }
            else if (currentInputString == "-")
            {
                currentInputString = "";
            }
            
            UpdateInputUI();
        }

        /// <summary>
        /// Dipanggil oleh tombol [CLR] (Clear).
        /// </summary>
        public void PressClearButton()
        {
            if (isLocked) return;
            ResetInput();
        }

        /// <summary>
        /// Dipanggil oleh tombol [ENTER] untuk mengirim jawaban.
        /// </summary>
        public void PressEnterButton()
        {
            // Validasi ketat: Jangan kirim jika terkunci, kosong, atau hanya berisi minus
            if (isLocked || string.IsNullOrEmpty(currentInputString) || currentInputString == "-") return;

            // Konversi ke Integer secara aman tanpa resiko crash
            if (int.TryParse(currentInputString, out int submittedAnswer))
            {
                OnAnswerSubmitted?.Invoke(submittedAnswer);
            }
            else
            {
                Debug.LogWarning($"[{nameof(NumpadController)}] Gagal mem-parsing input: {currentInputString}");
            }
            
            ResetInput();
        }

        /// <summary>
        /// Dipanggil dari GameManager jika jawaban salah (Penalti Lock 1 Detik).
        /// </summary>
        public void TriggerWrongAnswerPenalty()
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(PenaltyCooldownCoroutine());
            }
        }

        private IEnumerator PenaltyCooldownCoroutine()
        {
            isLocked = true;
            
            if (numpadCanvasGroup != null)
            {
                numpadCanvasGroup.alpha = 0.5f; // Efek visual redup
                numpadCanvasGroup.blocksRaycasts = false; // Mencegah klik di level UI canvas secara total
            }

            if (inputDisplayTextField != null)
            {
                inputDisplayTextField.text = "Oops!";
                inputDisplayTextField.color = Color.red;
            }

            yield return new WaitForSeconds(1.0f); // Durasi lock sesuai GDD

            if (inputDisplayTextField != null)
            {
                inputDisplayTextField.color = originalTextColor;
            }

            ResetInput();

            if (numpadCanvasGroup != null)
            {
                numpadCanvasGroup.alpha = 1.0f;
                numpadCanvasGroup.blocksRaycasts = true;
            }

            isLocked = false;
        }

        public void ResetInput()
        {
            currentInputString = "";
            if (inputDisplayTextField != null)
            {
                inputDisplayTextField.text = "?";
            }
        }

        private void UpdateInputUI()
        {
            if (inputDisplayTextField != null)
            {
                // Jika input kosong (misal setelah toggle minus), tampilkan "?" kembali
                inputDisplayTextField.text = string.IsNullOrEmpty(currentInputString) ? "?" : currentInputString;
            }
        }
    }
}