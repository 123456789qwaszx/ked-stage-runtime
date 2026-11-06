using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

public sealed partial class StageCommandParser
{
    // shot 계열도 place·size와 같은 모양이다: 마지막 위치 인자가 이징이고,
    // 미지정이면 스펙 기본값(OutCubic). 해석은 YarnEaseParser 한 자리를 함께 쓴다.

    private void EnqueueShotZoomFocusSpec(string roleKey, string focusName, string screenPointName, float zoom, string durationToken, string easeToken)
    {
        CharacterFocusPresetParser.TryParse(focusName, out CharacterFocusPreset focusPreset);

        ScreenFocusPoint screenPoint = ParseScreenPoint("shot_focus_to", screenPointName);

        Ease ease = YarnEaseParser.Parse(easeToken);

        var spec = new ShotZoomFocusCommandSpec
        {
            focusRoleKey = roleKey,
            focusPreset = focusPreset,
            screenPoint = screenPoint,
            zoom = zoom,
            duration = YarnDurationParser.Parse(durationToken),
            ease = ease,
        };

        Emit(spec);
    }

    private void EnqueueShotToSpec(float zoom, string xToken, string yToken, string durationToken, string easeToken)
    {
        Ease ease = YarnEaseParser.Parse(easeToken);

        Emit(new ShotToCommandSpec
        {
            zoom = zoom,
            pan = new Vector2(ParseSignedUnit(xToken, 2.5f), ParseSignedUnit(yToken)),
            duration = YarnDurationParser.Parse(durationToken),
            ease = ease,
        });
    }

    private void EnqueueShotZoomSpec(float zoom, string durationToken, string easeToken)
    {
        Ease ease = YarnEaseParser.Parse(easeToken);

        Emit(new ShotZoomCommandSpec
        {
            zoom = zoom,
            duration = YarnDurationParser.Parse(durationToken),
            ease = ease,
        });
    }

    private void EnqueueShotTrackSpec(string xToken, string yToken, string durationToken, string easeToken)
    {
        Ease ease = YarnEaseParser.Parse(easeToken);

        Emit(new ShotTrackCommandSpec
        {
            pan = new Vector2(ParseSignedUnit(xToken, 2.5f), ParseSignedUnit(yToken)),
            duration = YarnDurationParser.Parse(durationToken),
            ease = ease,
        });
    }

    private void EnqueueShotResetSpec(string durationToken, string easeToken)
    {
        Ease ease = YarnEaseParser.Parse(easeToken);

        Emit(new ShotResetCommandSpec
        {
            duration = YarnDurationParser.Parse(durationToken),
            ease = ease,
        });
    }
}
