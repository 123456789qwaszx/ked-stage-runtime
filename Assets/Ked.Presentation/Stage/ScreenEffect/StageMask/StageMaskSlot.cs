using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// 무대 하나(s1·s2·s3)를 어디까지, 어떤 모양으로 보여줄지 — 장면 전역 의도다.
// tx 모션은 이 슬롯이 하나만 소유한다. 새 모션·clear는 앞 모션을 확정 없이 끊는다(새 모션은 제 시작 모양부터 다시 세운다).
// 장면 경계(정지)에서는 ResetToBaseline으로 씬에 저장된 기준 모양으로 돌아간다.
[DisallowMultipleComponent]
public sealed class StageMaskSlot : MonoBehaviour
{
    [SerializeField] private StageMaskGraphic _graphic;
    [SerializeField] private StageMaskEdgeGraphic _edgeGraphic;
    [SerializeField] private Mask _mask;

    private Tween _motion;

    // 씬에 저장된 테두리 — 기준으로 돌아갈 때 되쓴다. 마스크 모양의 기준은 FullRect·오프셋 0이다(씬 기본값).
    private bool _baselineCaptured;
    private bool _baselineEdgeVisible;
    private StageMaskEdgeMode _baselineEdgeMode;
    private Color _baselineEdgeColor;
    private float _baselineEdgeThickness;

    public StageMaskGraphic Graphic => _graphic;
    public StageMaskEdgeGraphic EdgeGraphic => _edgeGraphic;
    public Mask Mask => _mask;

    public bool HasMask => _graphic != null && _mask != null;

    public bool IsPlayingMotion => _motion != null;

    private void Awake()
    {
        CacheRefs();
        AutoBindEdgeSource();
        CaptureBaseline();
    }

    private void OnDestroy()
    {
        StopMotion();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheRefs();
        AutoBindEdgeSource();
    }
#endif

