using UnityEngine;
using MathBoxing.Core;

namespace MathBoxing.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject matchmakingPanel;
        [SerializeField] private GameObject leaderboardPanel;

        [Header("Game Elements To Hide On Main Menu")]
        [SerializeField] private GameObject gameplayHUDGroup;
        [SerializeField] private GameObject battleArena;

        [Header("Core Reference")]
        [SerializeField] private MathBoxing.Core.GameMatchController gameMatchController;

        private void Start()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBGM(AudioManager.Instance.bgmMenu);
            }
            // Tampilkan Main Menu di awal aplikasi dibuka
            ShowMainMenu();
        }

        public void ShowMainMenu()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (matchmakingPanel != null) matchmakingPanel.SetActive(false);
            if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
            if (gameplayHUDGroup != null) gameplayHUDGroup.SetActive(false);
            if (battleArena != null) battleArena.SetActive(false);
        }

        // Dipanggil saat pemain menekan tombol 'PLAY / START GAME' (Quick Match)
        public void OnPlayButtonClicked()
        {
            Debug.Log("<color=cyan>[MainMenu] Tombol PLAY ditekan!</color>");
            PlayButtonSFX();

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

            if (gameMatchController != null)
            {
                gameMatchController.StartQuickMatchFlow();
            }
            else
            {
                Debug.LogError("[MainMenu] Reference GameMatchController belum dipasang di Inspector!");
            }
        }

        public void OnLobbyButtonClicked()
        {
            PlayButtonSFX();

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

            if (gameMatchController != null)
            {
                gameMatchController.StartMatchmakingFlow();
            }
            else
            {
                Debug.LogError("[MainMenuController] Referensi GameMatchController belum dipasang di Inspector!");
            }
        }

        public void OnPrivateMatchButtonClicked()
        {
            PlayButtonSFX();

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

            if (gameMatchController != null)
            {
                gameMatchController.StartPrivateMatchFlow();
            }
            else
            {
                Debug.LogError("[MainMenuController] Referensi GameMatchController belum dipasang di Inspector!");
            }
        }

        public void OnLeaderboardButtonClicked()
        {
            PlayButtonSFX();
            Debug.Log("<color=yellow>[MainMenu] Membuka Leaderboard...</color>");
            if (leaderboardPanel != null) leaderboardPanel.SetActive(true);
        }

        public void OnCloseLeaderboardClicked()
        {
            PlayButtonSFX();
            if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        }

        public void OnQuitButtonClicked()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxButtonClick);
            }

            Application.Quit();
        }

        // Helper Method untuk SFX Klik Tombol
        private void PlayButtonSFX()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxButtonEnter);
            }
        }
    }
}