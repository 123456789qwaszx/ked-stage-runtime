using System.Collections.Generic;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

// 샷은 장면 전역 의도다 — 커맨드가 의도(Intent)를 쓰고, 화면(Presented)은 이 시스템이 따라간다.
// 전환은 IntentTransition 하나가 맡는다(화면 효과·블러와 같은 것). 새 의도가 오면 지금 보이는 값에서 다시 조준한다(앞 전환을 완주시키지 않는다).
//
// 무대에 쓰는 값은 전부 코어 ShotLayoutMath의 출력이다. 이 클래스는 그 출력을 노드에 쓰는 applier이고,
// 중립(IStageViewNeutral)은 같은 식에 Default를 넣은 값이다 — 따로 적은 정의가 없다.
// 어느 노드가 샷 소유인지는 StageRigRefs(빌더 출력)가 안다. 참조 비교 분기를 따로 두지 않는다.
//
// 카메라는 무대마다 있다(StageRigSchema). 샷은 본 무대만 움직인다(StageRoles) — 덮는 무대(컷인 · 인서트 · 커튼)의
// 카메라와 층은 빌더가 세운 항등, 곧 중립에 고정이다(2026-11-06 D2). 무대별 intent가 필요해지면 이 클래스가 무대 키를 받으면 된다.
public sealed class PresentationShotResponseSystem : IStageViewNeutral
{
    private readonly StageRigRegistry _stages;
    private readonly StageRigRefs _main;
    private readonly LayerProfileCurve _profile;

    private readonly IntentTransition<PresentationIntentState> _transition;
    private PresentationIntentState _presented = PresentationIntentState.Default;

    // 저작된 의도 — 다음 샷 커맨드의 출발점이고, 폴드가 접는 값이다.
    public PresentationIntentState Intent => _transition.Intent;

    // 지금 화면에 적용된 값. 전환 중에만 Intent와 다르다.
    public PresentationIntentState Presented => _presented;

    public bool IsTransitioning => _transition.IsTransitioning;

    // 층이 샷에 반응하는 곡선 — Apply가 쓰는 것과 같다. shot_focus_to가 팬을 풀 때 같은 곡선을 본다.
    public LayerProfileCurve LayerProfile => _profile;

    public PresentationShotResponseSystem(StageRigRegistry stages, LayerProfileCurve profile)
    {
        _stages = stages;
        _main = stages.Get(StageRoles.MainStageKey);
        _profile = profile;

        _transition = new IntentTransition<PresentationIntentState>(
            PresentationIntentState.Default,
            () => _presented,
            state => Apply(state),
            (from, to, t) => PresentationShotIntentMath.Interpolate(from, to, t),
            this);
    }

    public void Follow(
        in PresentationIntentState intent,
        float duration,
        Ease ease)
    {
        // 이미 그 자리면 전환을 만들지 않는다 — 기다리는 커맨드가 빈 전환을 기다리지 않게.
        if (PresentationShotIntentMath.ApproximatelyEqual(_presented, intent))
        {
            Snap(intent);
            return;
        }

        _transition.Follow(intent, duration, ease);
    }

    // 압축(빨리 넘기기·시크) — 의도로 바로 맞춘다.
    public void Snap(in PresentationIntentState intent) => _transition.Snap(intent);

    // 스텝 경계 — 지금 전환을 배율만큼 빠르게 끝까지 돌린다.
    public void AccelerateTransition(float speedUp) => _transition.Accelerate(speedUp);

    // 장면 경계 — 전환을 확정 없이 멈추고 의도·화면을 기본 샷으로.
    public void Clear()
    {
        Snap(PresentationIntentState.Default);
    }

    // ── 적용 — 숫자 셋 → 무대 노드 ───────────────────────────────────

    private void Apply(in PresentationIntentState state)
    {
        _presented = state;

        ShotIntentState intent = state.ToCore();
        ApplyToStage(in intent, _main);
    }

    private void ApplyToStage(in ShotIntentState intent, StageRigRefs stage)
    {
        ShotLayoutMath.SolveCamera(in intent, out float cameraScale, out Vec2 pan);

        stage.CameraZoom.localScale = new Vector3(cameraScale, cameraScale, 1f);
        stage.CameraPan.anchoredPosition = pan.ToUnity();

        IReadOnlyList<StageLayerRefs> layers = stage.Layers;

        for (int i = 0; i < layers.Count; i++)
        {
            StageLayerRefs layer = layers[i];
            LayerResponse response = ShotLayoutMath.SolveLayer(in intent, layer.Depth, _profile);

            layer.Rect.anchoredPosition = OffsetInLayerParentSpace(stage, layer, response);
            layer.Rect.localScale = new Vector3(response.Scale.X, response.Scale.Y, 1f);
        }
    }

    // 층 오프셋은 rig space(무대 루트) 벡터다. 층의 부모(줌 루트)는 그 아래라 배율이 다르다 —
    // 점이 아니라 벡터로 옮겨 translation을 빼면 공통 카메라의 팬이 상쇄되고 배율만 남는다.
    private static Vector2 OffsetInLayerParentSpace(StageRigRefs stage, StageLayerRefs layer, in LayerResponse response)
        => PresentationCoordinateMath.ConvertVectorFromRigSpaceToTargetPositionParentSpace(
            response.OffsetInRigSpace.ToUnity(),
            stage.Root,
            (RectTransform)layer.Rect.parent);

    // ── 중립 = Solve(Default) ────────────────────────────────────────
    // 샷 소유는 구조다(무대마다 카메라 둘 · 층 전부). 덮는 무대의 것도 중립을 답한다 — 샷이 쓰지 않으니 라이브도 같은 항등이다.

    public bool TryGetNeutral(RectTransform rect, out Vector2 anchoredPosition, out Vector3 localScale)
    {
        anchoredPosition = default;
        localScale = Vector3.one;

        IReadOnlyList<StageRigRefs> stages = _stages.All;

        for (int i = 0; i < stages.Count; i++)
        {
            StageRigRefs stage = stages[i];

            if (!stage.IsShotOwned(rect, out bool isPan, out bool isZoom, out StageLayerRefs layer))
                continue;

            ShotIntentState neutral = ShotIntentState.Default;

            if (isPan || isZoom)
            {
                ShotLayoutMath.SolveCamera(in neutral, out float cameraScale, out Vec2 pan);

                if (isPan)
                    anchoredPosition = pan.ToUnity();
                else
                    localScale = new Vector3(cameraScale, cameraScale, 1f);

                return true;
            }

            LayerResponse response = ShotLayoutMath.SolveLayer(in neutral, layer.Depth, _profile);
            anchoredPosition = OffsetInLayerParentSpace(stage, layer, response);
            localScale = new Vector3(response.Scale.X, response.Scale.Y, 1f);

            return true;
        }

        return false;
    }
}
