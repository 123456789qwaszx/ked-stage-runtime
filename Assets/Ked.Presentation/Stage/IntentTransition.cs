using System;
using DG.Tweening;

// 장면 전역 의도 하나와 그것을 따라가는 전환 — 의도는 커맨드가 쓰고, 화면은 이것이 따라간다.
// 전환은 하나뿐이다. 새 의도가 오면 지금 보이는 값에서 다시 조준한다(앞 전환을 완주시키지 않는다).
public sealed class IntentTransition<TState>
{
    private readonly Func<TState> _readPresented;
    private readonly Action<TState> _apply;
    private readonly Func<TState, TState, float, TState> _lerp;
    private readonly object _tweenTarget;

    private Tween _tween;

    public IntentTransition(
        TState initial,
        Func<TState> readPresented,
        Action<TState> apply,
        Func<TState, TState, float, TState> lerp,
        object tweenTarget)
    {
        Intent = initial;
        _readPresented = readPresented;
        _apply = apply;
        _lerp = lerp;
        _tweenTarget = tweenTarget;
    }

    public TState Intent { get; private set; }

    public bool IsTransitioning => _tween != null;

    public void Follow(TState intent, float duration, Ease ease)
    {
        Intent = intent;
        Stop();

        if (duration <= 0f)
        {
            _apply(intent);
            return;
        }

        StartTween(duration, ease);
    }

    // 압축·초기화 — 의도로 바로 맞춘다.
    public void Snap(TState intent)
    {
        Intent = intent;
        Stop();
        _apply(intent);
    }

    // 스텝 경계 — 지금 전환을 배율만큼 빠르게 끝까지 돌린다. 같은 곡선을 이어 가고, 남은 시간은 트윈이 안다.
    // 끝값은 Intent가 쥐고 있으니 가속은 보이는 연출일 뿐이다.
    public void Accelerate(float speedUp)
    {
        if (_tween == null)
            return;

        _tween.timeScale = speedUp;
    }

    // 확정 없이 지금 자리에서 멈춘다.
    public void Stop()
    {
        _tween?.Kill(false);
        _tween = null;
    }

    private void StartTween(float duration, Ease ease)
    {
        TState from = _readPresented();
        TState to = Intent;

        _tween = DOTween
            .To(() => 0f, t => _apply(_lerp(from, to, t)), 1f, duration)
            .SetEase(ease)
            .SetUpdate(true)
            .SetTarget(_tweenTarget)
            .OnComplete(() =>
            {
                _tween = null;
                _apply(to);
            });
    }
}
