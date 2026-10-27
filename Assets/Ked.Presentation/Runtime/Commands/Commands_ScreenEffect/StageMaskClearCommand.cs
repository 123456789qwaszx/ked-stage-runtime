using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public enum StageMaskClearMode
{
    UnmaskedFullVisible = 0,
    MaskedHidden = 1,
    MaskedFullRectVisible = 2,
}

[Serializable]
public sealed class StageMaskClearCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public PresentationStageKey stage = PresentationStageKey.Stage01;

    [Header("Clear")]
    public StageMaskClearMode mode = StageMaskClearMode.UnmaskedFullVisible;
    public bool hideEdge = true;
}

public sealed class StageMaskClearCommand : CommandBase
{
    private readonly StageMaskClearCommandSpec _spec;
    private readonly IStageMaskProvider _stageMaskProvider;

    private StageMaskSlot _slot;
    private StageMaskGraphic _graphic;
    private StageMaskEdgeGraphic _edgeGraphic;

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
        if (!_resolveAttempted)
            ResolveRefs();

        if (_slot == null)
            yield break;

        Apply();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (_slot == null)
            return;

        Apply();
    }

    private void ResolveRefs()
    {
        _resolveAttempted = true;
        
        IStageMaskProvider provider = _stageMaskProvider;

        if (!provider.TryGetStageMaskSlot(_spec.stage, out _slot) || _slot == null)
        {
            Debug.LogWarning(
                $"[StageMaskClearCommand] StageMaskSlot is missing. " +
                $"stage='{_spec.stage}'.");
            return;
        }

        _graphic = _slot.Graphic;
        _edgeGraphic = _slot.EdgeGraphic;
    }

    private void Apply()
    {
        if (_graphic != null)
            DOTween.Kill(_graphic, true);

        if (_edgeGraphic != null)
            DOTween.Kill(_edgeGraphic, true);

        switch (_spec.mode)
        {
            case StageMaskClearMode.UnmaskedFullVisible:
                _slot.SetFullVisible();
                break;

            case StageMaskClearMode.MaskedHidden:
                _slot.SetMaskedHidden();
                break;

            case StageMaskClearMode.MaskedFullRectVisible:
                _slot.SetMaskedFullRectVisible();
                break;
        }

        if (_spec.hideEdge)
            _slot.SetEdgeVisible(false);
    }
}
