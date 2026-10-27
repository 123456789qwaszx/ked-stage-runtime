// 초상 조회 키의 정규화 — 코어 PortraitKeyNormalizer가 같은 규칙을 들고 있다.
// 두 곳이 갈라지면 폴드가 재생과 다른 스프라이트를 고르므로 반드시 함께 바꾼다.
public static class PresentationKeyNormalizer
{
    public static string NormalizeCharacterKey(string key)
    {
        return (key ?? "").Trim().ToLowerInvariant();
    }

    // 변형 키는 문자열 전체가 키다. 초상 에셋이
    // <뿌리>/<캐릭터>/<변형>/<표정>.png 폴더 규약을 쓰면서 변형은 폴더 이름 그 자체가 됐다.
    // 종전에는 파일 이름에 캐릭터가 눌어붙어 'yoonsaea_b'로 들어왔기에 마지막 글자만 봤는데,
    // 그 규칙은 'school'과 'casual'을 조용히 같은 키('l')로 뭉개므로 폐기했다.
    public static string NormalizeVariantKey(string key)
    {
        return (key ?? "").Trim().ToLowerInvariant();
    }
}
