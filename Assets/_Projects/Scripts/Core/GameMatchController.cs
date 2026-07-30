using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

namespace MathBoxing.Core
{
    public class GameMatchController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MathBoxing.UI.NumpadController numpadController;
        [SerializeField] private MathGenerator mathGenerator; 
        [SerializeField] private TextMeshProUGUI questionTextField; // Display_text di dalam UI_Display_Panel
        [SerializeField] private TextMeshProUGUI timerTextField;   // Teks di dalam Timer_Panel

        [Header("Visual & Animations")]
        [SerializeField] private Animator player1Animator;
        [SerializeField] private Animator player2Animator; 

        [Header("Multiplayer Net & Config")]
        [SerializeField] private MathBoxing.Backend.MatchmakingManager matchmakingManager;
        [SerializeField] private MathBoxing.Backend.SupabaseRealtimeListener realtimeListener;
        [SerializeField] private MathBoxing.Backend.SupabaseManager supabaseManager;

        [Header("Game Over UI Components")]
        [SerializeField] private GameObject gameOverPanel; 
        [SerializeField] private TextMeshProUGUI finalScoreTextField; 

        [Header("Gameplay UI & Arena Elements")]
        [SerializeField] private GameObject numpadPanel;
        [SerializeField] private GameObject inputPanel;
        [SerializeField] private GameObject gameplayHUDGroup;
        [SerializeField] private GameObject battleArena;

        [Header("UI Score Elements")]
        [SerializeField] private TextMeshProUGUI player1ScoreTextField;
        [SerializeField] private TextMeshProUGUI player2ScoreTextField;

        [Header("Score System")]
        [SerializeField] private int totalScore = 0; 

        // Pelacak Coroutine Animasi
        private Coroutine player1ResetCoroutine;
        private Coroutine player2ResetCoroutine;

        [Header("Timer Settings")]
        [SerializeField] private float timeRemaining = 60f; 
        private bool isGameActive = false;

        private MathQuestion currentQuestion; 

        [Header("Matchmaking Settings")]
        [SerializeField] private float matchmakingTimeout = 10f; 

        [Header("UI Panel References")]
        [SerializeField] private MathBoxing.UI.LobbyPanelController lobbyPanelController;
        [SerializeField] private GameObject mainMenuPanel;

        private void OnEnable()
        {
            if (numpadController != null) numpadController.OnAnswerSubmitted += HandleAnswerSubmitted;
            if (realtimeListener != null) realtimeListener.OnOpponentScoreChanged += HandleOpponentAttacked;
        }

        private void OnDisable()
        {
            if (numpadController != null) numpadController.OnAnswerSubmitted -= HandleAnswerSubmitted;
            if (realtimeListener != null) realtimeListener.OnOpponentScoreChanged -= HandleOpponentAttacked;
        }

        private void Start()
        {
            isGameActive = false;

            // Sembunyikan elemen gameplay saat di awal game (Main Menu)
            if (numpadPanel != null) numpadPanel.SetActive(false);
            if (inputPanel != null) inputPanel.SetActive(false);
            if (gameplayHUDGroup != null) gameplayHUDGroup.SetActive(false);
            if (battleArena != null) battleArena.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }

        // ========================================================================
        // ALUR DARI MAIN MENU CONTROLLER
        // ========================================================================

        public void StartQuickMatchFlow()
        {
            Debug.Log("<color=cyan>[Controller] Memulai alur Quick Match...</color>");
            if (lobbyPanelController != null)
            {
                lobbyPanelController.SetupForQuickMatch();
            }
            
            StartCoroutine(WaitForMatchmakingCoroutine());
        }

        public void StartPrivateMatchFlow()
        {
            Debug.Log("<color=cyan>[Controller] Memulai alur Private Match...</color>");
            if (lobbyPanelController != null)
            {
                lobbyPanelController.SetupForPrivateMatch();
            }
        }

        public void StartMatchmakingFlow()
        {
            StartQuickMatchFlow();
        }

        private IEnumerator WaitForMatchmakingCoroutine()
        {
            if (matchmakingManager != null)
            {
                matchmakingManager.FindMatch();
                
                if (matchmakingManager.forceAsPlayer1)
                {
                    StartCoroutine(matchmakingManager.StartTimeoutCountdown());
                }

                float searchTimer = matchmakingTimeout;

                while (!matchmakingManager.isMatchReady && searchTimer > 0)
                {
                    if (string.IsNullOrEmpty(matchmakingManager.currentMatchId))
                    {
                        if (lobbyPanelController != null) lobbyPanelController.HideLobby();
                        yield break; 
                    }

                    if (lobbyPanelController != null)
                    {
                        lobbyPanelController.UpdateMatchmakingTimer(Mathf.CeilToInt(searchTimer));
                    }

                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxTimerTick);
                    }

                    yield return new WaitForSeconds(1f);
                    searchTimer--;
                }

                if (!matchmakingManager.isMatchReady)
                {
                    Debug.LogWarning("<color=yellow>[Controller] Matchmaking Timeout!</color>");
                    if (lobbyPanelController != null) lobbyPanelController.HideLobby();
                    
                    ExitToMainMenu(); 
                    yield break;
                }

