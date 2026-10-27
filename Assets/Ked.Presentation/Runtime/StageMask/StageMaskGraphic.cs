using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
[RequireComponent(typeof(Mask))]
public sealed class StageMaskGraphic : Graphic
{
    private const int MinCircleSegments = 12;
    private const int MaxCircleSegments = 128;

    [Header("Common")]
    [SerializeField] private StageMaskKind _kind = StageMaskKind.FullRect;
    [SerializeField] private Vector2 _shapeOffsetPixels;
    [SerializeField] private Vector2 _hiddenOffsetPixels = new(-2200f, 0f);
    [SerializeField] private bool _hideMaskGraphic = true;

    [Header("Slanted")]
    [SerializeField] private float _slantPixels = 220f;
    [SerializeField] private bool _slantToRight = true;
    [SerializeField] private bool _flipVertical;

    [Header("Horizontal Strip")]
    [SerializeField] private float _stripHeightPixels = 320f;
    [SerializeField] private float _horizontalBleedPixels = 64f;

    [Header("Vertical Strip")]
    [SerializeField] private float _verticalStripWidthPixels = 420f;
    [SerializeField] private float _verticalBleedPixels = 64f;

    [Header("Diagonal Band")]
    [SerializeField] private float _diagonalBandWidthPixels = 620f;
    [SerializeField] private float _diagonalBandSlantPixels = 420f;
    [SerializeField] private float _diagonalBandBleedPixels = 220f;
    [SerializeField] private bool _diagonalBandToRight = true;

    [Header("Circle Iris")]
    [SerializeField] private float _irisRadiusPixels = 720f;
    [SerializeField] private float _irisAspect = 1f;
    [SerializeField, Range(MinCircleSegments, MaxCircleSegments)]
    private int _irisSegments = 64;

    public StageMaskKind Kind
    {
        get => _kind;
        set
        {
            if (_kind == value)
                return;

            _kind = value;
            SetVerticesDirty();
        }
    }

    public Vector2 ShapeOffsetPixels
    {
        get => _shapeOffsetPixels;
        set
        {
            if (_shapeOffsetPixels == value)
                return;

            _shapeOffsetPixels = value;
            SetVerticesDirty();
        }
    }

    public Vector2 HiddenOffsetPixels
    {
        get => _hiddenOffsetPixels;
        set => _hiddenOffsetPixels = value;
    }

    public bool HideMaskGraphic
    {
        get => _hideMaskGraphic;
        set
        {
            if (_hideMaskGraphic == value)
                return;

            _hideMaskGraphic = value;
            ApplyMaskGraphicVisibility();
        }
    }

    public float SlantPixels
    {
        get => _slantPixels;
        set
        {
            value = Mathf.Max(0f, value);

            if (Mathf.Approximately(_slantPixels, value))
                return;

            _slantPixels = value;
            SetVerticesDirty();
        }
    }

    public bool SlantToRight
    {
        get => _slantToRight;
        set
        {
            if (_slantToRight == value)
                return;

            _slantToRight = value;
            SetVerticesDirty();
        }
    }

    public bool FlipVertical
    {
        get => _flipVertical;
        set
        {
            if (_flipVertical == value)
                return;

            _flipVertical = value;
            SetVerticesDirty();
        }
    }

    public float StripHeightPixels
    {
        get => _stripHeightPixels;
        set
        {
            value = Mathf.Max(0f, value);

            if (Mathf.Approximately(_stripHeightPixels, value))
                return;

            _stripHeightPixels = value;
            SetVerticesDirty();
        }
    }

    public float HorizontalBleedPixels
    {
        get => _horizontalBleedPixels;
        set
        {
            value = Mathf.Max(0f, value);

            if (Mathf.Approximately(_horizontalBleedPixels, value))
                return;

            _horizontalBleedPixels = value;
            SetVerticesDirty();
        }
    }

    public float VerticalStripWidthPixels
    {
        get => _verticalStripWidthPixels;
        set
        {
            value = Mathf.Max(0f, value);

            if (Mathf.Approximately(_verticalStripWidthPixels, value))
                return;

            _verticalStripWidthPixels = value;
            SetVerticesDirty();
        }
    }

