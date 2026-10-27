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

public sealed class ScreenFlashCommand : CommandBase
{
    private readonly ScreenFlashCommandSpec _spec;
    private readonly ScreenEffectRig _screenEffects;
    private readonly ScreenFlashPresetDBSO _presetDb;

    private ScreenFlashEffectController _controller;
    private FlashSettings _settings;

    private bool HasClaimedController { get; set; }

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
        ClaimController();

        if (_settings.AttackDuration <= 0f &&
            _settings.HoldDuration <= 0f &&
            _settings.ReleaseDuration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _controller.ApplyImmediate(0f, _settings.Color);

        Sequence sequence = DOTween.Sequence()
            .SetTarget(_controller.transform)
            .SetUpdate(true);

        if (_settings.AttackDuration > 0f)
        {
            sequence.Append(DOTween.To(
                    () => _controller.FlashAmount,
                    value => _controller.ApplyImmediate(value, _settings.Color),
                    _settings.Amount,
                    _settings.AttackDuration)
                .SetEase(_settings.AttackEase)
                .SetTarget(_controller.transform));
        }
        else
        {
            sequence.AppendCallback(() =>
            {
                _controller.ApplyImmediate(_settings.Amount, _settings.Color);
            });
        }

        if (_settings.HoldDuration > 0f)
            sequence.AppendInterval(_settings.HoldDuration);

        if (_settings.ReleaseDuration > 0f)
        {
            sequence.Append(DOTween.To(
                    () => _controller.FlashAmount,
                    value => _controller.ApplyImmediate(value, _settings.Color),
                    0f,
                    _settings.ReleaseDuration)
                .SetEase(_settings.ReleaseEase)
                .SetTarget(_controller.transform));
        }
        else
        {
            sequence.AppendCallback(() =>
            {
                _controller.ApplyImmediate(0f, _settings.Color);
            });
        }

        sequence.OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return sequence.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!HasClaimedController)
            ClaimController();

        CommitFinalState();
    }

    private void ClaimController()
    {
        _controller = _screenEffects.Flash;
        _controller.KillTween(true);

        _settings = BuildSettings();

        HasClaimedController = true;
    }

    private void CommitFinalState()
    {
        _controller.KillTween(false);
        _controller.ApplyImmediate(0f, _settings.Color);

        HasClaimedController = false;
    }

    private FlashSettings BuildSettings()
    {
        float intensity = Mathf.Clamp01(_spec.intensity);
        return BuildPresetSettings(_spec.presetKey, intensity);
    }

    private FlashSettings BuildPresetSettings(string presetKey, float intensity)
    {
        if (_presetDb != null &&
            _presetDb.TryGet(presetKey, out ScreenFlashPresetDBSO.Entry e))
        {
            return new FlashSettings(
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
            $"presetKey='{presetKey}'. Using fallback.",
            _controller);

        if (_presetDb != null &&
            _presetDb.TryGet(ScreenFlashPresetDBSO.DefaultPresetKey, out e))
        {
            return new FlashSettings(
                e.color,
                e.amount * intensity,
                e.attackDuration,
                e.holdDuration,
                e.releaseDuration,
                e.attackEase,
                e.releaseEase);
        }

        return new FlashSettings(
            Color.white,
            1f * intensity,
            0.02f,
            0.01f,
            0.16f,
            Ease.OutCubic,
            Ease.OutCubic);
    }

    private readonly struct FlashSettings
    {
        public readonly Color Color;
        public readonly float Amount;
        public readonly float AttackDuration;
        public readonly float HoldDuration;
        public readonly float ReleaseDuration;
        public readonly Ease AttackEase;
        public readonly Ease ReleaseEase;

        public FlashSettings(
            Color color,
            float amount,
            float attackDuration,
            float holdDuration,
            float releaseDuration,
            Ease attackEase,
            Ease releaseEase)
        {
            Color = color;
            Amount = Mathf.Clamp01(amount);
            AttackDuration = Mathf.Max(0f, attackDuration);
            HoldDuration = Mathf.Max(0f, holdDuration);
            ReleaseDuration = Mathf.Max(0f, releaseDuration);
            AttackEase = attackEase;
            ReleaseEase = releaseEase;
        }
    }
}