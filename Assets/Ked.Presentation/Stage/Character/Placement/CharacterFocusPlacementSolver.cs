using System.Collections.Generic;
using Ked.Presentation.Core;
using UnityEngine;

/// <summary>
/// place — 캐릭터의 focus 지점을 무대의 지정 지점으로 보내는 이동 축 끝값.
///
/// 지점은 **무대 좌표**다: 카메라가 중립일 때 그 화면 지점과 겹치는 무대 위 자리.
/// 계산 사슬이 샷이 움직이는 무대 노드를 지날 때는 중립값을 쓴다(<see cref="IStageViewNeutral"/>) —
/// 그래서 결과가 샷의 진행도와 무관하고, 일반 재생 · 롤백 · 코어 폴드가 같은 자리를 낸다.
///
/// 계산은 코어 SettledFocusMath.SolveFocusPlacement가 한다.
/// 여기 남는 일은 입력 수집뿐이다: facing·focus 오프셋·정착 체인·노드 인덱스·화면 지점.
/// </summary>
public static class CharacterFocusPlacementSolver
{
    // 체인 인덱스를 찾기 위한 스크래치. 커맨드 실행은 단일 스레드다.
    private static readonly List<RectTransform> ChainRects = new(48);

    public static Vector2 CalculateFocusPlacement(
        PresentationStage stage,
        string roleKey,
        RectTransform moveRect,
        CharacterFocusPreset focusPreset,
        Vector2 focusOffset,
        CharacterFocusTuningDBSO focusTuningDb,
        ScreenFocusPoint screenPoint,
        Vector2 screenOffset)
    {
        CharacterRigRefs rigRefs = CharacterRigResolver.ResolveRig(stage, roleKey);

        // 리그 좌표 계산의 경계는 이 리그가 선 무대의 루트다.
        RectTransform rigSpaceRoot = rigRefs.Stage.Root;

        string tuningKey = CharacterRigResolver.ResolveCharacterKey(stage, roleKey);
        CharacterFacing facing = CharacterFocusPointResolver.ResolveFacing(stage, roleKey);

        CharacterFocusPointResolver.ResolveFocusMeasureInputs(
            rigRefs,
            tuningKey,
            focusPreset,
            focusOffset,
            focusTuningDb,
            facing,
            mirrorCommandOffset: true,
            out RectTransform measureRect,
            out Vector3 focusLocalOffset);

        RectNodeState[] chain = rigRefs.PlacementTargets.CaptureSettledChain(
            measureRect, rigSpaceRoot, ChainRects, stage.StageView);

        // 이동 축은 리그 스키마상 언제나 측정 체인 위에 있다. 아니면 코어(RequireIndex)가 예외로 드러낸다.
        int moveIndex = ChainRects.IndexOf(moveRect);

        // 지점 비율표는 아직 호스트 지식이다 — 게임별 값이라 언젠가 tuning으로 간다.
        Vector2 desiredFocusInRigSpace =
            ScreenFocusPointResolver.Resolve(rigSpaceRoot, screenPoint) + screenOffset;

        Vec2 solved = SettledFocusMath.SolveFocusPlacement(
            chain,
            CharacterPlacementTargetLedger.SpaceOf(rigSpaceRoot),
            moveIndex,
            new Vector2(focusLocalOffset.x, focusLocalOffset.y).ToCore(),
            desiredFocusInRigSpace.ToCore(),
            moveRect.anchoredPosition.ToCore());

        return solved.ToUnity();
    }
}