using System;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

public sealed partial class StageCommandParser
{
    private void EnqueueSetupCharRigSpec(string slotKey, string stageKey, string layerKey)
        => Emit(new SetupCharRigCommandSpec
        {
            roleKey = slotKey,
            rigPrefab = _charRigPrefab,

            stage = PresentationStageKeyParser.Parse(stageKey),
            layer = PresentationDepthLayerKeyParser.Parse(layerKey)
        });

    private void EnqueueSetupCharRigStage00Spec(string slotKey, string layerKey)
        => EnqueueSetupCharRigAtDepthSpec(slotKey, PresentationStageKey.Stage00, layerKey);

    private void EnqueueSetupCharRigStage01Spec(string slotKey, string layerKey)
        => EnqueueSetupCharRigAtDepthSpec(slotKey, PresentationStageKey.Stage01, layerKey);

    private void EnqueueSetupCharRigStage02Spec(string slotKey, string layerKey)
        => EnqueueSetupCharRigAtDepthSpec(slotKey, PresentationStageKey.Stage02, layerKey);

    private void EnqueueSetupCharRigAtDepthSpec(
        string slotKey,
        PresentationStageKey stage,
        string layerKey)
        => Emit(new SetupCharRigCommandSpec
        {
            roleKey = slotKey,
            rigPrefab = _charRigPrefab,

            stage = stage,
            layer = PresentationDepthLayerKeyParser.Parse(layerKey)
        });

    private void EnqueueCastCharacterSpec(string slotKey, string characterKey, string variantKey, string emotionKey)
    {
        var castSpec = new CastCharacterCommandSpec
        {
            slotKey = slotKey,
            characterKey = characterKey
        };

        Emit(castSpec);

        EnqueueSetPortraitPoseSpec(slotKey, variantKey);
        EnqueueSetPortraitFaceSpec(slotKey, emotionKey);
        EnqueueSetAnchorSpecs(slotKey);
    }

    private void EnqueueSetAnchorSpecs(string slotKey, bool resetSlotPos = true,
        bool resetCharPos = true)
        => Emit(new SetAnchorCommandSpecCharR
        {
            slotKey = slotKey,
            target = CharacterRigTarget.CharacterPortrait_VisualOffset,
            resetSlotPos = resetSlotPos,
            resetCharacterPos = resetCharPos
        });

    private void EnqueueMirrorSetSpec(string roleKey, string directionToken)
        => Emit(new MirrorCharacterCommandSpecCharR
        {
            slotKey = roleKey,
            mode = CharacterMirrorModeParser.Parse(directionToken),
            target = CharacterRigTarget.CharacterPortrait_ActingScale_X,
            duration = 0f,
        });

    private void EnqueueSetPortraitPoseSpec(string slotKey, string variantKey)
        => Emit(new SetPortraitPoseCommandSpecCharR
        {
            slotKey = slotKey,
            variantKey = variantKey,
        });

    private void EnqueueSetPortraitFaceSpec(string slotKey, string emotionKey)
        => Emit(new SetPortraitSpriteCommandSpecCharR
        {
            slotKey = slotKey,
            portrait = new PortraitIdentity
            {
                character = "",
                variant = "",
                emotion = emotionKey
            }
        });

    private void EnqueueSetAnchorOffsetSpecs(string slotKey, string xToken, string yToken, string durationToken, string easeToken)
    {
        EaseSelection ease = ResolveEase(easeToken);

        Emit(new MoveByCommandSpecCharR
        {
            slotKey = slotKey,
            target = CharacterRigTarget.CharSlot_Track,
            useAbsolutePosition = false,
            delta = new Vector2(ParseSignedUnit(xToken), ParseSignedUnit(yToken)),
            duration = YarnDurationParser.Parse(durationToken),
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys
        });
    }

    /// <summary>
    /// 이징 토큰의 두 갈래: "@이름" = curves.json의 커스텀 곡선,
    /// 그 외 = EaseKind 이름. 미지정("")·실패는 스펙 기본값 OutCubic —
    /// 기존 대본은 인자가 없으므로 재생 결과가 그대로다.
    /// move_by·place 계열·size 계열·rotate 계열이 이 한 자리를 함께 쓴다.
    /// </summary>
    private EaseSelection ResolveEase(string easeToken, Ease fallback = Ease.OutCubic)
    {
        if (!string.IsNullOrWhiteSpace(easeToken) && easeToken[0] == '@')
        {
            string curveName = easeToken.Substring(1);

            if (_easeCurves.TryGet(
                    curveName, CurveKind.Motion, out CurveKey[] keys, out bool wrongKind))
                return new EaseSelection(fallback, keys);

            // 침묵 금지 — 1차 방어는 VnTool 저작 검증이고, 여기서는 소리 내고 굴러간다.
            Debug.LogError(
                wrongKind
                    ? $"[StageCommandParser] Ease curve '{easeToken}' is an oscillation curve " +
                      $"(gesture 전용 — 끝값이 0이다). 이동 커맨드에는 쓸 수 없다. Fallback to {fallback}."
                    : $"[StageCommandParser] Unknown ease curve '{easeToken}' — " +
                      $"{EaseCurveLibrary.BundleFileName}에 없다. Fallback to {fallback}.");

            return new EaseSelection(fallback);
        }

        return new EaseSelection(YarnEaseParser.Parse(easeToken, fallback));
    }

