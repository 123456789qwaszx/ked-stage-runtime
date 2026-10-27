using System;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

[Serializable]
public sealed class RotateByCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharSlot_Rotation;

    [Header("Rotation Delta")]
    public Vector3 deltaEuler = Vector3.zero;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;

    [Tooltip("커스텀 이징 곡선 키(@이름 인자에서). null/빈 배열이면 ease를 쓴다.")]
    public CurveKey[] customCurveKeys;
}

public sealed class RotateByCommandCharR : ClaimTweenCommandBase
{
    private readonly RotateByCommandSpecCharR _spec;

    private RectTransform _rect;

    private Vector3 _startEuler;
    private Vector3 _destEuler;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    // 진행률(0→1) 트윈이라 가속 재시작이 처음부터 다시 도는 것이다 —
    // 스텝 경계에서는 종전대로 즉시 확정한다.
    protected override bool AccelerateOnStepFinish => false;

    private readonly PresentationStage _stage;

    public RotateByCommandCharR(PresentationStage stage, RotateByCommandSpecCharR spec)
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
        _rect.DOKill(true);

        _startEuler = _rect.localEulerAngles;
        _destEuler = _startEuler + _spec.deltaEuler;
    }

    protected override Tween CreateTween(float duration)
        => DOTween
            .To(() => 0f, ApplyProgress, 1f, duration)
            .ApplyEase(_spec.ease, _spec.customCurveKeys)
            .SetTarget(_rect);

    protected override void OnCommitFinalState()
    {
        _rect.localRotation = Quaternion.Euler(_destEuler);
    }

    // AccelerateOnStepFinish = false라 불리지 않지만, 계약은 정직하게 채운다.
    protected override float MeasureRemainingRatio()
        => RemainingRatio(_spec.deltaEuler.magnitude, 0f);

    private void ApplyProgress(float progress)
    {
        Vector3 euler = Vector3.LerpUnclamped(_startEuler, _destEuler, progress);
        _rect.localRotation = Quaternion.Euler(euler);
    }
}