                if (lobbyPanelController != null)
                {
                    lobbyPanelController.OnOpponentFound("Player 2 (Online)");
                }

                yield return new WaitForSeconds(1.5f);

                if (lobbyPanelController != null) lobbyPanelController.HideLobby();

                StartMatch(); 
            }
        }

        private void StartMatch()
        {   
            Time.timeScale = 1f; 
            InitializeScoreUI();

            // Aktifkan Arena dan HUD Gameplay
            if (battleArena != null) battleArena.SetActive(true);
            if (gameplayHUDGroup != null) gameplayHUDGroup.SetActive(true);
            if (numpadPanel != null) numpadPanel.SetActive(true);
            if (inputPanel != null) inputPanel.SetActive(true);

            if (player1Animator != null) player1Animator.speed = 1f;
            if (player2Animator != null) player2Animator.speed = 1f;
            
            Debug.Log("<color=cyan>[Controller] Memulai pertarungan matematika!</color>");
            totalScore = 0;
            timeRemaining = 60f; 
            isGameActive = true; 

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxRoundBell);
                AudioManager.Instance.PlayBGM(AudioManager.Instance.bgmGameplay);
            }
            
            StartNewQuestion(); 
            StartCoroutine(MatchTimerCoroutine()); 
        }

        private IEnumerator MatchTimerCoroutine()
        {
            while (timeRemaining > 0 && isGameActive)
            {
                if (timerTextField != null) 
                    timerTextField.text = $"Timer: {Mathf.CeilToInt(timeRemaining)}s";

                if (timeRemaining <= 10f && timeRemaining > 0)
                {
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxTimerTick);
                    }
                }
                
                yield return new WaitForSeconds(1f);
                timeRemaining--;
            }
            EndMatch();
        }

        private void StartNewQuestion()
        {
            if (!isGameActive) return; 

            if (mathGenerator != null)
            {
                currentQuestion = mathGenerator.GenerateRandomQuestion();
                if (questionTextField != null) 
                {
                    questionTextField.text = currentQuestion.questionText;
                }
            }
            else
            {
                Debug.LogError("[Controller] MathGenerator tidak ditemukan di Scene!");
            }
        }

        private void HandleAnswerSubmitted(int playerAnswer)
        {
            if (!isGameActive) return;

            if (playerAnswer == currentQuestion.correctAnswer)
            {
                totalScore += currentQuestion.scoreValue;

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxCorrectAnswer);
                }

                Debug.Log($"<color=green>Jawaban BENAR!</color> +{currentQuestion.scoreValue} Poin. Total: {totalScore}");
                
                UpdateLocalScoreUI(totalScore);

                int randomAttack = Random.Range(1, 5); 

                if (player1Animator != null)
                {
                    if (player1ResetCoroutine != null) StopCoroutine(player1ResetCoroutine);
                    player1Animator.SetInteger("actionType", randomAttack);
                    player1ResetCoroutine = StartCoroutine(ResetActionTypeCoroutine(player1Animator, 1));
                }

                if (player2Animator != null)
                {
                    if (player2ResetCoroutine != null) StopCoroutine(player2ResetCoroutine);
                    
                    bool isEnemyBlocking = Random.value > 0.5f; 
                    if (isEnemyBlocking)
                    {
                        player2Animator.SetBool("isBlocking", true);
                        player2ResetCoroutine = StartCoroutine(ResetBlockStatusCoroutine(player2Animator, 2));

                        if (AudioManager.Instance != null)
                        {
                            AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxPunchMiss);
                        }
                    }
                    else
                    {
                        player2Animator.SetInteger("actionType", 6); 
                        player2ResetCoroutine = StartCoroutine(ResetActionTypeCoroutine(player2Animator, 2));

                        if (AudioManager.Instance != null)
                        {
                            AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxPunchHit);
                        }
                    }
                }

                if (supabaseManager != null && matchmakingManager != null)
                {
                    supabaseManager.UpdateMatchScore(matchmakingManager.currentMatchId, matchmakingManager.isPlayer1, totalScore);
                }

                StartNewQuestion();
            }
            else
            {
                Debug.Log("<color=red>Jawaban SALAH!</color>");

                if (numpadController != null) 
                {
                    numpadController.TriggerWrongAnswerPenalty();
                }

                int randomEnemyAttack = Random.Range(1, 5);

                if (player1Animator != null)
                {
                    if (player1ResetCoroutine != null) StopCoroutine(player1ResetCoroutine);
                    
                    bool isPlayer1Blocking = Random.value > 0.5f;
                    if (isPlayer1Blocking)
                    {
                        player1Animator.SetBool("isBlocking", true);
                        player1ResetCoroutine = StartCoroutine(ResetBlockStatusCoroutine(player1Animator, 1));

                        if (AudioManager.Instance != null)
                        {
                            AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxPunchMiss);
                        }
                    }
                    else
                    {
                        player1Animator.SetInteger("actionType", 6); 
                        player1ResetCoroutine = StartCoroutine(ResetActionTypeCoroutine(player1Animator, 1));

                        if (AudioManager.Instance != null)
                        {
                            AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxPunchHit);
                        }
                    }
                }
                
                if (player2Animator != null)
                {
                    if (player2ResetCoroutine != null) StopCoroutine(player2ResetCoroutine);
                    player2Animator.SetInteger("actionType", randomEnemyAttack);
                    player2ResetCoroutine = StartCoroutine(ResetActionTypeCoroutine(player2Animator, 2));
                }
            }
        }

        private void HandleOpponentAttacked(int newOpponentScore)
        {
            Debug.Log($"<color=magenta>[Realtime] Lawan menyerang! Skor mereka: {newOpponentScore}</color>");

            int randomEnemyAttack = Random.Range(1, 5);

            if (player2Animator != null)
            {
                if (player2ResetCoroutine != null) StopCoroutine(player2ResetCoroutine);
                player2Animator.SetInteger("actionType", randomEnemyAttack);
                player2ResetCoroutine = StartCoroutine(ResetActionTypeCoroutine(player2Animator, 2));
            }

            if (player1Animator != null)
            {
                if (player1ResetCoroutine != null) StopCoroutine(player1ResetCoroutine);
                player1Animator.SetInteger("actionType", 6); 
                player1ResetCoroutine = StartCoroutine(ResetActionTypeCoroutine(player1Animator, 1));
            }

            UpdateOpponentScoreUI(newOpponentScore);
        }

        private IEnumerator ResetActionTypeCoroutine(Animator targetAnimator, int playerIndex)
        {
            yield return new WaitForSeconds(0.4f); 
            
            if (targetAnimator != null)
            {
                targetAnimator.SetInteger("actionType", 0); 
            }

            if (playerIndex == 1) player1ResetCoroutine = null;
            if (playerIndex == 2) player2ResetCoroutine = null;
        }

        private IEnumerator ResetBlockStatusCoroutine(Animator targetAnimator, int playerIndex)
        {
            yield return new WaitForSeconds(0.3f); 
            
            if (targetAnimator != null)
            {
                targetAnimator.SetBool("isBlocking", false); 
            }

            if (playerIndex == 1) player1ResetCoroutine = null;
            if (playerIndex == 2) player2ResetCoroutine = null;
        }        
        
        private void InitializeScoreUI()
        {
            if (player1ScoreTextField != null) player1ScoreTextField.text = "PLAYER_1 SCORE: 0";
            if (player2ScoreTextField != null) player2ScoreTextField.text = "PLAYER_2 SCORE: 0";
        }

        private void UpdateLocalScoreUI(int newScore)
        {
            if (player1ScoreTextField != null)
            {
                player1ScoreTextField.text = $"PLAYER_1 SCORE: {newScore}";
            }
        }

        public void UpdateOpponentScoreUI(int opponentScore)
        {
            if (player2ScoreTextField != null)
            {
                player2ScoreTextField.text = $"PLAYER_2 SCORE: {opponentScore}";
            }
        }

        private void EndMatch()
        {
            isGameActive = false; 

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopBGM(); 
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxRoundBell);
                AudioManager.Instance.PlaySFX(AudioManager.Instance.bgmVictory); 
            }

            if (player1ResetCoroutine != null) StopCoroutine(player1ResetCoroutine);
            if (player2ResetCoroutine != null) StopCoroutine(player2ResetCoroutine);
            
            if (timerTextField != null) timerTextField.text = "TIME UP!"; 
            if (questionTextField != null) questionTextField.text = "FINISHED"; 

            if (realtimeListener != null)
            {
                realtimeListener.StopListening(); 
                
                int finalOpponentScore = realtimeListener.opponentScore; 
                
                if (totalScore < finalOpponentScore)
                {
                    if (player1Animator != null) player1Animator.SetBool("isDead", true);
                    Debug.Log("<color=red>[Match Over] Kamu KO!</color>");
                }
                else if (totalScore > finalOpponentScore)
                {
                    if (player2Animator != null) player2Animator.SetBool("isDead", true);
                    Debug.Log("<color=green>[Match Over] Lawan KO!</color>");
                }
            }

            if (gameOverPanel != null) gameOverPanel.SetActive(true); 
            if (finalScoreTextField != null) finalScoreTextField.text = $"FINAL SCORE: {totalScore}"; 

            if (numpadPanel != null) numpadPanel.SetActive(false);
            if (inputPanel != null) inputPanel.SetActive(false);
        }

        public void RetryGame()
        {
            Time.timeScale = 1f;

            if (realtimeListener != null)
            {
                realtimeListener.StopListening();
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ExitToMainMenu()
        {
            Time.timeScale = 1f; 

            if (realtimeListener != null)
            {
                realtimeListener.StopListening();
            }

            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (lobbyPanelController != null) lobbyPanelController.HideLobby();
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void OpenLeaderboard()
        {
            Debug.Log("<color=yellow>[UI] Membuka Panel Leaderboard...</color>");
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }
}