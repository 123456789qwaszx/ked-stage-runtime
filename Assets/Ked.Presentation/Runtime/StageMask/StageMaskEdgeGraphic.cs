using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class StageMaskEdgeGraphic : Graphic
{
    [SerializeField] private StageMaskGraphic _source;
    [SerializeField] private StageMaskEdgeMode _edgeMode = StageMaskEdgeMode.Leading;
    [SerializeField] private float _thickness = 6f;

    private readonly List<StageMaskLineSegment> _segments = new();

    private StageMaskKind _lastKind;
    private Vector2 _lastOffset;
    private float _lastSlantPixels;
    private bool _lastSlantToRight;
    private bool _lastFlipVertical;
    private float _lastStripHeightPixels;
    private float _lastHorizontalBleedPixels;
    private float _lastVerticalStripWidthPixels;
    private float _lastVerticalBleedPixels;
    private float _lastDiagonalBandWidthPixels;
    private float _lastDiagonalBandSlantPixels;
    private float _lastDiagonalBandBleedPixels;
    private bool _lastDiagonalBandToRight;
    private float _lastIrisRadiusPixels;
    private float _lastIrisAspect;
    private int _lastIrisSegments;

    public StageMaskGraphic Source
    {
        get => _source;
        set
        {
            _source = value;
            CacheSourceState();
            SetVerticesDirty();
        }
    }

    public StageMaskEdgeMode EdgeMode
    {
        get => _edgeMode;
        set
        {
            if (_edgeMode == value)
                return;

            _edgeMode = value;
            SetVerticesDirty();
        }
    }

    public float Thickness
    {
        get => _thickness;
        set
        {
            value = Mathf.Max(0f, value);

            if (Mathf.Approximately(_thickness, value))
                return;

            _thickness = value;
            SetVerticesDirty();
        }
    }

    protected override void Awake()
    {
        base.Awake();

        raycastTarget = false;
        CacheSourceState();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        _thickness = Mathf.Max(0f, _thickness);
        raycastTarget = false;

        CacheSourceState();
        SetVerticesDirty();
    }
#endif

    private void LateUpdate()
    {
        if (_source == null)
            return;

        if (!HasSourceStateChanged())
            return;

        CacheSourceState();
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (_source == null)
            return;

        if (_edgeMode == StageMaskEdgeMode.None)
            return;

        _source.CollectEdgeSegments(_edgeMode, _segments);

        for (int i = 0; i < _segments.Count; i++)
        {
            StageMaskLineSegment segment = _segments[i];
            AddLineQuad(vh, segment.A, segment.B, _thickness);
        }
    }

    private void AddLineQuad(
        VertexHelper vh,
        Vector2 a,
        Vector2 b,
        float thickness)
    {
        if (thickness <= 0f)
            return;

        Vector2 dir = b - a;

        if (dir.sqrMagnitude <= 0.0001f)
            return;

        dir.Normalize();

        Vector2 normal = new(-dir.y, dir.x);
        Vector2 half = normal * (thickness * 0.5f);

        Vector2 p0 = a - half;
        Vector2 p1 = a + half;
        Vector2 p2 = b + half;
        Vector2 p3 = b - half;

        int startIndex = vh.currentVertCount;

        UIVertex v = UIVertex.simpleVert;
        v.color = color;

        v.position = p0;
        vh.AddVert(v);

        v.position = p1;
        vh.AddVert(v);

        v.position = p2;
        vh.AddVert(v);

        v.position = p3;
        vh.AddVert(v);

        vh.AddTriangle(startIndex + 0, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex + 0, startIndex + 2, startIndex + 3);
    }

    private bool HasSourceStateChanged()
    {
        if (_source == null)
            return false;

        return _lastKind != _source.Kind ||
               _lastOffset != _source.ShapeOffsetPixels ||
               !Mathf.Approximately(_lastSlantPixels, _source.SlantPixels) ||
               _lastSlantToRight != _source.SlantToRight ||
               _lastFlipVertical != _source.FlipVertical ||
               !Mathf.Approximately(_lastStripHeightPixels, _source.StripHeightPixels) ||
               !Mathf.Approximately(_lastHorizontalBleedPixels, _source.HorizontalBleedPixels) ||
               !Mathf.Approximately(_lastVerticalStripWidthPixels, _source.VerticalStripWidthPixels) ||
               !Mathf.Approximately(_lastVerticalBleedPixels, _source.VerticalBleedPixels) ||
               !Mathf.Approximately(_lastDiagonalBandWidthPixels, _source.DiagonalBandWidthPixels) ||
               !Mathf.Approximately(_lastDiagonalBandSlantPixels, _source.DiagonalBandSlantPixels) ||
               !Mathf.Approximately(_lastDiagonalBandBleedPixels, _source.DiagonalBandBleedPixels) ||
               _lastDiagonalBandToRight != _source.DiagonalBandToRight ||
               !Mathf.Approximately(_lastIrisRadiusPixels, _source.IrisRadiusPixels) ||
               !Mathf.Approximately(_lastIrisAspect, _source.IrisAspect) ||
               _lastIrisSegments != _source.IrisSegments;
    }

    private void CacheSourceState()
    {
        if (_source == null)
            return;

        _lastKind = _source.Kind;
        _lastOffset = _source.ShapeOffsetPixels;
        _lastSlantPixels = _source.SlantPixels;
        _lastSlantToRight = _source.SlantToRight;
        _lastFlipVertical = _source.FlipVertical;
        _lastStripHeightPixels = _source.StripHeightPixels;
        _lastHorizontalBleedPixels = _source.HorizontalBleedPixels;
        _lastVerticalStripWidthPixels = _source.VerticalStripWidthPixels;
        _lastVerticalBleedPixels = _source.VerticalBleedPixels;
        _lastDiagonalBandWidthPixels = _source.DiagonalBandWidthPixels;
        _lastDiagonalBandSlantPixels = _source.DiagonalBandSlantPixels;
        _lastDiagonalBandBleedPixels = _source.DiagonalBandBleedPixels;
        _lastDiagonalBandToRight = _source.DiagonalBandToRight;
        _lastIrisRadiusPixels = _source.IrisRadiusPixels;
        _lastIrisAspect = _source.IrisAspect;
        _lastIrisSegments = _source.IrisSegments;
    }
}