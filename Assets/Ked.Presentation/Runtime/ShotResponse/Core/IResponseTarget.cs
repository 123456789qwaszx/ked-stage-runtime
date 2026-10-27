using UnityEngine;

public interface IResponseTarget
{
    RectTransform MeasureRect { get; }
    RectTransform PositionRect { get; }
    RectTransform ScaleRect { get; }

    void ApplyResponse(in PresentationTargetResponse response);
}