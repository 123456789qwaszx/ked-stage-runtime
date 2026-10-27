using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
public sealed class FadeOutCommandSpecBgR : BackgroundRigCommandSpecBase
{
    [Header("Target")]
    public BackgroundRigTarget target = BackgroundRigTarget.Background_Root;

    [Tooltip("페이드 시간(초). 0 이하이면 즉시 스냅합니다.")]
    public float duration = 0.4f;

    public Ease ease = Ease.OutCubic;
}

public sealed class FadeOutCommandBgR : CanvasFadeCommandBase
{
    private readonly FadeOutCommandSpecBgR _spec;

    private readonly PresentationStage _stage;

    public FadeOutCommandBgR(PresentationStage stage, FadeOutCommandSpecBgR spec)
    {
        _stage = stage;
        _spec = spec;
    }

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;
    protected override Ease FadeEase => _spec.ease;
    protected override float TargetAlpha => 0f;
    protected override bool InteractableAfterCommit => false;

    protected override RectTransform ResolveFadeRect(CommandRunScope scope)
    {
        _stage.backgroundRigs.TryGetRig(_spec.rigKey, out BackgroundRigRefs rig);
        return rig?.GetRect(_spec.target);
    }
}