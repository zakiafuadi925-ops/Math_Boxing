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
        [SerializeField] private TextMeshProUGUI questionTextField; 
        [SerializeField] private TextMeshProUGUI timerTextField;   

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

        [Header("Scene Config")]
        [SerializeField] private string mainMenuSceneName = "01-MainMenu"; 

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

            if (numpadPanel != null) numpadPanel.SetActive(false);
            if (inputPanel != null) inputPanel.SetActive(false);
            if (gameplayHUDGroup != null) gameplayHUDGroup.SetActive(false);
            if (battleArena != null) battleArena.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }

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

            StartCoroutine(WaitForPrivateMatchReadyCoroutine());
        }

        private IEnumerator WaitForPrivateMatchReadyCoroutine()
        {
            // Tunggu sampai Join/Create private room selesai
            while (matchmakingManager != null && !matchmakingManager.isMatchReady)
            {
                yield return null;
            }

            if (matchmakingManager != null && matchmakingManager.isMatchReady)
            {
                Debug.Log("<color=green>[Controller] Private Match Valid! Memulai pertarungan...</color>");

                if (lobbyPanelController != null)
                {
                    lobbyPanelController.OnOpponentFound("Player Online (Private)");
                }

                yield return new WaitForSeconds(1.5f);

                if (lobbyPanelController != null) 
                {
                    lobbyPanelController.HideLobby();
                }

                // Transisi ke Arena Pertarungan
                StartMatch(); 
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
                
                // Jeda steril untuk memberi waktu HTTP Request membuat Room ID
                yield return new WaitForSeconds(0.5f);

                if (matchmakingManager.forceAsPlayer1)
                {
                    StartCoroutine(matchmakingManager.StartTimeoutCountdown());
                }

                float searchTimer = matchmakingTimeout;

                while (!matchmakingManager.isMatchReady && searchTimer > 0)
                {
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
                    lobbyPanelController.OnOpponentFound("Player Online");
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

            bool isP1 = matchmakingManager != null ? matchmakingManager.isPlayer1 : true;

            if (playerAnswer == currentQuestion.correctAnswer)
            {
                totalScore += currentQuestion.scoreValue;

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxCorrectAnswer);
                }

                Debug.Log($"<color=green>Jawaban BENAR!</color> +{currentQuestion.scoreValue} Poin. Total: {totalScore}");
                
                UpdateScoreDisplay(isP1, totalScore);

                int randomAttack = Random.Range(1, 5); 

                Animator myAnimator = isP1 ? player1Animator : player2Animator;
                Animator enemyAnimator = isP1 ? player2Animator : player1Animator;

                if (myAnimator != null)
                {
                    myAnimator.SetInteger("actionType", randomAttack);
                    StartCoroutine(ResetActionTypeCoroutine(myAnimator, isP1 ? 1 : 2));
                }

                if (enemyAnimator != null)
                {
                    bool isEnemyBlocking = Random.value > 0.5f; 
                    if (isEnemyBlocking)
                    {
                        enemyAnimator.SetBool("isBlocking", true);
                        StartCoroutine(ResetBlockStatusCoroutine(enemyAnimator, isP1 ? 2 : 1));

                        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxPunchMiss);
                    }
                    else
                    {
                        enemyAnimator.SetInteger("actionType", 6); 
                        StartCoroutine(ResetActionTypeCoroutine(enemyAnimator, isP1 ? 2 : 1));

                        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxPunchHit);
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
            }
        }

        private void HandleOpponentAttacked(int newOpponentScore)
        {
            Debug.Log($"<color=magenta>[Realtime] Lawan menyerang! Skor mereka: {newOpponentScore}</color>");

            bool isP1 = matchmakingManager != null ? matchmakingManager.isPlayer1 : true;
            UpdateScoreDisplay(!isP1, newOpponentScore);

            Animator enemyAnimator = isP1 ? player2Animator : player1Animator;
            Animator myAnimator = isP1 ? player1Animator : player2Animator;

            if (enemyAnimator != null)
            {
                enemyAnimator.SetInteger("actionType", Random.Range(1, 5));
                StartCoroutine(ResetActionTypeCoroutine(enemyAnimator, isP1 ? 2 : 1));
            }

            if (myAnimator != null)
            {
                myAnimator.SetInteger("actionType", 6); 
                StartCoroutine(ResetActionTypeCoroutine(myAnimator, isP1 ? 1 : 2));
            }
        }

        private IEnumerator ResetActionTypeCoroutine(Animator targetAnimator, int playerIndex)
        {
            yield return new WaitForSeconds(0.4f); 
            if (targetAnimator != null) targetAnimator.SetInteger("actionType", 0); 
        }

        private IEnumerator ResetBlockStatusCoroutine(Animator targetAnimator, int playerIndex)
        {
            yield return new WaitForSeconds(0.3f); 
            if (targetAnimator != null) targetAnimator.SetBool("isBlocking", false); 
        }        
        
        private void InitializeScoreUI()
        {
            if (player1ScoreTextField != null) player1ScoreTextField.text = "P1 SCORE: 0";
            if (player2ScoreTextField != null) player2ScoreTextField.text = "P2 SCORE: 0";
        }

        private void UpdateScoreDisplay(bool isPlayer1Target, int score)
        {
            if (isPlayer1Target && player1ScoreTextField != null)
            {
                player1ScoreTextField.text = $"P1 SCORE: {score}";
            }
            else if (!isPlayer1Target && player2ScoreTextField != null)
            {
                player2ScoreTextField.text = $"P2 SCORE: {score}";
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

            if (timerTextField != null) timerTextField.text = "TIME UP!"; 
            if (questionTextField != null) questionTextField.text = "FINISHED"; 

            if (realtimeListener != null)
            {
                realtimeListener.StopListening(); 
                
                int finalOpponentScore = realtimeListener.opponentScore; 
                
                if (totalScore < finalOpponentScore)
                {
                    if (player1Animator != null) player1Animator.SetBool("isDead", true);
                }
                else if (totalScore > finalOpponentScore)
                {
                    if (player2Animator != null) player2Animator.SetBool("isDead", true);
                }
            }

            if (gameOverPanel != null) gameOverPanel.SetActive(true); 
            if (finalScoreTextField != null) finalScoreTextField.text = $"FINAL SCORE: {totalScore}"; 

            if (numpadPanel != null) numpadPanel.SetActive(false);
            if (inputPanel != null) inputPanel.SetActive(false);
        }

        public void ExitToMainMenu()
        {
            Time.timeScale = 1f; 

            if (realtimeListener != null)
            {
                realtimeListener.StopListening();
            }

            if (matchmakingManager != null)
            {
                matchmakingManager.CancelMatchmaking();
            }

            // Berpindah ke Scene Main Menu dengan steril
            if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
            else
            {
                // Fallback jika Single-Scene UI
                if (gameOverPanel != null) gameOverPanel.SetActive(false);
                if (lobbyPanelController != null) lobbyPanelController.HideLobby();
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            }
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }
}