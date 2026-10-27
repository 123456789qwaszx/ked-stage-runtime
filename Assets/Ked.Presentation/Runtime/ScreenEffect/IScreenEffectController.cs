using UnityEngine;
using UnityEngine.UI;

public interface IScreenEffectController
{
    void Bind(Image image, Material sourceMaterial);

    void KillTween(bool complete);
    void ClearImmediate();
}