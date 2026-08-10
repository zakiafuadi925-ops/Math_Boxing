using UnityEngine;
using TMPro;
using UnityEngine.UI;
using MathBoxing.Core;
using MathBoxing.Backend;

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
        [SerializeField] private GameObject player2LoadingSpinner;

        [Header("Matchmaking Info")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Private Room UI Elements")]
        [SerializeField] private GameObject privateRoomGroup; 
        [SerializeField] private TextMeshProUGUI roomCodeDisplayText; 
        [SerializeField] private TMP_InputField roomCodeInputField; 
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Button createRoomButton;

        [Header("General Buttons")]
        [SerializeField] private Button cancelButton;

        [Header("Manager Reference")]
        [SerializeField] private MatchmakingManager matchmakingManager;

        public delegate void CancelMatchmakingHandler();
        public event CancelMatchmakingHandler OnCancelMatchmakingPressed;

        private void Awake()
        {
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(OnCancelButtonClicked);
            }
            if (joinRoomButton != null) 
            {
                joinRoomButton.onClick.RemoveAllListeners();
                joinRoomButton.onClick.AddListener(OnJoinRoomButtonClicked);
            }
            if (createRoomButton != null)
            {
                createRoomButton.onClick.RemoveAllListeners();
                createRoomButton.onClick.AddListener(OnCreateRoomButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (cancelButton != null) cancelButton.onClick.RemoveAllListeners();
            if (joinRoomButton != null) joinRoomButton.onClick.RemoveAllListeners();
            if (createRoomButton != null) createRoomButton.onClick.RemoveAllListeners();
        }

        public void ShowLobby()
        {
            if (lobbyPanelObject != null) lobbyPanelObject.SetActive(true);
            
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
            ShowLobby();
            if (privateRoomGroup != null) privateRoomGroup.SetActive(false);
        }

        public void SetupForPrivateMatch()
        {
            ShowLobby();
            if (privateRoomGroup != null) privateRoomGroup.SetActive(true);

            if (roomCodeDisplayText != null) roomCodeDisplayText.text = "MAIN BERSAMA TEMAN";
            if (roomCodeInputField != null)
            {
                roomCodeInputField.text = "";
                roomCodeInputField.interactable = true;
            }

            if (createRoomButton != null) createRoomButton.gameObject.SetActive(true);
            if (joinRoomButton != null) joinRoomButton.gameObject.SetActive(true);
        }

        public void UpdateMatchmakingTimer(int secondsRemaining)
        {
            if (timerText != null)
            {
                timerText.text = $"Mencari Lawan: {secondsRemaining}s";
            }
        }

        public void OnOpponentFound(string opponentName)
        {
            if (player2NameText != null) player2NameText.text = opponentName;
            if (player2StatusText != null) player2StatusText.text = "SIAP!";
            if (player2LoadingSpinner != null) player2LoadingSpinner.SetActive(false);
        }

        private void OnCreateRoomButtonClicked()
        {
            PlayClickSFX();
            FindMatchmakingManager();
            if (matchmakingManager != null)
            {
                matchmakingManager.CreatePrivateRoom();
            }
            else
            {
                Debug.LogError("[Lobby] MatchmakingManager tidak ditemukan di scene!");
            }
        }

        private void OnJoinRoomButtonClicked()
        {
            PlayClickSFX();

            if (roomCodeInputField == null)
            {
                Debug.LogError("[Lobby] Component roomCodeInputField belum di-drag ke Inspector!");
                return;
            }

            string inputCode = roomCodeInputField.text.Trim().ToUpper();

            if (string.IsNullOrEmpty(inputCode))
            {
                Debug.LogWarning("[Lobby] Kode room tidak boleh kosong!");
                return;
            }

            FindMatchmakingManager();
            if (matchmakingManager != null)
            {
                matchmakingManager.JoinPrivateRoom(inputCode);
            }
            else
            {
                Debug.LogError("[Lobby] MatchmakingManager tidak ditemukan di scene!");
            }
        }

        private void OnCancelButtonClicked()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxButtonClear);
            }

            // Hentikan proses matchmaking backend secara langsung
            FindMatchmakingManager();
            if (matchmakingManager != null)
            {
                matchmakingManager.CancelMatchmaking();
            }

            OnCancelMatchmakingPressed?.Invoke();
            HideLobby();
        }

        private void FindMatchmakingManager()
        {
            if (matchmakingManager == null)
            {
                matchmakingManager = MatchmakingManager.Instance != null ? 
                    MatchmakingManager.Instance : FindAnyObjectByType<MatchmakingManager>();
            }
        }

        private void PlayClickSFX()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxButtonClick);
            }
        }

        public void DisplayCreatedRoomCode(string roomCode)
        {
            if (roomCodeDisplayText != null)
            {
                roomCodeDisplayText.text = "KODE ROOM KAMU: " + roomCode;
            }

            if (roomCodeInputField != null)
            {
                roomCodeInputField.text = roomCode;
                roomCodeInputField.interactable = false;
            }

            if (createRoomButton != null) createRoomButton.gameObject.SetActive(false);
            if (joinRoomButton != null) joinRoomButton.gameObject.SetActive(false);
            
            if (player2LoadingSpinner != null) player2LoadingSpinner.SetActive(true);
        }
    }
}