using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class SetPortraitSpriteCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Portrait Identity")]
    public PortraitIdentity portrait;
    
    [Header("Node")] 
    public CharacterRigNode node = CharacterRigNode.CharacterPortraitSprite_Image;
    
    [Header("Sizing Policy")] 
    public CharRigImageSizingMode sizingMode = CharRigImageSizingMode.HeightFitPreserveAspect;
    
    public CharRigImageSizingPolicy.HorizontalAlign horizontalAlign = CharRigImageSizingPolicy.HorizontalAlign.Center;
}

public sealed class SetPortraitSpriteCommandCharR : CommandBase
{
    private readonly SetPortraitSpriteCommandSpecCharR _spec;
    private readonly PortraitResolver _resolver;

    private CharacterRigRefs _rig;
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
            ResolveRefs();

        Apply();
        yield break;
    }

    protected override void OnSkip()
    {
        if (!_resolveAttempted)
            ResolveRefs();

        Apply();
    }

    private void Apply()
    {
        Sprite sprite = _resolver.Resolve(_stage, _spec.roleKey, _spec.portrait, nameof(SetPortraitSpriteCommandCharR));

        // 밑장에 쓰기 전에 진행 중인 face_swap을 완주시킨다 — 나중 쓰기가 이긴다.
        if (_spec.node == CharacterRigNode.CharacterPortraitSprite_Image)
            SetEmotionPortraitWipeCommandCharR.CompleteInFlight(_rig);

        _image.sprite = sprite;
        CharRigImageSizingPolicy.Apply(_image, sprite, _spec.sizingMode, _spec.horizontalAlign);
    }
    
    private void ResolveRefs()
    {
        _resolveAttempted = true;

        _rig = CharacterRigResolver.ResolveRig(_stage, _spec.roleKey);
        _image = _rig.GetImage(_spec.node);
    }
}