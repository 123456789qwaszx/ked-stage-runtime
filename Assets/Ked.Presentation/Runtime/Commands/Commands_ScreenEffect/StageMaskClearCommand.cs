using System;
using System.Collections;
using UnityEngine;

[Serializable]
public sealed class StageMaskClearCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public PresentationStageKey stage = PresentationStageKey.Stage01;

    [Header("Clear")]
    public StageMaskClearMode mode = StageMaskClearMode.UnmaskedFullVisible;
    public bool hideEdge = true;
}

// 무대 마스크를 정해진 모양으로 바로 둔다 — 진행 중인 tx 모션은 슬롯이 끊는다.
public sealed class StageMaskClearCommand : CommandBase
{
    private readonly StageMaskClearCommandSpec _spec;
    private readonly IStageMaskProvider _stageMaskProvider;

    private StageMaskSlot _slot;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => false;

    public StageMaskClearCommand(
        StageMaskClearCommandSpec spec,
        IStageMaskProvider stageMaskProvider)
    {
        _spec = spec;
        _stageMaskProvider = stageMaskProvider;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Apply();
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        Apply();
    }

    private void Apply()
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (_slot == null)
            return;

        _slot.Clear(_spec.mode, _spec.hideEdge);
    }

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        if (!_stageMaskProvider.TryGetStageMaskSlot(_spec.stage, out _slot) || _slot == null)
        {
            Debug.LogWarning(
                $"[StageMaskClearCommand] StageMaskSlot is missing. " +
                $"stage='{_spec.stage}'.");
        }
    }
}
