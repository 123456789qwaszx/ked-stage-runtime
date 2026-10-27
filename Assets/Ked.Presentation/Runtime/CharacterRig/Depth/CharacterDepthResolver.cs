using System.Collections.Generic;
using Ked.Presentation.Core;
using UnityEngine;

public readonly struct CharacterDepthResult
{
    public readonly Vector2 RawDepthYAnchoredPosition;
    public readonly Vector2 DepthScale;
    public readonly CharacterFocusPreset PreserveFocusPreset;
    public readonly Vector2 PreserveFocusOffset;

    public CharacterDepthResult(
        Vector2 rawDepthYAnchoredPosition,
        Vector2 depthScale,
        CharacterFocusPreset preserveFocusPreset,
        Vector2 preserveFocusOffset)
    {
        RawDepthYAnchoredPosition = rawDepthYAnchoredPosition;
        DepthScale = depthScale;
        PreserveFocusPreset = preserveFocusPreset;
        PreserveFocusOffset = preserveFocusOffset;
    }
}

public static class CharacterDepthResolver
{
    private static readonly List<RectTransform> ChainRects = new(48);

    public static void ResolveRawDepth(
        CharacterDepthKey depthKey,
        bool useLevel,
        float level,
        CharacterDepthTuningSO globalTuning,
        CharacterFocusPreset focusPreset,
        Vector2 focusOffset,
        out CharacterDepthResult result)
    {
        CharacterDepthPresetValue value = useLevel
            ? globalTuning.ResolveLevel(level)
            : globalTuning.ResolvePreset(depthKey);

        value.preserveFocusPreset = focusPreset;
        value.preserveFocusOffset = focusOffset;

        result = new CharacterDepthResult(
            value.depthY,
            new Vector2(value.depthScale, value.depthScale),
            value.preserveFocusPreset,
            value.preserveFocusOffset);
    }

    public static void CalculateDepthYThatPreservesCurrentFocus(
        PresentationStage stage,
        IShotResponseStageProvider stageProvider,
        string roleKey,
        RectTransform depthYRect,
        RectTransform depthScaleRect,
        Vector2 rawDepthY,
        Vector2 targetDepthScale,
        CharacterFocusPreset preserveFocusPreset,
        Vector2 preserveFocusOffset,
        CharacterFocusTuningDBSO focusTuningDb,
        out Vector2 finalDepthY)
    {
        finalDepthY = rawDepthY;

        if (stageProvider == null || depthYRect == null || depthScaleRect == null)
            return;

        RectTransform rigSpaceRoot = stageProvider.RigSpaceRoot;

        if (rigSpaceRoot == null)
            return;

        string resolvedRigKey = CharacterRigTargetResolver.ResolveRigKeyByPolicy(stage, roleKey);
        stage.characterRigs.TryGetRig(resolvedRigKey, out CharacterRigRefs rigRefs);

        string tuningKey = CharacterRigTargetResolver.ResolveCharacterKeyFromTargetKey(stage, roleKey);
        CharacterFacing facing = CharacterFocusPointResolver.ResolveFacing(stage, roleKey);

        if (!CharacterFocusPointResolver.TryResolveFocusMeasureInputs(
                rigRefs,
                tuningKey,
                preserveFocusPreset,
                preserveFocusOffset,
                focusTuningDb,
                facing,
                mirrorCommandOffset: true,
                out RectTransform measureRect,
                out Vector3 focusLocalOffset))
        {
            return;
        }

        RectNodeState[] chain = rigRefs.PlacementTargets.CaptureSettledChain(
            measureRect, rigSpaceRoot, ChainRects);

        int depthYIndex = ChainRects.IndexOf(depthYRect);
        int depthScaleIndex = ChainRects.IndexOf(depthScaleRect);

        if (depthYIndex < 0 || depthScaleIndex < 0)
        {
            Debug.LogWarning(
                $"[CharacterDepthResolver] depth 축이 focus 측정 체인에 없다 " +
                $"(depthY={depthYIndex}, depthScale={depthScaleIndex}). " +
                $"보정 없이 raw depthY를 쓴다. role='{roleKey}'");

            return;
        }

        Vec2 solved = SettledFocusMath.SolveDepthYPreservingFocus(
            chain,
            CharacterPlacementTargetLedger.SpaceOf(rigSpaceRoot),
            depthYIndex,
            depthScaleIndex,
            new Vector2(focusLocalOffset.x, focusLocalOffset.y).ToCore(),
            rawDepthY.ToCore(),
            targetDepthScale.ToCore());

        finalDepthY = solved.ToUnity();
    }
}