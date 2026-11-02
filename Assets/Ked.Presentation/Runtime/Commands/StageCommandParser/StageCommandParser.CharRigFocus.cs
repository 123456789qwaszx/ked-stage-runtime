using Ked.Presentation.Core;
using UnityEngine;

public sealed partial class StageCommandParser
{
    private const string DefaultFocusPlacementFocusToken = "face";
    private const string DefaultFocusPlacementDurationToken = "0fr";
    
    private const string DefaultFocusDepthPresetToken = "bust";
    private const string DefaultFocusDepthDurationToken = "10fr";
    
    #region CharFocusPlacement
    private void EnqueueFocusToLeftSpec(string roleKey, string focus, string durationToken, string easeToken)
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.Left, durationToken, easeToken);

    private void EnqueueFocusToCenterSpec(string roleKey, string focus, string durationToken, string easeToken)
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.Center, durationToken, easeToken);

    private void EnqueueFocusToRightSpec(string roleKey, string focus, string durationToken, string easeToken)
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.Right, durationToken, easeToken);

    private void EnqueuePlaceCharacterFocusToSpec(
        string roleKey,
        string focus,
        ScreenFocusPoint screenPoint,
        string durationToken,
        string easeToken = "")
    {
        EaseSelection ease = ResolveEase(easeToken);

        Emit(new PlaceCharacterFocusCommandSpecCharR
        {
            slotKey = roleKey,
            focusPreset = CharacterFocusPresetParser.Parse(focus),
            screenPoint = screenPoint,
            moveTarget = CharacterRigTarget.CharSlot_Track_Focus,
            duration = YarnDurationParser.Parse(durationToken),
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys
        });
    }

    private void EnqueuePlaceCharacterFocusSpec(string roleKey, string focus, string screenPoint, string durationToken, string easeToken)
    {
        EaseSelection ease = ResolveEase(easeToken);

        Emit(new PlaceCharacterFocusCommandSpecCharR
        {
            slotKey = roleKey,
            focusPreset = CharacterFocusPresetParser.Parse(focus),
            screenPoint = ParseScreenPoint("place", screenPoint),
            moveTarget = CharacterRigTarget.CharSlot_Track_Focus,
            duration = YarnDurationParser.Parse(durationToken),
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys
        });
    }
    #endregion
    
    #region CharFocusDepth
    private void EnqueueDepthAtPresetSpec(string roleKey, string depthArg, string preserveFocusArg, string durationToken, string easeToken)
    {
        var spec = new SetDepthCommandSpecCharR
        {
            slotKey = roleKey,
            duration = YarnDurationParser.Parse(durationToken),
        };

        if (CharacterDepthPresetParser.TryParseDepthLevel(depthArg, out float level))
        {
            spec.useLevel = true;
            spec.level = level;
        }
        else
        {
            spec.useLevel = false;

            if (!CharacterDepthPresetParser.TryParse(depthArg, out CharacterDepthKey preset))
            {
                Debug.LogWarning(
                    $"[StageCommandParser] Unknown depth preset '{depthArg}'. " +
                    $"Fallback to '{CharacterDepthKey.Mid}'.");

                preset = CharacterDepthKey.Mid;
            }

            spec.preset = preset;
        }

        if (!CharacterFocusPresetParser.TryParse(preserveFocusArg, out CharacterFocusPreset focusPreset))
        {
            Debug.LogWarning(
                $"[StageCommandParser] Unknown preserve focus preset '{preserveFocusArg}'. " +
                $"Fallback to '{CharacterFocusPreset.Bust}'.");

            focusPreset = CharacterFocusPreset.Bust;
        }

        spec.focusPreset = focusPreset;

        EaseSelection ease = ResolveEase(easeToken);
        spec.ease = ease.Ease;
        spec.customCurveKeys = ease.CurveKeys;

        Emit(spec);
    }

    private void EnqueueDepthResetSpec(string roleKey, float duration, string easeToken)
    {
        EaseSelection ease = ResolveEase(easeToken);

        Emit(new SetDepthCommandSpecCharR
        {
            slotKey = roleKey,
            preset = CharacterDepthKey.Mid,
            useLevel = false,
            duration = duration,
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys
        });
    }
    #endregion
}
