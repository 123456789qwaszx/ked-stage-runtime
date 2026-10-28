using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[Serializable]
public sealed class DepthFocusCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Mode")]
    [Tooltip("true면 slotKey를 무시하고 모든 캐릭터의 블러를 0으로 되돌린다.")]
    public bool clear;

    [Header("Circle of Confusion")]
    [Tooltip("초점면에서 depthScale이 1만큼 떨어질 때 붙는 블러량. " +
             "depthScale은 Far 0.68 ~ Close 1.38이라 최대 간격이 0.70이다 — " +
             "1.43이면 무대 양 끝이 최대로 흐려진다.")]
    [Min(0f)] public float falloff = 1.43f;

    [Tooltip("착란원이 아무리 커도 이 값을 넘지 않는다.")]
    [Range(0f, 1f)] public float maxBlur = 1f;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;
}

/// <summary>
/// 초점 캐릭터를 지명하고, 나머지를 깊이 차이에 비례해 흐린다.
///
/// v2를 버린 이유가 여기서 해결된다 — 레이어를 통째로 균일하게 흐리면 초점이 나간 게 아니라
/// 렌즈에 자국이 남은 것처럼 보인다. 흐림이 깊이마다 달라야 디포커스로 읽힌다.
///
/// 깊이는 레이어가 아니라 <see cref="CharacterRigRefs.SettledDepthScale"/>에서 온다.
/// 캐릭터는 대부분 mid 레이어에 몰려 있고 깊이는 size 커맨드의 프리셋으로 갈리기 때문에,
/// 레이어를 기준으로 삼으면 전원이 같은 값이 되어 v2의 실패를 반복한다.
///
/// 배경은 대상이 아니다 — screen_blur가 이미 레이어 단위로 처리한다. 배경도 캐릭터 리그로 서지만
/// **캐스팅하지 않은 슬롯**이라, 배역이 있는 슬롯만 훑는다(focus_clear도 같다 — 배경의 screen_blur를 지우지 않는다).
/// 플래그가 아니라 구조로 빠진다.
/// </summary>
public sealed class DepthFocusCommandCharR : ClaimTweenCommandBase
{
    private struct Binding
    {
        public RigVisualEffectController Controller;
        public float FromValue;
        public float DestValue;

        public Binding(RigVisualEffectController controller, float fromValue, float destValue)
        {
            Controller = controller;
            FromValue = fromValue;
            DestValue = destValue;
        }
    }

    private readonly DepthFocusCommandSpecCharR _spec;

    private readonly List<Binding> _bindings = new();
    private readonly List<KeyValuePair<string, CharacterRigRefs>> _scratch = new();

    private CharacterRigRefs _focusRig;

    // 트윈 타깃은 커맨드 인스턴스가 아니라 레지스트리다 —
    // this로 잡으면 다음 focus_on이 이전 트윈을 죽이지 못해 둘이 매 프레임 싸운다.
    private CharacterRigRegistry _tweenScope;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    // 화면 전체의 초점이 한꺼번에 옮겨가면 눈에 띄게 튄다 — 훨씬 완만하게 붙인다.
    protected override float StepFinishSpeedUpMultiplier => 1.5f;

    private readonly PresentationStage _stage;

    public DepthFocusCommandCharR(PresentationStage stage, DepthFocusCommandSpecCharR spec)
    {
        _stage = stage;
        _spec = spec;
    }

    protected override void ResolveTargets(CommandRunScope scope)
    {
        if (_spec.clear)
            return;

        _focusRig = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(_stage, _spec.slotKey);

        if (_focusRig == null)
            Debug.LogWarning(
                $"[DepthFocusCommandCharR] Focus character not found. " +
                $"slotKey='{_spec.slotKey}'. 초점면을 정할 수 없어 아무것도 흐리지 않는다.");
    }

    protected override void ClaimTarget(CommandRunScope scope)
    {
        _tweenScope = _stage.characterRigs;
        DOTween.Kill(_tweenScope, false);

        _bindings.Clear();

        if (!_spec.clear && _focusRig == null)
            return;

        float focusScale = _focusRig != null ? _focusRig.SettledDepthScale : 0f;

        _scratch.Clear();
        _stage.characterRigs.CollectAliveRigEntries(_scratch);

        for (int i = 0; i < _scratch.Count; i++)
        {
            CharacterRigRefs refs = _scratch[i].Value;

            if (refs?.VisualEffect == null)
                continue;

            // 배경(캐스팅하지 않은 슬롯)은 초점면 계산에 끼지 않는다 — SettledDepthScale 1로 섞이면
            // 배경이 초점 캐릭터와 같은 깊이에 있는 척 흐려지거나 선명해진다.
            if (!_stage.castRegistry.TryPeekCharacter(_scratch[i].Key, out _))
                continue;

            _bindings.Add(new Binding(
                refs.VisualEffect,
                refs.VisualEffect.BlurAmount,
                _spec.clear ? 0f : CircleOfConfusion(refs.SettledDepthScale, focusScale)));
        }
    }

    private float CircleOfConfusion(float depthScale, float focusScale)
        => Mathf.Clamp01(Mathf.Abs(depthScale - focusScale) * _spec.falloff)
           * Mathf.Clamp01(_spec.maxBlur);

    protected override Tween CreateTween(float duration)
        => DOTween
            .To(
                () => 0f,
                ApplyAt,
                1f,
                duration)
            .SetEase(_spec.ease)
            .SetTarget(_tweenScope);

    /// <summary>
    /// 진행률(0→1) 트윈이라 그대로 재시작하면 처음부터 다시 돈다 —
    /// 현재 값을 새 출발점으로 삼아 남은 구간만 태운다.
    /// </summary>
    protected override Tween CreateAcceleratedTween(float duration)
    {
        RefreshFromValues();

        return CreateTween(duration);
    }

    protected override void OnCommitFinalState()
    {
        DOTween.Kill(_tweenScope, false);

        ApplyAt(1f);
    }

    protected override float MeasureRemainingRatio()
        => RemainingRatio(ClaimedDistance(), CurrentDistance());

    private void RefreshFromValues()
    {
        for (int i = 0; i < _bindings.Count; i++)
        {
            Binding binding = _bindings[i];

            if (binding.Controller == null)
                continue;

            binding.FromValue = binding.Controller.BlurAmount;
            _bindings[i] = binding;
        }
    }

    private void ApplyAt(float t)
    {
        t = Mathf.Clamp01(t);

        for (int i = 0; i < _bindings.Count; i++)
        {
            Binding binding = _bindings[i];

            if (binding.Controller == null)
                continue;

            binding.Controller.SetBlurAmountImmediate(
                Mathf.Lerp(binding.FromValue, binding.DestValue, t));
        }
    }

    // 가장 멀리 가야 하는 캐릭터를 기준으로 잰다 — 하나라도 남아 있으면 아직 안 끝난 것이다.
    private float ClaimedDistance()
    {
        float max = 0f;

        for (int i = 0; i < _bindings.Count; i++)
            max = Mathf.Max(max, Mathf.Abs(_bindings[i].DestValue - _bindings[i].FromValue));

        return max;
    }

    private float CurrentDistance()
    {
        float max = 0f;

        for (int i = 0; i < _bindings.Count; i++)
        {
            Binding binding = _bindings[i];

            if (binding.Controller == null)
                continue;

            max = Mathf.Max(max, Mathf.Abs(binding.DestValue - binding.Controller.BlurAmount));
        }

        return max;
    }
}
