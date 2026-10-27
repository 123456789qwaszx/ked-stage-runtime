using System;

public static class CharacterDepthPresetParser
{
    public static bool TryParseDepthLevel(string raw, out float level)
    {
        if (!YarnNumberParser.TryParseFloat(raw, out level))
            return false;

        return !float.IsNaN(level) && !float.IsInfinity(level);
    }
    
    public static bool TryParse(string raw, out CharacterDepthKey preset)
    {
        preset = CharacterDepthKey.Mid;
        
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        
        string s = raw.Trim().ToLowerInvariant();

        switch (s)
        {
            case "none":
            case "n":
                preset = CharacterDepthKey.None;
                return true;

            case "far":
            case "f":
                preset = CharacterDepthKey.Far;
                return true;
            
            case "back":
            case "b":
                preset = CharacterDepthKey.Back;
                return true;

            case "mid":
            case "middle":
            case "normal":
            case "default":
            case "m":
                preset = CharacterDepthKey.Mid;
                return true;

            case "front":
            case "fore":
            case "foreground":
                preset = CharacterDepthKey.Front;
                return true;
            
            case "close":
            case "near":
            case "c":
                preset = CharacterDepthKey.Close;
                return true;


            case "exp1":
            case "experimental1":
                preset = CharacterDepthKey.Exp1;
                return true;

            case "exp2":
            case "experimental2":
                preset = CharacterDepthKey.Exp2;
                return true;
        }

        return Enum.TryParse(raw.Trim(), true, out preset);
    }
}