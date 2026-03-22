using System.Collections.Generic;
using UnityEngine;

public class MeteorSpawner : MonoBehaviour
{
    [SerializeField] private GameObject meteorPrefab;
    [SerializeField, Min(0.05f)] private float minSpawnInterval = 0.35f;
    [SerializeField, Min(0.05f)] private float maxSpawnInterval = 1.05f;
    [SerializeField, Min(1)] private int maxActiveMeteors = 25;

    [Header("Spawn Area")]
    [SerializeField] private bool useCameraBounds = true;
    [SerializeField] private Vector2 spawnXRange = new Vector2(-9f, 9f);
    [SerializeField] private float spawnY = 6.5f;
    [SerializeField] private float horizontalPadding = 1f;
    [SerializeField] private float topPadding = 1f;

    private readonly List<GameObject> activeMeteors = new List<GameObject>();
    private float spawnTimer;
    private bool missingPrefabWarningShown;

    private void Start()
    {
        ResetSpawnTimer();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            return;
        }

        CleanupMeteorList();

        if (activeMeteors.Count >= maxActiveMeteors)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
        {
            return;
        }

        SpawnMeteor();
        ResetSpawnTimer();
    }

    private void SpawnMeteor()
    {
        if (meteorPrefab == null)
        {
            if (!missingPrefabWarningShown)
            {
                Debug.LogWarning("MeteorSpawner is missing a meteor prefab reference.");
                missingPrefabWarningShown = true;
            }

            return;
        }

        Vector2 spawnPosition = CalculateSpawnPosition();
        GameObject meteor = Instantiate(meteorPrefab, spawnPosition, Quaternion.identity);
        activeMeteors.Add(meteor);
    }

    private void ResetSpawnTimer()
    {
        float maxInterval = Mathf.Max(minSpawnInterval, maxSpawnInterval);
        spawnTimer = Random.Range(minSpawnInterval, maxInterval);
    }

    private Vector2 CalculateSpawnPosition()
    {
        if (useCameraBounds && Camera.main != null)
        {
            Vector3 leftTop = Camera.main.ViewportToWorldPoint(new Vector3(0f, 1f, 0f));
            Vector3 rightTop = Camera.main.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));
            float minX = leftTop.x - horizontalPadding;
            float maxX = rightTop.x + horizontalPadding;
            float y = leftTop.y + topPadding;

            return new Vector2(Random.Range(minX, maxX), y);
        }

        return new Vector2(Random.Range(spawnXRange.x, spawnXRange.y), spawnY);
    }

    private void CleanupMeteorList()
    {
        for (int i = activeMeteors.Count - 1; i >= 0; i--)
        {
            if (activeMeteors[i] == null)
            {
                activeMeteors.RemoveAt(i);
            }
        }
    }
}
