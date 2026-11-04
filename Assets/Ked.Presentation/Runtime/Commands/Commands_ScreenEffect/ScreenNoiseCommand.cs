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

// 노이즈 의도를 프리셋에서 계산해 넘기기만 한다 — 전환은 컨트롤러가 소유한다.
public sealed class ScreenNoiseCommand : CommandBase
{
    private readonly ScreenNoiseCommandSpec _spec;
    private readonly ScreenEffectRig _screenEffects;
    private readonly ScreenNoisePresetDBSO _presetDb;

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
        ScreenNoiseEffectController controller = _screenEffects.Noise;
        controller.Follow(BuildDestState(controller), _spec.duration, _spec.ease);

        if (!_spec.wait)
            yield break;

        while (controller.IsTransitioning)
            yield return null;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        ScreenNoiseEffectController controller = _screenEffects.Noise;
        controller.Snap(BuildDestState(controller));
    }

    private ScreenNoiseEffectController.State BuildDestState(ScreenNoiseEffectController controller)
    {
        float intensity = Mathf.Clamp01(_spec.intensity);

        if (_presetDb != null &&
            _presetDb.TryGet(_spec.presetKey, out ScreenNoisePresetDBSO.Entry e))
        {
            return new ScreenNoiseEffectController.State(
                e.amount * intensity,
                e.color,
                e.scale,
                e.speedX,
                e.speedY,
                e.contrast);
        }

        Debug.LogWarning(
            $"[ScreenNoiseCommand] Noise preset not found. " +
            $"presetKey='{_spec.presetKey}'. Using fallback.",
            controller);

        if (_presetDb != null &&
            _presetDb.TryGet(ScreenNoisePresetDBSO.DefaultPresetKey, out e))
        {
            return new ScreenNoiseEffectController.State(
                e.amount * intensity,
                e.color,
                e.scale,
                e.speedX,
                e.speedY,
                e.contrast);
        }

        return new ScreenNoiseEffectController.State(
            1f * intensity,
            Color.white,
            0.8f,
            0.015f,
            0.012f,
            1f);
    }
}