    public float VerticalBleedPixels
    {
        get => _verticalBleedPixels;
        set
        {
            value = Mathf.Max(0f, value);

            if (Mathf.Approximately(_verticalBleedPixels, value))
                return;

            _verticalBleedPixels = value;
            SetVerticesDirty();
        }
    }

    public float DiagonalBandWidthPixels
    {
        get => _diagonalBandWidthPixels;
        set
        {
            value = Mathf.Max(0f, value);

            if (Mathf.Approximately(_diagonalBandWidthPixels, value))
                return;

            _diagonalBandWidthPixels = value;
            SetVerticesDirty();
        }
    }

    public float DiagonalBandSlantPixels
    {
        get => _diagonalBandSlantPixels;
        set
        {
            if (Mathf.Approximately(_diagonalBandSlantPixels, value))
                return;

            _diagonalBandSlantPixels = value;
            SetVerticesDirty();
        }
    }

    public float DiagonalBandBleedPixels
    {
        get => _diagonalBandBleedPixels;
        set
        {
            value = Mathf.Max(0f, value);

            if (Mathf.Approximately(_diagonalBandBleedPixels, value))
                return;

            _diagonalBandBleedPixels = value;
            SetVerticesDirty();
        }
    }

    public bool DiagonalBandToRight
    {
        get => _diagonalBandToRight;
        set
        {
            if (_diagonalBandToRight == value)
                return;

            _diagonalBandToRight = value;
            SetVerticesDirty();
        }
    }

    public float IrisRadiusPixels
    {
        get => _irisRadiusPixels;
        set
        {
            value = Mathf.Max(0f, value);

            if (Mathf.Approximately(_irisRadiusPixels, value))
                return;

            _irisRadiusPixels = value;
            SetVerticesDirty();
        }
    }

    public float IrisAspect
    {
        get => _irisAspect;
        set
        {
            value = Mathf.Max(0.001f, value);

            if (Mathf.Approximately(_irisAspect, value))
                return;

            _irisAspect = value;
            SetVerticesDirty();
        }
    }

    public int IrisSegments
    {
        get => _irisSegments;
        set
        {
            value = Mathf.Clamp(value, MinCircleSegments, MaxCircleSegments);

            if (_irisSegments == value)
                return;

            _irisSegments = value;
            SetVerticesDirty();
        }
    }

    protected override void Awake()
    {
        base.Awake();

        raycastTarget = false;
        ApplyMaskGraphicVisibility();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        ApplyMaskGraphicVisibility();
        SetVerticesDirty();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        _slantPixels = Mathf.Max(0f, _slantPixels);
        _stripHeightPixels = Mathf.Max(0f, _stripHeightPixels);
        _horizontalBleedPixels = Mathf.Max(0f, _horizontalBleedPixels);
        _verticalStripWidthPixels = Mathf.Max(0f, _verticalStripWidthPixels);
        _verticalBleedPixels = Mathf.Max(0f, _verticalBleedPixels);
        _diagonalBandWidthPixels = Mathf.Max(0f, _diagonalBandWidthPixels);
        _diagonalBandBleedPixels = Mathf.Max(0f, _diagonalBandBleedPixels);
        _irisRadiusPixels = Mathf.Max(0f, _irisRadiusPixels);
        _irisAspect = Mathf.Max(0.001f, _irisAspect);
        _irisSegments = Mathf.Clamp(_irisSegments, MinCircleSegments, MaxCircleSegments);

        raycastTarget = false;

        ApplyMaskGraphicVisibility();
        SetVerticesDirty();
    }
#endif

    public void ResetToHiddenOffset()
    {
        ShapeOffsetPixels = _hiddenOffsetPixels;
    }

    public void SetShapeOffsetImmediate(Vector2 offset)
    {
        ShapeOffsetPixels = offset;
    }

    public void SetFullRect()
    {
        _kind = StageMaskKind.FullRect;
        SetVerticesDirty();
    }

    public void SetSlanted(
        float slantPixels,
        bool slantToRight,
        bool flipVertical)
    {
        _kind = StageMaskKind.Slanted;
        _slantPixels = Mathf.Max(0f, slantPixels);
        _slantToRight = slantToRight;
        _flipVertical = flipVertical;

        SetVerticesDirty();
    }

