public static class CharacterVisualFocusPresetKeyParser
{
    public const string DefaultPresetKey = "focus";
    public const string ClearPresetKey = "clear";

    public static string Parse(string raw)
    {
        string key = (raw ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(key))
            return DefaultPresetKey;

        key = key.ToLowerInvariant();
        key = key.Replace(' ', '_');
        key = key.Replace('-', '_');

        switch (key)
        {
            case "default":
                return DefaultPresetKey;

            case "none":
            case "off":
            case "reset":
                return ClearPresetKey;

            case "de_focus":
                return "defocus";

            case "rim":
            case "outer":
            case "outerrim":
                return "outer_rim";

            case "inner":
            case "innerrim":
                return "inner_rim";

            case "sil":
            case "black":
            case "shadow":
                return "silhouette";

            default:
                return key;
        }
    }
}