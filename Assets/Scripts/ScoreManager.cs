using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField, Min(0f)] private float timeScale = 1f;

    private bool isScoring;
    private float elapsedTime;

    public float CurrentScore { get; private set; }
    public event Action<float> ScoreChanged;

    private void Update()
    {
        if (!isScoring)
        {
            return;
        }

        elapsedTime += Time.deltaTime * timeScale;
        CurrentScore = elapsedTime;
        ScoreChanged?.Invoke(CurrentScore);
    }

    public void ResetScore()
    {
        elapsedTime = 0f;
        CurrentScore = 0;
        ScoreChanged?.Invoke(CurrentScore);
    }

    public void BeginScoring()
    {
        isScoring = true;
    }

    public void StopScoring()
    {
        isScoring = false;
    }
}
