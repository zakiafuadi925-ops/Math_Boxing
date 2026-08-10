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
        [SerializeField] private GameMatchController gameMatchController;

        private void Start()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBGM(AudioManager.Instance.bgmMenu);
            }
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

        public void OnPlayButtonClicked()
        {
            PlayButtonSFX();

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

            FindGameMatchController();
            if (gameMatchController != null)
            {
                gameMatchController.StartQuickMatchFlow();
            }
            else
            {
                Debug.LogError("[MainMenu] Reference GameMatchController belum dipasang di Inspector!");
            }
        }

        public void OnPrivateMatchButtonClicked()
        {
            PlayButtonSFX();

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

            FindGameMatchController();
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

        private void FindGameMatchController()
        {
            if (gameMatchController == null)
            {
                gameMatchController = FindAnyObjectByType<GameMatchController>();
            }
        }

        private void PlayButtonSFX()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxButtonEnter);
            }
        }
    }
}