using System;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

[Serializable]
public class MoveByCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target (Track or Rig)")]
    public CharacterRigTarget target = CharacterRigTarget.CharSlot_Track;

    [Header("Move")]
    [Tooltip(
        "false: delta를 현재 anchoredPosition 기준 상대 오프셋으로 사용.\n" +
        "true: delta를 목표 anchoredPosition 절대값으로 사용.")]
    public bool useAbsolutePosition = false;

    [Tooltip(
        "useAbsolutePosition=false: 현재 anchoredPosition에 더해질 오프셋.\n" +
        "useAbsolutePosition=true: 이동할 목표 anchoredPosition.")]
    public Vector2 delta = Vector2.zero;

    [Header("Tween")]
    [Tooltip("트윈 시간. <= 0이면 즉시 dest로 스냅")]
    public float duration = 0.4f;

    public Ease ease = Ease.OutCubic;

    [Tooltip(
        "커스텀 이징 곡선 키(@이름 다섯째 인자에서). " +
        "null/빈 배열이면 ease를 쓴다. 종점(dest)에는 관여하지 않는다 — 모양만 바꾼다.")]
    public CurveKey[] customCurveKeys;
}

public sealed class MoveByCommandCharR : ClaimTweenCommandBase
{
    private readonly MoveByCommandSpecCharR _spec;

    private CharacterRigRefs _rigRefs;
    private RectTransform _rect;

    private Vector2 _startPos;
    private Vector2 _destPos;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    private readonly PresentationStage _stage;

    public MoveByCommandCharR(PresentationStage stage, MoveByCommandSpecCharR spec)
    {
        _stage = stage;
        _spec = spec;
    }

    protected override void ResolveTargets(CommandRunScope scope)
    {
        _rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(_stage, _spec.slotKey);
        _rect = _rigRefs?.GetRect(_spec.target);
    }

    protected override void ClaimTarget(CommandRunScope scope)
    {
        _rect.DOKill(true);
        _startPos = _rect.anchoredPosition;

        StageNodeClaim claim = MoveByReduction.Reduce(
            _rect.name,
            new MoveByReduction.Args(_spec.useAbsolutePosition, _spec.delta.ToCore()),
            _startPos.ToCore());

        _destPos = claim.Value.XY.ToUnity();
        _rigRefs.PlacementTargets.PublishAnchoredPosition(_rect, _destPos);
    }

    protected override Tween CreateTween(float duration)
        => _rect
            .DOAnchorPos(_destPos, duration)
            .ApplyEase(_spec.ease, _spec.customCurveKeys)
            .SetTarget(_rect);

    protected override void OnCommitFinalState()
    {
        _rect.anchoredPosition = _destPos;
        _rigRefs.PlacementTargets.Clear(_rect);
    }

    protected override float MeasureRemainingRatio()
        => RemainingRatio(
            Vector2.Distance(_startPos, _destPos),
            Vector2.Distance(_rect.anchoredPosition, _destPos));
}
