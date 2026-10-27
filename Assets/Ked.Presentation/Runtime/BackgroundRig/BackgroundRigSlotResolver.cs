using UnityEngine;

public sealed class BackgroundRigSlotResolver
{
    private readonly IStageDepthContentSlotProvider _slots;

    public BackgroundRigSlotResolver(IStageDepthContentSlotProvider slots)
    {
        _slots = slots;
    }

    public bool TryResolve(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        out RectTransform parent)
    {
        parent = _slots?.GetDepthContent(stage, layer);
        return parent != null;
    }
}