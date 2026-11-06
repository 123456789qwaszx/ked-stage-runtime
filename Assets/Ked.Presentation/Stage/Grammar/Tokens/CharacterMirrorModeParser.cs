public static class CharacterMirrorModeParser
{
    public static CharacterMirrorMode Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return CharacterMirrorMode.Toggle;

        string s = raw.Trim().ToLowerInvariant();

        switch (s)
        {
            case "left":
            case "l":
            case "mirror":
            case "mirrored":
            case "true":
            case "1":
                return CharacterMirrorMode.Left;

            case "right":
            case "r":
            case "normal":
            case "default":
            case "unmirror":
            case "unmirrored":
            case "false":
            case "0":
                return CharacterMirrorMode.Right;

            case "toggle":
            case "t":
            case "flip":
            case "switch":
                return CharacterMirrorMode.Toggle;

            default:
                return CharacterMirrorMode.Toggle;
        }
    }
}