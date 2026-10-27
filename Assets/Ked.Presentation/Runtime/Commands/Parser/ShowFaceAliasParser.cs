public static class ShowFaceAliasParser
{
    public static string Parse(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return "2";

        string s = token.Trim().ToLowerInvariant();

        if (s.StartsWith("emotion"))
            return s.Substring("emotion".Length);

        if (s.StartsWith("emo"))
            return s.Substring("emo".Length);

        if (s.StartsWith("face"))
            return s.Substring("face".Length);

        if (s.StartsWith("e"))
            return s.Substring(1);

        return token.Trim();
    }
}