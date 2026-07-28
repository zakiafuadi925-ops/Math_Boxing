using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace MathBoxing.UI
{
    public class LobbyPanelController : MonoBehaviour
    {
        [Header("Panel Reference")]
        [SerializeField] private GameObject lobbyPanelObject;

        [Header("Player 1 UI (Local)")]
        [SerializeField] private TextMeshProUGUI player1NameText;
        [SerializeField] private TextMeshProUGUI player1StatusText;

        [Header("Player 2 UI (Opponent)")]
        [SerializeField] private TextMeshProUGUI player2NameText;
        [SerializeField] private TextMeshProUGUI player2StatusText;
        [SerializeField] private GameObject player2LoadingSpinner; // Objek putar/loading

        [Header("Matchmaking Info")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Button cancelButton;

        // Delegate / Event untuk membatalkan matchmaking
        public delegate void CancelMatchmakingHandler();
        public event CancelMatchmakingHandler OnCancelMatchmakingPressed;

        private void Awake()
        {
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelButtonClicked);
            }
        }

        public void ShowLobby()
        {
            if (lobbyPanelObject != null) lobbyPanelObject.SetActive(true);
            
            // Reset Tampilan Awal Lobby
            if (player1NameText != null) player1NameText.text = "Kamu (Player 1)";
            if (player1StatusText != null) player1StatusText.text = "SIAP!";

            if (player2NameText != null) player2NameText.text = "Mencari Lawan...";
            if (player2StatusText != null) player2StatusText.text = "Menunggu...";
            if (player2LoadingSpinner != null) player2LoadingSpinner.SetActive(true);
        }

        public void HideLobby()
        {
            if (lobbyPanelObject != null) lobbyPanelObject.SetActive(false);
        }

        // Update Timer Hitung Mundur Matchmaking
        public void UpdateMatchmakingTimer(int secondsRemaining)
        {
            if (timerText != null)
            {
                timerText.text = $"Mencari Lawan: {secondsRemaining}s";
            }
        }

        // Dipanggil saat Lawan Berhasil Ditemukan!
        public void OnOpponentFound(string opponentName)
        {
            if (player2NameText != null) player2NameText.text = opponentName;
            if (player2StatusText != null) player2StatusText.text = "SIAP!";
            if (player2LoadingSpinner != null) player2LoadingSpinner.SetActive(false);
        }

        private void OnCancelButtonClicked()
        {
            // Play SFX Click
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxButtonClear);
            }

            OnCancelMatchmakingPressed?.Invoke();
            HideLobby();
        }
    }
}