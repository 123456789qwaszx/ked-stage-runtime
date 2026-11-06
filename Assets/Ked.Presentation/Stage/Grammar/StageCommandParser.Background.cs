using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

/// <summary>
/// bg_* — 배경은 **캐스팅하지 않은 캐릭터 슬롯**이다.
///
/// 배경 전용 리그는 없고 어휘만 있다. 여기서 `bg_*`를 캐릭터 Spec으로 펼친다 —
/// 대본·문서·외부 도구는 그대로이고, "이건 배경"이라는 저작 의도가 대본에 남는다.
/// 코어 `StageReducer.Background.cs`가 같은 대응으로 접는다. 표적 노드를 바꾸면 거기도 함께 바꾼다.
///
///   bg_spawn [stage] [layer] →  SetupCharRig(stage00 · far 생략값, asBackground) + SetSprite(키)
///                            (캐릭터 slot처럼 숨은 채 선다 — bg_fade_in으로 들인다. 배역 있는 슬롯을 덮으면 배역을 푼다)
///   bg_sprite             →  SetSprite(키), HeightFitPreserveAspect, 레이캐스트 끔
///   bg_fade_in / out      →  FadeIn/Out  → CharacterPortraitSprite_Root
///   bg_move               →  MoveBy      → CharSlot_Track            (move_by와 같은 축)
///   bg_scale              →  ScaleTo     → CharacterPortrait_ActingScale (가운데 피벗 — CharSlot_Scale은 발 피벗이다)
///
/// bg_size · bg_place · bg_slot00/01/02는 두지 않는다 — 크기는 bg_scale, 무대 · 층은 bg_spawn의 인자다.
/// </summary>
public sealed partial class StageCommandParser
{
    private const float BackgroundDefaultDurationSeconds = 0.4f;

    private void EnqueueSpawnBackgroundSpec(string rigKey, string spriteKey, string stageKey, string layerKey)
        => EnqueueSpawnBackgroundAtDepthSpec(
            rigKey, spriteKey, StageKeyParser.ParseStage(stageKey), StageKeyParser.ParseLayerDepth(layerKey));

    private void EnqueueSpawnBackgroundAtDepthSpec(
        string rigKey,
        string spriteKey,
        string stage,
        float layerDepth)
    {
        Emit(new SetupCharRigCommandSpec
        {
            roleKey = rigKey,
            rigPrefab = _charRigPrefab,

            stage = stage,
            layerDepth = layerDepth,

            asBackground = true
        });

        // 숨은 채 선다(스프라이트 루트 α 0) — 캐릭터 slot과 같다. 대본은 bg_fade_in으로 들인다.
        // (옛 배경 리그는 세우면 바로 보여서, 대본마다 뒤에 쓴 bg_fade_in 24fr이 한 번도 보이지 않았다.)
        EnqueueSetBackgroundSpriteSpec(rigKey, spriteKey);
    }

    private void EnqueueSetBackgroundSpriteSpec(string rigKey, string spriteKey)
        => Emit(new SetSpriteCommandSpecCharR
        {
            roleKey = rigKey,
            node = CharacterRigNode.CharacterPortraitSprite_Image,
            resourcesRoot = ResourcesSpriteResolver.BackgroundsRoot,
            spriteKey = spriteKey,
            sizingMode = CharRigImageSizingMode.HeightFitPreserveAspect,
            horizontalAlign = CharRigImageSizingPolicy.HorizontalAlign.Center,
            raycastTarget = false
        });

    private void EnqueueFadeInBackgroundDslSpec(string rigKey, string durationToken)
        => Emit(new FadeCommandSpecCharR
        {
            roleKey = rigKey,
            node = CharacterRigNode.CharacterPortraitSprite_Root,
            targetAlpha = NodeTargets.FadeInAlpha,
            duration = YarnDurationParser.Parse(durationToken, BackgroundDefaultDurationSeconds)
        });

    private void EnqueueFadeOutBackgroundDslSpec(string rigKey, string durationToken)
        => Emit(new FadeCommandSpecCharR
        {
            roleKey = rigKey,
            node = CharacterRigNode.CharacterPortraitSprite_Root,
            targetAlpha = NodeTargets.FadeOutAlpha,
            duration = YarnDurationParser.Parse(durationToken, BackgroundDefaultDurationSeconds)
        });

    private void EnqueueMoveBackgroundDslSpec(string rigKey, string xToken, string yToken, string durationToken)
        => Emit(new MoveByCommandSpecCharR
        {
            roleKey = rigKey,
            node = CharacterRigNode.CharSlot_Track,
            useAbsolutePosition = false,
            delta = new Vector2(ParseSignedUnit(xToken), ParseSignedUnit(yToken)),
            duration = YarnDurationParser.Parse(durationToken, BackgroundDefaultDurationSeconds),
            ease = Ease.OutCubic
        });

    private void EnqueueScaleBackgroundDslSpec(string rigKey, float scale, string durationToken)
        => Emit(new ScaleToCommandSpecCharR
        {
            roleKey = rigKey,
            node = CharacterRigNode.CharacterPortrait_ActingScale,
            toScale = new Vector2(scale, scale),
            duration = YarnDurationParser.Parse(durationToken, BackgroundDefaultDurationSeconds)
        });
}
