using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
public sealed class CharVisualFocusCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Preset")]
    [Tooltip("CharacterVisualFocusPresetDBSO entry key. ex) clear, focus, defocus, dim, silhouette, outer_rim")]
    public string presetKey = CharacterVisualFocusPresetDBSO.DefaultPresetKey;

    [Range(0f, 1f)]
    public float intensity = 1f;

    [Header("Tween")]
    public float duration = 0.25f;
    public Ease ease = Ease.OutCubic;
}

public sealed class CharVisualFocusCommandCharR : ClaimTweenCommandBase
{
    private readonly CharVisualFocusCommandSpecCharR _spec;
    private readonly CharacterVisualFocusPresetDBSO _presetDb;

    private RigVisualEffectController _controller;

    private VisualState _fromState;
    private VisualState _destState;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    // 캐릭터 전체의 밝기, 림이 한꺼번에 끊기면 튐. 완만하게 조정
    protected override float StepFinishSpeedUpMultiplier => 1.5f;

    private readonly PresentationStage _stage;

    public CharVisualFocusCommandCharR(
        PresentationStage stage,
        CharVisualFocusCommandSpecCharR spec,
        CharacterVisualFocusPresetDBSO presetDb)
    {
        _stage = stage;
        _spec = spec;
        _presetDb = presetDb;
    }

    protected override void ResolveTargets(CommandRunScope scope)
    {
        CharacterRigRefs rigRefs =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(_stage, _spec.slotKey);

        _controller = rigRefs?.VisualEffect;

        if (_controller == null)
            Debug.LogWarning(
                $"[CharVisualFocusCommandCharR] VisualEffect controller is missing. " +
                $"SetupCharRig가 컨트롤러를 생성했는지, source material 경로가 맞는지 확인하세요. " +
                $"slotKey='{_spec.slotKey}'.");
    }

    protected override void ClaimTarget(CommandRunScope scope)
    {
        DOTween.Kill(_controller, false);

        _fromState = CaptureCurrentState();
        _destState = BuildDestState();
    }

    protected override Tween CreateTween(float duration)
        => DOTween
            .To(
                () => 0f,
                t => ApplyState(VisualState.Lerp(_fromState, _destState, t)),
                1f,
                duration)
            .SetEase(_spec.ease)
            .SetTarget(_controller);

    /// <summary>
    /// 진행률(0→1) 트윈이라 그대로 재시작하면 처음부터 다시 돈다 —
    /// 현재 상태를 새 출발점으로 삼아 남은 구간만 태운다.
    /// </summary>
    protected override Tween CreateAcceleratedTween(float duration)
    {
        _fromState = CaptureCurrentState();

        return CreateTween(duration);
    }

    protected override void OnCommitFinalState()
    {
        DOTween.Kill(_controller, false);

        ApplyState(_destState);
    }

    protected override float MeasureRemainingRatio()
        => RemainingRatio(
            VisualState.Distance(_fromState, _destState),
            VisualState.Distance(CaptureCurrentState(), _destState));

    private VisualState CaptureCurrentState()
    {
        return new VisualState(
            _controller.DimAmount,
            _controller.DimTintColor,
            _controller.OuterRimAmount,
            _controller.InnerRimAmount,
            _controller.OuterRimColor,
            _controller.InnerRimColor,
            _controller.BlurAmount);
    }

    private VisualState BuildDestState()
    {
        float intensity = Mathf.Clamp01(_spec.intensity);
        return BuildPresetState(_spec.presetKey, intensity);
    }

    private VisualState BuildPresetState(string presetKey, float intensity)
    {
        if (_presetDb != null &&
            _presetDb.TryGet(presetKey, out CharacterVisualFocusPresetDBSO.Entry entry))
        {
            return EntryToState(entry, intensity);
        }

        Debug.LogWarning(
            $"[CharVisualFocusCommandCharR] Visual focus preset not found. " +
            $"presetKey='{presetKey}'. Using fallback.");

        if (_presetDb != null &&
            _presetDb.TryGet(CharacterVisualFocusPresetDBSO.DefaultPresetKey, out entry))
        {
            return EntryToState(entry, intensity);
        }

        return new VisualState(
            0f,
            new Color(0.45f, 0.48f, 0.55f, 1f),
            0.4f * intensity,
            0.09f * intensity,
            Color.white,
            new Color(1f, 0.96f, 0.86f, 1f),
            0f);
    }

    private static VisualState EntryToState(
        CharacterVisualFocusPresetDBSO.Entry entry,
        float intensity)
    {
        return new VisualState(
            entry.dim * intensity,
            entry.dimTintColor,
            entry.outerRim * intensity,
            entry.innerRim * intensity,
            entry.outerRimColor,
            entry.innerRimColor,
            entry.blur * intensity);
    }

    private void ApplyState(VisualState state)
    {
        _controller.ApplyImmediate(
            state.Dim,
            state.DimTintColor,
            state.OuterRim,
            state.InnerRim,
            state.OuterRimColor,
            state.InnerRimColor,
            state.Blur);
    }

    private readonly struct VisualState
    {
        public readonly float Dim;
        public readonly Color DimTintColor;
        public readonly float OuterRim;
        public readonly float InnerRim;
        public readonly Color OuterRimColor;
        public readonly Color InnerRimColor;
        public readonly float Blur;

        public VisualState(
            float dim,
            Color dimTintColor,
            float outerRim,
            float innerRim,
            Color outerRimColor,
            Color innerRimColor,
            float blur)
        {
            Dim = Mathf.Clamp01(dim);
            DimTintColor = dimTintColor;
            OuterRim = Mathf.Clamp01(outerRim);
            InnerRim = Mathf.Clamp01(innerRim);
            OuterRimColor = outerRimColor;
            InnerRimColor = innerRimColor;
            Blur = Mathf.Clamp01(blur);
        }

        public static VisualState Lerp(VisualState from, VisualState to, float t)
        {
            t = Mathf.Clamp01(t);

            return new VisualState(
                Mathf.Lerp(from.Dim, to.Dim, t),
                Color.Lerp(from.DimTintColor, to.DimTintColor, t),
                Mathf.Lerp(from.OuterRim, to.OuterRim, t),
                Mathf.Lerp(from.InnerRim, to.InnerRim, t),
                Color.Lerp(from.OuterRimColor, to.OuterRimColor, t),
                Color.Lerp(from.InnerRimColor, to.InnerRimColor, t),
                Mathf.Lerp(from.Blur, to.Blur, t));
        }

        public static float Distance(VisualState from, VisualState to)
        {
            float dim = Mathf.Abs(to.Dim - from.Dim);
            float outerRim = Mathf.Abs(to.OuterRim - from.OuterRim);
            float innerRim = Mathf.Abs(to.InnerRim - from.InnerRim);
            float blur = Mathf.Abs(to.Blur - from.Blur);

            float dimTint = ColorDistance(from.DimTintColor, to.DimTintColor);
            float outerRimColor = ColorDistance(from.OuterRimColor, to.OuterRimColor);
            float innerRimColor = ColorDistance(from.InnerRimColor, to.InnerRimColor);

            return dim +
                   outerRim +
                   innerRim +
                   blur +
                   dimTint +
                   outerRimColor +
                   innerRimColor;
        }

        private static float ColorDistance(Color from, Color to)
        {
            float r = to.r - from.r;
            float g = to.g - from.g;
            float b = to.b - from.b;
            float a = to.a - from.a;

            return Mathf.Sqrt(r * r + g * g + b * b + a * a);
        }
    }
}