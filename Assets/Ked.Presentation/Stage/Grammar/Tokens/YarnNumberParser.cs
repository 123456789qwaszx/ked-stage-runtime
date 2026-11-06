using Ked.Presentation.Core;

public static class YarnNumberParser
{
    public static bool TryParseFloat(string value, out float result)
        => NumberToken.TryParseFloat(value, out result);
}