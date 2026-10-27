using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
public sealed class ScreenNoiseCommandSpec : CommandSpecBase
{
    [Header("Preset")]
    [Tooltip("ScreenNoisePresetDBSO entry key. ex) clear, default, memory, horror, broadcast, dream, rain_mood")]
    public string presetKey = ScreenNoisePresetDBSO.DefaultPresetKey;

    [Range(0f, 1f)]
    public float intensity = 1f;

    [Header("Tween")]
    public float duration = 0.35f;
    public Ease ease = Ease.OutCubic;
}

public sealed class ScreenNoiseCommand : CommandBase
{
    private readonly ScreenNoiseCommandSpec _spec;
    private readonly ScreenEffectRig _screenEffects;
    private readonly ScreenNoisePresetDBSO _presetDb;

    private ScreenNoiseEffectController _controller;

    private NoiseState _fromState;
    private NoiseState _destState;

    private bool HasClaimedController { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public ScreenNoiseCommand(
        ScreenNoiseCommandSpec spec,
        ScreenEffectRig screenEffects,
        ScreenNoisePresetDBSO presetDb)
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
                    NoiseState state = NoiseState.Lerp(_fromState, _destState, t);
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
        _controller = _screenEffects.Noise;
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

    private NoiseState CaptureCurrentState()
    {
        return new NoiseState(
            _controller.Amount,
            _controller.Color,
            _controller.Scale,
            _controller.SpeedX,
            _controller.SpeedY,
            _controller.Contrast);
    }

    private NoiseState BuildDestState()
    {
        float intensity = Mathf.Clamp01(_spec.intensity);
        return BuildPresetState(_spec.presetKey, intensity);
    }

    private NoiseState BuildPresetState(string presetKey, float intensity)
    {
        if (_presetDb != null &&
            _presetDb.TryGet(presetKey, out ScreenNoisePresetDBSO.Entry e))
        {
            return new NoiseState(
                e.amount * intensity,
                e.color,
                e.scale,
                e.speedX,
                e.speedY,
                e.contrast);
        }

        Debug.LogWarning(
            $"[ScreenNoiseCommand] Noise preset not found. " +
            $"presetKey='{presetKey}'. Using fallback.",
            _controller);

        if (_presetDb != null &&
            _presetDb.TryGet(ScreenNoisePresetDBSO.DefaultPresetKey, out e))
        {
            return new NoiseState(
                e.amount * intensity,
                e.color,
                e.scale,
                e.speedX,
                e.speedY,
                e.contrast);
        }

        return new NoiseState(
            1f * intensity,
            Color.white,
            0.8f,
            0.015f,
            0.012f,
            1f);
    }

    private void ApplyState(NoiseState state)
    {
        _controller.ApplyImmediate(
            state.Amount,
            state.Color,
            state.Scale,
            state.SpeedX,
            state.SpeedY,
            state.Contrast);
    }

    private readonly struct NoiseState
    {
        public readonly float Amount;
        public readonly Color Color;
        public readonly float Scale;
        public readonly float SpeedX;
        public readonly float SpeedY;
        public readonly float Contrast;

        public NoiseState(
            float amount,
            Color color,
            float scale,
            float speedX,
            float speedY,
            float contrast)
        {
            Amount = Mathf.Clamp01(amount);
            Color = color;
            Scale = Mathf.Max(0f, scale);
            SpeedX = speedX;
            SpeedY = speedY;
            Contrast = Mathf.Max(0f, contrast);
        }

        public static NoiseState Lerp(NoiseState from, NoiseState to, float t)
        {
            t = Mathf.Clamp01(t);

            return new NoiseState(
                Mathf.Lerp(from.Amount, to.Amount, t),
                Color.Lerp(from.Color, to.Color, t),
                Mathf.Lerp(from.Scale, to.Scale, t),
                Mathf.Lerp(from.SpeedX, to.SpeedX, t),
                Mathf.Lerp(from.SpeedY, to.SpeedY, t),
                Mathf.Lerp(from.Contrast, to.Contrast, t));
        }
    }
}
