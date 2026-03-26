using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class Meteor : MonoBehaviour
{
    [Header("Impact Cleanup")]
    [SerializeField, Min(0.05f)] private float postImpactLifetime = 0.15f;
    [SerializeField, Min(0.5f)] private float maxLifetime = 12f;

    private Transform planetCenter;
    private MeteorSpawner owner;
    private Collider2D cachedCollider;
    private Rigidbody2D cachedRigidbody;
    private SpriteRenderer cachedSpriteRenderer;
    private TrailRenderer cachedTrailRenderer;
    private Vector2 moveDirection;
    private float moveSpeed;
    private int planetHitBonus;
    private float lifeTimer;
    private float returnToPoolTimer;
    private bool isResolved;
    private bool isWaitingForPoolReturn;
    private bool hasPlayedPassSound;

    public bool IsPooled { get; private set; }

    private void Awake()
    {
        cachedCollider = GetComponent<Collider2D>();
        cachedRigidbody = GetComponent<Rigidbody2D>();
        cachedSpriteRenderer = GetComponent<SpriteRenderer>();
        cachedTrailRenderer = GetComponent<TrailRenderer>();

        if (cachedCollider != null)
        {
            cachedCollider.isTrigger = true;
        }

        if (cachedRigidbody != null)
        {
            cachedRigidbody.bodyType = RigidbodyType2D.Kinematic;
            cachedRigidbody.gravityScale = 0f;
        }
    }

    private void Update()
    {
        if (IsPooled)
        {
            return;
        }

        if (isWaitingForPoolReturn)
        {
            returnToPoolTimer -= Time.deltaTime;
            if (returnToPoolTimer <= 0f)
            {
                ReturnToPool();
            }

            return;
        }

        if (isResolved)
        {
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            return;
        }

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifetime)
        {
            ReturnToPool();
            return;
        }

        if (moveDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.position += (Vector3)(moveDirection * moveSpeed * Time.deltaTime);
        transform.up = moveDirection;
        TryPlayPassSoundWhenVisible();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isResolved || other == null)
        {
            return;
        }

        if (other.GetComponent<PlayerController>() != null || other.GetComponentInParent<PlayerController>() != null)
        {
            ResolvePlayerHit();
            return;
        }

        if (planetCenter != null && (other.transform == planetCenter || other.transform.IsChildOf(planetCenter)))
        {
            ResolvePlanetHit();
        }
    }

    private void OnDestroy()
    {
        owner?.UnregisterMeteor(this);
    }

    public void PrepareForPool(MeteorSpawner meteorSpawner)
    {
        owner = meteorSpawner;
        DeactivateForPool();
    }

    public void Initialize(Transform targetPlanetCenter, Vector2 direction, float speed, int bonusScore, MeteorSpawner meteorSpawner)
    {
        owner = meteorSpawner;
        planetCenter = targetPlanetCenter;
        moveDirection = direction.normalized;
        if (moveDirection.sqrMagnitude < 0.0001f)
        {
            moveDirection = Vector2.down;
        }

        moveSpeed = speed;
        planetHitBonus = bonusScore;
        lifeTimer = 0f;
        returnToPoolTimer = 0f;
        isResolved = false;
        isWaitingForPoolReturn = false;
        hasPlayedPassSound = false;
        IsPooled = false;

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (cachedCollider != null)
        {
            cachedCollider.enabled = true;
        }

        if (cachedRigidbody != null)
        {
            cachedRigidbody.linearVelocity = Vector2.zero;
            cachedRigidbody.angularVelocity = 0f;
            cachedRigidbody.simulated = true;
        }

        if (cachedSpriteRenderer != null)
        {
            cachedSpriteRenderer.enabled = true;
        }

        if (cachedTrailRenderer != null)
        {
            cachedTrailRenderer.Clear();
            cachedTrailRenderer.emitting = true;
        }

        transform.up = moveDirection;
    }

    public void DeactivateForPool()
    {
        lifeTimer = 0f;
        returnToPoolTimer = 0f;
        moveDirection = Vector2.zero;
        moveSpeed = 0f;
        planetHitBonus = 0;
        planetCenter = null;
        isResolved = false;
        isWaitingForPoolReturn = false;
        hasPlayedPassSound = false;
        IsPooled = true;

        if (cachedCollider != null)
        {
            cachedCollider.enabled = false;
        }

        if (cachedRigidbody != null)
        {
            cachedRigidbody.linearVelocity = Vector2.zero;
            cachedRigidbody.angularVelocity = 0f;
            cachedRigidbody.simulated = false;
        }

        if (cachedTrailRenderer != null)
        {
            cachedTrailRenderer.emitting = false;
            cachedTrailRenderer.Clear();
        }

        if (cachedSpriteRenderer != null)
        {
            cachedSpriteRenderer.enabled = true;
        }

        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    public bool IsVisibleInCamera(Camera camera)
    {
        if (camera == null || IsPooled || !gameObject.activeInHierarchy)
        {
            return false;
        }

        if (cachedSpriteRenderer == null || !cachedSpriteRenderer.enabled)
        {
            return false;
        }

        Bounds bounds = cachedSpriteRenderer.bounds;
        Vector3 minViewport = camera.WorldToViewportPoint(bounds.min);
        Vector3 maxViewport = camera.WorldToViewportPoint(bounds.max);

        if (maxViewport.z < 0f && minViewport.z < 0f)
        {
            return false;
        }

        return maxViewport.x >= 0f
            && minViewport.x <= 1f
            && maxViewport.y >= 0f
            && minViewport.y <= 1f;
    }

    private void ResolvePlayerHit()
    {
        if (isResolved)
        {
            return;
        }

        isResolved = true;
        owner?.UnregisterMeteor(this);

        if (owner != null && owner.IsProfiling)
        {
            owner.RecordProfilingEvent(
                "PLAYER_HIT_IGNORED",
                $"position=({transform.position.x:F2};{transform.position.y:F2})");
            BeginImpactCleanup();
            return;
        }

        GameManager.Instance?.HandlePlayerHit(transform.position);
        FreezeAfterPlayerHit();
    }

    private void ResolvePlanetHit()
    {
        if (isResolved)
        {
            return;
        }

        isResolved = true;
        owner?.UnregisterMeteor(this);
        GameManager.Instance?.RegisterMeteorPlanetHit(planetHitBonus);
        BeginImpactCleanup();
    }

    private void TryPlayPassSoundWhenVisible()
    {
        if (hasPlayedPassSound || owner == null)
        {
            return;
        }

        Camera targetCamera = Camera.main;
        if (targetCamera == null || !IsVisibleInCamera(targetCamera))
        {
            return;
        }

        hasPlayedPassSound = true;
        GameAudio.Instance?.PlayMeteorPass();
    }

    private void BeginImpactCleanup()
    {
        if (cachedCollider != null)
        {
            cachedCollider.enabled = false;
        }

        if (cachedRigidbody != null)
        {
            cachedRigidbody.linearVelocity = Vector2.zero;
            cachedRigidbody.angularVelocity = 0f;
            cachedRigidbody.simulated = false;
        }

        if (cachedSpriteRenderer != null)
        {
            cachedSpriteRenderer.enabled = false;
        }

        float cleanupDelay = postImpactLifetime;
        if (cachedTrailRenderer != null)
        {
            cachedTrailRenderer.emitting = false;
            cleanupDelay = Mathf.Max(cleanupDelay, cachedTrailRenderer.time);
        }

        isWaitingForPoolReturn = true;
        returnToPoolTimer = cleanupDelay;
    }

    private void FreezeAfterPlayerHit()
    {
        if (cachedCollider != null)
        {
            cachedCollider.enabled = false;
        }

        if (cachedRigidbody != null)
        {
            cachedRigidbody.linearVelocity = Vector2.zero;
            cachedRigidbody.angularVelocity = 0f;
            cachedRigidbody.simulated = false;
        }

        if (cachedTrailRenderer != null)
        {
            cachedTrailRenderer.emitting = false;
        }

        isWaitingForPoolReturn = false;
        returnToPoolTimer = 0f;
    }

    private void ReturnToPool()
    {
        if (owner == null)
        {
            DeactivateForPool();
            return;
        }

        owner.ReturnMeteorToPool(this);
    }
}