using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Core References")]
    [SerializeField] private ScoreManager scoreManager = null;
    [SerializeField] private UIManager uiManager = null;
    [SerializeField] private MeteorSpawner meteorSpawner = null;

    public bool IsGameOver { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        AutoAssignReferences();
    }

    private void Start()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        scoreManager.ScoreChanged += HandleScoreChanged;
        StartNewRound();
    }

    private void Update()
    {
        if (!IsGameOver)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartGame();
        }
    }

    private void OnDestroy()
    {
        if (scoreManager != null)
        {
            scoreManager.ScoreChanged -= HandleScoreChanged;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void HandlePlayerHit()
    {
        if (IsGameOver)
        {
            return;
        }

        IsGameOver = true;
        scoreManager.StopScoring();
        meteorSpawner.StopSpawning();
        uiManager.ShowGameOver(scoreManager.CurrentScore);
    }

    public void RegisterMeteorPlanetHit(int bonusScore)
    {
        // Score is based only on survival time.
    }

    public void RestartGame()
    {
        meteorSpawner.ClearAllMeteors();

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    private void AutoAssignReferences()
    {
        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<ScoreManager>();
        }

        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<UIManager>();
        }

        if (meteorSpawner == null)
        {
            meteorSpawner = FindFirstObjectByType<MeteorSpawner>();
        }
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (scoreManager == null)
        {
            Debug.LogError("GameManager requires a ScoreManager reference.");
            isValid = false;
        }

        if (uiManager == null)
        {
            Debug.LogError("GameManager requires a UIManager reference.");
            isValid = false;
        }

        if (meteorSpawner == null)
        {
            Debug.LogError("GameManager requires a MeteorSpawner reference.");
            isValid = false;
        }

        return isValid;
    }

    private void StartNewRound()
    {
        IsGameOver = false;
        scoreManager.ResetScore();
        uiManager.ShowGameplay(scoreManager.CurrentScore);
        scoreManager.BeginScoring();
        meteorSpawner.BeginSpawning();
    }

    private void HandleScoreChanged(float score)
    {
        uiManager.SetScore(score);
    }
}
