using UnityEngine;

public interface IStageDepthContentSlotProvider
{
    RectTransform GetDepthContent(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer);
}