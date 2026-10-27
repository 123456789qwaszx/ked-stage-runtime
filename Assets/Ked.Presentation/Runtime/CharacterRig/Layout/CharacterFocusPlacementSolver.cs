using System.Collections.Generic;
using Ked.Presentation.Core;
using UnityEngine;

/// <summary>
/// place — 캐릭터의 focus 지점을 화면의 지정 지점으로 보내는 이동 축 목표값.
///
/// 계산은 코어 SettledFocusMath.SolveFocusPlacement가 한다.
/// 여기 남는 일은 입력 수집뿐이다: facing·focus 오프셋·정착 체인·노드 인덱스·화면 지점.
/// </summary>
public static class CharacterFocusPlacementSolver
{
    // 체인 인덱스를 찾기 위한 스크래치. 커맨드 실행은 단일 스레드다.
    private static readonly List<RectTransform> ChainRects = new(48);

    public static bool TryCalculateFocusPlacement(
        PresentationStage stage,
        IShotResponseStageProvider stageProvider,
        string roleKey,
        RectTransform moveRect,
        CharacterFocusPreset focusPreset,
        Vector2 focusOffset,
        CharacterFocusTuningDBSO focusTuningDb,
        ScreenFocusPoint screenPoint,
        Vector2 screenOffset,
        out Vector2 destinationAnchoredPosition)
    {
        destinationAnchoredPosition = default;

        if (moveRect == null || stageProvider == null)
            return false;

        RectTransform rigSpaceRoot = stageProvider.RigSpaceRoot;

        if (rigSpaceRoot == null)
            return false;

        string resolvedRigKey = CharacterRigTargetResolver.ResolveRigKeyByPolicy(stage, roleKey);
        stage.characterRigs.TryGetRig(resolvedRigKey, out CharacterRigRefs rigRefs);

        string tuningKey = CharacterRigTargetResolver.ResolveCharacterKeyFromTargetKey(stage, roleKey);
        CharacterFacing facing = CharacterFocusPointResolver.ResolveFacing(stage, roleKey);

        if (!CharacterFocusPointResolver.TryResolveFocusMeasureInputs(
                rigRefs,
                tuningKey,
                focusPreset,
                focusOffset,
                focusTuningDb,
                facing,
                mirrorCommandOffset: true,
                out RectTransform measureRect,
                out Vector3 focusLocalOffset))
        {
            return false;
        }

        RectNodeState[] chain = rigRefs.PlacementTargets.CaptureSettledChain(
            measureRect, rigSpaceRoot, ChainRects);

        int moveIndex = ChainRects.IndexOf(moveRect);

        if (moveIndex < 0)
        {
            Debug.LogWarning(
                $"[CharacterFocusPlacementSolver] 이동 축 '{moveRect.name}'이 focus 측정 체인에 없다. " +
                $"배치를 건너뛴다. role='{roleKey}'");

            return false;
        }

        // 화면 지점 비율표는 아직 호스트 지식이다 — 게임별 값이라 언젠가 tuning으로 간다.
        Vector2 desiredFocusInRigSpace =
            ScreenFocusPointResolver.Resolve(rigSpaceRoot, screenPoint) + screenOffset;

        Vec2 solved = SettledFocusMath.SolveFocusPlacement(
            chain,
            CharacterPlacementTargetLedger.SpaceOf(rigSpaceRoot),
            moveIndex,
            new Vector2(focusLocalOffset.x, focusLocalOffset.y).ToCore(),
            desiredFocusInRigSpace.ToCore(),
            moveRect.anchoredPosition.ToCore());

        destinationAnchoredPosition = solved.ToUnity();

        return true;
    }
}