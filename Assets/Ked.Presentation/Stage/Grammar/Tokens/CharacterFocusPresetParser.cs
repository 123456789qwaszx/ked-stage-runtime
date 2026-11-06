using System;

public static class CharacterFocusPresetParser
{
    public static CharacterFocusPreset Parse(
        string raw,
        CharacterFocusPreset fallback = CharacterFocusPreset.Face)
    {
        if (TryParse(raw, out CharacterFocusPreset result))
            return result;

        return fallback;
    }

    public static bool TryParse(string raw, out CharacterFocusPreset preset)
    {
        preset = CharacterFocusPreset.Face;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string s = Normalize(raw);

        switch (s)
        {
            case "p4":
            case "feet":
            case "foot":
            case "base":
            case "bottom":
            case "f":
            case "w1":
                preset = CharacterFocusPreset.Feet;
                return true;

            case "p3":
            case "body":
            case "torso":
            case "mid":
            case "middle":
            case "b":
            case "x1":
                preset = CharacterFocusPreset.Body;
                return true;

            case "p2":
            case "bust":
            case "chest":
            case "upper":
            case "u":
            case "y1":
                preset = CharacterFocusPreset.Bust;
                return true;

            case "p1":
            case "face":
            case "head":
            case "eye":
            case "eyes":
            case "h":
            case "z1":
                preset = CharacterFocusPreset.Face;
                return true;

            case "p5":
            case "hand_left":
            case "left_hand":
            case "lefthand":
            case "left":
            case "lh":
            case "v1":
                preset = CharacterFocusPreset.HandLeft;
                return true;

            case "p6":
            case "hand_right":
            case "right_hand":
            case "righthand":
            case "right":
            case "rh":
            case "v2":
                preset = CharacterFocusPreset.HandRight;
                return true;
        }

        return Enum.TryParse(raw.Trim(), true, out preset);
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