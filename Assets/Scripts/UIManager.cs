using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Gameplay UI")]
    [SerializeField] private TextMeshProUGUI scoreText = null;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel = null;
    [SerializeField] private TextMeshProUGUI finalScoreText = null;
    [SerializeField] private Button restartButton = null;

    private void Awake()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(HandleRestartPressed);
            restartButton.onClick.AddListener(HandleRestartPressed);
        }
    }

    private void OnDestroy()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(HandleRestartPressed);
        }
    }

    public void ShowGameplay(float score)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        SetScore(score);
    }

    public void SetScore(float score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score:F2}";
        }
    }

    public void ShowGameOver(float finalScore)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (finalScoreText != null)
        {
            finalScoreText.text = $"Final Score: {finalScore:F2}";
        }
    }

    private void HandleRestartPressed()
    {
        GameManager.Instance?.RestartGame();
    }
}
