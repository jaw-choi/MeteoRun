using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private const string BestRecordKey = "best_record";

    [Header("Gameplay UI")]
    [SerializeField] private TextMeshProUGUI scoreText = null;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel = null;
    [SerializeField] private TextMeshProUGUI finalScoreText = null;
    [SerializeField] private TextMeshProUGUI bestRecordText = null;
    [SerializeField] private Button restartButton = null;
    [SerializeField] private Button gameOverSettingsButton = null;

    [Header("Start Screen UI")]
    [SerializeField] private RectTransform screenCanvasRoot = null;
    [SerializeField] private RectTransform titleRect = null;
    [SerializeField] private RectTransform tutorialRect = null;
    [SerializeField] private RectTransform startRect = null;
    [SerializeField, Min(0.1f)] private float startTransitionDuration = 1f;
    [SerializeField, Range(0f, 1f)] private float gameplayStartProgress = 0.8f;
    [SerializeField, Min(0f)] private float titleDipDistance = 90f;
    [SerializeField, Min(0f)] private float tutorialLiftDistance = 90f;
    [SerializeField, Range(0.05f, 0.5f)] private float anticipationPortion = 0.22f;

    [Header("Settings UI")]
    [SerializeField] private RectTransform overlayCanvasRoot = null;
    [SerializeField] private Button settingsButton = null;
    [SerializeField] private GameObject settingsPanel = null;
    [SerializeField] private GameObject settingsPanelImage = null;
    [SerializeField] private Button settingsCloseButton = null;
    [SerializeField] private Slider bgmSlider = null;
    [SerializeField] private Slider sfxSlider = null;
    [SerializeField] private Button hapticsOnButton = null;
    [SerializeField] private Button hapticsOffButton = null;
    [SerializeField] private Vector2 settingsPanelSize = new Vector2(480f, 320f);
    [SerializeField] private int settingsPanelSortingOrder = 100;

    private Button startButton;
    private CanvasGroup titleCanvasGroup;
    private CanvasGroup tutorialCanvasGroup;
    private CanvasGroup startCanvasGroup;
    private Vector2 titleStartPosition;
    private Vector2 tutorialStartPosition;
    private Vector2 startStartPosition;
    private Coroutine startSequenceCoroutine;
    private bool hasCachedMenuPositions;
    private bool isStartSequencePlaying;
    private bool suppressSettingsCallbacks;
    private bool restoreGameOverPanelOnSettingsClose;
    private float bestRecord;
    private Font runtimeUiFont;

    private void Awake()
    {
        AutoAssignGameOverReferences();
        AutoAssignCanvasRoots();
        AutoAssignStartScreenReferences();
        AutoAssignSettingsReferences();
        EnsureSettingsUi();
        CacheStartScreenPositions();
        LoadBestRecord();
        ConfigureButtons();
    }

    private void Start()
    {
        RefreshSettingsControls();
        SetSettingsButtonsVisible(true, false);
        CloseSettingsPanel();
    }

    private void OnDestroy()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(HandleRestartPressed);
        }

        if (gameOverSettingsButton != null)
        {
            gameOverSettingsButton.onClick.RemoveListener(HandleSettingsPressed);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(HandleStartPressed);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(HandleSettingsPressed);
        }

        if (settingsCloseButton != null)
        {
            settingsCloseButton.onClick.RemoveListener(HandleSettingsClosePressed);
        }

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(HandleBgmVolumeChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(HandleSfxVolumeChanged);
        }

        if (hapticsOnButton != null)
        {
            hapticsOnButton.onClick.RemoveListener(HandleHapticsOnPressed);
        }

        if (hapticsOffButton != null)
        {
            hapticsOffButton.onClick.RemoveListener(HandleHapticsOffPressed);
        }
    }

    public void ShowStartScreen(float score)
    {
        if (startSequenceCoroutine != null)
        {
            StopCoroutine(startSequenceCoroutine);
            startSequenceCoroutine = null;
        }

        isStartSequencePlaying = false;
        CloseSettingsPanel();
        RefreshSettingsControls();
        SetScoreDisplayVisible(false);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        ResetStartScreenVisualState();
        SetSettingsButtonsVisible(true, false);
        SetScore(score);
    }

    public void ShowGameplay(float score)
    {
        CloseSettingsPanel();
        RefreshSettingsControls();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        SetStartScreenVisible(false);
        SetSettingsButtonsVisible(false, false);
        SetScoreDisplayVisible(true);
        SetScore(score);
    }

    public void SetScore(float score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"{score:F2}";
        }
    }

    public void ShowGameOver(float finalScore)
    {
        CloseSettingsPanel();
        SetStartScreenVisible(false);
        SetScoreDisplayVisible(false);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (finalScoreText != null)
        {
            finalScoreText.text = $"{finalScore:F2}";
        }

        SetSettingsButtonsVisible(false, true);
        UpdateBestRecord(finalScore);
    }

    private void ConfigureButtons()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(HandleRestartPressed);
            restartButton.onClick.AddListener(HandleRestartPressed);
        }

        if (gameOverSettingsButton != null)
        {
            gameOverSettingsButton.onClick.RemoveListener(HandleSettingsPressed);
            gameOverSettingsButton.onClick.AddListener(HandleSettingsPressed);
        }

        if (startRect != null)
        {
            startButton = startRect.GetComponent<Button>();
            Image startImage = startRect.GetComponent<Image>();

            if (startButton == null)
            {
                startButton = startRect.gameObject.AddComponent<Button>();
            }

            if (startButton != null)
            {
                startButton.targetGraphic = startImage;
                startButton.onClick.RemoveListener(HandleStartPressed);
                startButton.onClick.AddListener(HandleStartPressed);
            }
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(HandleSettingsPressed);
            settingsButton.onClick.AddListener(HandleSettingsPressed);
        }

        if (settingsCloseButton != null)
        {
            settingsCloseButton.onClick.RemoveListener(HandleSettingsClosePressed);
            settingsCloseButton.onClick.AddListener(HandleSettingsClosePressed);
        }

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(HandleBgmVolumeChanged);
            bgmSlider.onValueChanged.AddListener(HandleBgmVolumeChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(HandleSfxVolumeChanged);
            sfxSlider.onValueChanged.AddListener(HandleSfxVolumeChanged);
        }

        if (hapticsOnButton != null)
        {
            hapticsOnButton.onClick.RemoveListener(HandleHapticsOnPressed);
            hapticsOnButton.onClick.AddListener(HandleHapticsOnPressed);
        }

        if (hapticsOffButton != null)
        {
            hapticsOffButton.onClick.RemoveListener(HandleHapticsOffPressed);
            hapticsOffButton.onClick.AddListener(HandleHapticsOffPressed);
        }
    }

    private void HandleRestartPressed()
    {
        CloseSettingsPanel();
        GameManager.Instance?.RestartGame();
    }

    private void HandleStartPressed()
    {
        if (isStartSequencePlaying)
        {
            return;
        }

        CloseSettingsPanel();
        startSequenceCoroutine = StartCoroutine(PlayStartSequence());
    }

    private void HandleSettingsPressed()
    {
        OpenSettingsPanel();
    }

    private void HandleSettingsClosePressed()
    {
        CloseSettingsPanel();
    }

    private void HandleBgmVolumeChanged(float value)
    {
        if (suppressSettingsCallbacks)
        {
            return;
        }

        GameAudio.Instance?.SetBgmVolume(value);
    }

    private void HandleSfxVolumeChanged(float value)
    {
        if (suppressSettingsCallbacks)
        {
            return;
        }

        GameAudio.Instance?.SetSfxVolume(value);
    }

    private void HandleHapticsOnPressed()
    {
        if (suppressSettingsCallbacks)
        {
            return;
        }

        GameManager.Instance?.SetHapticsEnabled(true);
        RefreshSettingsControls();
    }

    private void HandleHapticsOffPressed()
    {
        if (suppressSettingsCallbacks)
        {
            return;
        }

        GameManager.Instance?.SetHapticsEnabled(false);
        RefreshSettingsControls();
    }

    private void OpenSettingsPanel()
    {
        if (settingsPanel == null)
        {
            return;
        }

        restoreGameOverPanelOnSettingsClose = gameOverPanel != null && gameOverPanel.activeSelf;
        if (restoreGameOverPanelOnSettingsClose)
        {
            gameOverPanel.SetActive(false);
        }

        RefreshSettingsControls();
        settingsPanel.SetActive(true);
        if (settingsPanelImage != null)
        {
            settingsPanelImage.SetActive(true);
        }
        settingsPanel.transform.SetAsLastSibling();
    }

    private void CloseSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            if (settingsPanelImage != null)
            {
                settingsPanelImage.SetActive(false);
            }
        }

        if (restoreGameOverPanelOnSettingsClose && gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        restoreGameOverPanelOnSettingsClose = false;
    }

    private void RefreshSettingsControls()
    {
        suppressSettingsCallbacks = true;

        if (bgmSlider != null && GameAudio.Instance != null)
        {
            bgmSlider.SetValueWithoutNotify(GameAudio.Instance.BgmVolume);
        }

        if (sfxSlider != null && GameAudio.Instance != null)
        {
            sfxSlider.SetValueWithoutNotify(GameAudio.Instance.SfxVolume);
        }

        if (GameManager.Instance != null)
        {
            UpdateHapticsButtonVisuals(GameManager.Instance.HapticsEnabled);
        }

        suppressSettingsCallbacks = false;
    }

    private void AutoAssignGameOverReferences()
    {
        if (gameOverPanel == null)
        {
            GameObject gameOverPanelObject = FindSceneObjectByName("GameOverPanel");
            if (gameOverPanelObject != null)
            {
                gameOverPanel = gameOverPanelObject;
            }
        }

        if (gameOverPanel == null)
        {
            return;
        }

        if (finalScoreText == null)
        {
            finalScoreText = FindComponentInChildrenByName<TextMeshProUGUI>(gameOverPanel.transform, "FinalScoreText");
        }

        if (bestRecordText == null)
        {
            bestRecordText = FindComponentInChildrenByName<TextMeshProUGUI>(gameOverPanel.transform, "BestRecordText");
        }

        if (gameOverSettingsButton == null)
        {
            gameOverSettingsButton = FindComponentInChildrenByName<Button>(gameOverPanel.transform, "GameOverSettingsButton");
        }
    }

    private void AutoAssignCanvasRoots()
    {
        if (screenCanvasRoot == null)
        {
            GameObject screenCanvasObject = FindSceneObjectByName("ScreenCanvas");
            if (screenCanvasObject != null)
            {
                screenCanvasRoot = screenCanvasObject.GetComponent<RectTransform>();
            }
        }

        if (overlayCanvasRoot == null)
        {
            GameObject overlayCanvasObject = FindSceneObjectByName("OverlayCanvas");
            if (overlayCanvasObject != null)
            {
                overlayCanvasRoot = overlayCanvasObject.GetComponent<RectTransform>();
            }
        }
    }

    private void AutoAssignStartScreenReferences()
    {
        if (screenCanvasRoot == null)
        {
            return;
        }

        if (titleRect == null)
        {
            titleRect = FindComponentInChildrenByName<RectTransform>(screenCanvasRoot, "Title");
        }

        if (tutorialRect == null)
        {
            tutorialRect = FindComponentInChildrenByName<RectTransform>(screenCanvasRoot, "Tutorial");
        }

        if (startRect == null)
        {
            startRect = FindComponentInChildrenByName<RectTransform>(screenCanvasRoot, "Start");
        }

        titleCanvasGroup = GetOrAddCanvasGroup(titleRect);
        tutorialCanvasGroup = GetOrAddCanvasGroup(tutorialRect);
        startCanvasGroup = GetOrAddCanvasGroup(startRect);
    }

    private void AutoAssignSettingsReferences()
    {
        if (screenCanvasRoot == null && overlayCanvasRoot == null)
        {
            return;
        }

        if (settingsButton == null)
        {
            if (screenCanvasRoot != null)
            {
                settingsButton = FindComponentInChildrenByName<Button>(screenCanvasRoot, "SettingsButton");
            }

            if (settingsButton == null && overlayCanvasRoot != null)
            {
                settingsButton = FindComponentInChildrenByName<Button>(overlayCanvasRoot, "SettingsButton");
            }
        }

        if (settingsPanel == null)
        {
            Transform settingsPanelTransform = overlayCanvasRoot != null ? FindTransformInChildren(overlayCanvasRoot, "SettingsPanel") : null;
            if (settingsPanelTransform != null)
            {
                settingsPanel = settingsPanelTransform.gameObject;
            }
        }

        if (settingsPanel == null)
        {
            return;
        }

        if (settingsPanelImage == null)
        {
            settingsPanelImage = settingsPanel;
        }

        ConfigureSettingsPanelCanvas();

        if (settingsCloseButton == null)
        {
            settingsCloseButton = FindComponentInChildrenByName<Button>(settingsPanel.transform, "SettingsCloseButton");
            if (settingsCloseButton == null)
            {
                settingsCloseButton = FindComponentInChildrenByName<Button>(settingsPanel.transform, "CloseButton");
            }
        }

        if (bgmSlider == null)
        {
            bgmSlider = FindComponentInChildrenByName<Slider>(settingsPanel.transform, "BgmSlider");
        }

        if (sfxSlider == null)
        {
            sfxSlider = FindComponentInChildrenByName<Slider>(settingsPanel.transform, "SfxSlider");
        }

        if (hapticsOnButton == null)
        {
            hapticsOnButton = FindComponentInChildrenByName<Button>(settingsPanel.transform, "HapticsOnButton");
        }

        if (hapticsOffButton == null)
        {
            hapticsOffButton = FindComponentInChildrenByName<Button>(settingsPanel.transform, "HapticsOffButton");
        }

        Transform legacyHapticsToggle = FindTransformInChildren(settingsPanel.transform, "HapticsToggle");
        if (legacyHapticsToggle != null)
        {
            legacyHapticsToggle.gameObject.SetActive(false);
        }
    }

    private void EnsureSettingsUi()
    {
        RectTransform settingsButtonParent = screenCanvasRoot != null ? screenCanvasRoot : overlayCanvasRoot;
        RectTransform settingsPanelParent = overlayCanvasRoot != null ? overlayCanvasRoot : screenCanvasRoot;

        if (settingsButtonParent == null || settingsPanelParent == null)
        {
            return;
        }

        DefaultControls.Resources resources = new DefaultControls.Resources();

        if (settingsButton == null)
        {
            settingsButton = CreateRuntimeButton(
                settingsButtonParent,
                resources,
                "SettingsButton",
                "Settings",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-24f, -24f),
                new Vector2(160f, 42f));
        }

        if (settingsPanel == null)
        {
            settingsPanel = CreateRuntimePanel(settingsPanelParent, resources, "SettingsPanel", settingsPanelSize);
        }

        if (settingsPanelImage == null)
        {
            settingsPanelImage = settingsPanel;
        }

        ConfigureSettingsPanelCanvas();

        if (gameOverPanel != null && gameOverSettingsButton == null)
        {
            gameOverSettingsButton = CreateRuntimeButton(gameOverPanel.transform, resources, "GameOverSettingsButton", "Settings", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -700f), new Vector2(280f, 90f));
        }
        Transform panelTransform = settingsPanel.transform;

        Transform settingsTitle = FindTransformInChildren(panelTransform, "SettingsTitle");
        if (settingsTitle != null)
        {
            settingsTitle.gameObject.SetActive(false);
        }

        if (settingsCloseButton == null)
        {
            settingsCloseButton = CreateRuntimeButton(
                panelTransform,
                resources,
                "SettingsCloseButton",
                "Close",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-18f, -18f),
                new Vector2(92f, 34f));
        }

        if (bgmSlider == null)
        {
            bgmSlider = CreateRuntimeLabeledSlider(panelTransform, resources, "BgmSlider", "BGM", new Vector2(52f, 42f));
        }

        if (sfxSlider == null)
        {
            sfxSlider = CreateRuntimeLabeledSlider(panelTransform, resources, "SfxSlider", "SFX", new Vector2(52f, -22f));
        }

        Transform hapticsLabel = FindTransformInChildren(panelTransform, "HapticsLabel");
        if (hapticsLabel != null)
        {
            hapticsLabel.gameObject.SetActive(false);
        }

        if (hapticsOnButton == null)
        {
            hapticsOnButton = CreateRuntimeButton(panelTransform, resources, "HapticsOnButton", "ON", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, -94f), new Vector2(84f, 36f));
        }

        if (hapticsOffButton == null)
        {
            hapticsOffButton = CreateRuntimeButton(panelTransform, resources, "HapticsOffButton", "OFF", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(128f, -94f), new Vector2(84f, 36f));
        }

        Transform legacyHapticsToggle = FindTransformInChildren(panelTransform, "HapticsToggle");
        if (legacyHapticsToggle != null)
        {
            legacyHapticsToggle.gameObject.SetActive(false);
        }

        CloseSettingsPanel();
    }

    private void SetSettingsButtonsVisible(bool isStartSettingsVisible, bool isGameOverSettingsVisible)
    {
        if (settingsButton != null)
        {
            settingsButton.gameObject.SetActive(isStartSettingsVisible);
        }

        if (gameOverSettingsButton != null)
        {
            gameOverSettingsButton.gameObject.SetActive(isGameOverSettingsVisible);
        }
    }
    private void UpdateHapticsButtonVisuals(bool isEnabled)
    {
        SetChoiceButtonState(hapticsOnButton, isEnabled);
        SetChoiceButtonState(hapticsOffButton, !isEnabled);
    }

    private void SetChoiceButtonState(Button button, bool isSelected)
    {
        if (button == null)
        {
            return;
        }

        Color baseColor = isSelected ? new Color(0.24f, 0.73f, 0.98f, 1f) : new Color(0.12f, 0.16f, 0.2f, 0.95f);
        Color textColor = isSelected ? Color.white : new Color(0.78f, 0.9f, 0.98f, 1f);
        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = baseColor;
        }

        Text legacyText = button.GetComponentInChildren<Text>(true);
        if (legacyText != null)
        {
            legacyText.color = textColor;
        }

        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.color = textColor;
        }
    }


    private void CacheStartScreenPositions()
    {
        if (hasCachedMenuPositions)
        {
            return;
        }

        hasCachedMenuPositions = true;
        titleStartPosition = titleRect != null ? titleRect.anchoredPosition : Vector2.zero;
        tutorialStartPosition = tutorialRect != null ? tutorialRect.anchoredPosition : Vector2.zero;
        startStartPosition = startRect != null ? startRect.anchoredPosition : Vector2.zero;
    }

    private void LoadBestRecord()
    {
        bestRecord = PlayerPrefs.GetFloat(BestRecordKey, 0f);
        RefreshBestRecordLabel();
    }

    private void UpdateBestRecord(float finalScore)
    {
        if (finalScore > bestRecord)
        {
            bestRecord = finalScore;
            PlayerPrefs.SetFloat(BestRecordKey, bestRecord);
            PlayerPrefs.Save();
        }

        RefreshBestRecordLabel();
    }

    private void RefreshBestRecordLabel()
    {
        if (bestRecordText != null)
        {
            bestRecordText.text = $"{bestRecord:F2}";
        }
    }

    private void ResetStartScreenVisualState()
    {
        CacheStartScreenPositions();
        SetStartScreenVisible(true);

        if (titleRect != null)
        {
            titleRect.anchoredPosition = titleStartPosition;
        }

        if (tutorialRect != null)
        {
            tutorialRect.anchoredPosition = tutorialStartPosition;
        }

        if (startRect != null)
        {
            startRect.anchoredPosition = startStartPosition;
        }

        SetCanvasGroupAlpha(titleCanvasGroup, 1f);
        SetCanvasGroupAlpha(tutorialCanvasGroup, 1f);
        SetCanvasGroupAlpha(startCanvasGroup, 1f);

        if (startButton != null)
        {
            startButton.interactable = true;
        }
    }

    private void SetStartScreenVisible(bool isVisible)
    {
        SetActive(titleRect, isVisible);
        SetActive(tutorialRect, isVisible);
        SetActive(startRect, isVisible);
    }

    private void SetScoreDisplayVisible(bool isVisible)
    {
        if (scoreText == null)
        {
            return;
        }

        Transform scoreRoot = scoreText.transform.parent;
        if (scoreRoot != null)
        {
            scoreRoot.gameObject.SetActive(isVisible);
            return;
        }

        scoreText.gameObject.SetActive(isVisible);
    }

    private IEnumerator PlayStartSequence()
    {
        isStartSequencePlaying = true;

        if (startButton != null)
        {
            startButton.interactable = false;
        }

        SetStartScreenVisible(true);
        CacheStartScreenPositions();

        Vector2 titleDipPosition = titleStartPosition + Vector2.down * titleDipDistance;
        Vector2 titleTarget = titleStartPosition + Vector2.up * GetVerticalTravelDistance(titleRect);
        Vector2 tutorialLiftPosition = tutorialStartPosition + Vector2.up * tutorialLiftDistance;
        Vector2 tutorialTarget = tutorialStartPosition + Vector2.down * GetVerticalTravelDistance(tutorialRect);

        float elapsed = 0f;
        bool gameplayStarted = false;

        while (elapsed < startTransitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = startTransitionDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / startTransitionDuration);

            SetAnchoredPosition(titleRect, EvaluateAnticipatedMotion(titleStartPosition, titleDipPosition, titleTarget, progress));
            SetAnchoredPosition(tutorialRect, EvaluateAnticipatedMotion(tutorialStartPosition, tutorialLiftPosition, tutorialTarget, progress));
            SetAnchoredPosition(startRect, startStartPosition);

            float alpha = 1f - Mathf.SmoothStep(0f, 1f, progress);
            SetCanvasGroupAlpha(titleCanvasGroup, alpha);
            SetCanvasGroupAlpha(tutorialCanvasGroup, alpha);
            SetCanvasGroupAlpha(startCanvasGroup, alpha);

            if (!gameplayStarted && progress >= gameplayStartProgress)
            {
                gameplayStarted = true;
                GameManager.Instance?.StartGameFromMenu();
            }

            yield return null;
        }

        if (!gameplayStarted)
        {
            GameManager.Instance?.StartGameFromMenu();
        }

        SetStartScreenVisible(false);
        SetCanvasGroupAlpha(titleCanvasGroup, 1f);
        SetCanvasGroupAlpha(tutorialCanvasGroup, 1f);
        SetCanvasGroupAlpha(startCanvasGroup, 1f);
        startSequenceCoroutine = null;
        isStartSequencePlaying = false;
    }

    private float GetVerticalTravelDistance(RectTransform target)
    {
        float screenHeight = screenCanvasRoot != null ? screenCanvasRoot.rect.height : Screen.height;
        float targetHeight = target != null ? target.rect.height * target.lossyScale.y : 0f;
        return (screenHeight * 0.5f) + targetHeight + 120f;
    }

    private Vector2 EvaluateAnticipatedMotion(Vector2 start, Vector2 anticipation, Vector2 target, float progress)
    {
        float clampedPortion = Mathf.Clamp(anticipationPortion, 0.05f, 0.5f);
        if (progress <= clampedPortion)
        {
            float anticipationProgress = Mathf.InverseLerp(0f, clampedPortion, progress);
            float anticipationEase = Mathf.SmoothStep(0f, 1f, anticipationProgress);
            return Vector2.Lerp(start, anticipation, anticipationEase);
        }

        float exitProgress = Mathf.InverseLerp(clampedPortion, 1f, progress);
        float exitEase = Mathf.SmoothStep(0f, 1f, exitProgress);
        return Vector2.Lerp(anticipation, target, exitEase);
    }

    private Button CreateRuntimeButton(Transform parent, DefaultControls.Resources resources, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject buttonObject = DefaultControls.CreateButton(resources);
        buttonObject.name = name;
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

        Image image = buttonObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.12f, 0.16f, 0.2f, 0.95f);
        }

        Button button = buttonObject.GetComponent<Button>();
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 1f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.95f);
            colors.pressedColor = new Color(0.85f, 0.9f, 0.95f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
            button.colors = colors;
        }

        Text buttonText = buttonObject.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = label;
            buttonText.font = GetRuntimeFont();
            buttonText.fontStyle = FontStyle.Bold;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;
        }

        return button;
    }

    private void ConfigureSettingsPanelCanvas()
    {
        if (settingsPanel == null)
        {
            return;
        }

        Canvas panelCanvas = settingsPanel.GetComponent<Canvas>();
        if (panelCanvas == null)
        {
            panelCanvas = settingsPanel.AddComponent<Canvas>();
        }

        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = settingsPanelSortingOrder;

        if (settingsPanel.GetComponent<GraphicRaycaster>() == null)
        {
            settingsPanel.AddComponent<GraphicRaycaster>();
        }
    }

    private GameObject CreateRuntimePanel(Transform parent, DefaultControls.Resources resources, string name, Vector2 sizeDelta)
    {
        GameObject panelObject = DefaultControls.CreatePanel(resources);
        panelObject.name = name;
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = sizeDelta;

        Image image = panelObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.07f, 0.09f, 0.12f, 0.94f);
        }

        panelObject.SetActive(false);
        return panelObject;
    }

    private Slider CreateRuntimeLabeledSlider(Transform parent, DefaultControls.Resources resources, string name, string label, Vector2 anchoredPosition)
    {
        CreateRuntimeText(parent, name + "Label", label, anchoredPosition + new Vector2(-160f, 0f), new Vector2(120f, 28f), 18, TextAnchor.MiddleLeft, FontStyle.Bold);

        GameObject sliderObject = DefaultControls.CreateSlider(resources);
        sliderObject.name = name;
        sliderObject.transform.SetParent(parent, false);

        RectTransform rectTransform = sliderObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(240f, 20f);

        Image background = sliderObject.transform.Find("Background")?.GetComponent<Image>();
        if (background != null)
        {
            background.color = new Color(0.19f, 0.21f, 0.26f, 0.95f);
        }

        Image fill = sliderObject.transform.Find("Fill Area/Fill")?.GetComponent<Image>();
        if (fill != null)
        {
            fill.color = new Color(0.31f, 0.68f, 0.96f, 1f);
        }

        Image handle = sliderObject.transform.Find("Handle Slide Area/Handle")?.GetComponent<Image>();
        if (handle != null)
        {
            handle.color = Color.white;
        }

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        return slider;
    }

    private Toggle CreateRuntimeToggle(Transform parent, DefaultControls.Resources resources, string name, string label, Vector2 anchoredPosition)
    {
        GameObject toggleObject = DefaultControls.CreateToggle(resources);
        toggleObject.name = name;
        toggleObject.transform.SetParent(parent, false);

        RectTransform rectTransform = toggleObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(220f, 28f);

        Image background = toggleObject.transform.Find("Background")?.GetComponent<Image>();
        if (background != null)
        {
            background.color = new Color(0.19f, 0.21f, 0.26f, 0.95f);
        }

        Image checkmark = toggleObject.transform.Find("Background/Checkmark")?.GetComponent<Image>();
        if (checkmark != null)
        {
            checkmark.color = new Color(0.31f, 0.68f, 0.96f, 1f);
        }

        Text labelText = toggleObject.transform.Find("Label")?.GetComponent<Text>();
        if (labelText != null)
        {
            labelText.text = label;
            labelText.font = GetRuntimeFont();
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;
        }

        return toggleObject.GetComponent<Toggle>();
    }

    private Text CreateRuntimeText(Transform parent, string name, string content, Vector2 anchoredPosition, Vector2 sizeDelta, int fontSize, TextAnchor alignment, FontStyle fontStyle)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

        Text text = textObject.GetComponent<Text>();
        text.font = GetRuntimeFont();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.fontStyle = fontStyle;
        text.color = Color.white;
        return text;
    }

    private Font GetRuntimeFont()
    {
        if (runtimeUiFont == null)
        {
            runtimeUiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (runtimeUiFont == null)
            {
                runtimeUiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        return runtimeUiFont;
    }

    private static GameObject FindSceneObjectByName(string objectName)
    {
        foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (transform.name != objectName)
            {
                continue;
            }

            if (!transform.gameObject.scene.IsValid())
            {
                continue;
            }

            return transform.gameObject;
        }

        return null;
    }

    private static Transform FindTransformInChildren(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private static T FindComponentInChildrenByName<T>(Transform root, string childName) where T : Component
    {
        Transform child = FindTransformInChildren(root, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static CanvasGroup GetOrAddCanvasGroup(RectTransform target)
    {
        if (target == null)
        {
            return null;
        }

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }

    private static void SetCanvasGroupAlpha(CanvasGroup canvasGroup, float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
    }

    private static void SetAnchoredPosition(RectTransform target, Vector2 anchoredPosition)
    {
        if (target != null)
        {
            target.anchoredPosition = anchoredPosition;
        }
    }

    private static void SetActive(RectTransform target, bool isActive)
    {
        if (target != null)
        {
            target.gameObject.SetActive(isActive);
        }
    }
}










