using UnityEngine;

public static class BackgroundSpriteResolver
{
    private const string ResourcesRoot = "Backgrounds";

    public static Sprite Resolve(string spriteKey)
    {
        string path = $"{ResourcesRoot}/{spriteKey}";

        Sprite sprite = Resources.Load<Sprite>(path);

        if (sprite == null)
            Debug.LogWarning($"Failed to resolve background sprite. path='{path}'.");

        return sprite;
    }
}