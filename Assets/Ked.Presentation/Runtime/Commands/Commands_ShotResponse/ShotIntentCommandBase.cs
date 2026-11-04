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

// 샷 커맨드는 다음 의도를 계산해 넘기기만 한다 — 전환은 PresentationShotResponseSystem이 소유한다.
// 출발점이 화면에 보이는 값이 아니라 의도라서, 앞 전환이 얼마나 갔는지와 무관하게 결과가 같다.
public abstract class ShotIntentCommandBase<TSpec> : CommandBase
    where TSpec : ShotIntentCommandSpecBase
{
    private const float StepFinishSpeedUpMultiplier = 20f;

    protected readonly PresentationShotResponseSystem rig;
    protected readonly TSpec spec;

    public override bool WaitForCompletion => spec.wait;

    protected ShotIntentCommandBase(PresentationShotResponseSystem rig, TSpec spec)
    {
        this.rig = rig;
        this.spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        rig.Follow(BuildTargetState(rig.Intent, scope), spec.duration, spec.ease, spec.customCurveKeys);

        if (!spec.wait)
            yield break;

        while (rig.IsTransitioning)
            yield return null;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        rig.Snap(BuildTargetState(rig.Intent, scope));
    }

    protected abstract PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope);

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        rig.AccelerateTransition(StepFinishSpeedUpMultiplier);
    }
}