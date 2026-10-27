using System;
using UnityEngine;

public enum ScreenFocusPoint
{
    Center,

    TopLeft,
    Top,
    TopRight,

    Left,
    Right,

    BottomLeft,
    Bottom,
    BottomRight,

    ThirdsUpperLeft,
    ThirdsUpperRight,
    ThirdsLowerLeft,
    ThirdsLowerRight
}

public static class ScreenFocusPointResolver
{
    private const float OuterXRatio = 0.24f;
    private const float OuterYRatio = 0.16f;

    private const float InnerXRatio = 0.14f;
    private const float InnerYRatio = 0.09f;

    public static Vector2 Resolve(RectTransform frameRoot, ScreenFocusPoint point)
    {
        if (frameRoot == null)
            return Vector2.zero;

        Rect rect = frameRoot.rect;
        float w = rect.width;
        float h = rect.height;

        float outerX = w * OuterXRatio;
        float outerY = h * OuterYRatio;

        float innerX = w * InnerXRatio;
        float innerY = h * InnerYRatio;

        switch (point)
        {
            case ScreenFocusPoint.TopLeft:
                return new Vector2(-outerX, outerY);

            case ScreenFocusPoint.Top:
                return new Vector2(0f, outerY);

            case ScreenFocusPoint.TopRight:
                return new Vector2(outerX, outerY);

            case ScreenFocusPoint.Left:
                return new Vector2(-outerX, 0f);

            case ScreenFocusPoint.Right:
                return new Vector2(outerX, 0f);

            case ScreenFocusPoint.BottomLeft:
                return new Vector2(-outerX, -outerY);

            case ScreenFocusPoint.Bottom:
                return new Vector2(0f, -outerY);

            case ScreenFocusPoint.BottomRight:
                return new Vector2(outerX, -outerY);

            case ScreenFocusPoint.ThirdsUpperLeft:
                return new Vector2(-innerX, innerY);

            case ScreenFocusPoint.ThirdsUpperRight:
                return new Vector2(innerX, innerY);

            case ScreenFocusPoint.ThirdsLowerLeft:
                return new Vector2(-innerX, -innerY);

            case ScreenFocusPoint.ThirdsLowerRight:
                return new Vector2(innerX, -innerY);

            default:
                return Vector2.zero;
        }
    }
}
public static class ScreenFocusPointParser
{
    public static bool TryParse(string raw, out ScreenFocusPoint point)
    {
        point = ScreenFocusPoint.Center;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string s = Normalize(raw);

        switch (s)
        {
            case "center":
            case "c":
            case "middle":
            case "mid":
            case "b2":
            case "22":
            case "5":
                point = ScreenFocusPoint.Center;
                return true;

            case "top_left":
            case "topleft":
            case "tl":
            case "upper_left":
            case "ul":
            case "a1":
            case "11":
            case "1":
                point = ScreenFocusPoint.TopLeft;
                return true;

            case "top":
            case "t":
            case "upper":
            case "up":
            case "a2":
            case "12":
            case "2":
                point = ScreenFocusPoint.Top;
                return true;

            case "top_right":
            case "topright":
            case "tr":
            case "upper_right":
            case "ur":
            case "a3":
            case "13":
            case "3":
                point = ScreenFocusPoint.TopRight;
                return true;

            case "left":
            case "l":
            case "b1":
            case "21":
            case "4":
                point = ScreenFocusPoint.Left;
                return true;

            case "right":
            case "r":
            case "b3":
            case "23":
            case "6":
                point = ScreenFocusPoint.Right;
                return true;

            case "bottom_left":
            case "bottomleft":
            case "bl":
            case "lower_left":
            case "ll":
            case "c1":
            case "31":
            case "7":
                point = ScreenFocusPoint.BottomLeft;
                return true;

            case "bottom":
            case "b":
            case "lower":
            case "down":
            case "c2":
            case "32":
            case "8":
                point = ScreenFocusPoint.Bottom;
                return true;

            case "bottom_right":
            case "bottomright":
            case "br":
            case "lower_right":
            case "lr":
            case "c3":
            case "33":
            case "9":
                point = ScreenFocusPoint.BottomRight;
                return true;

            case "thirds_upper_left":
            case "third_upper_left":
            case "rule_upper_left":
            case "rule_ul":
            case "third_ul":
            case "thirds_ul":
            case "inner_ul":
            case "inner_a1":
            case "ab1":
                point = ScreenFocusPoint.ThirdsUpperLeft;
                return true;

            case "thirds_upper_right":
            case "third_upper_right":
            case "rule_upper_right":
            case "rule_ur":
            case "third_ur":
            case "thirds_ur":
            case "inner_ur":
            case "inner_a3":
            case "ab2":
                point = ScreenFocusPoint.ThirdsUpperRight;
                return true;

            case "thirds_lower_left":
            case "third_lower_left":
            case "rule_lower_left":
            case "rule_ll":
            case "third_ll":
            case "thirds_ll":
            case "inner_ll":
            case "inner_c1":
            case "bc1":
                point = ScreenFocusPoint.ThirdsLowerLeft;
                return true;

            case "thirds_lower_right":
            case "third_lower_right":
            case "rule_lower_right":
            case "rule_lr":
            case "third_lr":
            case "thirds_lr":
            case "inner_lr":
            case "inner_c3":
            case "bc2":
                point = ScreenFocusPoint.ThirdsLowerRight;
                return true;
        }

        return Enum.TryParse(raw.Trim(), true, out point);
    }

    private static string Normalize(string raw)
    {
        string s = raw.Trim().ToLowerInvariant();

        s = s.Replace("-", "_");
        s = s.Replace(".", "_");
        s = s.Replace(" ", "_");

        return s;
    }
}