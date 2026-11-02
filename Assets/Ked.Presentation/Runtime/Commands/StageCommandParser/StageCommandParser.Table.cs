using System;
using System.Collections.Generic;
using Ked.Presentation.Core;

public sealed partial class StageCommandParser
{
    private delegate void Handler(StageCommandParser parser, StageCommand c);

    // ── 커맨드 표 — 이름 · 인자 자리 · 생략값이 전부 여기 있다 ───────────────
    // 생략값은 핸들러 시그니처가 아니라 이 표가 갖는다(한 곳). 브리지의 Yarn 등록은
    // 이름과 인자 수만 안다 — 뜻은 여기서 정해진다.
    //
    // 정본만 둔다(2026-11-03). 같은 일을 인자 하나만 고정해 다시 부르는 이름(slotNN · char_to_sN · bg_slotNN ·
    // screen_blur_sN · place_* · size_<라벨> · char_visual_<프리셋> · tx_<프리셋> · <<Nfr>>)은 걷었다 —
    // 대본은 저작 툴(ked-story-compositor)이 내고, 툴은 정본만 낸다. 예외는 place_left · place_center ·
    // place_right 셋이다(소유자 결정). 변형은 이름이 아니라 인자로 받는다.
    private static readonly Dictionary<string, Handler> Table = new(StringComparer.Ordinal)
    {
        ["pause"] = (p, c) => p.EnqueueWaitSpec(c.Arg(0)),
        ["actor"] = (p, c) => p.EnqueuePresentationActorAliasSpec(c.Arg(0), c.Arg(1)),
        ["slot"] = (p, c) => p.EnqueueSetupCharRigSpec(c.Arg(0), c.Arg(1, "stage00"), c.Arg(2, "mid")),
        ["cast"] = (p, c) => p.EnqueueCastCharacterSpec(c.Arg(0), c.Arg(1), c.Arg(2, "a"), c.Arg(3, "1")),
        ["pose"] = (p, c) => p.EnqueueSetPortraitPoseSpec(c.Arg(0), c.Arg(1)),
        ["face"] = (p, c) => p.EnqueueSetPortraitFaceSpec(c.Arg(0), c.Arg(1)),
        ["face_swap"] = (p, c) => p.EnqueueSetEmotionPortraitWipeSpec(c.Arg(0), c.Arg(1), c.Arg(2, "10fr")),
        ["mirror"] = (p, c) => p.EnqueueMirrorSetSpec(c.Arg(0), c.Arg(1, "")),
        ["move_by"] = (p, c) => p.EnqueueSetAnchorOffsetSpecs(c.Arg(0), c.Arg(1, "0u"), c.Arg(2, "0u"), c.Arg(3, "0.4s"), c.Arg(4, "")),
        ["rotate_by"] = (p, c) => p.EnqueueRotateBySpec(c.Arg(0), Float(c, 1, 0f), c.Arg(2, "0.4s"), c.Arg(3, "")),
        ["scale_by"] = (p, c) => p.EnqueueSizeBySpec(c.Arg(0), Float(c, 1, 0f), c.Arg(2, "0.4s"), c.Arg(3, "")),
        ["move_reset"] = (p, c) => p.EnqueueSetPlaceResetSpecs(c.Arg(0), c.Arg(1, "0.4s")),
        ["rotate_reset"] = (p, c) => p.EnqueueRotateResetSpec(c.Arg(0), c.Arg(1, "0.4s"), c.Arg(2, "")),
        ["scale_reset"] = (p, c) => p.EnqueueSizeResetSpec(c.Arg(0), c.Arg(1, "0.4s"), c.Arg(2, "")),
        ["gesture"] = (p, c) => p.EnqueueGestureSpec(c.Arg(0), c.Arg(1, "0u"), c.Arg(2, "0u"), c.Arg(3, "12fr"), c.Arg(4, ""), c.Arg(5, "")),
        ["motion"] = (p, c) => p.EnqueueMotionSpec(c.Arg(0), c.Arg(1), c.Arg(2, MotionModeToken.Relative)),
        ["sibling_front"] = (p, c) => p.EnqueueCharacterSiblingFrontSpec(c.Arg(0)),
        ["sibling_back"] = (p, c) => p.EnqueueCharacterSiblingBackSpec(c.Arg(0)),
        ["char_to"] = (p, c) => p.EnqueueMoveCharacterRigToStageLayerSpec(c.Arg(0), c.Arg(1, "stage00"), c.Arg(2, "mid")),
        ["shot_focus_to"] = (p, c) => p.EnqueueShotZoomFocusSpec(c.Arg(0), c.Arg(1, "body"), c.Arg(2, "center"), Float(c, 3, 2.5f), c.Arg(4, "1.2s"), c.Arg(5, "")),
        ["shot_to"] = (p, c) => p.EnqueueShotToSpec(Float(c, 0, 1f), c.Arg(1, "2.5u"), c.Arg(2, "0u"), c.Arg(3, "0.45s"), c.Arg(4, "")),
        ["shot_zoom"] = (p, c) => p.EnqueueShotZoomSpec(Float(c, 0, 1f), c.Arg(1, "0.45s"), c.Arg(2, "")),
        ["shot_track"] = (p, c) => p.EnqueueShotTrackSpec(c.Arg(0, "2.5u"), c.Arg(1, "0u"), c.Arg(2, "0.35s"), c.Arg(3, "")),
        ["shot_reset"] = (p, c) => p.EnqueueShotResetSpec(c.Arg(0, "0.3s"), c.Arg(1, "")),
        ["show"] = (p, c) => p.EnqueueShowAtSpec(c.Arg(0), c.Arg(1, DefaultShowFaceToken), c.Arg(2, DefaultShowDurationToken)),
        ["left"] = (p, c) => p.EnqueueNudgeLeftSpec(c.Arg(0), c.Arg(1), c.Arg(2, DefaultNudgeDurationToken), c.Arg(3, "")),
        ["right"] = (p, c) => p.EnqueueNudgeRightSpec(c.Arg(0), c.Arg(1), c.Arg(2, DefaultNudgeDurationToken), c.Arg(3, "")),
        ["up"] = (p, c) => p.EnqueueNudgeUpSpec(c.Arg(0), c.Arg(1), c.Arg(2, DefaultNudgeDurationToken), c.Arg(3, "")),
        ["down"] = (p, c) => p.EnqueueNudgeDownSpec(c.Arg(0), c.Arg(1), c.Arg(2, DefaultNudgeDurationToken), c.Arg(3, "")),
        ["place"] = (p, c) => p.EnqueuePlaceCharacterFocusSpec(c.Arg(0), c.Arg(1, "bust"), c.Arg(2, "center"), c.Arg(3, "0fr"), c.Arg(4, "")),
        ["place_left"] = (p, c) => p.EnqueueFocusToLeftSpec(c.Arg(0), c.Arg(1, DefaultFocusPlacementFocusToken), c.Arg(2, DefaultFocusPlacementDurationToken), c.Arg(3, "")),
        ["place_center"] = (p, c) => p.EnqueueFocusToCenterSpec(c.Arg(0), c.Arg(1, DefaultFocusPlacementFocusToken), c.Arg(2, DefaultFocusPlacementDurationToken), c.Arg(3, "")),
        ["place_right"] = (p, c) => p.EnqueueFocusToRightSpec(c.Arg(0), c.Arg(1, DefaultFocusPlacementFocusToken), c.Arg(2, DefaultFocusPlacementDurationToken), c.Arg(3, "")),
        ["size"] = (p, c) => p.EnqueueDepthAtPresetSpec(c.Arg(0), c.Arg(1), c.Arg(2, DefaultFocusDepthPresetToken), c.Arg(3, DefaultFocusDepthDurationToken), c.Arg(4, "")),
        ["size_reset"] = (p, c) => p.EnqueueDepthResetSpec(c.Arg(0), Float(c, 1, 0f), c.Arg(2, "")),
        ["fade_in"] = (p, c) => p.EnqueueFadeInSpec(c.Arg(0), c.Arg(1, "14fr")),
        ["fade_out"] = (p, c) => p.EnqueueFadeOutSpec(c.Arg(0), c.Arg(1, "14fr")),
        ["slide_in"] = (p, c) => p.EnqueueSlideInSpec(c.Arg(0), c.Arg(1, "left"), c.Arg(2, "12u"), c.Arg(3, "10fr")),
        ["slide_out"] = (p, c) => p.EnqueueSlideOutSpec(c.Arg(0), c.Arg(1, "right"), c.Arg(2, "12u"), c.Arg(3, "10fr")),
        ["char_move_to"] = (p, c) => p.EnqueueMoveByUnitCharSpec(c.Arg(0), c.Arg(1), c.Arg(2), c.Arg(3, "10fr")),
        ["char_scale_to"] = (p, c) => p.EnqueueScaleSpec(c.Arg(0), Float(c, 1, 0f), c.Arg(2, "10fr"), c.Arg(3, "")),
        ["char_rotate_to"] = (p, c) => p.EnqueuePortraitRotateToSpec(c.Arg(0), Float(c, 1, 0f), c.Arg(2, "10fr"), c.Arg(3, "")),
        ["char_visual"] = (p, c) => p.EnqueueCharVisualPresetSpec(c.Arg(0), c.Arg(1), Float(c, 2, 1f), c.Arg(3, "6fr")),
        // 배경 = 캐스팅하지 않은 캐릭터 슬롯(StageCommandParser.Background). bg_place·bg_size·bg_slotNN은 지운 어휘다.
        ["bg_spawn"] = (p, c) => p.EnqueueSpawnBackgroundSpec(c.Arg(0), c.Arg(1), c.Arg(2, "stage00"), c.Arg(3, "far")),
        ["bg_sprite"] = (p, c) => p.EnqueueSetBackgroundSpriteSpec(c.Arg(0), c.Arg(1, "")),
        ["bg_fade_in"] = (p, c) => p.EnqueueFadeInBackgroundDslSpec(c.Arg(0), c.Arg(1, "10fr")),
        ["bg_fade_out"] = (p, c) => p.EnqueueFadeOutBackgroundDslSpec(c.Arg(0), c.Arg(1, "10fr")),
        ["bg_move"] = (p, c) => p.EnqueueMoveBackgroundDslSpec(c.Arg(0), c.Arg(1), c.Arg(2), c.Arg(3, "10fr")),
        ["bg_scale"] = (p, c) => p.EnqueueScaleBackgroundDslSpec(c.Arg(0), Float(c, 1, 0f), c.Arg(2, "10fr")),
        ["tx"] = (p, c) => p.EnqueueStageMaskMotionPresetSpec(c.Arg(0), c.Arg(1, "stage01"), c.Arg(2, "")),
        ["tx_stage_mask_clear"] = (p, c) => p.EnqueueStageMaskClearSpec(),
        ["bgm"] = (p, c) => p.EnqueuePlayBgmSpec(c.Arg(0), c.Arg(1, "1s")),
        ["stop_bgm"] = (p, c) => p.EnqueueStopBgmSpec(c.Arg(0, "1s")),
        ["sfx"] = (p, c) => p.EnqueuePlaySfxSpec(c.Arg(0)),
        ["stop_all_sfx"] = (p, c) => p.EnqueueStopAllSfxSpec(),
        ["voice"] = (p, c) => p.EnqueuePlayVoiceSpec(c.Arg(0)),
        ["stop_voice"] = (p, c) => p.EnqueueStopVoiceSpec(),
        ["screen_flash"] = (p, c) => p.EnqueueScreenFlashPresetSpec(c.Arg(0), Float(c, 1, 1f)),
        ["screen_flash_clear"] = (p, c) => p.EnqueueScreenFlashClearSpec(),
        ["screen_vignette"] = (p, c) => p.EnqueueScreenVignettePresetSpec(c.Arg(0), Float(c, 1, 1f), c.Arg(2, "0.35s")),
        ["screen_vignette_clear"] = (p, c) => p.EnqueueScreenVignetteClearSpec(c.Arg(0, "0.35s")),
        ["screen_noise"] = (p, c) => p.EnqueueScreenNoisePresetSpec(c.Arg(0, ScreenNoisePresetDBSO.DefaultPresetKey), Float(c, 1, 1f), c.Arg(2, "0.35s")),
        ["screen_noise_clear"] = (p, c) => p.EnqueueScreenNoiseClearSpec(c.Arg(0, "0.35s")),
        ["focus_on"] = (p, c) => p.EnqueueDepthFocusOnSpec(c.Arg(0), Float(c, 1, 1f), c.Arg(2, "0.4s")),
        ["focus_clear"] = (p, c) => p.EnqueueDepthFocusClearSpec(c.Arg(0, "0.4s")),
        ["screen_blur"] = (p, c) => p.EnqueueLayerBlurSpec(c.Arg(2, "stage00"), c.Arg(0, "mid"), Float(c, 1, 1f)),
        ["screen_blur_reset"] = (p, c) => p.EnqueueStageLayerBlurClearSpec(),
    };

    /// <summary>이 파서가 아는 커맨드 이름 전부.</summary>
    public static IEnumerable<string> CommandNames => Table.Keys;
}
