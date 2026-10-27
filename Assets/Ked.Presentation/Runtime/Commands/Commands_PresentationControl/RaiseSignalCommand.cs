using System;
using UnityEngine;
using IEnumerator = System.Collections.IEnumerator;

[Serializable]
public sealed class RaiseSignalCommandSpec : CommandSpecBase
{
    [Header("Signal")]
    public string signalKey;

    [Tooltip("true면 Skip에서도 신호를 발생시킴. (기본 true 추천)")]
    public bool raiseOnSkip = true;
}

public sealed class RaiseSignalCommand : CommandBase
{
    private readonly ISignalBus _signals;
    private readonly string _key;
    private readonly bool _raiseOnSkip;

    public RaiseSignalCommand(ISignalBus signals, string key, bool raiseOnSkip = true)
    {
        _signals = signals;
        _key = key;
        _raiseOnSkip = raiseOnSkip;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Raise();
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (_raiseOnSkip)
            Raise();
    }

    private void Raise()
    {
        if (_signals == null)
            return;

        if (string.IsNullOrWhiteSpace(_key))
            return;

        _signals.Raise(_key);
        Debug.Log($"Signal: {_key}");
    }
}