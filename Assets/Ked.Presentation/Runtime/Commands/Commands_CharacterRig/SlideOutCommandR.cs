using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
public sealed class SlideOutCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target (Track)")]
    public CharacterRigTarget target = CharacterRigTarget.CharSlot_Track;

    [Header("Slide")]
    public CharRigDirection to = CharRigDirection.Right;
    public float distance = 480f;

    [Header("Tween")]
    public float duration = 0.45f;
    public Ease ease = Ease.InCubic;

    [Header("Juice (launch kick at the start)")]
    [Tooltip("0이면 심심한 SlideOut. 8~20 정도가 예쁘게 튐.")]
    public float punch = 14f;
}

public sealed class SlideOutCommandCharR : SlideCommandBase
{
    private readonly SlideOutCommandSpecCharR _spec;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    protected override Ease SlideEase => _spec.ease;
    protected override CharRigDirection SlideDirection => _spec.to;
    protected override float SlideDistance => _spec.distance;
    protected override float Punch => _spec.punch;

    protected override bool CurrentPositionIsDestination => false;

    private readonly PresentationStage _stage;

    public SlideOutCommandCharR(PresentationStage stage, SlideOutCommandSpecCharR spec)
    {
        _stage = stage;
        _spec = spec;
    }

    protected override float Bump(float easedProgress) => BumpFromStart(easedProgress);

    protected override RectTransform ResolveSlideRect(CommandRunScope scope)
    {
        CharacterRigRefs rig = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(_stage, _spec.slotKey);

        return rig?.GetRect(_spec.target);
    }
}