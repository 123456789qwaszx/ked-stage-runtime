using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class ScreenEffectRig : MonoBehaviour
{
    private readonly ScreenEffectRigBuilder _builder = new();

    public ScreenEffectRigRefs Refs { get; private set; }

    public ScreenVignetteEffectController Vignette => Refs?.Vignette;
    public ScreenNoiseEffectController Noise => Refs?.Noise;
    public ScreenFlashEffectController Flash => Refs?.Flash;

    public void Initialize()
    {
        RectTransform root = transform as RectTransform;
        _builder.BindRefsFromRoot(root, out ScreenEffectRigRefs refs);
        Refs = refs;

        ResetToBaselineImmediate();
    }

    public void KillAllTweens(bool complete)
    {
        Vignette?.KillTween(complete);
        Noise?.KillTween(complete);
        Flash?.KillTween(complete);
    }

    public void ClearAllImmediate()
    {
        Vignette?.ClearImmediate();
        Noise?.ClearImmediate();
        Flash?.ClearImmediate();
    }

    public void ResetToBaselineImmediate()
    {
        ClearAllImmediate();
    }

    private void OnDestroy()
    {
        KillAllTweens(false);
    }
}