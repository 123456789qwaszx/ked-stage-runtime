using UnityEngine;

public static class PresentationCommandKeyParser
{
    public static bool TryParseDepthLayerKey(string key, out PresentationDepthLayerKey layer)
    {
        string normalized = Normalize(key);

        switch (normalized)
        {
            case "far":
            case "f":
                layer = PresentationDepthLayerKey.Far;
                return true;

            case "back":
            case "b":
                layer = PresentationDepthLayerKey.Back;
                return true;

            case "mid":
            case "middle":
            case "m":
                layer = PresentationDepthLayerKey.Mid;
                return true;

            case "front":
            case "fr":
                layer = PresentationDepthLayerKey.Front;
                return true;

            case "close":
            case "c":
                layer = PresentationDepthLayerKey.Close;
                return true;
        }

        if (System.Enum.TryParse(key, true, out layer))
            return true;

        Debug.LogWarning($"[PresentationCommandKeyParser] Unknown depth layer key: {key}");
        layer = default;
        return false;
    }

    public static string Normalize(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        return key
            .Trim()
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .ToLowerInvariant();
    }
}