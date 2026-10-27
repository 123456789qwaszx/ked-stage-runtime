using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class SetBackgroundSpriteCommandSpecBgR : BackgroundRigCommandSpecBase
{
    [Header("Sprite")]
    public string spriteKey;

    [Header("Target")]
    public BackgroundRigTarget target = BackgroundRigTarget.BackgroundSprite_Image;

    [Header("Sizing Policy")]
    public CharRigImageSizingMode sizingMode = CharRigImageSizingMode.HeightFitPreserveAspect;

    public CharRigImageSizingPolicy.HorizontalAlign horizontalAlign =
        CharRigImageSizingPolicy.HorizontalAlign.Center;
}

public sealed class SetBackgroundSpriteCommandBgR : CommandBase
{
    private readonly SetBackgroundSpriteCommandSpecBgR _spec;

    private Image _image;
    private bool _resolveAttempted;

    private readonly PresentationStage _stage;

    public SetBackgroundSpriteCommandBgR(
        PresentationStage stage,
        SetBackgroundSpriteCommandSpecBgR spec)
    {
        _stage = stage;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply();
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply();
    }

    private void Apply()
    {
        Sprite sprite = BackgroundSpriteResolver.Resolve(_spec.spriteKey);

        _image.sprite = sprite;
        CharRigImageSizingPolicy.Apply(
            _image,
            sprite,
            _spec.sizingMode,
            _spec.horizontalAlign);
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;
        
        _stage.backgroundRigs.TryGetRig(_spec.rigKey, out BackgroundRigRefs rig);
        _image = rig.GetRect(_spec.target).GetComponent<Image>();
    }
}