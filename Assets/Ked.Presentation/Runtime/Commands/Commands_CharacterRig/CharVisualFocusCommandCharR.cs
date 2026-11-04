using System;
using System.Collections;
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

// char_visual — 프리셋으로 그 리그의 모양(어둡게·림·자체 블러) 의도를 계산해 넘기기만 한다.
// 전환은 리그의 RigVisualEffectController가 소유한다 — 같은 줄에 두 번 걸어도 나중 것이 이긴다.
public sealed class CharVisualFocusCommandCharR : CommandBase
{
    // 캐릭터 전체의 밝기, 림이 한꺼번에 끊기면 튐. 완만하게 조정
    private const float StepFinishSpeedUpMultiplier = 1.5f;

    private readonly CharVisualFocusCommandSpecCharR _spec;
    private readonly CharacterVisualFocusPresetDBSO _presetDb;
    private readonly PresentationStage _stage;

    private RigVisualEffectController _controller;
    private bool _resolved;

    public override bool WaitForCompletion => _spec.wait;

    public CharVisualFocusCommandCharR(
        PresentationStage stage,
        CharVisualFocusCommandSpecCharR spec,
        CharacterVisualFocusPresetDBSO presetDb)
    {
        _stage = stage;
        _spec = spec;
        _presetDb = presetDb;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!TryResolve())
            yield break;

        _controller.FollowStyle(BuildDestState(), _spec.duration, _spec.ease);

        if (!_spec.wait)
            yield break;

        while (_controller.IsStyleTransitioning)
            yield return null;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (TryResolve())
            _controller.SnapStyle(BuildDestState());
    }

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        _controller?.AccelerateStyle(StepFinishSpeedUpMultiplier);
    }

    private bool TryResolve()
    {
        if (_resolved)
            return _controller != null;

        _resolved = true;

        CharacterRigRefs rigRefs =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(_stage, _spec.slotKey);

        _controller = rigRefs?.VisualEffect;

        if (_controller == null)
            Debug.LogWarning(
                $"[CharVisualFocusCommandCharR] VisualEffect controller is missing. " +
                $"SetupCharRig가 컨트롤러를 생성했는지, source material 경로가 맞는지 확인하세요. " +
                $"slotKey='{_spec.slotKey}'.");

        return _controller != null;
    }

    private RigVisualEffectController.Style BuildDestState()
    {
        float intensity = Mathf.Clamp01(_spec.intensity);
        return BuildPresetState(_spec.presetKey, intensity);
    }

    private RigVisualEffectController.Style BuildPresetState(string presetKey, float intensity)
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

        return new RigVisualEffectController.Style(
            0f,
            new Color(0.45f, 0.48f, 0.55f, 1f),
            0.4f * intensity,
            0.09f * intensity,
            Color.white,
            new Color(1f, 0.96f, 0.86f, 1f),
            0f);
    }

    private static RigVisualEffectController.Style EntryToState(
        CharacterVisualFocusPresetDBSO.Entry entry,
        float intensity)
    {
        return new RigVisualEffectController.Style(
            entry.dim * intensity,
            entry.dimTintColor,
            entry.outerRim * intensity,
            entry.innerRim * intensity,
            entry.outerRimColor,
            entry.innerRimColor,
            entry.blur * intensity);
    }
}
