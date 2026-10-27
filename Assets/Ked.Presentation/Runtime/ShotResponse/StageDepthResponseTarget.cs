using UnityEngine;

// MeasureRect(Root)
// - response 영향을 받지 않는 중립 기준점.
// - bind 시점에 PresentationResponseCoordinateMapper.CaptureBaseMeasure()로 한 번만 측정되어야 한다.
//
// PositionRect(FramingTransform)
// - pan-follow + focus-spread offset을 anchoredPosition으로 받는 노드.
//
// ScaleRect(FramingScale)
// - zoomScaleResponse를 localScale로 받는 노드.
public sealed class StageDepthResponseTarget : IResponseTarget
{
    private readonly RectTransform _measure;
    private readonly RectTransform _position;
    private readonly RectTransform _scale;

    public RectTransform MeasureRect => _measure;
    public RectTransform PositionRect => _position;
    public RectTransform ScaleRect => _scale;

    public StageDepthResponseTarget(
        RectTransform measure,
        RectTransform position,
        RectTransform scale)
    {
        _measure = measure;
        _position = position;
        _scale = scale;
    }

    public void ApplyResponse(in PresentationTargetResponse response)
    {
        if (_position != null)
            _position.anchoredPosition = response.anchoredPosition;

        if (_scale != null)
            _scale.localScale = new Vector3(response.scale.x, response.scale.y, 1f);
    }
}