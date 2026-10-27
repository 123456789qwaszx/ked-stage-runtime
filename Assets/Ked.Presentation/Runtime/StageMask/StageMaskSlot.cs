using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StageMaskSlot : MonoBehaviour
{
    [SerializeField] private StageMaskGraphic _graphic;
    [SerializeField] private StageMaskEdgeGraphic _edgeGraphic;
    [SerializeField] private Mask _mask;

    public StageMaskGraphic Graphic => _graphic;
    public StageMaskEdgeGraphic EdgeGraphic => _edgeGraphic;
    public Mask Mask => _mask;

    public bool HasMask => _graphic != null && _mask != null;

    private void Awake()
    {
        CacheRefs();
        AutoBindEdgeSource();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheRefs();
        AutoBindEdgeSource();
    }
#endif

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