    /// <summary>
    /// gesture의 축 진동 해석. 셋을 받는다:
    ///   빈 토큰    → 내장 기본 혹(sin πt)
    ///   "@이름"    → curves.json의 진동 곡선
    ///   표준 이징  → 그 이징의 **핑퐁**(왕복의 절반). 0→1 이징이 그대로 몸짓이 된다.
    ///
    /// 못 읽는 낱말만 경고 + 기본 혹으로 굴러간다(침묵 금지).
    /// </summary>
    private OscillationSource ResolveOscillation(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return OscillationSource.Default;

        if (token[0] == '@')
        {
            string curveName = token.Substring(1);

            if (_easeCurves.TryGet(
                    curveName, CurveKind.Oscillation, out CurveKey[] keys, out bool wrongKind))
                return OscillationSource.FromCurve(keys);

            Debug.LogError(
                wrongKind
                    ? $"[StageCommandParser] Gesture curve '{token}' is a motion curve " +
                      $"(끝값이 1이라 제자리로 안 돌아온다). 기본 혹으로 재생한다."
                    : $"[StageCommandParser] Unknown gesture curve '{token}' — " +
                      $"{EaseCurveLibrary.BundleFileName}에 없다. 기본 혹으로 재생한다.");

            return OscillationSource.Default;
        }

        // 표준 이징 이름 → 핑퐁. 숫자 토큰은 거부한다(EaseKind가 임의 정수로도 파싱되므로).
        string trimmed = token.Trim();

        if (!char.IsDigit(trimmed[0]) && trimmed[0] != '-' && trimmed[0] != '+'
            && Enum.TryParse(trimmed, ignoreCase: true, out EaseKind kind))
            return OscillationSource.FromEase(kind);

        Debug.LogWarning(
            $"[StageCommandParser] Invalid gesture ease token '{token}'. " +
            "진동 곡선(@이름)이나 표준 이징 이름(OutBack 등)을 써라. 기본 혹으로 재생한다.");

        return OscillationSource.Default;
    }


    // ── 제자리 몸짓 ───────────────────────────────────────────────
    // 순변위 0이 정체다. 표적(CharacterPortrait_Shake)이 이동 계열과 다른 노드라
    // 같은 라인에서 move_by와 나란히 놀 수 있다 — "총총 뛰며 이동"이 그 조합이다.

