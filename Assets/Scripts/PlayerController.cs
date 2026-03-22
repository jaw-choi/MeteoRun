using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Orbit")]
    [SerializeField] private Transform planetCenter;
    [SerializeField, Min(0.1f)] private float planetRadius = 2.25f;
    [SerializeField, Min(1f)] private float angularSpeedDegrees = 180f;
    [SerializeField] private float startAngleDegrees = 90f;

    private Collider2D cachedCollider;
    private Rigidbody2D cachedRigidbody;
    private float currentAngleDegrees;

    private void Awake()
    {
        cachedCollider = GetComponent<Collider2D>();
        cachedRigidbody = GetComponent<Rigidbody2D>();

        if (cachedCollider != null)
        {
            cachedCollider.isTrigger = true;
        }

        if (cachedRigidbody != null)
        {
            cachedRigidbody.bodyType = RigidbodyType2D.Kinematic;
            cachedRigidbody.gravityScale = 0f;
        }

        if (planetCenter == null)
        {
            GameObject planet = GameObject.Find("Planet");
            if (planet != null)
            {
                planetCenter = planet.transform;
            }
        }

        currentAngleDegrees = startAngleDegrees;
        UpdateOrbitPosition();
    }

    private void Update()
    {
        if (planetCenter == null)
        {
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            return;
        }

        float inputDirection = -Mathf.Clamp(ReadKeyboardInput() + ReadPointerInput(), -1f, 1f);
        currentAngleDegrees += inputDirection * angularSpeedDegrees * Time.deltaTime;
        UpdateOrbitPosition();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        if (other.GetComponent<Meteor>() != null || other.GetComponentInParent<Meteor>() != null)
        {
            GameManager.Instance?.HandlePlayerHit();
        }
    }

    private float ReadKeyboardInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return 0f;
        }

        float input = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            input += 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            input -= 1f;
        }

        return Mathf.Clamp(input, -1f, 1f);
    }

    private float ReadPointerInput()
    {
        bool leftPressed = false;
        bool rightPressed = false;
        float halfWidth = Screen.width * 0.5f;

        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            foreach (TouchControl touch in touchscreen.touches)
            {
                if (!touch.press.isPressed)
                {
                    continue;
                }

                if (touch.position.ReadValue().x < halfWidth)
                {
                    leftPressed = true;
                }
                else
                {
                    rightPressed = true;
                }
            }
        }

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed)
        {
            if (mouse.position.ReadValue().x < halfWidth)
            {
                leftPressed = true;
            }
            else
            {
                rightPressed = true;
            }
        }

        if (leftPressed == rightPressed)
        {
            return 0f;
        }

        return leftPressed ? -1f : 1f;
    }

    private void UpdateOrbitPosition()
    {
        float radians = currentAngleDegrees * Mathf.Deg2Rad;
        Vector2 orbitOffset = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * planetRadius;
        Vector3 worldPosition = planetCenter.position + (Vector3)orbitOffset;

        transform.position = worldPosition;

        Vector2 outward = orbitOffset.normalized;
        if (outward.sqrMagnitude > 0.0001f)
        {
            transform.up = outward;
        }
    }
}
