using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
public sealed class ScreenVignetteCommandSpec : CommandSpecBase
{
    [Header("Preset")]
    [Tooltip("ScreenVignettePresetDBSO entry key. ex) clear, focus, tension, horror, danger, memory, dream, letterbox")]
    public string presetKey = ScreenVignettePresetDBSO.DefaultPresetKey;

    [Range(0f, 1f)]
    public float intensity = 1f;

    [Header("Tween")]
    public float duration = 0.35f;
    public Ease ease = Ease.OutCubic;
}

public sealed class ScreenVignetteCommand : CommandBase
{
    private readonly ScreenVignetteCommandSpec _spec;
    private readonly ScreenEffectRig _screenEffects;
    private readonly ScreenVignettePresetDBSO _presetDb;

    private ScreenVignetteEffectController _controller;

    private VignetteState _fromState;
    private VignetteState _destState;

    private bool HasClaimedController { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public ScreenVignetteCommand(
        ScreenVignetteCommandSpec spec,
        ScreenEffectRig screenEffects,
        ScreenVignettePresetDBSO presetDb)
    {
        _spec = spec;
        _screenEffects = screenEffects;
        _presetDb = presetDb;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        ClaimController();

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        Tween tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    VignetteState state = VignetteState.Lerp(_fromState, _destState, t);
                    ApplyState(state);
                },
                1f,
                _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_controller.transform)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!HasClaimedController)
            ClaimController();

        CommitFinalState();
    }

    private void ClaimController()
    {
        _controller = _screenEffects.Vignette;
        _controller.KillTween(true);

        _fromState = CaptureCurrentState();
        _destState = BuildDestState();

        HasClaimedController = true;
    }
    
    private void CommitFinalState()
    {
        _controller.KillTween(false);
        ApplyState(_destState);

        HasClaimedController = false;
    }

    private VignetteState CaptureCurrentState()
    {
        return new VignetteState(
            _controller.Amount,
            _controller.Color,
            _controller.Radius,
            _controller.Softness,
            _controller.Aspect);
    }

    private VignetteState BuildDestState()
    {
        float intensity = Mathf.Clamp01(_spec.intensity);
        return BuildPresetState(_spec.presetKey, intensity);
    }

    private VignetteState BuildPresetState(string presetKey, float intensity)
    {
        if (_presetDb != null &&
            _presetDb.TryGet(presetKey, out ScreenVignettePresetDBSO.Entry e))
        {
            return new VignetteState(
                e.amount * intensity,
                e.color,
                e.radius,
                e.softness,
                e.aspect);
        }

        Debug.LogWarning(
            $"[ScreenVignetteCommand] Vignette preset not found. " +
            $"presetKey='{presetKey}'. Using fallback.",
            _controller);

        if (_presetDb != null &&
            _presetDb.TryGet(ScreenVignettePresetDBSO.DefaultPresetKey, out e))
        {
            return new VignetteState(
                e.amount * intensity,
                e.color,
                e.radius,
                e.softness,
                e.aspect);
        }

        return new VignetteState(
            0.35f * intensity,
            Color.black,
            0.25f,
            0.10f,
            1.2f);
    }

    private void ApplyState(VignetteState state)
    {
        _controller.ApplyImmediate(
            state.Amount,
            state.Color,
            state.Radius,
            state.Softness,
            state.Aspect);
    }

    private readonly struct VignetteState
    {
        public readonly float Amount;
        public readonly Color Color;
        public readonly float Radius;
        public readonly float Softness;
        public readonly float Aspect;

        public VignetteState(
            float amount,
            Color color,
            float radius,
            float softness,
            float aspect)
        {
            Amount = Mathf.Clamp01(amount);
            Color = color;
            Radius = Mathf.Clamp01(radius);
            Softness = Mathf.Max(0.001f, softness);
            Aspect = Mathf.Max(0f, aspect);
        }

        public static VignetteState Lerp(VignetteState from, VignetteState to, float t)
        {
            t = Mathf.Clamp01(t);

            return new VignetteState(
                Mathf.Lerp(from.Amount, to.Amount, t),
                Color.Lerp(from.Color, to.Color, t),
                Mathf.Lerp(from.Radius, to.Radius, t),
                Mathf.Lerp(from.Softness, to.Softness, t),
                Mathf.Lerp(from.Aspect, to.Aspect, t));
        }
    }
}
