using System;
using System.Collections.Generic;
using UnityEngine;

public enum CleanupPolicy { Cancel, Finish }

/// <summary>
/// Tracks cleanup callbacks registered by commands (e.g., tweens, coroutines, event handlers).
/// Intended to be owned by CommandRunScope and cleaned up at a boundary (step or run).
/// </summary>
public sealed class LifetimeScope
{
    private readonly List<(Action cancel, Action finish)> _items = new();

    public void Track(Action cancel, Action finish = null)
    {
        if (cancel == null && finish == null) return;
        _items.Add((cancel, finish));
    }

    public void Cleanup(CleanupPolicy policy)
    {
        for (int i = _items.Count - 1; i >= 0; --i)
        {
            var (cancel, finish) = _items[i];
            try
            {
                if (policy == CleanupPolicy.Finish) 
                    (finish ?? cancel)?.Invoke();
                else cancel?.Invoke();
            }
            // 하나가 던져도 나머지 정리는 계속한다 — 다만 삼키지 않고 남긴다.
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        _items.Clear();
    }

    public void Clear() => _items.Clear();
}