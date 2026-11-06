using UnityEngine;

/// <summary>
/// 키 하나로 Resources 아래 스프라이트를 연다 — `bg_sprite`의 해석기.
///
/// 배경은 캐스팅하지 않은 캐릭터 슬롯이라 초상 DB(PortraitResolver)를 거치지 않는다.
/// 키는 뿌리 아래 경로 그대로다(확장자 없음). Resources.Load는 대소문자를 가리지 않고, 코어의
/// background-dimensions.json 색인도 그렇게 맞췄다. 같은 키로 종횡비를 찾으므로
/// 여기 규약을 바꾸면 익스포터(PresentationTuningExporter)와 함께 바꾼다.
///
/// 실패는 null을 돌려주고 경고를 남긴다 — 부르는 쪽(SetSpriteCommandCharR)이 이미지를 끈다.
/// 흰 사각형(sprite 없는 Image)이 조용히 남지 않게 하는 것은 그쪽 책임이다.
/// </summary>
public static class ResourcesSpriteResolver
{
    public const string BackgroundsRoot = "Backgrounds";

    public static Sprite Resolve(string root, string spriteKey)
    {
        if (string.IsNullOrEmpty(spriteKey))
        {
            Debug.LogWarning($"[ResourcesSpriteResolver] Sprite key is empty. root='{root}'.");
            return null;
        }

        string path = $"{root}/{spriteKey}";

        Sprite sprite = Resources.Load<Sprite>(path);

        if (sprite == null)
            Debug.LogWarning($"[ResourcesSpriteResolver] Failed to resolve sprite. path='{path}'.");

        return sprite;
    }
}
