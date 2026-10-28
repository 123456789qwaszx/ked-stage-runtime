using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 리그의 Image 하나에 Resources 스프라이트를 키로 넣는다 — 배경(`bg_sprite`)이 쓰는 길이다.
///
/// 초상(SetPortraitSpriteCommandCharR)과 다른 점은 출처 하나다: 배경은 캐스팅하지 않은 슬롯이라
/// 초상 DB가 아니라 `Resources/{resourcesRoot}/{spriteKey}`를 연다. 사이징은 같다(HeightFitPreserveAspect) —
/// 코어 StageReducer.Background가 background-dimensions.json의 종횡비로 같은 폭을 접는다.
/// </summary>
[Serializable]
public sealed class SetSpriteCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortraitSprite_Image;

    [Header("Sprite (Resources)")]
    [Tooltip("Resources 아래 뿌리 폴더. 배경은 'Backgrounds'.")]
    public string resourcesRoot = ResourcesSpriteResolver.BackgroundsRoot;

    [Tooltip("뿌리 아래 에셋 경로(확장자 없이). 정규화하지 않는다 — Resources 경로 그대로.")]
    public string spriteKey;

    [Header("Sizing Policy (Recommended)")]
    public CharRigImageSizingMode sizingMode = CharRigImageSizingMode.HeightFitPreserveAspect;

    [Tooltip("HeightFit일 때 가로 정렬")]
    public CharRigImageSizingPolicy.HorizontalAlign horizontalAlign =
        CharRigImageSizingPolicy.HorizontalAlign.Center;

    [Tooltip("꺼 두면 이 Image가 클릭을 받지 않는다. 배경은 화면 전체를 덮으므로 끈다 — 무대 아래 UI가 클릭을 잃지 않게.")]
    public bool raycastTarget = false;
}

public sealed class SetSpriteCommandCharR : CommandBase
{
    private readonly SetSpriteCommandSpecCharR _spec;

    private Image _image;
    private bool _resolveAttempted;

    private readonly PresentationStage _stage;

    public SetSpriteCommandCharR(PresentationStage stage, SetSpriteCommandSpecCharR spec)
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

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rig = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(_stage, _spec.slotKey);
        _image = rig.GetImage(_spec.target);
    }

    private void Apply()
    {
        Sprite sprite = ResourcesSpriteResolver.Resolve(_spec.resourcesRoot, _spec.spriteKey);

        // 못 찾으면 이미지를 끈다 — sprite 없는 Image는 흰 사각형을 그린다. 조용히 남기지 않는다.
        // (해석기가 이미 경고를 남겼다.) sizeDelta는 건드리지 않는다 — 코어도 그 축을 Unhandled로 두고 값을 유지한다.
        _image.sprite = sprite;
        _image.enabled = sprite != null;
        _image.raycastTarget = _spec.raycastTarget;

        if (sprite == null)
            return;

        CharRigImageSizingPolicy.Apply(_image, sprite, _spec.sizingMode, _spec.horizontalAlign);
    }
}
