using UnityEngine;

public static class ResourcesAudioClipResolver
{
    public static bool TryResolve(string clipKey, out AudioClip clip)
    {
        clip = null;

        if (string.IsNullOrWhiteSpace(clipKey))
            return false;

        clip = Resources.Load<AudioClip>(clipKey);
        return clip != null;
    }
}