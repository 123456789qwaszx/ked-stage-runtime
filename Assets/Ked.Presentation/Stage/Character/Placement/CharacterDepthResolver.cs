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
        // 실패하면 raw Y로 떨어진다 — 크기는 바뀌고 초점만 보정 안 된다. 어느 경로든 반드시 경고한다.
        finalDepthY = rawDepthY;

        CharacterRigRefs rigRefs = CharacterRigResolver.ResolveRig(stage, roleKey);
        RectTransform rigSpaceRoot = rigRefs.Stage.Root;

        string tuningKey = CharacterRigResolver.ResolveCharacterKey(stage, roleKey);
        CharacterFacing facing = CharacterFocusPointResolver.ResolveFacing(stage, roleKey);

        CharacterFocusPointResolver.ResolveFocusMeasureInputs(
            rigRefs,
            tuningKey,
            preserveFocusPreset,
            preserveFocusOffset,
            focusTuningDb,
            facing,
            mirrorCommandOffset: true,
            out RectTransform measureRect,
            out Vector3 focusLocalOffset);

        // 초점 고정도 무대 좌표로 푼다 — 샷이 움직이는 무대 노드는 중립값.
        RectNodeState[] chain = rigRefs.PlacementTargets.CaptureSettledChain(
            measureRect, rigSpaceRoot, ChainRects, stage.StageView);

        int depthYIndex = ChainRects.IndexOf(depthYRect);
        int depthScaleIndex = ChainRects.IndexOf(depthScaleRect);

        if (depthYIndex < 0 || depthScaleIndex < 0)
        {
            WarnFallback(roleKey, $"깊이 축이 초점 측정 체인에 없다(depthY={depthYIndex}, depthScale={depthScaleIndex})");
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

    private static void WarnFallback(string roleKey, string reason)
        => Debug.LogWarning($"[CharacterDepthResolver] 초점 보존 없이 raw depthY를 쓴다 — {reason}. role='{roleKey}'");
}