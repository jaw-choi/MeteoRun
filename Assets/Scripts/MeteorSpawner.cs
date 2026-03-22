using System.Collections.Generic;
using UnityEngine;

public class MeteorSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Meteor meteorPrefab = null;
    [SerializeField] private Transform planetCenter = null;
    [SerializeField] private Camera targetCamera = null;

    [Header("Spawn Timing")]
    [SerializeField, Min(0.05f)] private float startingSpawnInterval = 0.12f;
    [SerializeField, Min(0.05f)] private float minimumSpawnInterval = 0.07f;
    [SerializeField, Min(1f)] private float spawnRampDuration = 90f;

    [Header("Visible Meteor Cap")]
    [SerializeField] private bool useVisibleMeteorCap = false;
    [SerializeField, Min(1)] private int startingMaxVisibleMeteors = 14;
    [SerializeField, Min(1)] private int maximumVisibleMeteors = 28;
    [SerializeField, Min(1f)] private float visibleMeteorIncreaseInterval = 10f;

    [Header("Meteor Speed")]
    [SerializeField, Min(0.1f)] private float baseMeteorSpeed = 1.35f;
    [SerializeField, Min(0f)] private float maximumSpeedVariation = 0.45f;
    [SerializeField, Min(0f)] private float speedVariationUnlockTime = 15f;
    [SerializeField, Min(0.1f)] private float speedVariationRampDuration = 60f;
    [SerializeField, Min(0)] private int planetHitBonusScore = 0;

    [Header("Trajectory Mix")]
    [SerializeField, Range(0f, 1f)] private float randomTrajectoryChance = 0.45f;
    [SerializeField, Min(0f)] private float randomTargetPadding = 0.5f;

    [Header("Spawn Placement")]
    [SerializeField, Min(0.1f)] private float spawnPadding = 1.5f;

    private readonly List<Meteor> activeMeteors = new List<Meteor>();
    private float elapsedTime;
    private float spawnTimer;
    private bool isSpawning;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (planetCenter == null)
        {
            GameObject planet = GameObject.Find("Planet");
            if (planet != null)
            {
                planetCenter = planet.transform;
            }
        }
    }

    private void Update()
    {
        if (!isSpawning || meteorPrefab == null || planetCenter == null)
        {
            return;
        }

        CleanupNullEntries();

        elapsedTime += Time.deltaTime;
        spawnTimer -= Time.deltaTime;

        if (useVisibleMeteorCap && activeMeteors.Count >= GetCurrentVisibleMeteorCap())
        {
            return;
        }

        if (spawnTimer > 0f)
        {
            return;
        }

        SpawnMeteor();
        spawnTimer = GetCurrentSpawnInterval();
    }

    public void BeginSpawning()
    {
        ClearAllMeteors();
        elapsedTime = 0f;
        spawnTimer = 0f;
        isSpawning = true;
    }

    public void StopSpawning()
    {
        isSpawning = false;
    }

    public void ClearAllMeteors()
    {
        CleanupNullEntries();

        for (int i = activeMeteors.Count - 1; i >= 0; i--)
        {
            if (activeMeteors[i] != null)
            {
                Destroy(activeMeteors[i].gameObject);
            }
        }

        activeMeteors.Clear();

        Meteor[] remainingMeteors = FindObjectsByType<Meteor>(FindObjectsSortMode.None);
        for (int i = 0; i < remainingMeteors.Length; i++)
        {
            if (remainingMeteors[i] != null)
            {
                Destroy(remainingMeteors[i].gameObject);
            }
        }
    }

    public void UnregisterMeteor(Meteor meteor)
    {
        if (meteor == null)
        {
            return;
        }

        activeMeteors.Remove(meteor);
    }

    private void SpawnMeteor()
    {
        Vector2 spawnPosition = GetSpawnPosition();
        Vector2 moveDirection = GetMoveDirection(spawnPosition);
        Meteor meteorInstance = Instantiate(meteorPrefab, spawnPosition, Quaternion.identity);
        meteorInstance.Initialize(planetCenter, moveDirection, GetCurrentMeteorSpeed(), planetHitBonusScore, this);
        activeMeteors.Add(meteorInstance);
    }

    private Vector2 GetSpawnPosition()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        float verticalExtent = targetCamera != null ? targetCamera.orthographicSize : 5f;
        float horizontalExtent = targetCamera != null ? verticalExtent * targetCamera.aspect : 5f;
        float spawnRadius = Mathf.Sqrt((verticalExtent * verticalExtent) + (horizontalExtent * horizontalExtent)) + spawnPadding;

        Vector2 direction = Random.insideUnitCircle.normalized;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.up;
        }

        return (Vector2)planetCenter.position + (direction * spawnRadius);
    }

    private Vector2 GetMoveDirection(Vector2 spawnPosition)
    {
        Vector2 targetPoint = (Vector2)planetCenter.position;

        if (Random.value < randomTrajectoryChance)
        {
            targetPoint = GetRandomTargetPoint();
        }

        Vector2 direction = (targetPoint - spawnPosition).normalized;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = ((Vector2)planetCenter.position - spawnPosition).normalized;
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.down;
        }

        return direction;
    }

    private Vector2 GetRandomTargetPoint()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        float verticalExtent = targetCamera != null ? targetCamera.orthographicSize : 5f;
        float horizontalExtent = targetCamera != null ? verticalExtent * targetCamera.aspect : 5f;
        Vector2 center = planetCenter != null ? (Vector2)planetCenter.position : Vector2.zero;

        float minX = center.x - horizontalExtent - randomTargetPadding;
        float maxX = center.x + horizontalExtent + randomTargetPadding;
        float minY = center.y - verticalExtent - randomTargetPadding;
        float maxY = center.y + verticalExtent + randomTargetPadding;

        return new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
    }

    private float GetCurrentSpawnInterval()
    {
        if (spawnRampDuration <= 0f)
        {
            return minimumSpawnInterval;
        }

        float t = Mathf.Clamp01(elapsedTime / spawnRampDuration);
        return Mathf.Lerp(startingSpawnInterval, minimumSpawnInterval, t);
    }

    private int GetCurrentVisibleMeteorCap()
    {
        int growthSteps = Mathf.FloorToInt(elapsedTime / visibleMeteorIncreaseInterval);
        int currentCap = startingMaxVisibleMeteors + growthSteps;
        return Mathf.Clamp(currentCap, startingMaxVisibleMeteors, maximumVisibleMeteors);
    }

    private float GetCurrentMeteorSpeed()
    {
        if (elapsedTime < speedVariationUnlockTime)
        {
            return baseMeteorSpeed;
        }

        float variationProgress = Mathf.Clamp01((elapsedTime - speedVariationUnlockTime) / speedVariationRampDuration);
        float activeVariation = Mathf.Lerp(0f, maximumSpeedVariation, variationProgress);
        float randomizedSpeed = baseMeteorSpeed + Random.Range(-activeVariation, activeVariation);
        return Mathf.Max(0.1f, randomizedSpeed);
    }

    private void CleanupNullEntries()
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