    private void EnqueueGestureSpec(string roleKey, string xAmpToken, string yAmpToken, string durationToken, string xEaseToken, string yEaseToken)
        => Emit(new GestureCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_Shake,

            amplitude = new Vector2(ParseSignedUnit(xAmpToken, 0f), ParseSignedUnit(yAmpToken, 0f)),
            duration = YarnDurationParser.Parse(durationToken),

            xOscillation = ResolveOscillation(xEaseToken),
            yOscillation = ResolveOscillation(yEaseToken)
        });

    // motion(slot, name, mode) — motions.json의 클립을 그대로 싣는다. 길이는 클립이 안다.
    // mode(relative|absolute)는 클립이 아니라 이 커맨드가 정한다 — 생략값은 표에 있다(relative).
    // 모르는 이름·낱말은 소리 내고 아무것도 내지 않는다 — 대신 재생할 기본 모션이 없다(이징의 OutCubic과 다른 점).
    private void EnqueueMotionSpec(string roleKey, string motionName, string modeToken)
    {
        if (!_motions.TryGet(motionName, out MotionClip clip))
        {
            Debug.LogError(
                $"[StageCommandParser] Unknown motion '{motionName}' — {MotionLibrary.BundleFileName}에 없다. 이 커맨드는 재생하지 않는다.");
            return;
        }

        if (!MotionModeToken.TryParse(modeToken, out MotionChannelMode mode))
        {
            Debug.LogError(
                $"[StageCommandParser] motion '{motionName}': mode는 relative|absolute 여야 한다 ('{modeToken}'). 이 커맨드는 재생하지 않는다.");
            return;
        }

        Emit(new MotionCommandSpecCharR { slotKey = roleKey, clip = clip, mode = mode });
    }

    // ── 회전 (3a75d4f6에서 지운 것을 되살린 자리) ──────────────────
    // 표적은 CharSlot_SwayPivot이다 — 코어 리듀서(ApplyRotateBy/ApplyRotateReset)가
    // 접는 노드와 같아야 "재생 = 정지 프레임"이 유지된다.

    private void EnqueueRotateBySpec(string roleKey, float degree, string durationToken, string easeToken)
    {
        EaseSelection ease = ResolveEase(easeToken);

        Emit(new RotateToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharSlot_SwayPivot,

            toEuler = new Vector3(0f, 0f, degree),
            relativeToCurrent = true,

            duration = YarnDurationParser.Parse(durationToken),
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys
        });
    }

    private void EnqueueRotateResetSpec(string roleKey, string durationToken, string easeToken)
    {
        EaseSelection ease = ResolveEase(easeToken);

        Emit(new RotateToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharSlot_SwayPivot,

            toEuler = Vector3.zero,

            duration = YarnDurationParser.Parse(durationToken),
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys
        });
    }

    // 초상 축의 절대 회전. 종전 char_rotate_to는 다단 연출(PivotRotateTo)이었고
    // 그 커맨드는 되살리지 않았다 — 여기서는 RotateTo의 절대 모드로 선다.
    private void EnqueuePortraitRotateToSpec(string roleKey, float degree, string durationToken, string easeToken)
    {
        EaseSelection ease = ResolveEase(easeToken);

        Emit(new RotateToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_SwayPivot,

            toEuler = new Vector3(0f, 0f, degree),
            relativeToCurrent = false,

            duration = YarnDurationParser.Parse(durationToken),
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys
        });
    }

    private void EnqueueSizeBySpec(string roleKey, float multiplier, string durationToken, string easeToken)
    {
        EaseSelection ease = ResolveEase(easeToken);

        Emit(new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharSlot_Scale,

            toScale = new Vector2(multiplier, multiplier),
            relativeToCurrent = true,

            duration = YarnDurationParser.Parse(durationToken),
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys
        });
    }

    private void EnqueueSetPlaceResetSpecs(string slotKey, string durationToken)
    {
        float duration = YarnDurationParser.Parse(durationToken);

        var slotOffsetSpec = new MoveByCommandSpecCharR
        {
            slotKey = slotKey,
            target = CharacterRigTarget.CharSlot_Track,
            useAbsolutePosition = true,
            delta = new Vector2(0, 0),
            duration = duration
        };

        var spec2 = new MoveByCommandSpecCharR
        {
            slotKey = slotKey,
            target = CharacterRigTarget.CharSlot_Track_Focus,
            useAbsolutePosition = true,
            delta = new Vector2(0, 0),
            duration = duration
        };

        Emit(slotOffsetSpec);
        Emit(spec2);
    }

    private void EnqueueSizeResetSpec(string roleKey, string durationToken, string easeToken)
    {
        EaseSelection ease = ResolveEase(easeToken);

        Emit(new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharSlot_Scale,

            toScale = new Vector2(1, 1),

            duration = YarnDurationParser.Parse(durationToken),
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys
        });
    }

    private void EnqueueCharacterSiblingFrontSpec(string roleKey)
        => Emit(new SetCharacterSiblingOrderCommandSpecCharR
        {
            slotKey = roleKey,
            mode = CharacterRigSiblingOrderMode.Front
        });

    private void EnqueueCharacterSiblingBackSpec(string roleKey)
        => Emit(new SetCharacterSiblingOrderCommandSpecCharR
        {
            slotKey = roleKey,
            mode = CharacterRigSiblingOrderMode.Back
        });

    private void EnqueueMoveCharacterRigToStageLayerSpec(string roleKey, string stageKey, string layerKey)
        => Emit(new MoveCharacterRigToStageLayerCommandSpecCharR
        {
            slotKey = roleKey,

            stage = PresentationStageKeyParser.Parse(stageKey),
            layer = PresentationDepthLayerKeyParser.Parse(layerKey),

            siblingMode = CharacterRigReparentSiblingMode.Front
        });

    private void EnqueueMoveCharacterRigToStage00LayerSpec(string roleKey, string layerKey)
        => EnqueueMoveCharacterRigToStageLayerSpec(roleKey, "stage00", layerKey);

    private void EnqueueMoveCharacterRigToStage01LayerSpec(string roleKey, string layerKey)
        => EnqueueMoveCharacterRigToStageLayerSpec(roleKey, "stage01", layerKey);

    private void EnqueueMoveCharacterRigToStage02LayerSpec(string roleKey, string layerKey)
        => EnqueueMoveCharacterRigToStageLayerSpec(roleKey, "stage02", layerKey);
}
