using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class SetPortraitSpriteCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Portrait Identity")]
    public PortraitIdentity portrait;
    
    [Header("Target")] 
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortraitSprite_Image;
    
    [Header("Sizing Policy")] 
    public CharRigImageSizingMode sizingMode = CharRigImageSizingMode.HeightFitPreserveAspect;
    
    public CharRigImageSizingPolicy.HorizontalAlign horizontalAlign = CharRigImageSizingPolicy.HorizontalAlign.Center;
}

public sealed class SetPortraitSpriteCommandCharR : CommandBase
{
    private readonly SetPortraitSpriteCommandSpecCharR _spec;
    private readonly PortraitResolver _resolver;

    private Image _image;
    private bool _resolveAttempted;

    private readonly PresentationStage _stage;

    public SetPortraitSpriteCommandCharR(PresentationStage stage, SetPortraitSpriteCommandSpecCharR spec, PortraitResolver resolver)
    {
        _stage = stage;
        _spec = spec;
        _resolver = resolver;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply(scope);
    }

    private void Apply(CommandRunScope scope)
    {
        Sprite sprite = _resolver.Resolve(_stage, _spec.slotKey, _spec.portrait, nameof(SetPortraitSpriteCommandCharR));
        _image.sprite = sprite;
        CharRigImageSizingPolicy.Apply(_image, sprite, _spec.sizingMode, _spec.horizontalAlign);
    }
    
    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(_stage, _spec.slotKey);
        _image = rigRefs.GetImage(_spec.target);
    }
}