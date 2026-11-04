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

// 비네트 의도를 프리셋에서 계산해 넘기기만 한다 — 전환은 컨트롤러가 소유한다.
public sealed class ScreenVignetteCommand : CommandBase
{
    private readonly ScreenVignetteCommandSpec _spec;
    private readonly ScreenEffectRig _screenEffects;
    private readonly ScreenVignettePresetDBSO _presetDb;

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
        ScreenVignetteEffectController controller = _screenEffects.Vignette;
        controller.Follow(BuildDestState(controller), _spec.duration, _spec.ease);

        if (!_spec.wait)
            yield break;

        while (controller.IsTransitioning)
            yield return null;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        ScreenVignetteEffectController controller = _screenEffects.Vignette;
        controller.Snap(BuildDestState(controller));
    }

    private ScreenVignetteEffectController.State BuildDestState(ScreenVignetteEffectController controller)
    {
        float intensity = Mathf.Clamp01(_spec.intensity);

        if (_presetDb != null &&
            _presetDb.TryGet(_spec.presetKey, out ScreenVignettePresetDBSO.Entry e))
        {
            return new ScreenVignetteEffectController.State(
                e.amount * intensity,
                e.color,
                e.radius,
                e.softness,
                e.aspect);
        }

        Debug.LogWarning(
            $"[ScreenVignetteCommand] Vignette preset not found. " +
            $"presetKey='{_spec.presetKey}'. Using fallback.",
            controller);

        if (_presetDb != null &&
            _presetDb.TryGet(ScreenVignettePresetDBSO.DefaultPresetKey, out e))
        {
            return new ScreenVignetteEffectController.State(
                e.amount * intensity,
                e.color,
                e.radius,
                e.softness,
                e.aspect);
        }

        return new ScreenVignetteEffectController.State(
            0.35f * intensity,
            Color.black,
            0.25f,
            0.10f,
            1.2f);
    }
}
