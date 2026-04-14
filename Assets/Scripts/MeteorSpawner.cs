using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

public class MeteorSpawner : MonoBehaviour
{
    private const string ProfilingLogFileName = "meteor_pool_profile.csv";
    private const string ProfilingRecoveryFilePrefix = "meteor_pool_profile_recovery_";
    private const string ProfilingCsvHeader = "timestamp_utc,session_id,entry_type,elapsed_seconds,total_pool_size,active_count,visible_count,inactive_count,peak_active_count,pool_miss_count,pool_expansion_count,average_active_count,average_visible_count,max_visible_count,details";
    private const int RollingAverageWindow = 10;
    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

    [Header("References")]
    [SerializeField] private Meteor meteorPrefab = null;
    [SerializeField] private Transform planetCenter = null;
    [SerializeField] private Camera targetCamera = null;

    [Header("Object Pool")]
    [SerializeField, Min(1)] private int initialPoolSize = 64;
    [SerializeField, Min(1)] private int poolExpansionSize = 16;
    [SerializeField] private bool allowPoolExpansion = true;

    [Header("Profiling")]
    [SerializeField] private bool isProfiling = false;

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
    private readonly List<Meteor> pooledMeteors = new List<Meteor>();
    private readonly Queue<Meteor> inactiveMeteors = new Queue<Meteor>();
    private float elapsedTime;
    private float spawnTimer;
    private bool isSpawning;

    private bool profilingSessionActive;
    private string profilingSessionId = string.Empty;
    private string profilingLogPath = string.Empty;
    private float profilingSampleTimer;
    private float profilingElapsedTime;
    private float profilingAccumulatedActive;
    private float profilingAccumulatedVisible;
    private int profilingSampleCount;
    private int profilingMaxVisible;
    private readonly List<string> pendingProfilingLines = new List<string>();
    private bool hasLoggedProfilingIoWarning;

    public bool IsProfiling => isProfiling;
    public int ActiveMeteorCount => activeMeteors.Count;
    public int InactiveMeteorCount => inactiveMeteors.Count;
    public int TotalPoolSize => pooledMeteors.Count;
    public int PoolMissCount { get; private set; }
    public int PoolExpansionCount { get; private set; }
    public int PeakActiveMeteors { get; private set; }

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

        profilingLogPath = Path.Combine(Application.persistentDataPath, ProfilingLogFileName);
        if (isProfiling)
        {
            EnsureProfilingLogFile();
        }

