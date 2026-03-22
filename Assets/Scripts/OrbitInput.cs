public static class OrbitInput
{
    private static bool leftPressed;
    private static bool rightPressed;

    public static float Horizontal
    {
        get
        {
            float value = 0f;
            if (leftPressed)
            {
                value -= 1f;
            }

            if (rightPressed)
            {
                value += 1f;
            }

            return value;
        }
    }

    public static void SetLeftPressed(bool isPressed)
    {
        leftPressed = isPressed;
    }

    public static void SetRightPressed(bool isPressed)
    {
        rightPressed = isPressed;
    }

    public static void Reset()
    {
        leftPressed = false;
        rightPressed = false;
    }
}
