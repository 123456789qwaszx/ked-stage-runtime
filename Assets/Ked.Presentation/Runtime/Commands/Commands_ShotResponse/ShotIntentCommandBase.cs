using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
public abstract class ShotIntentCommandSpecBase : CommandSpecBase
{
    [Header("Tween")]
    [Tooltip("0 이하이면 즉시 스냅합니다.")]
    public float duration = 0.45f;

    public Ease ease = Ease.OutCubic;

    [Tooltip(
        "커스텀 이징 곡선 키(@이름 인자에서). null/빈 배열이면 ease를 쓴다. " +
        "종점(intent)에는 관여하지 않는다 — 모양만 바꾼다.")]
    public Ked.Presentation.Core.CurveKey[] customCurveKeys;
}

public abstract class ShotIntentCommandBase<TSpec> : CommandBase
    where TSpec : ShotIntentCommandSpecBase
{
    private const float StepFinishSpeedUpMultiplier = 20f;

    protected readonly PresentationShotResponseSystem rig;
    protected readonly TSpec spec;

    private PresentationIntentState _fromState;
    private PresentationIntentState _toState;

    private Tween _tween;

    private bool HasClaimedRig { get; set; }

    public override bool WaitForCompletion => spec.wait;

    protected ShotIntentCommandBase(PresentationShotResponseSystem rig, TSpec spec)
    {
        this.rig = rig;
        this.spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        ClaimRig(scope);

        if (spec.duration <= 0f || PresentationShotIntentMath.ApproximatelyEqual(_fromState, _toState))
        {
            CommitFinalState();
            yield break;
        }

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    PresentationIntentState state =
                        PresentationShotIntentMath.Interpolate(_fromState, _toState, t);

                    rig.ApplyToAllBindings(state);
                },
                1f,
                spec.duration)
            .ApplyEase(spec.ease, spec.customCurveKeys)
            .SetUpdate(true)
            .SetTarget(rig)
            .OnComplete(CommitFinalState);

        if (spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!HasClaimedRig)
            ClaimRig(scope);

        CommitFinalState();
    }

    protected abstract PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope);

    private void ClaimRig(CommandRunScope scope)
    {
        DOTween.Kill(rig, true);

        _fromState = rig.CurrentState;
        _toState = BuildTargetState(_fromState, scope);

        HasClaimedRig = true;
    }

    private void CommitFinalState()
    {
        rig.ApplyToAllBindings(_toState);

        HasClaimedRig = false;
        _tween = null;
    }

    #region StepLifetimeHook

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!HasClaimedRig)
            return;
        
        _tween.Kill(false);

        PresentationIntentState currentState = rig.CurrentState;
        float duration = CalculateAcceleratedRemainingDuration(in currentState);

        _fromState = currentState;

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    PresentationIntentState state =
                        PresentationShotIntentMath.Interpolate(_fromState, _toState, t);

                    rig.ApplyToAllBindings(state);
                },
                1f,
                duration)
            .ApplyEase(spec.ease, spec.customCurveKeys)
            .SetUpdate(true)
            .SetTarget(rig)
            .OnComplete(CommitFinalState);
    }

    private float CalculateAcceleratedRemainingDuration(
        in PresentationIntentState currentState)
    {
        float zoomRatio = CalculateScalarRemainingRatio(
            _fromState.zoom,
            _toState.zoom,
            currentState.zoom);

        float panRatio = CalculateVectorRemainingRatio(
            _fromState.panInRigSpace,
            _toState.panInRigSpace,
            currentState.panInRigSpace);

        float focusRatio = CalculateVectorRemainingRatio(
            _fromState.focusPointInRigSpace,
            _toState.focusPointInRigSpace,
            currentState.focusPointInRigSpace);

        float remainingRatio = Mathf.Max(zoomRatio, panRatio, focusRatio);
        float remainingDuration = spec.duration * remainingRatio;

        return Mathf.Max(0.01f, remainingDuration / StepFinishSpeedUpMultiplier);
    }

    private static float CalculateScalarRemainingRatio(
        float from,
        float to,
        float current)
    {
        float originalDistance = Mathf.Abs(to - from);
        float remainingDistance = Mathf.Abs(to - current);

        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        return Mathf.Clamp01(remainingDistance / originalDistance);
    }

    private static float CalculateVectorRemainingRatio(
        Vector2 from,
        Vector2 to,
        Vector2 current)
    {
        float originalDistance = Vector2.Distance(from, to);
        float remainingDistance = Vector2.Distance(current, to);

        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        return Mathf.Clamp01(remainingDistance / originalDistance);
    }

    #endregion
}