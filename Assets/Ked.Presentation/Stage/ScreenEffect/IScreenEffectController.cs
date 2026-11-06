using UnityEngine;
using UnityEngine.UI;

public interface IScreenEffectController
{
    // 프리팹이 들고 온 원본 재질. 없으면 빌더가 Resources에서 읽어 Bind로 넘긴다.
    Material SourceMaterial { get; }

    void Bind(Image image, Material sourceMaterial);

    void KillTween(bool complete);
    void ClearImmediate();
}