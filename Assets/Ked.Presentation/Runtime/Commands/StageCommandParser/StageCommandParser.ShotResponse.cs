using Ked.Presentation.Core;
using UnityEngine;

public sealed partial class StageCommandParser
{
    // shot 계열도 place·size와 같은 모양이다: 마지막 위치 인자가 이징이고,
    // 미지정이면 스펙 기본값(OutCubic), "@이름"이면 curves.json의 커스텀 곡선.
    // 해석은 ResolveEase 한 자리를 함께 쓴다.

    private void EnqueueShotZoomFocusSpec(string roleKey, string focusName, string screenPointName, float zoom, string durationToken, string easeToken)
    {
        CharacterFocusPresetParser.TryParse(focusName, out CharacterFocusPreset focusPreset);

        if (!ScreenFocusPointParser.TryParse(screenPointName, out ScreenFocusPoint screenPoint))
            screenPoint = ScreenFocusPoint.Center;

        EaseSelection ease = ResolveEase(easeToken);

        var spec = new ShotZoomFocusCommandSpec
        {
            focusRoleKey = roleKey,
            focusPreset = focusPreset,
            screenPoint = screenPoint,
            zoom = zoom,
            duration = YarnDurationParser.Parse(durationToken),
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys,
        };

        Emit(spec);
    }

    private void EnqueueShotToSpec(float zoom, string xToken, string yToken, string durationToken, string easeToken)
    {
        EaseSelection ease = ResolveEase(easeToken);

        Emit(new ShotToCommandSpec
        {
            zoom = zoom,
            pan = new Vector2(ParseSignedUnit(xToken, 2.5f), ParseSignedUnit(yToken)),
            duration = YarnDurationParser.Parse(durationToken),
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys,
        });
    }

    private void EnqueueShotZoomSpec(float zoom, string durationToken, string easeToken)
    {
        EaseSelection ease = ResolveEase(easeToken);

        Emit(new ShotZoomCommandSpec
        {
            zoom = zoom,
            duration = YarnDurationParser.Parse(durationToken),
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys,
        });
    }

    private void EnqueueShotTrackSpec(string xToken, string yToken, string durationToken, string easeToken)
    {
        EaseSelection ease = ResolveEase(easeToken);

        Emit(new ShotTrackCommandSpec
        {
            pan = new Vector2(ParseSignedUnit(xToken, 2.5f), ParseSignedUnit(yToken)),
            duration = YarnDurationParser.Parse(durationToken),
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys,
        });
    }

    private void EnqueueShotResetSpec(string durationToken, string easeToken)
    {
        EaseSelection ease = ResolveEase(easeToken);

        Emit(new ShotResetCommandSpec
        {
            duration = YarnDurationParser.Parse(durationToken),
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys,
        });
    }
}
