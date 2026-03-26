using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    private const string HapticsEnabledKey = "haptics_enabled";

    public static GameManager Instance { get; private set; }

    [Header("Core References")]
    [SerializeField] private ScoreManager scoreManager = null;
    [SerializeField] private UIManager uiManager = null;
    [SerializeField] private MeteorSpawner meteorSpawner = null;
    [SerializeField] private Camera gameplayCamera = null;
    [SerializeField] private PlayerController playerController = null;

    [Header("Game Over Camera")]
    [SerializeField] private bool enableGameOverZoom = true;
    [SerializeField, Range(0f, 1f)] private float hitFocusStrength = 0.35f;
    [SerializeField, Min(0.1f)] private float zoomMultiplier = 0.85f;
    [SerializeField, Min(0f)] private float zoomDuration = 0.35f;
    [SerializeField, Min(0f)] private float restartZoomOutDuration = 0.35f;

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
    private Coroutine restartCoroutine;

    public bool HasGameplayStarted { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool HapticsEnabled => enableGameOverHaptics;
    public bool IsRestarting => restartCoroutine != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        AutoAssignReferences();
        LoadSavedSettings();
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
        ShowStartMenu();
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

    public void StartGameFromMenu()
    {
        if (HasGameplayStarted)
        {
            return;
        }

        StartNewRound();
    }

    public void RestartGame()
    {
        if (restartCoroutine != null)
        {
            return;
        }

        restartCoroutine = StartCoroutine(RestartRoutine());
    }

    public void SetHapticsEnabled(bool isEnabled)
    {
        enableGameOverHaptics = isEnabled;
        PlayerPrefs.SetInt(HapticsEnabledKey, enableGameOverHaptics ? 1 : 0);
        PlayerPrefs.Save();
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

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }
    }

    private void LoadSavedSettings()
    {
        enableGameOverHaptics = PlayerPrefs.GetInt(HapticsEnabledKey, enableGameOverHaptics ? 1 : 0) == 1;
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

        if (playerController == null)
        {
            Debug.LogError("GameManager requires a PlayerController reference.");
            isValid = false;
        }

        return isValid;
    }

    private void ShowStartMenu()
    {
        HasGameplayStarted = false;
        IsGameOver = false;
        ResetCameraInstantly();

        if (meteorSpawner != null)
        {
            meteorSpawner.StopSpawning();
            meteorSpawner.ClearAllMeteors();
        }

        playerController?.ResetToStartPosition();
        scoreManager.StopScoring();
        scoreManager.ResetScore();
        uiManager.ShowStartScreen(scoreManager.CurrentScore);
    }

    private void StartNewRound()
    {
        HasGameplayStarted = true;
        IsGameOver = false;
        ResetCameraInstantly();
        playerController?.ResetToStartPosition();
        GameAudio.Instance?.PlayAmbience();
        scoreManager.ResetScore();
        uiManager.ShowGameplay(scoreManager.CurrentScore);
        scoreManager.BeginScoring();
        meteorSpawner.BeginSpawning();
    }

    private IEnumerator RestartRoutine()
    {
        HasGameplayStarted = false;
        IsGameOver = false;
        isCameraZoomPlaying = false;

        if (meteorSpawner != null)
        {
            meteorSpawner.StopSpawning();
            meteorSpawner.ClearAllMeteors();
        }

        scoreManager.StopScoring();
        scoreManager.ResetScore();
        uiManager.ShowGameplay(scoreManager.CurrentScore);

        yield return AnimateCameraToDefault(restartZoomOutDuration);

        StartNewRound();
        restartCoroutine = null;
    }

    private IEnumerator AnimateCameraToDefault(float duration)
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        if (gameplayCamera == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            ResetCameraInstantly();
            yield break;
        }

        Vector3 startPosition = gameplayCamera.transform.position;
        float startValue = gameplayCamera.orthographic ? gameplayCamera.orthographicSize : gameplayCamera.fieldOfView;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            gameplayCamera.transform.position = Vector3.Lerp(startPosition, defaultCameraPosition, easedProgress);

            if (gameplayCamera.orthographic)
            {
                gameplayCamera.orthographicSize = Mathf.Lerp(startValue, defaultOrthographicSize, easedProgress);
            }
            else
            {
                gameplayCamera.fieldOfView = Mathf.Lerp(startValue, defaultFieldOfView, easedProgress);
            }

            yield return null;
        }

        ResetCameraInstantly();
    }

    private void HandleScoreChanged(float score)
    {
        uiManager.SetScore(score);
    }

    private void HandlePlayerHit(Vector3 hitWorldPosition, bool hasHitPosition)
    {
        if (IsGameOver || !HasGameplayStarted)
        {
            return;
        }

        IsGameOver = true;
        GameAudio.Instance?.PlayImpact();
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
