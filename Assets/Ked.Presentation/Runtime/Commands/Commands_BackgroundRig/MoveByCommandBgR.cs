using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
public sealed class MoveByCommandSpecBgR : BackgroundRigCommandSpecBase
{
    [Header("Target")]
    public BackgroundRigTarget target = BackgroundRigTarget.Background_Track_Move;

    [Header("Delta (relative offset)")]
    [Tooltip("현재 anchoredPosition 기준으로 더해질 오프셋(픽셀 단위).")]
    public Vector2 delta = Vector2.zero;

    [Header("Tween")]
    [Tooltip("트윈 시간. <= 0이면 즉시 dest로 스냅")]
    public float duration = 0.4f;

    public Ease ease = Ease.OutCubic;
}

public sealed class MoveByCommandBgR : ClaimTweenCommandBase
{
    private readonly MoveByCommandSpecBgR _spec;

    private RectTransform _rect;
    private Vector2 _startPos;
    private Vector2 _destPos;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    private readonly PresentationStage _stage;

    public MoveByCommandBgR(PresentationStage stage, MoveByCommandSpecBgR spec)
    {
        _stage = stage;
        _spec = spec;
    }

    protected override void ResolveTargets(CommandRunScope scope)
    {
        _stage.backgroundRigs.TryGetRig(_spec.rigKey, out BackgroundRigRefs rig);
        _rect = rig?.GetRect(_spec.target);
    }

    protected override void ClaimTarget(CommandRunScope scope)
    {
        _rect.DOKill(true);

        _startPos = _rect.anchoredPosition;
        _destPos = _startPos + _spec.delta;
    }

    protected override Tween CreateTween(float duration)
        => _rect
            .DOAnchorPos(_destPos, duration)
            .SetEase(_spec.ease)
            .SetTarget(_rect);

    protected override void OnCommitFinalState()
    {
        _rect.anchoredPosition = _destPos;
    }

    protected override float MeasureRemainingRatio()
        => RemainingRatio(
            Vector2.Distance(_startPos, _destPos),
            Vector2.Distance(_rect.anchoredPosition, _destPos));
}