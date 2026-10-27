using System.Collections;
using DG.Tweening;
using UnityEngine;

[System.Serializable]
public sealed class StageMaskMotionCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public PresentationStageKey stage = PresentationStageKey.Stage01;

    [Header("Preset")]
    [Tooltip("StageMaskMotionPresetDBSO entry key. ex) hstrip_open, hstrip_in, iris_in")]
    public string presetKey = StageMaskMotionPresetDBSO.DefaultPresetKey;

    [Header("Tween Override")]
    [Tooltip("음수이면 preset의 duration을 사용합니다. 0이면 즉시 커밋.")]
    public float durationOverride = -1f;
}

public sealed class StageMaskMotionCommand : CommandBase
{
    private readonly StageMaskMotionCommandSpec _spec;
    private readonly StageMaskMotionPresetDBSO _presetDb;
    private readonly IStageMaskProvider _stageMaskProvider;

    private StageMaskMotionPresetDBSO.Entry _entry;
    private float _duration;

    private StageMaskSlot _slot;
    private StageMaskGraphic _graphic;
    private StageMaskEdgeGraphic _edgeGraphic;

    private Tween _tween;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public StageMaskMotionCommand(
        StageMaskMotionCommandSpec spec,
        StageMaskMotionPresetDBSO presetDb,
        IStageMaskProvider stageMaskProvider)
    {
        _spec = spec;
        _presetDb = presetDb;
        _stageMaskProvider = stageMaskProvider;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (_graphic == null || _slot == null)
            yield break;

        ClaimTarget();

        if (scope.ShouldCompressCommandExecution || _duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        Vector2 start = _entry.fromOffset;
        Vector2 dest = _entry.toOffset;

        Vector2 moveDir = dest - start;
        moveDir = moveDir.sqrMagnitude > 0f
            ? moveDir.normalized
            : Vector2.right;

        _graphic.ShapeOffsetPixels = start;
        ApplyShapeAt(0f);

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    float e = DOVirtual.EasedValue(0f, 1f, t, _entry.ease);

                    Vector2 baseOffset = Vector2.LerpUnclamped(start, dest, e);
                    Vector2 rubberOffset = CalculateRubberOffset(e, moveDir);

                    _graphic.ShapeOffsetPixels = baseOffset + rubberOffset;
                    ApplyShapeAt(e);
                },
                1f,
                _duration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(_graphic)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (_graphic == null || _slot == null)
            return;

        if (!HasClaimedTarget)
            ClaimTarget();

        CommitFinalState();
    }

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        _entry = ResolvePresetEntry();
        _duration = _spec.durationOverride >= 0f
            ? _spec.durationOverride
            : _entry.duration;

        IStageMaskProvider provider = _stageMaskProvider;

        if (!provider.TryGetStageMaskSlot(_spec.stage, out _slot) || _slot == null)
        {
            Debug.LogWarning(
                $"[StageMaskMotionCommand] StageMaskSlot is missing. " +
                $"stage='{_spec.stage}'.");
            return;
        }

        _graphic = _slot.Graphic;
        _edgeGraphic = _slot.EdgeGraphic;

