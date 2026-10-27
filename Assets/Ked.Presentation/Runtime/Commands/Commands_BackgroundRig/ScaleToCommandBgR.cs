using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
public sealed class ScaleToCommandSpecBgR : BackgroundRigCommandSpecBase
{
    [Header("Target")]
    public BackgroundRigTarget target = BackgroundRigTarget.Background_Scale;

    [Header("Scale (XY)")]
    public Vector2 toScale = Vector2.one;

    [Header("From")]
    public bool overrideFromScale = false;
    public Vector2 fromScale = Vector2.one;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;
}

public sealed class ScaleToCommandBgR : ClaimTweenCommandBase
{
    private readonly ScaleToCommandSpecBgR _spec;

    private RectTransform _rect;

    private Vector2 _startScale;
    private Vector2 _targetScale;
    private Vector3 _endScale;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    private readonly PresentationStage _stage;

    public ScaleToCommandBgR(PresentationStage stage, ScaleToCommandSpecBgR spec)
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

        if (_spec.overrideFromScale)
            ApplyScaleXY(_rect, _spec.fromScale);

        Vector3 currentScale = _rect.localScale;

        _startScale = new Vector2(currentScale.x, currentScale.y);
        _targetScale = _spec.toScale;

        _endScale = currentScale;
        _endScale.x = _targetScale.x;
        _endScale.y = _targetScale.y;
    }

    protected override Tween CreateTween(float duration)
        => _rect
            .DOScale(_endScale, duration)
            .SetEase(_spec.ease)
            .SetTarget(_rect);

    protected override void OnCommitFinalState()
    {
        ApplyScaleXY(_rect, _targetScale);
    }

    protected override float MeasureRemainingRatio()
    {
        Vector3 current = _rect.localScale;

        return RemainingRatio(
            Vector2.Distance(_startScale, _targetScale),
            Vector2.Distance(new Vector2(current.x, current.y), _targetScale));
    }

    private static void ApplyScaleXY(RectTransform rect, Vector2 targetXY)
    {
        Vector3 scale = rect.localScale;
        scale.x = targetXY.x;
        scale.y = targetXY.y;
        rect.localScale = scale;
    }
}