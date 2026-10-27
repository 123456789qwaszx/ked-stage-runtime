using UnityEngine;

public sealed class CharRigSlotResolver
{
    private readonly IStageDepthContentSlotProvider _stageSlots;

    public CharRigSlotResolver(IStageDepthContentSlotProvider stageSlots)
    {
        _stageSlots = stageSlots;
    }

    public bool TryResolve(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        out RectTransform rect)
    {
        rect = null;

        if (_stageSlots == null)
            return false;

        rect = _stageSlots.GetDepthContent(stage, layer);

        return true;
    }
}