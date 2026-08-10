using UnityEngine;
using TMPro;
using System.Collections;

namespace MathBoxing.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class NumpadController : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI inputDisplayTextField; 
        [SerializeField] private CanvasGroup numpadCanvasGroup;         

        [Header("Settings")]
        [SerializeField] private int maxInputLength = 5; 

        private string currentInputString = "";
        private bool isLocked = false;
        private Color originalTextColor;

        public delegate void SubmitAnswerHandler(int answer);
        public event SubmitAnswerHandler OnAnswerSubmitted;

        private void Awake()
        {
            if (numpadCanvasGroup == null)
            {
                numpadCanvasGroup = GetComponent<CanvasGroup>();
            }

            if (inputDisplayTextField != null)
            {
                originalTextColor = inputDisplayTextField.color;
            }
        }

        private void OnEnable()
        {
            // Pastikan state terkunci selalu di-reset saat Numpad aktif kembali
            UnlockNumpadUI();
            ResetInput();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            UnlockNumpadUI();
        }

        public void PressNumberButton(string number)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxButtonClick);
            }

            if (isLocked) return;

            // Jika input hanya "0", ganti langsung dengan angka baru
            if (currentInputString == "0")
            {
                currentInputString = number;
                UpdateInputUI();
                return;
            }

            if (currentInputString.Length < maxInputLength)
            {
                currentInputString += number;
                UpdateInputUI();
            }
        }

        public void PressMinusButton()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxButtonClick);
            }

            if (isLocked) return;

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

        public void PressClearButton()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxButtonClear);
            }
            
            if (isLocked) return;
            ResetInput();
        }

        public void PressEnterButton()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxButtonEnter);
            }

            if (isLocked || string.IsNullOrEmpty(currentInputString) || currentInputString == "-") return;

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

        public void TriggerWrongAnswerPenalty()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxWrongAnswer);
            }

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
                numpadCanvasGroup.alpha = 0.5f; 
                numpadCanvasGroup.blocksRaycasts = false; 
            }

            if (inputDisplayTextField != null)
            {
                inputDisplayTextField.text = "Oops!";
                inputDisplayTextField.color = Color.red;
            }

            yield return new WaitForSeconds(1.0f); 

            UnlockNumpadUI();
            ResetInput();
        }

        private void UnlockNumpadUI()
        {
            isLocked = false;

            if (inputDisplayTextField != null)
            {
                inputDisplayTextField.color = originalTextColor;
            }

            if (numpadCanvasGroup != null)
            {
                numpadCanvasGroup.alpha = 1.0f;
                numpadCanvasGroup.blocksRaycasts = true;
            }
        }

        public void ResetInput()
        {
            currentInputString = "";
            UpdateInputUI();
        }

        private void UpdateInputUI()
        {
            if (inputDisplayTextField != null)
            {
                inputDisplayTextField.text = string.IsNullOrEmpty(currentInputString) ? "?" : currentInputString;
            }
        }
    }
}