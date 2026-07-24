using UnityEngine;

namespace MathBoxing.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject matchmakingPanel;
        [SerializeField] private GameObject leaderboardPanel;

        [Header("Core Reference")]
        [SerializeField] private MathBoxing.Core.GameMatchController gameMatchController;

        private void Start()
        {
            // Tampilkan Main Menu di awal aplikasi dibuka
            ShowMainMenu();
        }

        public void ShowMainMenu()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (matchmakingPanel != null) matchmakingPanel.SetActive(false);
            if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        }

        // Dipanggil saat pemain menekan tombol 'PLAY / START GAME'
        public void OnPlayButtonClicked()
        {
            Debug.Log("<color=cyan>[MainMenu] Tombol PLAY ditekan! Mengalihkan ke Matchmaking...</color>");
            
            // Sembunyikan Main Menu
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

            // Jalankan alur pencarian game di GameMatchController
            if (gameMatchController != null)
            {
                // Mulai pencarian match
                gameMatchController.gameObject.SetActive(true);
            }
        }

        public void OnLeaderboardButtonClicked()
        {
            Debug.Log("<color=yellow>[MainMenu] Membuka Leaderboard...</color>");
            if (leaderboardPanel != null) leaderboardPanel.SetActive(true);
        }

        public void OnCloseLeaderboardClicked()
        {
            if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        }

        public void OnQuitButtonClicked()
        {
            Application.Quit();
        }
    }
}