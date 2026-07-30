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

        [Header("Private Room UI Elements")]
        [SerializeField] private GameObject privateRoomGroup; // Wadah Private_Room_UI_Group
        [SerializeField] private TextMeshProUGUI roomCodeDisplayText; // Teks penampil kode jika jadi Host
        [SerializeField] private TMP_InputField roomCodeInputField; // Kolom ketik kode jika mau Join
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Button createRoomButton;

        [Header("General Buttons")]
        [SerializeField] private Button cancelButton;

        // Reference ke MatchmakingManager
        [SerializeField] private MatchmakingManager matchmakingManager;

        // Delegate / Event untuk membatalkan matchmaking
        public delegate void CancelMatchmakingHandler();
        public event CancelMatchmakingHandler OnCancelMatchmakingPressed;

        private void Awake()
        {
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelButtonClicked);
            }
            if (joinRoomButton != null) 
            {
                joinRoomButton.onClick.AddListener(OnJoinRoomButtonClicked);
            }

            if (createRoomButton != null)
            {
                createRoomButton.onClick.AddListener(OnCreateRoomButtonClicked);
            }
        }

        private void OnCreateRoomButtonClicked()
        {
            PlayClickSFX();
            if (matchmakingManager != null)
            {
                matchmakingManager.CreatePrivateRoom();
            }
        }

        private void PlayClickSFX()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxButtonClick);
            }
        }
        
        private void OnJoinRoomButtonClicked()
        {
            PlayClickSFX();
            if (roomCodeInputField != null && !string.IsNullOrEmpty(roomCodeInputField.text))
            {
                string inputCode = roomCodeInputField.text.Trim().ToUpper();
                Debug.Log($"<color=yellow>[Lobby] Mencoba Join ke Room: {inputCode}</color>");
                
                if (matchmakingManager != null)
                {
                    matchmakingManager.JoinPrivateRoom(inputCode);
                }
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

        public void SetupForQuickMatch()
        {
            if (lobbyPanelObject != null) lobbyPanelObject.SetActive(true);
            if (privateRoomGroup != null) privateRoomGroup.SetActive(false);
        }

    // Panggil fungsi ini jika masuk dari tombol Main Bersama Teman
        // 
        
        public void SetupForPrivateMatch()
        {
            if (lobbyPanelObject != null) lobbyPanelObject.SetActive(true);
            if (privateRoomGroup != null) privateRoomGroup.SetActive(true);
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