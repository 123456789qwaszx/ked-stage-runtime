using Ked.Presentation.Core;
using UnityEngine;

public static class YarnDurationParser
{
    // "12fr" / "1.2s" / "0.4"
    public static float Parse(string token, float fallbackSeconds = 8f)
    {
        // 빈 토큰은 인자를 안 준 것. 기본값 사용.
        if (string.IsNullOrWhiteSpace(token))
            return Mathf.Max(0f, fallbackSeconds);

        if (DurationToken.TryParseSeconds(token, out float seconds))
            return seconds;

        Debug.LogWarning(
            $"[YarnDurationParser] Invalid duration token '{token}'. " +
            $"Expected format: 4fr, 12fr, 0.4s, or 0.4. " +
            $"Fallback to {fallbackSeconds:0.###} seconds.");

        return Mathf.Max(0f, fallbackSeconds);
    }

    // "12fr" / "12frame" / "12frames" / "12" -> 프레임 수.
    public static float ParseFrames(string token, float fallbackFrames = 8f)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Mathf.Max(0f, fallbackFrames);

        if (DurationToken.TryParseFrames(token, out float frames))
            return frames;

        Debug.LogWarning(
            $"[YarnDurationParser] Invalid frame token '{token}'. " +
            $"Expected format: 4fr, 12fr, 12frame, 12frames, or 12. " +
            $"Fallback to {fallbackFrames:0.###} frames.");

        return Mathf.Max(0f, fallbackFrames);
    }

    // 프레임 -> 초. fps 규약(24)
    public static float FramesToSeconds(float frames) => DurationToken.FramesToSeconds(frames);
}