using Ked.Presentation.Core;
using UnityEngine;

public sealed partial class StageCommandParser
{
    private void EnqueueFadeInSpec(string roleKey, string durationToken)
        => Emit(new FadeInCommandSpecCharR
        {
            target = CharacterRigTarget.CharacterPortraitSprite_Root,
            slotKey = roleKey,
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueFadeOutSpec(string roleKey, string durationToken)
        => Emit(new FadeOutCommandSpecCharR
        {
            target = CharacterRigTarget.CharacterPortraitSprite_Root,
            slotKey = roleKey,
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueSlideInSpec(string roleKey, string direction, string distanceToken, string durationToken)
        => Emit(new SlideInCommandSpecCharR
        {
            slotKey = roleKey,
            direction = CharRigDirectionParser.ParseSlideDirection(direction),
            distance = YarnUnitParser.Parse(distanceToken),
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueSlideOutSpec(string roleKey, string direction, string distanceToken, string durationToken)
        => Emit(new SlideOutCommandSpecCharR
        {
            slotKey = roleKey,
            to = CharRigDirectionParser.ParseSlideDirection(direction),
            distance = YarnUnitParser.Parse(distanceToken),
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueMoveByUnitCharSpec(string roleKey, string xToken, string yToken, string durationToken)
        => Emit(new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_Track,
            delta = new Vector2(YarnUnitParser.Parse(xToken), YarnUnitParser.Parse(yToken)),
            duration = YarnDurationParser.Parse(durationToken),
        });

    // 표정 교체. 밑장을 즉시 갈아끼우는 face와 달리 위 겹을 페이드로 덮었다 걷는다 —
    // 정지 프레임의 결과는 face와 같으므로 코어는 둘을 한 자리에서 접는다.
    private void EnqueueSetEmotionPortraitWipeSpec(string targetKey, string emotion, string durationToken)
        => Emit(new SetEmotionPortraitWipeCommandSpecCharR
        {
            slotKey = targetKey,
            portrait = new PortraitIdentity { emotion = emotion },
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueScaleSpec(string roleKey, float xyValue, string durationToken, string easeToken)
    {
        EaseSelection ease = ResolveEase(easeToken);

        Emit(new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_ActingScale,
            duration = YarnDurationParser.Parse(durationToken),
            toScale = new Vector2(xyValue, xyValue),
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys
        });
    }
}