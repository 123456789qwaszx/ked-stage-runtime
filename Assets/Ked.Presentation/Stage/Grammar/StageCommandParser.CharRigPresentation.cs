using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

public sealed partial class StageCommandParser
{
    private void EnqueueFadeInSpec(string roleKey, string durationToken)
        => Emit(new FadeCommandSpecCharR
        {
            node = CharacterRigNode.CharacterPortraitSprite_Root,
            targetAlpha = NodeTargets.FadeInAlpha,
            roleKey = roleKey,
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueFadeOutSpec(string roleKey, string durationToken)
        => Emit(new FadeCommandSpecCharR
        {
            node = CharacterRigNode.CharacterPortraitSprite_Root,
            targetAlpha = NodeTargets.FadeOutAlpha,
            roleKey = roleKey,
            duration = YarnDurationParser.Parse(durationToken)
        });

    // fade(slot, alpha, dur) — 반투명까지의 페이드. fade_in · fade_out은 이것의 두 끝(1 · 0)이다.
    // [0,1] 밖이면 소리 내고 잘라 낸다 — 자르는 규칙은 코어 NodeTargets.Fade 한 곳이라 폴드와 같은 값이다.
    private void EnqueueFadeToSpec(string roleKey, float alpha, string durationToken)
    {
        if (alpha < 0f || alpha > 1f)
            Debug.LogWarning($"[StageCommandParser] <<fade>> alpha {alpha}는 [0,1] 밖이다. 잘라 낸다.");

        Emit(new FadeCommandSpecCharR
        {
            node = CharacterRigNode.CharacterPortraitSprite_Root,
            targetAlpha = NodeTargets.FadeAlpha(alpha),
            roleKey = roleKey,
            duration = YarnDurationParser.Parse(durationToken)
        });
    }

    // slide_in · slide_out(slot, dir, dist, dur, ease, punch) — 커맨드는 하나(SlideCommandCharR), 종류만 다르다.
    // in/out은 방향이 아니라 "지금 위치가 어느 끝이냐"다. ease · punch의 생략값은 종류별 코어 값(SlideMotion).
    private void EnqueueSlideSpec(
        SlideMotion.Kind kind, string roleKey, string direction, string distanceToken, string durationToken, string easeToken, string punchToken)
        => Emit(new SlideCommandSpecCharR
        {
            roleKey = roleKey,
            kind = kind,
            direction = CharRigDirectionParser.ParseSlideDirection(direction),
            distance = YarnUnitParser.Parse(distanceToken),
            duration = YarnDurationParser.Parse(durationToken),
            ease = YarnEaseParser.Parse(easeToken, SlideMotion.EaseOf(kind).ToDOTween()),
            punch = ParsePunch(kind, punchToken)
        });

    // 튐의 양 — 거리와 같은 단위 토큰("0.6u" · "0.6")을 받는다. 못 읽으면 소리 내고 종류별 기본값.
    private static float ParsePunch(SlideMotion.Kind kind, string token)
    {
        float fallback = SlideMotion.PunchPixels(kind);

        if (string.IsNullOrWhiteSpace(token))
            return fallback;

        if (UnitToken.TryParsePixels(token, UnitToken.DefaultReferenceStageWidth, out float pixels))
            return pixels;

        Debug.LogWarning($"[StageCommandParser] slide punch '{token}'를 읽지 못했다. 기본값 {fallback}px로 간다.");

        return fallback;
    }

    private void EnqueueMoveByUnitCharSpec(string roleKey, string xToken, string yToken, string durationToken)
        => Emit(new MoveByCommandSpecCharR
        {
            roleKey = roleKey,
            node = CharacterRigNode.CharacterPortrait_Track,
            delta = new Vector2(YarnUnitParser.Parse(xToken), YarnUnitParser.Parse(yToken)),
            duration = YarnDurationParser.Parse(durationToken),
        });

    // 표정 교체. 밑장을 즉시 갈아끼우는 face와 달리 위 겹을 페이드로 덮었다 걷는다 —
    // 정지 프레임의 결과는 face와 같으므로 코어는 둘을 한 자리에서 접는다.
    private void EnqueueSetEmotionPortraitWipeSpec(string roleKey, string emotion, string durationToken)
        => Emit(new SetEmotionPortraitWipeCommandSpecCharR
        {
            roleKey = roleKey,
            portrait = new PortraitIdentity { emotion = emotion },
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueScaleSpec(string roleKey, float xyValue, string durationToken, string easeToken)
    {
        Ease ease = YarnEaseParser.Parse(easeToken);

        Emit(new ScaleToCommandSpecCharR
        {
            roleKey = roleKey,
            node = CharacterRigNode.CharacterPortrait_ActingScale,
            duration = YarnDurationParser.Parse(durationToken),
            toScale = new Vector2(xyValue, xyValue),
            ease = ease
        });
    }
}