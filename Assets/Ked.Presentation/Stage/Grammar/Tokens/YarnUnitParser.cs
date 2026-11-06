using Ked.Presentation.Core;
using UnityEngine;

public static class YarnUnitParser
{
    private const float ReferenceStageWidth = UnitToken.DefaultReferenceStageWidth;

    // "3u" / "3" -> 픽셀. 음수는 0으로 클램프.
    public static float Parse(string token, float fallbackUnits = 1)
    {
        // 빈 토큰은 인자를 안 준 것. 기본값 사용.
        if (string.IsNullOrWhiteSpace(token))
            return FallbackPixels(fallbackUnits);

        if (UnitToken.TryParsePixels(token, ReferenceStageWidth, out float pixels))
            return pixels;

        Debug.LogWarning(
            $"[YarnUnitParser] Invalid unit token '{token}'. " +
            $"Fallback to {fallbackUnits:0.###}u.");

        return FallbackPixels(fallbackUnits);
    }

    private static float FallbackPixels(float units)
        => UnitToken.UnitsToPixels(units, ReferenceStageWidth);
}