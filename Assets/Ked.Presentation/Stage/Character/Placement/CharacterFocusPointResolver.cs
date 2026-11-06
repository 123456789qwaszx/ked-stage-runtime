using UnityEngine;

// 캐릭터의 특정 포커스 지점이,
// 현재 Presentation RigSpaceRoot 좌표계에서 어디에 있는지 측정한 값.
public struct CharacterFocusPointResult
{
    // FocusPointInRigSpace가 소속된 기준 좌표계.
    public RectTransform RigSpaceRoot;

    // Final focus estimate in RigSpaceRoot local space.
    // 정착 측정(useSettledPlacementTargets)이면 카메라 중립의 무대 좌표다 — 샷이 움직이는 무대 노드를 중립으로 잰다.
    public Vector2 FocusPointInRigSpace;

    // 그 점이 선 무대와 층(리그의 무대 · 층). 샷이 이 층에 주는 반응으로 화면 위치가 정해진다.
    public StageRigRefs Stage;
    public StageLayerRefs Layer;
}

public static class CharacterFocusPointResolver
{
    public static CharacterFocusPointResult Resolve(
        PresentationStage stage,
        string roleKey,
        CharacterFocusPreset preset,
        Vector2 commandOffset,
        CharacterFocusTuningDBSO tuningDb,
        bool useSettledPlacementTargets)
    {
        CharacterRigRefs rigRefs = CharacterRigResolver.ResolveRig(stage, roleKey);
        string tuningKey = CharacterRigResolver.ResolveCharacterKey(stage, roleKey);

        CharacterFacing facing = ResolveFacing(stage, roleKey);

        return ResolveFromRigRefs(
            rigRefs,
            rigRefs.Stage.Root,
            tuningKey,
            preset,
            commandOffset,
            tuningDb,
            useSettledPlacementTargets,
            facing,
            mirrorCommandOffset: true,
            useSettledPlacementTargets ? stage.StageView : null);
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
        result = default;

        if (rigSpaceRoot == null || rigRefs == null || tuningDb == null || rigRefs.CharacterPortrait_VisualOffset == null)
            return false;

        result = ResolveFromRigRefs(
            rigRefs,
            rigSpaceRoot,
            tuningKey,
            preset,
            commandOffset,
            tuningDb,
            useSettledPlacementTargets,
            facing,
            mirrorCommandOffset: true,
            null);

        return true;
    }

    public static CharacterFocusPointResult ResolveFromRigRefs(
        CharacterRigRefs rigRefs,
        RectTransform rigSpaceRoot,
        string tuningKey,
        CharacterFocusPreset preset,
        Vector2 commandOffset,
        CharacterFocusTuningDBSO tuningDb,
        bool useSettledPlacementTargets,
        CharacterFacing facing,
        bool mirrorCommandOffset,
        IStageViewNeutral stageView)
    {
        ResolveFocusMeasureInputs(
            rigRefs,
            tuningKey,
            preset,
            commandOffset,
            tuningDb,
            facing,
            mirrorCommandOffset,
            out RectTransform measureRect,
            out Vector3 focusLocalOffset);

        // 움직이는 placement 조상(translation/scale/rotation)이 있으면,
        // 그들이 settled target에 도착했을 때의 focusWorld로 보정해 측정한다.
        // 아무도 안 움직이면 라이브 측정과 동일하다.
        Vector3 focusWorld =
            useSettledPlacementTargets
                ? rigRefs.PlacementTargets.MeasureSettledWorldPoint(
                    measureRect,
                    focusLocalOffset,
                    rigSpaceRoot,
                    stageView)
                : measureRect.TransformPoint(focusLocalOffset);

        Vector3 focusInRigSpace =
            rigSpaceRoot.InverseTransformPoint(focusWorld);

        return new CharacterFocusPointResult
        {
            RigSpaceRoot = rigSpaceRoot,
            FocusPointInRigSpace = new Vector2(
                focusInRigSpace.x,
                focusInRigSpace.y),
            Stage = rigRefs.Stage,
            Layer = rigRefs.Layer,
        };
    }

    /// <summary>
    /// focus 측정에 필요한 두 입력: 측정 노드와 그 로컬 오프셋.
    ///
    /// 이 수식이 사는 유일한 자리다 — depth 보정(CharacterDepthResolver)과
    /// 배치 해(CharacterFocusPlacementSolver)도 여기를 부른다. 사본을 만들지 말 것.
    /// </summary>
    public static void ResolveFocusMeasureInputs(
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
        // Focus는 "캐릭터의 논리적 위치"를 가리켜야 한다.
        // 즉 CharacterPortrait_VisualOffset를 사용한다.
        measureRect = rigRefs.CharacterPortrait_VisualOffset;

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
    }

    public static CharacterFacing ResolveFacing(PresentationStage stage, string roleKey)
    {
        if (stage.castRegistry.TryGetFacing(roleKey, out CharacterFacing facing))
            return facing;

        return CharacterFacing.Right;
    }
}