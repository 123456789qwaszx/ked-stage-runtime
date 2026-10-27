using System;
using UnityEngine;
using IEnumerator = System.Collections.IEnumerator;

[Serializable]
public sealed class HoldSignalCommandSpec : CommandSpecBase
{
    public string signalKey;
    public bool consume = true;
    
    [Header("최대 대기 시간 <=0이면 무제한 대기")]
    public float timeoutSeconds = -1f;  // <= 0이면 무제한 대기
    public bool respectTimeScale = true;
}

public sealed class HoldSignalCommand : CommandBase
{
    private readonly ISignalLatch _latch;
    private readonly ITimeSource _time;

    private readonly string _key;
    private readonly bool _consumeSignal;
    private readonly float _timeoutSeconds;
    private readonly bool _respectTimeScale;

    public HoldSignalCommand(
        ISignalLatch latch,
        ITimeSource time,
        string key,
        bool consume = true,
        float timeoutSeconds = -1f,
        bool respectTimeScale = true)
    {
        _latch = latch;
        _time = time;

        _key = key;
        _consumeSignal = consume;
        _timeoutSeconds = timeoutSeconds;
        _respectTimeScale = respectTimeScale;
    }

    public override bool WaitForCompletion => true;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (string.IsNullOrEmpty(_key) || _latch == null)
            yield break;

        if (IsSatisfied())
            yield break;

        bool hasTimeout = _timeoutSeconds > 0f;
        if (hasTimeout && _time == null)
        {
            Debug.LogWarning(
                $"[CpsHoldSignalCommand] timeoutSeconds>0 but TimeSource is null. Hold becomes infinite. key='{_key}'");
            hasTimeout = false;
        }

        float elapsed = 0f;

        while (true)
        {
            if (IsSatisfied())
                yield break;

            if (hasTimeout)
            {
                float dt = _time.UnscaledDeltaTime;
                if (_respectTimeScale)
                    dt *= (scope != null ? scope.TimeScale : 1f);

                elapsed += dt;
                if (elapsed >= _timeoutSeconds)
                    yield break;
            }

            yield return null;
        }
    }
    
    private bool IsSatisfied() => _consumeSignal 
        ? _latch.Consume(_key) 
        : _latch.IsLatched(_key);
}