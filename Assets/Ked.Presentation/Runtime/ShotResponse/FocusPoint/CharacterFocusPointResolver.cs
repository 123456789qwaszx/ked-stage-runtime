using UnityEngine;

// 캐릭터의 특정 포커스 지점이,
// 현재 Presentation RigSpaceRoot 좌표계에서 어디에 있는지 측정한 값.
public struct CharacterFocusPointResult
{
    // FocusPointInRigSpace가 소속된 기준 좌표계.
    public RectTransform RigSpaceRoot;

    // Final focus estimate in RigSpaceRoot local space.
    public Vector2 FocusPointInRigSpace;
}

public static class CharacterFocusPointResolver
{
    public static void TryResolve(
        PresentationStage stage,
        IShotResponseStageProvider stageProvider,
        string roleKey,
        CharacterFocusPreset preset,
        Vector2 commandOffset,
        CharacterFocusTuningDBSO tuningDb,
        bool useSettledPlacementTargets,
        out CharacterFocusPointResult result)
    {
        result = default;

        string resolvedRigKey = CharacterRigTargetResolver.ResolveRigKeyByPolicy(stage, roleKey);
        stage.characterRigs.TryGetRig(resolvedRigKey, out CharacterRigRefs rigRefs);
        string tuningKey = CharacterRigTargetResolver.ResolveCharacterKeyFromTargetKey(stage, roleKey);

        CharacterFacing facing = ResolveFacing(stage, roleKey);

        TryResolveFromRigRefs(
            rigRefs,
            stageProvider.RigSpaceRoot,
            tuningKey,
            preset,
            commandOffset,
            tuningDb,
            useSettledPlacementTargets,
            facing,
            out result);
    }

    public static bool TryResolveFromRigRefs(
        CharacterRigRefs rigRefs,
        RectTransform rigSpaceRoot,
        string tuningKey,
        CharacterFocusPreset preset,
        Vector2 commandOffset,
        CharacterFocusTuningDBSO tuningDb,
        bool useSettledPlacementTargets,
        CharacterFacing facing,
        out CharacterFocusPointResult result)
    {
        return TryResolveFromRigRefs(
            rigRefs,
            rigSpaceRoot,
            tuningKey,
            preset,
            commandOffset,
            tuningDb,
            useSettledPlacementTargets,
            facing,
            mirrorCommandOffset: true,
            out result);
    }

    public static bool TryResolveFromRigRefs(
        CharacterRigRefs rigRefs,
        RectTransform rigSpaceRoot,
        string tuningKey,
        CharacterFocusPreset preset,
        Vector2 commandOffset,
        CharacterFocusTuningDBSO tuningDb,
        bool useSettledPlacementTargets,
        CharacterFacing facing,
        bool mirrorCommandOffset,
        out CharacterFocusPointResult result)
    {
        result = default;

        if (rigSpaceRoot == null)
            return false;

        if (!TryResolveFocusMeasureInputs(
                rigRefs,
                tuningKey,
                preset,
                commandOffset,
                tuningDb,
                facing,
                mirrorCommandOffset,
                out RectTransform measureRect,
                out Vector3 focusLocalOffset))
        {
            return false;
        }

        // 움직이는 placement 조상(translation/scale/rotation)이 있으면,
        // 그들이 settled target에 도착했을 때의 focusWorld로 보정해 측정한다.
        // 아무도 안 움직이면 라이브 측정과 동일하다.
        Vector3 focusWorld =
            useSettledPlacementTargets && rigRefs.PlacementTargets != null
                ? rigRefs.PlacementTargets.MeasureSettledWorldPoint(
                    measureRect,
                    focusLocalOffset,
                    rigSpaceRoot)
                : measureRect.TransformPoint(focusLocalOffset);

        Vector3 focusInRigSpace =
            rigSpaceRoot.InverseTransformPoint(focusWorld);

        result = new CharacterFocusPointResult
        {
            RigSpaceRoot = rigSpaceRoot,
            FocusPointInRigSpace = new Vector2(
                focusInRigSpace.x,
                focusInRigSpace.y),
        };

        return true;
    }

    /// <summary>
    /// focus 측정에 필요한 두 입력: 측정 노드와 그 로컬 오프셋.
    ///
    /// 이 수식이 사는 유일한 자리다 — depth 보정(CharacterDepthResolver)과
    /// 배치 해(CharacterFocusPlacementSolver)도 여기를 부른다. 사본을 만들지 말 것.
    /// </summary>
    public static bool TryResolveFocusMeasureInputs(
        CharacterRigRefs rigRefs,
        string tuningKey,
        CharacterFocusPreset preset,
        Vector2 commandOffset,
        CharacterFocusTuningDBSO tuningDb,
        CharacterFacing facing,
        bool mirrorCommandOffset,
        out RectTransform measureRect,
        out Vector3 focusLocalOffset)
    {
        measureRect = null;
        focusLocalOffset = default;

        if (rigRefs == null || tuningDb == null)
            return false;

        // Focus는 "캐릭터의 논리적 위치"를 가리켜야 한다.
        // 즉 CharacterPortrait_VisualOffset를 사용한다.
        measureRect = rigRefs.CharacterPortrait_VisualOffset;

        if (measureRect == null)
            return false;

        // FocusPoint 자체는 캐릭터 facing을 따른다.
        // 하지만 commandOffset은 용도에 따라 mirror 여부가 달라진다.
        // 예: 카메라 focus offset은 mirror되는 편이 자연스럽지만,
        //     일부 emoji placement offset은 screen-space 보정처럼 유지되어야 할 수 있다.
        Vector2 focusOffset = tuningDb.ResolveOffset(tuningKey, preset, Vector2.zero);
        focusOffset = facing.MirrorX(focusOffset);

        Vector2 effectiveCommandOffset = mirrorCommandOffset
            ? facing.MirrorX(commandOffset)
            : commandOffset;

        focusOffset += effectiveCommandOffset;

        focusLocalOffset = new Vector3(focusOffset.x, focusOffset.y, 0f);

        return true;
    }

    public static CharacterFacing ResolveFacing(PresentationStage stage, string roleKey)
    {
        if (stage != null &&
            stage.castRegistry != null &&
            stage.castRegistry.TryGetFacing(roleKey, out CharacterFacing facing))
        {
            return facing;
        }

        return CharacterFacing.Right;
    }
}