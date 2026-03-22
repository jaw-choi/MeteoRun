using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Meteor : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float fallSpeed = 5f;
    [SerializeField] private float spinSpeed = 180f;
    [SerializeField] private float despawnY = -7.5f;
    [SerializeField, Min(0)] private int avoidedBonusScore = 1;

    private bool resolved;

    private void Update()
    {
        if (resolved)
        {
            return;
        }

        float speedMultiplier = 1f;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            speedMultiplier = 0.5f;
        }

        transform.position += Vector3.down * fallSpeed * speedMultiplier * Time.deltaTime;
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

        if (transform.position.y < despawnY)
        {
            ResolveAsAvoided();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.collider);
    }

    private void HandleCollision(Collider2D other)
    {
        if (resolved || other == null)
        {
            return;
        }

        if (other.GetComponent<Meteor>() != null || other.GetComponentInParent<Meteor>() != null)
        {
            return;
        }

        if (other.GetComponent<PlayerOrbitalController>() != null || other.GetComponentInParent<PlayerOrbitalController>() != null)
        {
            ResolveAsPlayerHit();
            return;
        }

        if (other.GetComponent<PlanetSurface>() != null || other.GetComponentInParent<PlanetSurface>() != null)
        {
            ResolveAsAvoided();
        }
    }

    private void ResolveAsAvoided()
    {
        if (resolved)
        {
            return;
        }

        resolved = true;
        GameManager.Instance?.RegisterMeteorAvoided(avoidedBonusScore);
        Destroy(gameObject);
    }

    private void ResolveAsPlayerHit()
    {
        if (resolved)
        {
            return;
        }

        resolved = true;
        GameManager.Instance?.OnPlayerHit();
        Destroy(gameObject);
    }
}
