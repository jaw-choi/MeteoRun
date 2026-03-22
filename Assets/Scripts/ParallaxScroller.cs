using UnityEngine;

public class ParallaxScroller : MonoBehaviour
{
    [SerializeField] private Vector2 moveDirection = Vector2.down;
    [SerializeField, Min(0f)] private float speed = 0.4f;
    [SerializeField] private float wrapAtY = -8f;
    [SerializeField] private float resetToY = 8f;

    private void Update()
    {
        if (moveDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.position += (Vector3)(moveDirection.normalized * speed * Time.deltaTime);

        Vector3 currentPosition = transform.position;
        if (moveDirection.y < 0f && currentPosition.y < wrapAtY)
        {
            transform.position = new Vector3(currentPosition.x, resetToY, currentPosition.z);
        }
        else if (moveDirection.y > 0f && currentPosition.y > resetToY)
        {
            transform.position = new Vector3(currentPosition.x, wrapAtY, currentPosition.z);
        }
    }
}
