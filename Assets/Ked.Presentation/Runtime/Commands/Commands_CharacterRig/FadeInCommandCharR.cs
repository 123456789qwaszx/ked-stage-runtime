using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
public class FadeInCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.RigRoot;

    [Tooltip("페이드 시간(초). 0 이하이면 즉시 스냅합니다.")]
    public float duration = 0.4f;

    public Ease ease = Ease.OutCubic;
}

public sealed class FadeInCommandCharR : CanvasFadeCommandBase
{
    private readonly FadeInCommandSpecCharR _spec;

    private readonly PresentationStage _stage;

    public FadeInCommandCharR(PresentationStage stage, FadeInCommandSpecCharR spec)
    {
        _stage = stage;
        _spec = spec;
    }

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;
    protected override Ease FadeEase => _spec.ease;

    // 목표 alpha의 원천은 코어 리덕션이다 — 트윈 종점과 정지 프레임 폴드가 같은 값을 본다.
    protected override float TargetAlpha => Ked.Presentation.Core.FadeInReduction.TargetAlpha;

    protected override bool InteractableAfterCommit => true;

    protected override RectTransform ResolveFadeRect(CommandRunScope scope)
        => CharacterRigTargetResolver.ResolveCharRigFromTargetKey(_stage, _spec.slotKey)?.GetRect(_spec.target);
}
