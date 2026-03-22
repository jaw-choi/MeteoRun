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
    private bool isResolved;

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
        if (isResolved)
        {
            return;
        }

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (moveDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.position += (Vector3)(moveDirection * moveSpeed * Time.deltaTime);

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            transform.up = moveDirection;
        }
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

    public void Initialize(Transform targetPlanetCenter, Vector2 direction, float speed, int bonusScore, MeteorSpawner meteorSpawner)
    {
        planetCenter = targetPlanetCenter;
        moveDirection = direction.normalized;
        moveSpeed = speed;
        planetHitBonus = bonusScore;
        owner = meteorSpawner;
        lifeTimer = 0f;
    }

    private void ResolvePlayerHit()
    {
        if (isResolved)
        {
            return;
        }

        isResolved = true;
        GameManager.Instance?.HandlePlayerHit();
        BeginImpactCleanup();
    }

    private void ResolvePlanetHit()
    {
        if (isResolved)
        {
            return;
        }

        isResolved = true;
        GameManager.Instance?.RegisterMeteorPlanetHit(planetHitBonus);
        BeginImpactCleanup();
    }

    private void BeginImpactCleanup()
    {
        owner?.UnregisterMeteor(this);
        owner = null;

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

        Destroy(gameObject, cleanupDelay);
    }
}
