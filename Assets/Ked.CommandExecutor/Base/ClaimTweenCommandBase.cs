using System.Collections;
using DG.Tweening;

/// <summary>
/// "클레임-트윈" 커맨드의 공통 수명 —
/// 57곳에 흩어져 있던 미니 상태머신(ResolveRefs → ClaimTarget → 트윈 → Commit /
/// OnSkip / OnStepLifetimeFinished 가속)을 한 곳으로 모은 것.
/// </summary>
public abstract class ClaimTweenCommandBase : CommandBase
{
    private Tween _tween;
    private bool _resolved;

    private bool _hasClaimedTarget;

    // 확정할 때 함께 풀 예약. ClaimTarget에서 예약을 거는 쪽(장부)이 스스로 등록한다.
    protected CommitReleases Releases { get; } = new();

    // 0이하일 시 즉시 스냅
    protected abstract float TweenDuration { get; }

    // 대상 해석(한 번만 불린다). 이 훅과 ClaimTarget은 스코프를 받지 않는다 — 무대는 커맨드가 생성자로 받고,
    // 스코프의 실행 상태(취소 · 압축 · 스텝 경계)는 이 기반이 다룬다.
    protected abstract void ResolveRefs();

    // DOKill -> (from-override) -> 목표 계산(코어)과 장부 예약. 타깃은 언제나 나온다.
    protected abstract void ClaimTarget();

    // ease, SetTarget은 구현 커맨드가 직접. (SetUpdate,OnComplete는 Base측 정책)
    protected abstract Tween CreateTween(float duration);

    // 최종 쓰기. (수명 플래그 정리는 Base측 정책)
    protected abstract void OnCommitFinalState();

    // 스텝 경계에서 남은 트윈을 몇 배로 돌릴지.
    private const float StepFinishSpeedUp = 30f;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolved)
        {
            _resolved = true;
            ResolveRefs();
        }
        
        ClaimTarget();
        _hasClaimedTarget = true;

        float duration = TweenDuration;

        if (duration <= 0f)
        {
            Commit();
            yield break;
        }

        _tween = CreateTween(duration)
            .SetUpdate(true)
            .OnComplete(Commit);

        if (WaitForCompletion)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip()
    {
        if (!_resolved)
        {
            _resolved = true;
            ResolveRefs();
        }

        if (!_hasClaimedTarget)
        {
            ClaimTarget();
            _hasClaimedTarget = true;
        }

        Commit();
    }

    protected override void OnStepLifetimeFinished()
    {
        if (!_hasClaimedTarget)
            return;

        _tween.timeScale = StepFinishSpeedUp;
    }

    private void Commit()
    {
        OnCommitFinalState();
        Releases.ReleaseAll();

        _hasClaimedTarget = false;
        _tween = null;
    }
}