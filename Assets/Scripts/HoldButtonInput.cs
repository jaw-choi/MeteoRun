using UnityEngine;
using UnityEngine.EventSystems;

public class HoldButtonInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public enum Direction
    {
        Left,
        Right
    }

    [SerializeField] private Direction direction;

    public void OnPointerDown(PointerEventData eventData)
    {
        SetPressed(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetPressed(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetPressed(false);
    }

    private void OnDisable()
    {
        SetPressed(false);
    }

    private void SetPressed(bool pressed)
    {
        if (direction == Direction.Left)
        {
            OrbitInput.SetLeftPressed(pressed);
            return;
        }

        OrbitInput.SetRightPressed(pressed);
    }
}
