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
            // 기다리는 도중 압축(빨리 넘기기·시크)이 켜지면 그 자리에서 끝낸다 —
            // 줄의 남은 커맨드도 압축되어 같은 프레임에 끝 상태로 가고, 그 뒤에야 관문이 다음 줄을 연다.
            if (scope.Token.IsCancellationRequested || scope.ShouldCompressCommandExecution)
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