        if (_graphic == null)
        {
            Debug.LogWarning(
                $"[StageMaskMotionCommand] StageMaskGraphic is missing. " +
                $"stage='{_spec.stage}'.");
        }
    }

    private StageMaskMotionPresetDBSO.Entry ResolvePresetEntry()
    {
        if (_presetDb != null &&
            _presetDb.TryGet(_spec.presetKey, out StageMaskMotionPresetDBSO.Entry entry))
        {
            return entry;
        }

        Debug.LogWarning(
            $"[StageMaskMotionCommand] Stage mask preset not found. " +
            $"presetKey='{_spec.presetKey}'. Using fallback.");

        if (_presetDb != null &&
            _presetDb.TryGet(StageMaskMotionPresetDBSO.DefaultPresetKey, out entry))
        {
            return entry;
        }

        return BuildHardcodedFallbackEntry();
    }

    private static StageMaskMotionPresetDBSO.Entry BuildHardcodedFallbackEntry()
    {
        return new StageMaskMotionPresetDBSO.Entry
        {
            key = "fallback",
            kind = StageMaskKind.HorizontalStrip,
            fromOffset = Vector2.zero,
            toOffset = Vector2.zero,
            animateStripHeight = true,
            fromStripHeightPixels = 0f,
            stripHeightPixels = 360f,
            horizontalBleedPixels = 96f,
            showEdge = true,
            edgeMode = StageMaskEdgeMode.Both,
            edgeColor = new Color(1f, 1f, 1f, 0.82f),
            edgeThickness = 4f,
            hideEdgeOnComplete = false,
            duration = 0.45f,
            ease = Ease.OutCubic,
            rubberMode = StageMaskRubberMode.None,
            irisAspect = 1f,
            irisSegments = 64,
        };
    }

    private void ClaimTarget()
    {
        DOTween.Kill(_graphic, true);

        if (_edgeGraphic != null)
            DOTween.Kill(_edgeGraphic, true);

        _slot.ActivateMasked();

        ApplyFixedShapeOptions();
        ConfigureEdge();

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        ApplyFixedShapeOptions();
        ApplyShapeAt(1f);

        _graphic.ShapeOffsetPixels = _entry.toOffset;

        if (_entry.fullVisibleOnComplete)
            _slot.SetFullVisible();

        if (_entry.hideEdgeOnComplete)
            _slot.SetEdgeVisible(false);

        HasClaimedTarget = false;
        _tween = null;
    }

    private void ApplyFixedShapeOptions()
    {
        switch (_entry.kind)
        {
            case StageMaskKind.FullRect:
                _graphic.SetFullRect();
                break;

            case StageMaskKind.Slanted:
                _graphic.SetSlanted(
                    _entry.slantPixels,
                    _entry.slantToRight,
                    _entry.flipVertical);
                break;

            case StageMaskKind.HorizontalStrip:
                _graphic.SetHorizontalStrip(
                    _entry.animateStripHeight
                        ? _entry.fromStripHeightPixels
                        : _entry.stripHeightPixels,
                    _entry.horizontalBleedPixels);
                break;

            case StageMaskKind.VerticalStrip:
                _graphic.SetVerticalStrip(
                    _entry.animateStripWidth
                        ? _entry.fromVerticalStripWidthPixels
                        : _entry.verticalStripWidthPixels,
                    _entry.verticalBleedPixels);
                break;

            case StageMaskKind.DiagonalBand:
                _graphic.SetDiagonalBand(
                    _entry.diagonalBandWidthPixels,
                    _entry.diagonalBandSlantPixels,
                    _entry.diagonalBandBleedPixels,
                    _entry.diagonalBandToRight);
                break;

            case StageMaskKind.CircleIris:
                _graphic.SetCircleIris(
                    _entry.fromIrisRadiusPixels,
                    _entry.irisAspect,
                    _entry.irisSegments);
                break;
        }
    }

    private void ApplyShapeAt(float t)
    {
        t = Mathf.Clamp01(t);

        switch (_entry.kind)
        {
            case StageMaskKind.CircleIris:
            {
                float radius = Mathf.LerpUnclamped(
                    _entry.fromIrisRadiusPixels,
                    _entry.toIrisRadiusPixels,
                    t);

                _graphic.SetCircleIris(
                    radius,
                    _entry.irisAspect,
                    _entry.irisSegments);
                break;
            }

            case StageMaskKind.HorizontalStrip:
            {
                if (!_entry.animateStripHeight)
                    return;

                float height = Mathf.LerpUnclamped(
                    _entry.fromStripHeightPixels,
                    _entry.stripHeightPixels,
                    t);

                _graphic.SetHorizontalStrip(
                    height,
                    _entry.horizontalBleedPixels);
                break;
            }

            case StageMaskKind.VerticalStrip:
            {
                if (!_entry.animateStripWidth)
                    return;

                float width = Mathf.LerpUnclamped(
                    _entry.fromVerticalStripWidthPixels,
                    _entry.verticalStripWidthPixels,
                    t);

                _graphic.SetVerticalStrip(
                    width,
                    _entry.verticalBleedPixels);
                break;
            }
        }
    }

    private void ConfigureEdge()
    {
        if (_edgeGraphic == null)
            return;

        _slot.ConfigureEdge(
            _entry.edgeMode,
            _entry.edgeColor,
            _entry.edgeThickness);

        _slot.SetEdgeVisible(_entry.showEdge);
    }

    private Vector2 CalculateRubberOffset(
        float easedProgress,
        Vector2 moveDir)
    {
        switch (_entry.rubberMode)
        {
            case StageMaskRubberMode.OvershootEnd:
            {
                float rubber = RubberOvershootEnd(easedProgress, _entry.overshootStart);
                return moveDir * (_entry.overshootPixels * rubber);
            }

            case StageMaskRubberMode.PullStart:
            {
                float pull = RubberPullStart(easedProgress, _entry.pullEnd);
                return -moveDir * (_entry.pullPixels * pull);
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
}
