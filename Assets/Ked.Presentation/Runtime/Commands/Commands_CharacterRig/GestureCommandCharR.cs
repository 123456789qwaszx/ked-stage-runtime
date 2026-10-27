using System;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

[Serializable]
public sealed class GestureCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    [Tooltip(
        "제자리 몸짓 전용 축. move_by(CharSlot_Track)·place(Track_Focus)·넛지(Track_X/Y)와 " +
        "다른 노드라 DOKill 충돌 없이 겹친다 — 같은 라인에서 이동과 진동을 함께 쓸 수 있다.")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Shake;

    [Header("Amplitude (px)")]
    [Tooltip("축별 진폭(최대 변위). 부호는 곡선을 뒤집는다. 0이면 그 축은 내내 0이다.")]
    public Vector2 amplitude = Vector2.zero;

    [Header("Tween")]
    [Tooltip("0 이하이면 아무 일도 없다 — 순변위가 0이라 스냅할 것도 없다.")]
    public float duration = 0.5f;

    [Header("Oscillation")]
    [Tooltip(
        "가로 진동. 곡선 키가 있으면 키가 이기고, 없고 이징이 있으면 그 이징의 핑퐁, " +
        "둘 다 없으면 기본 혹(sin πt).")]
    public OscillationSource xOscillation;

    [Tooltip("세로 진동. 규칙은 가로와 같다.")]
    public OscillationSource yOscillation;
}

// ─────────────────────────────────────────────────────────────────────────────
// gesture — 제자리 몸짓. 순변위 0이 정체다.
//
// 변위(t) = (xAmp × xCurve(t), yAmp × yCurve(t)). 곡선이 (0,0)→(1,0)이라
// 시작도 끝도 제자리이고, 그래서 리듀서는 내용을 안 보고 무변으로 접는다
// ("이징은 종점에 관여하지 않는다"는 불변식이 유지된다).
//
// 축 곡선은 세 갈래다: @이름 진동 곡선 · 표준 이징의 핑퐁(왕복의 절반) · 기본 혹.
// 어느 쪽이든 (0,0)→(1,0)이라 순변위 0은 구조로 지켜진다.
//
// 트윈은 하나다: 0→1을 Linear로 흘리고 콜백에서 두 축을 각각 평가한다 —
// 축별 이징을 트윈 두 개 없이 얻는다(shot의 Interpolate 콜백과 같은 패턴).
// ─────────────────────────────────────────────────────────────────────────────
public sealed class GestureCommandCharR : ClaimTweenCommandBase
{
    private readonly GestureCommandSpecCharR _spec;

    private RectTransform _rect;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    // 진행률(0→1) 트윈이라 가속 재시작이 처음부터 다시 도는 것이다.
    // 게다가 도착이 곧 출발 자리(0,0)라 가속할 거리 자체가 없다 — 즉시 확정이 맞다.
    protected override bool AccelerateOnStepFinish => false;

    private readonly PresentationStage _stage;

    public GestureCommandCharR(PresentationStage stage, GestureCommandSpecCharR spec)
    {
        _stage = stage;
        _spec = spec;
    }

    protected override void ResolveTargets(CommandRunScope scope)
    {
        CharacterRigRefs rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(_stage, _spec.slotKey);
        _rect = rigRefs?.GetRect(_spec.target);
    }

    protected override void ClaimTarget(CommandRunScope scope)
    {
        // 앞 gesture를 완주(=제자리)시키고 시작한다. 장부 게시는 없다 —
        // 이 축의 정착값은 언제나 (0,0)이라 남이 알아야 할 것이 없다.
        _rect.DOKill(true);
    }

    protected override Tween CreateTween(float duration)
        => DOTween
            .To(() => 0f, ApplyProgress, 1f, duration)
            .SetEase(Ease.Linear)
            .SetTarget(_rect);

    protected override void OnCommitFinalState()
    {
        _rect.anchoredPosition = Vector2.zero;
    }

    // AccelerateOnStepFinish = false라 불리지 않지만, 계약은 정직하게 채운다.
    protected override float MeasureRemainingRatio() => 0f;

    private void ApplyProgress(float t)
    {
        _rect.anchoredPosition = new Vector2(
            _spec.amplitude.x * OscillationFunctions.Evaluate(_spec.xOscillation, t),
            _spec.amplitude.y * OscillationFunctions.Evaluate(_spec.yOscillation, t));
    }
}
