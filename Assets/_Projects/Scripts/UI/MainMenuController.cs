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
        [SerializeField] private GameObject gameplayHUDGroup; // Tarik 'Gameplay_HUD_Group' ke sini!
        [SerializeField] private GameObject battleArena;       // Tarik 'Battle_Arena' ke sini (opsional)!

        [Header("Core Reference")]
        [SerializeField] private MathBoxing.Core.GameMatchController gameMatchController;

        private void Start()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBGM(AudioManager.Instance.sfxButtonEnter); // Opsional: atur jika ada BGM khusus
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

        

        // Dipanggil saat pemain menekan tombol 'PLAY / START GAME'
        public void OnPlayButtonClicked()
        {
            Debug.Log("<color=cyan>[MainMenu] Tombol PLAY ditekan!</color>");

            // SFX Klik Tombol
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxButtonEnter);
            }
            
            // 1. Sembunyikan Panel Main Menu agar tidak menimpa layar game!
            if (mainMenuPanel != null) 
            {
                mainMenuPanel.SetActive(false);
            }

            // 2. Munculkan kembali Arena & HUD Gameplay
            if (battleArena != null) battleArena.SetActive(true);
            if (gameplayHUDGroup != null) gameplayHUDGroup.SetActive(true);

            // 2. Pemicu utama pencarian pertandingan
            if (gameMatchController != null)
            {
                gameMatchController.StartMatchmakingFlow();
            }
            else
            {
                Debug.LogError("[MainMenu] Reference GameMatchController belum dipasang di Inspector!");
            }
        }

        public void OnLobbyButtonClicked()
        {
            // 1. Play SFX Klik
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxButtonEnter);
            }

            // 2. Sembunyikan Panel Main Menu agar layar bersih saat Lobby terbuka
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }

            // 3. Panggil Alur Matchmaking & Lobby di GameMatchController!
            if (gameMatchController != null)
            {
                gameMatchController.StartMatchmakingFlow();
            }
            else
            {
                Debug.LogError("[MainMenuController] Referensi GameMatchController belum dipasang di Inspector!");
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
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxButtonClick);
            }

            Application.Quit();
        }
    }
}