    public void SetHorizontalStrip(
        float heightPixels,
        float horizontalBleedPixels)
    {
        _kind = StageMaskKind.HorizontalStrip;
        _stripHeightPixels = Mathf.Max(0f, heightPixels);
        _horizontalBleedPixels = Mathf.Max(0f, horizontalBleedPixels);

        SetVerticesDirty();
    }

    public void SetVerticalStrip(
        float widthPixels,
        float verticalBleedPixels)
    {
        _kind = StageMaskKind.VerticalStrip;
        _verticalStripWidthPixels = Mathf.Max(0f, widthPixels);
        _verticalBleedPixels = Mathf.Max(0f, verticalBleedPixels);

        SetVerticesDirty();
    }

    public void SetDiagonalBand(
        float widthPixels,
        float slantPixels,
        float bleedPixels,
        bool toRight)
    {
        _kind = StageMaskKind.DiagonalBand;
        _diagonalBandWidthPixels = Mathf.Max(0f, widthPixels);
        _diagonalBandSlantPixels = slantPixels;
        _diagonalBandBleedPixels = Mathf.Max(0f, bleedPixels);
        _diagonalBandToRight = toRight;

        SetVerticesDirty();
    }

    public void SetCircleIris(
        float radiusPixels,
        float aspect,
        int segments)
    {
        _kind = StageMaskKind.CircleIris;
        _irisRadiusPixels = Mathf.Max(0f, radiusPixels);
        _irisAspect = Mathf.Max(0.001f, aspect);
        _irisSegments = Mathf.Clamp(segments, MinCircleSegments, MaxCircleSegments);

        SetVerticesDirty();
    }

    public bool TryGetQuadPoints(
        out Vector2 p0,
        out Vector2 p1,
        out Vector2 p2,
        out Vector2 p3)
    {
        Rect rect = rectTransform.rect;

        switch (_kind)
        {
            case StageMaskKind.FullRect:
                GetFullRectQuad(rect, out p0, out p1, out p2, out p3);
                break;

            case StageMaskKind.Slanted:
                GetSlantedQuad(rect, out p0, out p1, out p2, out p3);
                break;

            case StageMaskKind.HorizontalStrip:
                GetHorizontalStripQuad(rect, out p0, out p1, out p2, out p3);
                break;

            case StageMaskKind.VerticalStrip:
                GetVerticalStripQuad(rect, out p0, out p1, out p2, out p3);
                break;

            case StageMaskKind.DiagonalBand:
                GetDiagonalBandQuad(rect, out p0, out p1, out p2, out p3);
                break;

            default:
                p0 = default;
                p1 = default;
                p2 = default;
                p3 = default;
                return false;
        }

        ApplyOffset(ref p0, ref p1, ref p2, ref p3);
        return true;
    }

