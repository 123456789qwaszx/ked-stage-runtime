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

// tx — 프리셋을 골라 무대 슬롯에 모션을 걸기만 한다. 모션은 슬롯이 소유한다(StageMaskSlot.PlayMotion).
public sealed class StageMaskMotionCommand : CommandBase
{
    private readonly StageMaskMotionCommandSpec _spec;
    private readonly StageMaskMotionPresetDBSO _presetDb;
    private readonly IStageMaskProvider _stageMaskProvider;

    private StageMaskMotionPresetDBSO.Entry _entry;
    private float _duration;
    private StageMaskSlot _slot;

    private bool _resolveAttempted;

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

        if (_slot == null || !_slot.HasMask)
            yield break;

        _slot.PlayMotion(_entry, _duration);

        if (!_spec.wait)
            yield break;

        while (_slot.IsPlayingMotion)
            yield return null;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (_slot == null || !_slot.HasMask)
            return;

        _slot.SnapMotionEnd(_entry);
    }

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        _entry = ResolvePresetEntry();
        _duration = _spec.durationOverride >= 0f
            ? _spec.durationOverride
            : _entry.duration;

        if (!_stageMaskProvider.TryGetStageMaskSlot(_spec.stage, out _slot) || _slot == null)
        {
            Debug.LogWarning(
                $"[StageMaskMotionCommand] StageMaskSlot is missing. " +
                $"stage='{_spec.stage}'.");
            return;
        }

        if (_slot.Graphic == null)
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
}
