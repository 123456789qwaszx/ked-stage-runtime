using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 무대 키 → StageRigRefs. 조립이 무대를 세워 등록하고, 커맨드 · 샷 시스템 · 블러 · 슬롯 리졸버가 여기서 꺼낸다.
/// 없는 무대를 꺼내면 예외다 — 무대는 조립 때 전부 서 있고, 대본의 무대 키는 어휘가 정한다.
/// 종전 세 인터페이스(IShotResponseStageProvider · IStageDepthContentSlotProvider · IStageMaskProvider)를 이것 하나가 대신한다.
/// </summary>
public sealed class StageRigRegistry
{
    private readonly StageRigBuilder _builder;
    private readonly Dictionary<string, StageRigRefs> _stages = new(StringComparer.Ordinal);
    private readonly List<StageRigRefs> _ordered = new();

    public StageRigRegistry(StageRigBuilder builder)
    {
        _builder = builder;
    }

    public IReadOnlyList<StageRigRefs> All => _ordered;

    /// <summary>mount 아래에 무대를 세우고 등록한다. 같은 키가 이미 있으면 예외 — 무대는 한 번만 선다.</summary>
    public StageRigRefs Build(RectTransform mount, string stageKey, ReadOnlySpan<float> initialDepths)
    {
        if (_stages.ContainsKey(stageKey))
            throw new InvalidOperationException($"[StageRigRegistry] 무대 '{stageKey}'는 이미 서 있다.");

        StageRigRefs refs = _builder.Build(mount, stageKey, initialDepths);

        _stages[stageKey] = refs;
        _ordered.Add(refs);

        return refs;
    }

    public StageRigRefs Get(string stageKey)
        => _stages.TryGetValue(stageKey, out StageRigRefs refs)
            ? refs
            : throw new InvalidOperationException($"[StageRigRegistry] 무대 '{stageKey}'가 없다. 조립이 세운 무대: {string.Join(", ", _stages.Keys)}");

    /// <summary>무대의 깊이 d 층을 보장하고 돌려준다 — 리그를 붙이거나 블러를 걸 때.</summary>
    public StageLayerRefs EnsureLayer(string stageKey, float depth)
        => _builder.EnsureLayer(Get(stageKey), depth);

    /// <summary>장면 경계 — 무대마다 마스크를 기준 모양으로.</summary>
    public void ResetAllMasksToBaseline()
    {
        for (int i = 0; i < _ordered.Count; i++)
            _ordered[i].Mask.ResetToBaseline();
    }

    /// <summary>이 rect를 품은 무대. 어느 무대에도 없으면 예외 — 무대 밖의 것을 무대 좌표로 재려는 것이다.</summary>
    public StageRigRefs StageOf(Transform rect)
    {
        for (int i = 0; i < _ordered.Count; i++)
        {
            if (rect.IsChildOf(_ordered[i].Root))
                return _ordered[i];
        }

        throw new InvalidOperationException($"[StageRigRegistry] '{rect.name}'은 어느 무대 아래에도 없다.");
    }
}
