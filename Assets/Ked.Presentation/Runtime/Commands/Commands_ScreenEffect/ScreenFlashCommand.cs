using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
public sealed class ScreenFlashCommandSpec : CommandSpecBase
{
    [Header("Preset")]
    [Tooltip("ScreenFlashPresetDBSO entry key. ex) clear, default, soft, hit, camera")]
    public string presetKey = ScreenFlashPresetDBSO.DefaultPresetKey;

    [Range(0f, 1f)]
    public float intensity = 1f;
}

// 플래시 이벤트를 프리셋에서 만들어 컨트롤러에 터뜨리게 한다 — 재생은 컨트롤러가 소유한다.
// 압축 중에는 터뜨리지 않는다(끝이 0이라 남길 상태가 없다). 대신 재생 중인 플래시를 끊는다.
public sealed class ScreenFlashCommand : CommandBase
{
    private readonly ScreenFlashCommandSpec _spec;
    private readonly ScreenEffectRig _screenEffects;
    private readonly ScreenFlashPresetDBSO _presetDb;

    public override bool WaitForCompletion => _spec.wait;

    public ScreenFlashCommand(
        ScreenFlashCommandSpec spec,
        ScreenEffectRig screenEffects,
        ScreenFlashPresetDBSO presetDb)
    {
        _spec = spec;
        _screenEffects = screenEffects;
        _presetDb = presetDb;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        ScreenFlashEffectController controller = _screenEffects.Flash;
        controller.Play(BuildPulse(controller));

        if (!_spec.wait)
            yield break;

        while (controller.IsPlaying)
            yield return null;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        _screenEffects.Flash.ClearImmediate();
    }

    private ScreenFlashEffectController.Pulse BuildPulse(ScreenFlashEffectController controller)
    {
        float intensity = Mathf.Clamp01(_spec.intensity);

        if (_presetDb != null &&
            _presetDb.TryGet(_spec.presetKey, out ScreenFlashPresetDBSO.Entry e))
        {
            return new ScreenFlashEffectController.Pulse(
                e.color,
                e.amount * intensity,
                e.attackDuration,
                e.holdDuration,
                e.releaseDuration,
                e.attackEase,
                e.releaseEase);
        }

        Debug.LogWarning(
            $"[ScreenFlashCommand] Flash preset not found. " +
            $"presetKey='{_spec.presetKey}'. Using fallback.",
            controller);

        if (_presetDb != null &&
            _presetDb.TryGet(ScreenFlashPresetDBSO.DefaultPresetKey, out e))
        {
            return new ScreenFlashEffectController.Pulse(
                e.color,
                e.amount * intensity,
                e.attackDuration,
                e.holdDuration,
                e.releaseDuration,
                e.attackEase,
                e.releaseEase);
        }

        return new ScreenFlashEffectController.Pulse(
            Color.white,
            1f * intensity,
            0.02f,
            0.01f,
            0.16f,
            Ease.OutCubic,
            Ease.OutCubic);
    }
}