        EnsurePoolSize(initialPoolSize);
    }

    private void Update()
    {
        if (profilingSessionActive)
        {
            UpdateProfilingSession();
        }

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

    private void OnDestroy()
    {
        EndProfilingSession("spawner_destroyed");
    }

    public void BeginSpawning()
    {
        EnsurePoolSize(initialPoolSize);
        ClearAllMeteors();
        elapsedTime = 0f;
        spawnTimer = 0f;
        PoolMissCount = 0;
        PoolExpansionCount = 0;
        PeakActiveMeteors = 0;
        isSpawning = true;

        if (isProfiling)
        {
            StartProfilingSession();
        }
    }

    public void StopSpawning()
    {
        isSpawning = false;

        if (profilingSessionActive)
        {
            EndProfilingSession("spawning_stopped");
        }
    }

    public void ClearAllMeteors()
    {
        CleanupNullEntries();

        for (int i = pooledMeteors.Count - 1; i >= 0; i--)
        {
            if (pooledMeteors[i] != null)
            {
                ReturnMeteorToPool(pooledMeteors[i]);
            }
        }

        activeMeteors.Clear();
        RebuildInactiveQueue();
    }

    public void UnregisterMeteor(Meteor meteor)
    {
        if (meteor == null)
        {
            return;
        }

        activeMeteors.Remove(meteor);
    }

    public void ReturnMeteorToPool(Meteor meteor)
    {
        if (meteor == null || meteor.IsPooled)
        {
            return;
        }

        activeMeteors.Remove(meteor);
        meteor.DeactivateForPool();
        inactiveMeteors.Enqueue(meteor);
    }

    public void RecordProfilingEvent(string entryType, string details)
    {
        if (!profilingSessionActive)
        {
            return;
        }

        int visibleCount = GetVisibleMeteorCount();
        AppendProfilingRow(
            entryType,
            TotalPoolSize,
            ActiveMeteorCount,
            visibleCount,
            InactiveMeteorCount,
            PeakActiveMeteors,
            PoolMissCount,
            PoolExpansionCount,
            null,
            null,
            null,
            details);

        if (!string.Equals(entryType, "SAMPLE", StringComparison.Ordinal))
        {
            Debug.Log($"[MeteorPoolProfile] {entryType} | {details} | active={ActiveMeteorCount}, visible={visibleCount}, pool={TotalPoolSize}");
        }
    }

    private void SpawnMeteor()
    {
        Meteor meteorInstance = GetPooledMeteor();
        if (meteorInstance == null)
        {
            return;
        }

        Vector2 spawnPosition = GetSpawnPosition();
        Vector2 moveDirection = GetMoveDirection(spawnPosition);
        meteorInstance.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
        meteorInstance.Initialize(planetCenter, moveDirection, GetCurrentMeteorSpeed(), planetHitBonusScore, this);
        activeMeteors.Add(meteorInstance);
        PeakActiveMeteors = Mathf.Max(PeakActiveMeteors, activeMeteors.Count);
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

        bool removedPoolEntry = false;
        for (int i = pooledMeteors.Count - 1; i >= 0; i--)
        {
            if (pooledMeteors[i] == null)
            {
                pooledMeteors.RemoveAt(i);
                removedPoolEntry = true;
            }
        }

        if (removedPoolEntry)
        {
            RebuildInactiveQueue();
        }
    }

    private Meteor GetPooledMeteor()
    {
        if (inactiveMeteors.Count == 0)
        {
            PoolMissCount++;
            RecordProfilingEvent("POOL_EMPTY", allowPoolExpansion ? "pool empty; requesting expansion" : "pool empty; spawn skipped");

            if (allowPoolExpansion && poolExpansionSize > 0)
            {
                ExpandPool(poolExpansionSize);
            }
        }

        if (inactiveMeteors.Count == 0)
        {
            RecordProfilingEvent("SPAWN_SKIPPED", "no inactive meteor available after pool check");
            return null;
        }

        return inactiveMeteors.Dequeue();
    }

    private void EnsurePoolSize(int desiredPoolSize)
    {
        if (meteorPrefab == null)
        {
            return;
        }

        while (pooledMeteors.Count < desiredPoolSize)
        {
            CreatePooledMeteor();
        }
    }

    private void ExpandPool(int expansionSize)
    {
        if (meteorPrefab == null || expansionSize <= 0)
        {
            return;
        }

        for (int i = 0; i < expansionSize; i++)
        {
            CreatePooledMeteor();
        }

        PoolExpansionCount++;
        RecordProfilingEvent("POOL_EXPAND", $"expanded_by={expansionSize};new_total_pool={TotalPoolSize}");
    }

    private void CreatePooledMeteor()
    {
        Meteor meteorInstance = Instantiate(meteorPrefab, transform);
        meteorInstance.PrepareForPool(this);
        pooledMeteors.Add(meteorInstance);
        inactiveMeteors.Enqueue(meteorInstance);
    }

    private void RebuildInactiveQueue()
    {
        inactiveMeteors.Clear();

        for (int i = 0; i < pooledMeteors.Count; i++)
        {
            Meteor meteor = pooledMeteors[i];
            if (meteor != null && meteor.IsPooled)
            {
                inactiveMeteors.Enqueue(meteor);
            }
        }
    }

    private void UpdateProfilingSession()
    {
        profilingElapsedTime += Time.deltaTime;
        profilingSampleTimer += Time.deltaTime;

        while (profilingSampleTimer >= 1f)
        {
            profilingSampleTimer -= 1f;
            LogProfilingSample();
        }
    }

    private void StartProfilingSession()
    {
        if (profilingSessionActive)
        {
            EndProfilingSession("new_session_started");
        }

        EnsureProfilingLogFile();
        profilingSessionActive = true;
        profilingSessionId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
        profilingSampleTimer = 0f;
        profilingElapsedTime = 0f;
        profilingAccumulatedActive = 0f;
        profilingAccumulatedVisible = 0f;
        profilingSampleCount = 0;
        profilingMaxVisible = 0;

        Debug.Log($"Meteor pool profiling enabled. Log file: {profilingLogPath}");
        RecordProfilingEvent(
            "SESSION_START",
            $"initial_pool={initialPoolSize};expansion_size={poolExpansionSize};allow_expand={allowPoolExpansion};visible_cap={useVisibleMeteorCap}");
    }

    private void EndProfilingSession(string reason)
    {
        if (!profilingSessionActive)
        {
            return;
        }

        int visibleCount = GetVisibleMeteorCount();
        float averageActive = profilingSampleCount > 0 ? profilingAccumulatedActive / profilingSampleCount : ActiveMeteorCount;
        float averageVisible = profilingSampleCount > 0 ? profilingAccumulatedVisible / profilingSampleCount : visibleCount;
        int maxVisible = profilingSampleCount > 0 ? profilingMaxVisible : visibleCount;

        AppendProfilingRow(
            "SESSION_END",
            TotalPoolSize,
            ActiveMeteorCount,
            visibleCount,
            InactiveMeteorCount,
            PeakActiveMeteors,
            PoolMissCount,
            PoolExpansionCount,
            averageActive,
            averageVisible,
            maxVisible,
            $"samples={profilingSampleCount};reason={SanitizeDetails(reason)}");

        profilingSessionActive = false;
        AppendRollingAverageIfNeeded();
        PersistPendingProfilingLinesToRecoveryFile();
    }

    private void LogProfilingSample()
    {
        int visibleCount = GetVisibleMeteorCount();
        profilingSampleCount++;
        profilingAccumulatedActive += ActiveMeteorCount;
        profilingAccumulatedVisible += visibleCount;
        profilingMaxVisible = Mathf.Max(profilingMaxVisible, visibleCount);

        AppendProfilingRow(
            "SAMPLE",
            TotalPoolSize,
            ActiveMeteorCount,
            visibleCount,
            InactiveMeteorCount,
            PeakActiveMeteors,
            PoolMissCount,
            PoolExpansionCount,
            null,
            null,
            null,
            string.Empty);
    }

    private int GetVisibleMeteorCount()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return 0;
        }

        int visibleCount = 0;
        for (int i = 0; i < pooledMeteors.Count; i++)
        {
            Meteor meteor = pooledMeteors[i];
            if (meteor != null && meteor.IsVisibleInCamera(targetCamera))
            {
                visibleCount++;
            }
        }

        return visibleCount;
    }

    private bool EnsureProfilingLogFile()
    {
        if (string.IsNullOrEmpty(profilingLogPath))
        {
            profilingLogPath = Path.Combine(Application.persistentDataPath, ProfilingLogFileName);
        }

        string directoryPath = Path.GetDirectoryName(profilingLogPath);
        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        try
        {
            using (FileStream stream = new FileStream(profilingLogPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                if (stream.Length == 0)
                {
                    using (StreamWriter writer = new StreamWriter(stream, Utf8NoBom, 1024, true))
                    {
                        writer.WriteLine(ProfilingCsvHeader);
                    }
                }
            }

            hasLoggedProfilingIoWarning = false;
            return true;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            LogProfilingIoWarning("create", exception);
            return false;
        }
    }

    private void AppendProfilingRow(
        string entryType,
        int totalPoolSize,
        int activeCount,
        int visibleCount,
        int inactiveCount,
        int peakActiveCount,
        int poolMissCount,
        int poolExpansionCount,
        float? averageActiveCount,
        float? averageVisibleCount,
        int? maxVisibleCount,
        string details)
    {
        string line = string.Join(
            ",",
            DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            profilingSessionId,
            entryType,
            profilingElapsedTime.ToString("F3", CultureInfo.InvariantCulture),
            totalPoolSize.ToString(CultureInfo.InvariantCulture),
            activeCount.ToString(CultureInfo.InvariantCulture),
            visibleCount.ToString(CultureInfo.InvariantCulture),
            inactiveCount.ToString(CultureInfo.InvariantCulture),
            peakActiveCount.ToString(CultureInfo.InvariantCulture),
            poolMissCount.ToString(CultureInfo.InvariantCulture),
            poolExpansionCount.ToString(CultureInfo.InvariantCulture),
            averageActiveCount.HasValue ? averageActiveCount.Value.ToString("F3", CultureInfo.InvariantCulture) : string.Empty,
            averageVisibleCount.HasValue ? averageVisibleCount.Value.ToString("F3", CultureInfo.InvariantCulture) : string.Empty,
            maxVisibleCount.HasValue ? maxVisibleCount.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
            SanitizeDetails(details));

        if (!EnsureProfilingLogFile())
        {
            pendingProfilingLines.Add(line);
            return;
        }

        if (!TryFlushPendingProfilingLines())
        {
            pendingProfilingLines.Add(line);
            return;
        }

        if (!TryAppendProfilingLines(new[] { line }))
        {
            pendingProfilingLines.Add(line);
        }
    }

    private bool TryFlushPendingProfilingLines()
    {
        if (pendingProfilingLines.Count == 0)
        {
            return true;
        }

        if (!TryAppendProfilingLines(pendingProfilingLines))
        {
            return false;
        }

        pendingProfilingLines.Clear();
        return true;
    }

    private bool TryAppendProfilingLines(IEnumerable<string> lines)
    {
        try
        {
            using (FileStream stream = new FileStream(profilingLogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (StreamWriter writer = new StreamWriter(stream, Utf8NoBom))
            {
                foreach (string line in lines)
                {
                    writer.WriteLine(line);
                }
            }

            hasLoggedProfilingIoWarning = false;
            return true;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            LogProfilingIoWarning("write", exception);
            return false;
        }
    }

    private bool TryReadProfilingLogLines(out List<string> lines)
    {
        lines = new List<string>();

        try
        {
            using (FileStream stream = new FileStream(profilingLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                while (!reader.EndOfStream)
                {
                    lines.Add(reader.ReadLine());
                }
            }

            hasLoggedProfilingIoWarning = false;
            return true;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            LogProfilingIoWarning("read", exception);
            return false;
        }
    }

    private void LogProfilingIoWarning(string operation, Exception exception)
    {
        if (hasLoggedProfilingIoWarning)
        {
            return;
        }

        hasLoggedProfilingIoWarning = true;
        Debug.LogWarning($"[MeteorPoolProfile] Log file access is temporarily unavailable. Buffered entries will retry automatically. operation={operation}, path={profilingLogPath}, message={exception.Message}");
    }

    private void PersistPendingProfilingLinesToRecoveryFile()
    {
        if (pendingProfilingLines.Count == 0)
        {
            return;
        }

        string recoveryPath = Path.Combine(Application.persistentDataPath, $"{ProfilingRecoveryFilePrefix}{profilingSessionId}.csv");

        try
        {
            string directoryPath = Path.GetDirectoryName(recoveryPath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            using (FileStream stream = new FileStream(recoveryPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                if (stream.Length == 0)
                {
                    using (StreamWriter writer = new StreamWriter(stream, Utf8NoBom, 1024, true))
                    {
                        writer.WriteLine(ProfilingCsvHeader);
                    }
                }

                stream.Seek(0, SeekOrigin.End);
                using (StreamWriter writer = new StreamWriter(stream, Utf8NoBom, 1024, true))
                {
                    foreach (string line in pendingProfilingLines)
                    {
                        writer.WriteLine(line);
                    }
                }
            }

            Debug.LogWarning($"[MeteorPoolProfile] Primary log file stayed locked. Buffered entries were written to recovery file: {recoveryPath}");
            pendingProfilingLines.Clear();
            hasLoggedProfilingIoWarning = false;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            Debug.LogWarning($"[MeteorPoolProfile] Failed to write recovery log file. path={recoveryPath}, message={exception.Message}");
        }
    }

    private void AppendRollingAverageIfNeeded()
    {
        List<ProfilingSessionSummary> summaries = LoadProfilingSessionSummaries();
        if (summaries.Count == 0 || summaries.Count % RollingAverageWindow != 0)
        {
            return;
        }

        int startIndex = summaries.Count - RollingAverageWindow;
        float durationTotal = 0f;
        float averageActiveTotal = 0f;
        float averageVisibleTotal = 0f;
        float totalPoolTotal = 0f;
        float peakActiveTotal = 0f;
        float maxVisibleTotal = 0f;
        float missTotal = 0f;
        float expansionTotal = 0f;

        for (int i = startIndex; i < summaries.Count; i++)
        {
            ProfilingSessionSummary summary = summaries[i];
            durationTotal += summary.ElapsedSeconds;
            averageActiveTotal += summary.AverageActiveCount;
            averageVisibleTotal += summary.AverageVisibleCount;
            totalPoolTotal += summary.TotalPoolSize;
            peakActiveTotal += summary.PeakActiveCount;
            maxVisibleTotal += summary.MaxVisibleCount;
            missTotal += summary.PoolMissCount;
            expansionTotal += summary.PoolExpansionCount;
        }

        string currentSessionId = profilingSessionId;
        profilingSessionId = $"ROLLING_{summaries.Count.ToString(CultureInfo.InvariantCulture)}";

        float rollingAverageActive = averageActiveTotal / RollingAverageWindow;
        float rollingAverageVisible = averageVisibleTotal / RollingAverageWindow;
        int rollingAveragePool = Mathf.RoundToInt(totalPoolTotal / RollingAverageWindow);
        int rollingPeakActive = Mathf.RoundToInt(peakActiveTotal / RollingAverageWindow);
        int rollingMaxVisible = Mathf.RoundToInt(maxVisibleTotal / RollingAverageWindow);
        int rollingAverageMiss = Mathf.RoundToInt(missTotal / RollingAverageWindow);
        int rollingAverageExpansion = Mathf.RoundToInt(expansionTotal / RollingAverageWindow);

        AppendProfilingRow(
            "ROLLING_AVG_10",
            rollingAveragePool,
            Mathf.RoundToInt(rollingAverageActive),
            Mathf.RoundToInt(rollingAverageVisible),
            Mathf.Max(0, rollingAveragePool - Mathf.RoundToInt(rollingAverageActive)),
            rollingPeakActive,
            rollingAverageMiss,
            rollingAverageExpansion,
            rollingAverageActive,
            rollingAverageVisible,
            rollingMaxVisible,
            $"sessions={summaries.Count - RollingAverageWindow + 1}-{summaries.Count};avg_duration={(durationTotal / RollingAverageWindow).ToString("F3", CultureInfo.InvariantCulture)}");

        profilingSessionId = currentSessionId;
    }

    private List<ProfilingSessionSummary> LoadProfilingSessionSummaries()
    {
        List<ProfilingSessionSummary> summaries = new List<ProfilingSessionSummary>();
        if (!EnsureProfilingLogFile())
        {
            return summaries;
        }

        if (!TryFlushPendingProfilingLines())
        {
            return summaries;
        }

        if (!TryReadProfilingLogLines(out List<string> lines))
        {
            return summaries;
        }

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("timestamp_utc", StringComparison.Ordinal))
            {
                continue;
            }

            string[] columns = line.Split(',');
            if (columns.Length < 15 || !string.Equals(columns[2], "SESSION_END", StringComparison.Ordinal))
            {
                continue;
            }

            ProfilingSessionSummary summary = new ProfilingSessionSummary
            {
                ElapsedSeconds = ParseFloat(columns[3]),
                TotalPoolSize = ParseInt(columns[4]),
                PeakActiveCount = ParseInt(columns[8]),
                PoolMissCount = ParseInt(columns[9]),
                PoolExpansionCount = ParseInt(columns[10]),
                AverageActiveCount = ParseFloat(columns[11]),
                AverageVisibleCount = ParseFloat(columns[12]),
                MaxVisibleCount = ParseInt(columns[13])
            };

            summaries.Add(summary);
        }

        return summaries;
    }

    private static int ParseInt(string value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedValue))
        {
            return parsedValue;
        }

        return 0;
    }

    private static float ParseFloat(string value)
    {
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedValue))
        {
            return parsedValue;
        }

        return 0f;
    }

    private static string SanitizeDetails(string details)
    {
        if (string.IsNullOrWhiteSpace(details))
        {
            return string.Empty;
        }

        return details.Replace(',', ';').Replace('\r', ' ').Replace('\n', ' ');
    }

    private struct ProfilingSessionSummary
    {
        public float ElapsedSeconds;
        public int TotalPoolSize;
        public int PeakActiveCount;
        public int PoolMissCount;
        public int PoolExpansionCount;
        public float AverageActiveCount;
        public float AverageVisibleCount;
        public int MaxVisibleCount;
    }
}