    public void PlayMotion(in StageMaskMotionPresetDBSO.Entry entry, float duration)
    {
        StopMotion();

        if (!HasMask)
            return;

        BeginMotion(entry);

        if (duration <= 0f)
        {
            ApplyMotionEnd(entry);
            return;
        }

        StageMaskMotionPresetDBSO.Entry e = entry;

        Vector2 start = e.fromOffset;
        Vector2 dest = e.toOffset;

        Vector2 moveDir = dest - start;
        moveDir = moveDir.sqrMagnitude > 0f
            ? moveDir.normalized
            : Vector2.right;

        _graphic.ShapeOffsetPixels = start;
        ApplyShapeAt(e, 0f);

        _motion = DOTween
            .To(
                () => 0f,
                t =>
                {
                    float eased = DOVirtual.EasedValue(0f, 1f, t, e.ease);

                    Vector2 baseOffset = Vector2.LerpUnclamped(start, dest, eased);
                    Vector2 rubberOffset = CalculateRubberOffset(e, eased, moveDir);

                    _graphic.ShapeOffsetPixels = baseOffset + rubberOffset;
                    ApplyShapeAt(e, eased);
                },
                1f,
                duration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(this)
            .OnComplete(() =>
            {
                _motion = null;
                ApplyMotionEnd(e);
            });
    }

    // 압축 — 모션의 끝 모양으로 바로.
    public void SnapMotionEnd(in StageMaskMotionPresetDBSO.Entry entry)
    {
        StopMotion();

        if (!HasMask)
            return;

        BeginMotion(entry);
        ApplyMotionEnd(entry);
    }

    public void Clear(StageMaskClearMode mode, bool hideEdge)
    {
        StopMotion();

        switch (mode)
        {
            case StageMaskClearMode.UnmaskedFullVisible:
                SetFullVisible();
                break;

            case StageMaskClearMode.MaskedHidden:
                SetMaskedHidden();
                break;

            case StageMaskClearMode.MaskedFullRectVisible:
                SetMaskedFullRectVisible();
                break;
        }

        if (hideEdge)
            SetEdgeVisible(false);
    }

    // 장면 경계 — 모션을 확정 없이 끊고 씬에 저장된 기준 모양으로.
    public void ResetToBaseline()
    {
        StopMotion();
        SetMaskedFullRectVisible();

        if (!_baselineCaptured || _edgeGraphic == null)
            return;

        ConfigureEdge(_baselineEdgeMode, _baselineEdgeColor, _baselineEdgeThickness);
        SetEdgeVisible(_baselineEdgeVisible);
    }

    public void StopMotion()
    {
        _motion?.Kill(false);
        _motion = null;
    }

    public void ActivateMasked()
    {
        if (!HasMask)
            return;

        _graphic.enabled = true;
        _mask.enabled = true;
    }

    public void SetFullVisible()
    {
        if (_mask != null)
            _mask.enabled = false;

        if (_graphic != null)
            _graphic.enabled = false;

        SetEdgeVisible(false);
    }

    public void SetMaskedFullRectVisible()
    {
        if (!HasMask)
            return;

        ActivateMasked();

        _graphic.SetFullRect();
        _graphic.SetShapeOffsetImmediate(Vector2.zero);
    }

    public void SetMaskedVisible()
    {
        if (!HasMask)
            return;

        ActivateMasked();
        _graphic.SetShapeOffsetImmediate(Vector2.zero);
    }

    public void SetMaskedHidden()
    {
        if (!HasMask)
            return;

        ActivateMasked();
        _graphic.ResetToHiddenOffset();
    }

    public void SetMaskOffsetImmediate(Vector2 offset)
    {
        if (!HasMask)
            return;

        ActivateMasked();
        _graphic.SetShapeOffsetImmediate(offset);
    }

    public void SetEdgeVisible(bool visible)
    {
        if (_edgeGraphic == null)
            return;

        _edgeGraphic.enabled = visible;
    }

    public void ConfigureEdge(
        StageMaskEdgeMode mode,
        Color color,
        float thickness)
    {
        if (_edgeGraphic == null)
            return;

        _edgeGraphic.Source = _graphic;
        _edgeGraphic.EdgeMode = mode;
        _edgeGraphic.color = color;
        _edgeGraphic.Thickness = thickness;
    }

    // 모션의 시작 — 마스크를 켜고, 모양 종류와 테두리를 프리셋대로 세운다.
    private void BeginMotion(in StageMaskMotionPresetDBSO.Entry entry)
    {
        ActivateMasked();
        ApplyFixedShapeOptions(entry);

        if (_edgeGraphic == null)
            return;

        ConfigureEdge(entry.edgeMode, entry.edgeColor, entry.edgeThickness);
        SetEdgeVisible(entry.showEdge);
    }

    private void ApplyMotionEnd(in StageMaskMotionPresetDBSO.Entry entry)
    {
        ApplyFixedShapeOptions(entry);
        ApplyShapeAt(entry, 1f);

        _graphic.ShapeOffsetPixels = entry.toOffset;

        if (entry.fullVisibleOnComplete)
            SetFullVisible();

        if (entry.hideEdgeOnComplete)
            SetEdgeVisible(false);
    }

    private void ApplyFixedShapeOptions(in StageMaskMotionPresetDBSO.Entry entry)
    {
        switch (entry.kind)
        {
            case StageMaskKind.FullRect:
                _graphic.SetFullRect();
                break;

            case StageMaskKind.Slanted:
                _graphic.SetSlanted(
                    entry.slantPixels,
                    entry.slantToRight,
                    entry.flipVertical);
                break;

            case StageMaskKind.HorizontalStrip:
                _graphic.SetHorizontalStrip(
                    entry.animateStripHeight
                        ? entry.fromStripHeightPixels
                        : entry.stripHeightPixels,
                    entry.horizontalBleedPixels);
                break;

            case StageMaskKind.VerticalStrip:
                _graphic.SetVerticalStrip(
                    entry.animateStripWidth
                        ? entry.fromVerticalStripWidthPixels
                        : entry.verticalStripWidthPixels,
                    entry.verticalBleedPixels);
                break;

            case StageMaskKind.DiagonalBand:
                _graphic.SetDiagonalBand(
                    entry.diagonalBandWidthPixels,
                    entry.diagonalBandSlantPixels,
                    entry.diagonalBandBleedPixels,
                    entry.diagonalBandToRight);
                break;

            case StageMaskKind.CircleIris:
                _graphic.SetCircleIris(
                    entry.fromIrisRadiusPixels,
                    entry.irisAspect,
                    entry.irisSegments);
                break;
        }
    }

    private void ApplyShapeAt(in StageMaskMotionPresetDBSO.Entry entry, float t)
    {
        t = Mathf.Clamp01(t);

        switch (entry.kind)
        {
            case StageMaskKind.CircleIris:
            {
                float radius = Mathf.LerpUnclamped(
                    entry.fromIrisRadiusPixels,
                    entry.toIrisRadiusPixels,
                    t);

                _graphic.SetCircleIris(
                    radius,
                    entry.irisAspect,
                    entry.irisSegments);
                break;
            }

            case StageMaskKind.HorizontalStrip:
            {
                if (!entry.animateStripHeight)
                    return;

                float height = Mathf.LerpUnclamped(
                    entry.fromStripHeightPixels,
                    entry.stripHeightPixels,
                    t);

                _graphic.SetHorizontalStrip(
                    height,
                    entry.horizontalBleedPixels);
                break;
            }

            case StageMaskKind.VerticalStrip:
            {
                if (!entry.animateStripWidth)
                    return;

                float width = Mathf.LerpUnclamped(
                    entry.fromVerticalStripWidthPixels,
                    entry.verticalStripWidthPixels,
                    t);

                _graphic.SetVerticalStrip(
                    width,
                    entry.verticalBleedPixels);
                break;
            }
        }
    }

    private static Vector2 CalculateRubberOffset(
        in StageMaskMotionPresetDBSO.Entry entry,
        float easedProgress,
        Vector2 moveDir)
    {
        switch (entry.rubberMode)
        {
            case StageMaskRubberMode.OvershootEnd:
            {
                float rubber = RubberOvershootEnd(easedProgress, entry.overshootStart);
                return moveDir * (entry.overshootPixels * rubber);
            }

            case StageMaskRubberMode.PullStart:
            {
                float pull = RubberPullStart(easedProgress, entry.pullEnd);
                return -moveDir * (entry.pullPixels * pull);
            }

            case StageMaskRubberMode.None:
            default:
                return Vector2.zero;
        }
    }

    private static float RubberOvershootEnd(float e, float overshootStart)
    {
        e = Mathf.Clamp01(e);
        overshootStart = Mathf.Clamp(overshootStart, 0.01f, 0.99f);

        if (e < overshootStart)
            return 0f;

        float t = Mathf.InverseLerp(overshootStart, 1f, e);
        return Mathf.Sin(t * Mathf.PI);
    }

    private static float RubberPullStart(float e, float pullEnd)
    {
        e = Mathf.Clamp01(e);
        pullEnd = Mathf.Clamp(pullEnd, 0.01f, 0.99f);

        if (e >= pullEnd)
            return 0f;

        float t = Mathf.InverseLerp(0f, pullEnd, e);
        return 1f - Mathf.SmoothStep(0f, 1f, t);
    }

    private void CaptureBaseline()
    {
        if (_baselineCaptured || _edgeGraphic == null)
            return;

        _baselineEdgeVisible = _edgeGraphic.enabled;
        _baselineEdgeMode = _edgeGraphic.EdgeMode;
        _baselineEdgeColor = _edgeGraphic.color;
        _baselineEdgeThickness = _edgeGraphic.Thickness;
        _baselineCaptured = true;
    }

    /// <summary>
    /// 빌더(StageRigBuilder)가 컴포넌트를 세운 직후 묶는다. 씬 직렬화 대신 코드가 참조를 준다 —
    /// 테두리는 형제 노드에 있어 GetComponent로는 찾을 수 없다.
    /// </summary>
    public void BindFromBuilder(StageMaskGraphic graphic, Mask mask, StageMaskEdgeGraphic edgeGraphic)
    {
        _graphic = graphic;
        _mask = mask;
        _edgeGraphic = edgeGraphic;

        AutoBindEdgeSource();
        CaptureBaseline();
    }

    private void CacheRefs()
    {
        if (_graphic == null)
            _graphic = GetComponent<StageMaskGraphic>();

        if (_mask == null)
            _mask = GetComponent<Mask>();
    }

    private void AutoBindEdgeSource()
    {
        if (_edgeGraphic == null)
            return;

        if (_graphic == null)
            return;

        _edgeGraphic.Source = _graphic;
    }
}
