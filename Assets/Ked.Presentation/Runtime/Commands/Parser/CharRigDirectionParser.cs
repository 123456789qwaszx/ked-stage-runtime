public static class CharRigDirectionParser
{
    public static CharRigDirection ParseSlideDirection(
        string direction)
    {
        switch (direction?.Trim().ToLowerInvariant())
        {
            case "left":
            case "l":
                return CharRigDirection.Left;

            case "right":
            case "r":
                return CharRigDirection.Right;

            case "up":
            case "u":
            case "top":
            case "t":
                return CharRigDirection.Up;

            case "down":
            case "d":
            case "bottom":
            case "b":
                return CharRigDirection.Down;

            default:
                return CharRigDirection.Left;
        }
    }
}