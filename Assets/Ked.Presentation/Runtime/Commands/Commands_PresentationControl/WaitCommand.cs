using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
public sealed class WaitCommandSpec : CommandSpecBase
{
    public float duration = 0.2f;
}

public sealed class WaitCommand : CommandBase
{
    private readonly WaitCommandSpec _spec;
    private Tween _tween;

    public override bool WaitForCompletion => true;

    public WaitCommand(WaitCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        float seconds = Mathf.Max(0f, _spec.duration);

        if (seconds <= 0f)
            yield break;

        _tween = DOTween
            .To(
                () => 0f,
                _ => { },
                1f,
                seconds)
            .SetEase(Ease.Linear)
            .SetUpdate(true);

        while (_tween != null &&
               _tween.IsActive() &&
               !_tween.IsComplete())
        {
            if (scope.Token.IsCancellationRequested)
            {
                _tween.Kill(false);
                _tween = null;
                yield break;
            }

            yield return null;
        }

        _tween = null;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (_tween != null && _tween.IsActive())
            _tween.Kill(false);

        _tween = null;
    }
}