    public void CollectEdgeSegments(
        StageMaskEdgeMode edgeMode,
        List<StageMaskLineSegment> results)
    {
        if (results == null)
            return;

        results.Clear();

        if (edgeMode == StageMaskEdgeMode.None)
            return;

        if (_kind == StageMaskKind.CircleIris)
        {
            CollectCircleOutlineSegments(results);
            return;
        }

        if (!TryGetQuadPoints(out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3))
            return;

        if ((edgeMode & StageMaskEdgeMode.Outline) != 0)
        {
            results.Add(new StageMaskLineSegment(p0, p1));
            results.Add(new StageMaskLineSegment(p1, p2));
            results.Add(new StageMaskLineSegment(p2, p3));
            results.Add(new StageMaskLineSegment(p3, p0));
            return;
        }

        if ((edgeMode & StageMaskEdgeMode.Leading) != 0)
        {
            GetLeadingEdge(p0, p1, p2, p3, out Vector2 a, out Vector2 b);
            results.Add(new StageMaskLineSegment(a, b));
        }

        if ((edgeMode & StageMaskEdgeMode.Trailing) != 0)
        {
            GetTrailingEdge(p0, p1, p2, p3, out Vector2 a, out Vector2 b);
            results.Add(new StageMaskLineSegment(a, b));
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (_kind == StageMaskKind.CircleIris)
        {
            PopulateCircleIrisMesh(vh);
            return;
        }

        if (!TryGetQuadPoints(out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3))
            return;

        AddQuad(vh, p0, p1, p2, p3);
    }

    private static void GetFullRectQuad(
        Rect rect,
        out Vector2 p0,
        out Vector2 p1,
        out Vector2 p2,
        out Vector2 p3)
    {
        p0 = new Vector2(rect.xMin, rect.yMin);
        p1 = new Vector2(rect.xMin, rect.yMax);
        p2 = new Vector2(rect.xMax, rect.yMax);
        p3 = new Vector2(rect.xMax, rect.yMin);
    }

    private void GetSlantedQuad(
        Rect rect,
        out Vector2 p0,
        out Vector2 p1,
        out Vector2 p2,
        out Vector2 p3)
    {
        float left = rect.xMin;
        float right = rect.xMax;
        float bottom = rect.yMin;
        float top = rect.yMax;

        if (_slantToRight)
        {
            if (!_flipVertical)
            {
                p0 = new Vector2(left, bottom);
                p1 = new Vector2(left + _slantPixels, top);
                p2 = new Vector2(right, top);
                p3 = new Vector2(right, bottom);
            }
            else
            {
                p0 = new Vector2(left + _slantPixels, bottom);
                p1 = new Vector2(left, top);
                p2 = new Vector2(right, top);
                p3 = new Vector2(right, bottom);
            }
        }
        else
        {
            if (!_flipVertical)
            {
                p0 = new Vector2(left, bottom);
                p1 = new Vector2(left, top);
                p2 = new Vector2(right - _slantPixels, top);
                p3 = new Vector2(right, bottom);
            }
            else
            {
                p0 = new Vector2(left, bottom);
                p1 = new Vector2(left, top);
                p2 = new Vector2(right, top);
                p3 = new Vector2(right - _slantPixels, bottom);
            }
        }
    }

    private void GetHorizontalStripQuad(
        Rect rect,
        out Vector2 p0,
        out Vector2 p1,
        out Vector2 p2,
        out Vector2 p3)
    {
        float halfHeight = _stripHeightPixels * 0.5f;

        float left = rect.xMin - _horizontalBleedPixels;
        float right = rect.xMax + _horizontalBleedPixels;
        float centerY = rect.center.y;
        float bottom = centerY - halfHeight;
        float top = centerY + halfHeight;

        p0 = new Vector2(left, bottom);
        p1 = new Vector2(left, top);
        p2 = new Vector2(right, top);
        p3 = new Vector2(right, bottom);
    }

    private void GetVerticalStripQuad(
        Rect rect,
        out Vector2 p0,
        out Vector2 p1,
        out Vector2 p2,
        out Vector2 p3)
    {
        float halfWidth = _verticalStripWidthPixels * 0.5f;

        float centerX = rect.center.x;
        float left = centerX - halfWidth;
        float right = centerX + halfWidth;
        float bottom = rect.yMin - _verticalBleedPixels;
        float top = rect.yMax + _verticalBleedPixels;

        p0 = new Vector2(left, bottom);
        p1 = new Vector2(left, top);
        p2 = new Vector2(right, top);
        p3 = new Vector2(right, bottom);
    }

    private void GetDiagonalBandQuad(
        Rect rect,
        out Vector2 p0,
        out Vector2 p1,
        out Vector2 p2,
        out Vector2 p3)
    {
        float left = rect.xMin - _diagonalBandBleedPixels;
        float right = rect.xMax + _diagonalBandBleedPixels;
        float bottom = rect.yMin - _diagonalBandBleedPixels;
        float top = rect.yMax + _diagonalBandBleedPixels;

        float width = _diagonalBandWidthPixels;
        float slant = _diagonalBandToRight
            ? _diagonalBandSlantPixels
            : -_diagonalBandSlantPixels;

        p0 = new Vector2(left, bottom);
        p1 = new Vector2(left + slant, top);
        p2 = new Vector2(left + slant + width, top);
        p3 = new Vector2(left + width, bottom);

        float centerOffsetX = rect.center.x - (left + width * 0.5f);
        p0.x += centerOffsetX;
        p1.x += centerOffsetX;
        p2.x += centerOffsetX;
        p3.x += centerOffsetX;

        float fullWidth = right - left;
        if (fullWidth > 0f)
        {
            p0.x -= fullWidth * 0.5f;
            p1.x -= fullWidth * 0.5f;
            p2.x -= fullWidth * 0.5f;
            p3.x -= fullWidth * 0.5f;
        }
    }

    private void PopulateCircleIrisMesh(VertexHelper vh)
    {
        Rect rect = rectTransform.rect;

        Vector2 center = rect.center + _shapeOffsetPixels;

        float radiusX = _irisRadiusPixels * _irisAspect;
        float radiusY = _irisRadiusPixels;

        if (radiusX <= 0f || radiusY <= 0f)
            return;

        UIVertex v = UIVertex.simpleVert;
        v.color = color;

        int centerIndex = vh.currentVertCount;
        v.position = center;
        vh.AddVert(v);

        int firstOuter = vh.currentVertCount;

        for (int i = 0; i <= _irisSegments; i++)
        {
            float t = i / (float)_irisSegments;
            float angle = t * Mathf.PI * 2f;

            Vector2 p = new(
                center.x + Mathf.Cos(angle) * radiusX,
                center.y + Mathf.Sin(angle) * radiusY);

            v.position = p;
            vh.AddVert(v);
        }

        for (int i = 0; i < _irisSegments; i++)
        {
            vh.AddTriangle(
                centerIndex,
                firstOuter + i,
                firstOuter + i + 1);
        }
    }

    private void CollectCircleOutlineSegments(List<StageMaskLineSegment> results)
    {
        Rect rect = rectTransform.rect;

        Vector2 center = rect.center + _shapeOffsetPixels;

        float radiusX = _irisRadiusPixels * _irisAspect;
        float radiusY = _irisRadiusPixels;

        if (radiusX <= 0f || radiusY <= 0f)
            return;

        Vector2 prev = default;

        for (int i = 0; i <= _irisSegments; i++)
        {
            float t = i / (float)_irisSegments;
            float angle = t * Mathf.PI * 2f;

            Vector2 p = new(
                center.x + Mathf.Cos(angle) * radiusX,
                center.y + Mathf.Sin(angle) * radiusY);

            if (i > 0)
                results.Add(new StageMaskLineSegment(prev, p));

            prev = p;
        }
    }

    private void GetLeadingEdge(
        Vector2 p0,
        Vector2 p1,
        Vector2 p2,
        Vector2 p3,
        out Vector2 a,
        out Vector2 b)
    {
        switch (_kind)
        {
            case StageMaskKind.Slanted:
                if (_slantToRight)
                {
                    a = p0;
                    b = p1;
                }
                else
                {
                    a = p2;
                    b = p3;
                }

                break;

            case StageMaskKind.HorizontalStrip:
                a = p1;
                b = p2;
                break;

            case StageMaskKind.VerticalStrip:
            case StageMaskKind.DiagonalBand:
            case StageMaskKind.FullRect:
            default:
                a = p0;
                b = p1;
                break;
        }
    }

    private void GetTrailingEdge(
        Vector2 p0,
        Vector2 p1,
        Vector2 p2,
        Vector2 p3,
        out Vector2 a,
        out Vector2 b)
    {
        switch (_kind)
        {
            case StageMaskKind.Slanted:
                if (_slantToRight)
                {
                    a = p2;
                    b = p3;
                }
                else
                {
                    a = p0;
                    b = p1;
                }

                break;

            case StageMaskKind.HorizontalStrip:
                a = p0;
                b = p3;
                break;

            case StageMaskKind.VerticalStrip:
            case StageMaskKind.DiagonalBand:
            case StageMaskKind.FullRect:
            default:
                a = p2;
                b = p3;
                break;
        }
    }

    private void ApplyOffset(
        ref Vector2 p0,
        ref Vector2 p1,
        ref Vector2 p2,
        ref Vector2 p3)
    {
        p0 += _shapeOffsetPixels;
        p1 += _shapeOffsetPixels;
        p2 += _shapeOffsetPixels;
        p3 += _shapeOffsetPixels;
    }

    private void ApplyMaskGraphicVisibility()
    {
        Mask mask = GetComponent<Mask>();

        if (mask == null)
            return;

        mask.showMaskGraphic = !_hideMaskGraphic;
    }

    private void AddQuad(
        VertexHelper vh,
        Vector2 p0,
        Vector2 p1,
        Vector2 p2,
        Vector2 p3)
    {
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
}