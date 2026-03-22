using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class PlayerOrbitalController : MonoBehaviour
{
    [Header("Orbit")]
    [SerializeField] private Transform planetCenter;
    [SerializeField, Min(0.1f)] private float orbitRadius = 3.8f;
    [SerializeField, Min(1f)] private float orbitSpeedDegrees = 140f;
    [SerializeField, Min(0f)] private float surfaceOffset = 0.08f;
    [SerializeField] private bool autoInitializeFromCurrentPosition = true;
    [SerializeField] private float startAngleDegrees;

    [Header("Input System")]
    [SerializeField] private InputActionReference moveActionReference;

    [Header("Audio")]
    [SerializeField] private GameAudio audioManager;

    private float currentAngleDegrees;
    private InputAction fallbackMoveAction;
    private Collider2D cachedCollider;

    private void Awake()
    {
        cachedCollider = GetComponent<Collider2D>();
        ResolvePlanetCenter();

        if (planetCenter != null)
        {
            Vector2 offset = (Vector2)transform.position - (Vector2)planetCenter.position;
            if (offset.sqrMagnitude > 0.0001f)
            {
                currentAngleDegrees = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            }
            else
            {
                currentAngleDegrees = startAngleDegrees;
            }

            if (autoInitializeFromCurrentPosition)
            {
                orbitRadius = Mathf.Max(offset.magnitude, CalculateSurfaceOrbitRadius());
            }
            else
            {
                orbitRadius = Mathf.Max(orbitRadius, CalculateSurfaceOrbitRadius());
            }
        }
        else
        {
            currentAngleDegrees = startAngleDegrees;
        }

        if (planetCenter != null)
        {
            UpdateOrbitTransform();
        }
    }

    private void OnEnable()
    {
        EnsureMoveAction();
        fallbackMoveAction?.Enable();
        moveActionReference?.action?.Enable();
    }

    private void OnDisable()
    {
        moveActionReference?.action?.Disable();
        fallbackMoveAction?.Disable();
    }

    private void OnDestroy()
    {
        fallbackMoveAction?.Dispose();
    }

    private void Update()
    {
        if (planetCenter == null || planetCenter == transform)
        {
            ResolvePlanetCenter();
        }

        if (planetCenter == null || planetCenter == transform)
        {
            return;
        }

        if (audioManager == null && GameManager.Instance != null)
        {
            audioManager = GameManager.Instance.AudioManager;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            audioManager?.TickMovement(false);
            return;
        }

        float horizontal = ReadHorizontalInput();
        currentAngleDegrees += horizontal * orbitSpeedDegrees * Time.deltaTime;
        UpdateOrbitTransform();
        audioManager?.TickMovement(Mathf.Abs(horizontal) > 0.01f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleMeteorCollision(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleMeteorCollision(collision.collider);
    }

    private float ReadHorizontalInput()
    {
        InputAction moveAction = GetMoveAction();
        float actionInput = 0f;

        if (moveAction != null)
        {
            actionInput = moveAction.ReadValue<Vector2>().x;
        }

        float mobile = OrbitInput.Horizontal;
        return Mathf.Clamp(actionInput + mobile, -1f, 1f);
    }

    private void UpdateOrbitTransform()
    {
        float radians = currentAngleDegrees * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * orbitRadius;
        Vector2 targetPosition = (Vector2)planetCenter.position + offset;

        transform.position = targetPosition;

        Vector2 outward = offset.normalized;
        if (outward.sqrMagnitude > 0.0001f)
        {
            transform.up = outward;
        }
    }

    private void HandleMeteorCollision(Collider2D collider2D)
    {
        if (collider2D == null)
        {
            return;
        }

        if (collider2D.GetComponent<Meteor>() == null && collider2D.GetComponentInParent<Meteor>() == null)
        {
            return;
        }

        GameManager.Instance?.OnPlayerHit();
    }

    private InputAction GetMoveAction()
    {
        EnsureMoveAction();
        return moveActionReference != null ? moveActionReference.action : fallbackMoveAction;
    }

    private void EnsureMoveAction()
    {
        if (moveActionReference != null && moveActionReference.action != null)
        {
            return;
        }

        if (fallbackMoveAction != null)
        {
            return;
        }

        fallbackMoveAction = new InputAction("OrbitMove", InputActionType.Value);
        fallbackMoveAction.AddCompositeBinding("2DVector")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        fallbackMoveAction.AddCompositeBinding("2DVector")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        fallbackMoveAction.AddBinding("<Gamepad>/leftStick");
        fallbackMoveAction.AddBinding("<Gamepad>/dpad");
    }

    private void ResolvePlanetCenter()
    {
        if (planetCenter != null && planetCenter != transform)
        {
            return;
        }

        PlanetSurface[] surfaces = FindObjectsOfType<PlanetSurface>();
        for (int i = 0; i < surfaces.Length; i++)
        {
            if (surfaces[i] != null && surfaces[i].transform != transform)
            {
                planetCenter = surfaces[i].transform;
                return;
            }
        }
    }

    private float CalculateSurfaceOrbitRadius()
    {
        if (planetCenter == null)
        {
            return orbitRadius;
        }

        float planetRadius = 0f;
        CircleCollider2D planetCircle = planetCenter.GetComponent<CircleCollider2D>();
        if (planetCircle != null)
        {
            float scale = Mathf.Max(Mathf.Abs(planetCenter.lossyScale.x), Mathf.Abs(planetCenter.lossyScale.y));
            planetRadius = planetCircle.radius * scale;
        }

        float playerRadius = 0f;
        CircleCollider2D playerCircle = cachedCollider as CircleCollider2D;
        if (playerCircle != null)
        {
            float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            playerRadius = playerCircle.radius * scale;
        }
        else if (cachedCollider != null)
        {
            playerRadius = Mathf.Max(cachedCollider.bounds.extents.x, cachedCollider.bounds.extents.y);
        }

        float calculatedRadius = planetRadius + playerRadius + surfaceOffset;
        return Mathf.Max(calculatedRadius, 0.1f);
    }
}
