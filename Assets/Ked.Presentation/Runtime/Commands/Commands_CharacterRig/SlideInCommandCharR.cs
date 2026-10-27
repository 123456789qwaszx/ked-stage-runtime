using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
public sealed class SlideInCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target (Track)")]
    public CharacterRigTarget target = CharacterRigTarget.CharSlot_Track;

    [Header("Slide")]
    public CharRigDirection direction = CharRigDirection.Left;
    public float distance = 480f;

    [Header("Tween")]
    public float duration = 0.55f;
    public Ease ease = Ease.OutCubic;

    [Header("(overshoot that settles back)")]
    [Tooltip("0이면 일반 SlideIn에 가까워짐.")]
    public float punch = 24f;
}

public sealed class SlideInCommandCharR : SlideCommandBase
{
    private readonly SlideInCommandSpecCharR _spec;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    protected override Ease SlideEase => _spec.ease;
    protected override CharRigDirection SlideDirection => _spec.direction;
    protected override float SlideDistance => _spec.distance;
    protected override float Punch => _spec.punch;

    protected override bool CurrentPositionIsDestination => true;

    private readonly PresentationStage _stage;

    public SlideInCommandCharR(PresentationStage stage, SlideInCommandSpecCharR spec)
    {
        _stage = stage;
        _spec = spec;
    }

    protected override float Bump(float easedProgress) => BumpTowardEnd(easedProgress);

    protected override RectTransform ResolveSlideRect(CommandRunScope scope)
    {
        CharacterRigRefs rig = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(_stage, _spec.slotKey);

        return rig?.GetRect(_spec.target);
    }
}
