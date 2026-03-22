using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;

    [Header("Scoring")]
    [SerializeField, Min(0f)] private float scorePerSecond = 5f;
    [SerializeField, Min(0)] private int avoidedMeteorBonus = 1;

    [Header("Audio")]
    [SerializeField] private GameAudio audioManager;

    public bool IsGameOver { get; private set; }
    public int Score { get; private set; }
    public GameAudio AudioManager => audioManager;

    private float scoreBuffer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (audioManager == null)
        {
            audioManager = FindObjectOfType<GameAudio>();
        }
    }

    private void Start()
    {
        OrbitInput.Reset();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        RefreshScoreUI();
        audioManager?.PlayAmbience();
    }

    private void Update()
    {
        if (IsGameOver)
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                RestartGame();
            }

            return;
        }

        scoreBuffer += Time.deltaTime * scorePerSecond;
        int pointsToAdd = Mathf.FloorToInt(scoreBuffer);

        if (pointsToAdd <= 0)
        {
            return;
        }

        scoreBuffer -= pointsToAdd;
        Score += pointsToAdd;
        RefreshScoreUI();
    }

    public void RegisterMeteorAvoided(int bonusPoints = -1)
    {
        if (IsGameOver)
        {
            return;
        }

        int points = bonusPoints >= 0 ? bonusPoints : avoidedMeteorBonus;
        Score += points;
        RefreshScoreUI();
        audioManager?.PlayMeteorPass();
    }

    public void OnPlayerHit()
    {
        if (IsGameOver)
        {
            return;
        }

        IsGameOver = true;
        audioManager?.PlayImpact();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (finalScoreText != null)
        {
            finalScoreText.text = $"Final Score: {Score}";
        }
    }

    public void RestartGame()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    private void RefreshScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {Score}";
        }
    }
}
