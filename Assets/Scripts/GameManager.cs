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
    [SerializeField] private Camera gameplayCamera = null;

    [Header("Game Over Camera")]
    [SerializeField] private bool enableGameOverZoom = true;
    [SerializeField, Range(0f, 1f)] private float hitFocusStrength = 0.35f;
    [SerializeField, Min(0.1f)] private float zoomMultiplier = 0.85f;
    [SerializeField, Min(0f)] private float zoomDuration = 0.35f;

    [Header("Haptics")]
    [SerializeField] private bool enableGameOverHaptics = true;

    private Vector3 defaultCameraPosition;
    private float defaultOrthographicSize;
    private float defaultFieldOfView;
    private Vector3 cameraZoomStartPosition;
    private Vector3 cameraZoomTargetPosition;
    private float cameraZoomStartValue;
    private float cameraZoomTargetValue;
    private float cameraZoomElapsed;
    private bool isCameraZoomPlaying;

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

        CacheDefaultCameraState();
        scoreManager.ScoreChanged += HandleScoreChanged;
        StartNewRound();
    }

    private void Update()
    {
        if (isCameraZoomPlaying)
        {
            UpdateGameOverCameraZoom();
        }

        bool profilingRestartEnabled = meteorSpawner != null && meteorSpawner.IsProfiling;
        if (!IsGameOver && !profilingRestartEnabled)
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
        HandlePlayerHit(Vector3.zero, false);
    }

    public void HandlePlayerHit(Vector3 hitWorldPosition)
    {
        HandlePlayerHit(hitWorldPosition, true);
    }

    public void RegisterMeteorPlanetHit(int bonusScore)
    {
        // Score is based only on survival time.
    }

    public void RestartGame()
    {
        ResetCameraInstantly();

        if (meteorSpawner != null)
        {
            meteorSpawner.StopSpawning();
            meteorSpawner.ClearAllMeteors();
        }

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

        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
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

        if (gameplayCamera == null)
        {
            Debug.LogError("GameManager requires a Camera reference or a MainCamera-tagged camera.");
            isValid = false;
        }

        return isValid;
    }

    private void StartNewRound()
    {
        IsGameOver = false;
        ResetCameraInstantly();
        scoreManager.ResetScore();
        uiManager.ShowGameplay(scoreManager.CurrentScore);
        scoreManager.BeginScoring();
        meteorSpawner.BeginSpawning();
    }

    private void HandleScoreChanged(float score)
    {
        uiManager.SetScore(score);
    }

    private void HandlePlayerHit(Vector3 hitWorldPosition, bool hasHitPosition)
    {
        if (IsGameOver)
        {
            return;
        }

        IsGameOver = true;
        TriggerGameOverHaptics();
        scoreManager.StopScoring();
        meteorSpawner.StopSpawning();

        if (enableGameOverZoom && hasHitPosition)
        {
            StartGameOverCameraZoom(hitWorldPosition);
        }
        else
        {
            isCameraZoomPlaying = false;
        }

        uiManager.ShowGameOver(scoreManager.CurrentScore);
    }

    private void CacheDefaultCameraState()
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        if (gameplayCamera == null)
        {
            return;
        }

        defaultCameraPosition = gameplayCamera.transform.position;
        defaultOrthographicSize = gameplayCamera.orthographicSize;
        defaultFieldOfView = gameplayCamera.fieldOfView;
    }

    private void ResetCameraInstantly()
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        if (gameplayCamera == null)
        {
            return;
        }

        gameplayCamera.transform.position = defaultCameraPosition;

        if (gameplayCamera.orthographic)
        {
            gameplayCamera.orthographicSize = defaultOrthographicSize;
        }
        else
        {
            gameplayCamera.fieldOfView = defaultFieldOfView;
        }

        cameraZoomElapsed = 0f;
        isCameraZoomPlaying = false;
    }

    private void StartGameOverCameraZoom(Vector3 hitWorldPosition)
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        if (gameplayCamera == null)
        {
            return;
        }

        Vector3 currentCameraPosition = gameplayCamera.transform.position;
        Vector3 focusPosition = new Vector3(hitWorldPosition.x, hitWorldPosition.y, defaultCameraPosition.z);

        cameraZoomStartPosition = currentCameraPosition;
        cameraZoomTargetPosition = Vector3.Lerp(defaultCameraPosition, focusPosition, hitFocusStrength);
        cameraZoomElapsed = 0f;
        isCameraZoomPlaying = true;

        if (gameplayCamera.orthographic)
        {
            cameraZoomStartValue = gameplayCamera.orthographicSize;
            cameraZoomTargetValue = Mathf.Max(0.1f, defaultOrthographicSize * zoomMultiplier);
        }
        else
        {
            cameraZoomStartValue = gameplayCamera.fieldOfView;
            cameraZoomTargetValue = Mathf.Clamp(defaultFieldOfView * zoomMultiplier, 1f, 179f);
        }
    }

    private void UpdateGameOverCameraZoom()
    {
        if (gameplayCamera == null)
        {
            isCameraZoomPlaying = false;
            return;
        }

        cameraZoomElapsed += Time.unscaledDeltaTime;
        float progress = zoomDuration <= 0f ? 1f : Mathf.Clamp01(cameraZoomElapsed / zoomDuration);
        float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

        gameplayCamera.transform.position = Vector3.Lerp(cameraZoomStartPosition, cameraZoomTargetPosition, easedProgress);

        if (gameplayCamera.orthographic)
        {
            gameplayCamera.orthographicSize = Mathf.Lerp(cameraZoomStartValue, cameraZoomTargetValue, easedProgress);
        }
        else
        {
            gameplayCamera.fieldOfView = Mathf.Lerp(cameraZoomStartValue, cameraZoomTargetValue, easedProgress);
        }

        if (progress >= 1f)
        {
            isCameraZoomPlaying = false;
        }
    }

    private void TriggerGameOverHaptics()
    {
        if (!enableGameOverHaptics)
        {
            return;
        }

#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }
}
