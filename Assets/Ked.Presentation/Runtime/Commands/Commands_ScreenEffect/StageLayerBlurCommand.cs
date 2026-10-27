using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[Serializable]
public sealed class StageLayerBlurCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public PresentationStageKey stage = PresentationStageKey.Stage00;
    public PresentationDepthLayerKey layer = PresentationDepthLayerKey.Far;

    [Header("Blur")]
    [Tooltip("0=선명, 1=최대. 셰이더의 MaxLod를 0~2로 스케일한다.")]
    [Range(0f, 1f)] public float amount = 1f;

    [Header("Tween")]
    public float duration = 0.8f;
    public Ease ease = Ease.OutCubic;
}

/// <summary>
/// 한 stage/layer 아래에 있는 리그들의 블러를 함께 움직인다.
///
/// 옛 캡처 방식(레이어를 통째로 RT에 굽고 오버레이로 덮기)을 대체한다. 판단은 그대로다 —
/// "이 레이어에 있는 것을 흐린다". 기구만 요소별 셰이더로 바뀌었고, 그래서
/// 착란원이 요소마다 따로 서고 렌더러 종류(Image / 훗날 SkeletonGraphic)를 가리지 않는다.
///
/// 배경과 캐릭터를 모두 훑는다. far 레이어에는 현재 배경만 있지만, 캐릭터를 그 레이어에
/// 올리면 같이 흐려지는 것이 이 커맨드의 의미상 맞다.
/// </summary>
public sealed class StageLayerBlurCommand : ClaimTweenCommandBase
{
    private struct Binding
    {
        public RigVisualEffectController Controller;
        public float FromValue;

        public Binding(RigVisualEffectController controller, float fromValue)
        {
            Controller = controller;
            FromValue = fromValue;
        }
    }

    private readonly StageLayerBlurCommandSpec _spec;
    private readonly IStageDepthContentSlotProvider _slots;

    private readonly List<Binding> _bindings = new();
    private readonly List<CharacterRigRefs> _charScratch = new();
    private readonly List<BackgroundRigRefs> _bgScratch = new();

    private RectTransform _contentRoot;
    private float _destAmount;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    // 화면의 큰 면적이 한꺼번에 초점을 되찾으면 눈에 띄게 튄다 — 훨씬 완만하게 붙인다.
    protected override float StepFinishSpeedUpMultiplier => 1.5f;

    private readonly PresentationStage _stage;

    public StageLayerBlurCommand(
        PresentationStage stage,
        StageLayerBlurCommandSpec spec,
        IStageDepthContentSlotProvider slots)
    {
        _stage = stage;
        _spec = spec;
        _slots = slots;
    }

    protected override void ResolveTargets(CommandRunScope scope)
    {
        _contentRoot = _slots?.GetDepthContent(_spec.stage, _spec.layer);

        if (_contentRoot == null)
            Debug.LogWarning(
                $"[StageLayerBlurCommand] Depth content slot is missing. " +
                $"stage='{_spec.stage}', layer='{_spec.layer}'.");
    }

    // Claim 시점에 레이어 아래에 있던 리그만 잡는다.
    // 뒤늦게 붙는 리그는 의도적으로 포함하지 않는다 (레거시 CollectEdgeHideControllers와 같은 규칙).
    private void CollectBindings(CommandRunScope scope)
    {
        _bindings.Clear();

        if (_contentRoot == null)
            return;

        _charScratch.Clear();
        _stage.characterRigs.CollectAliveRigs(_charScratch);

        for (int i = 0; i < _charScratch.Count; i++)
        {
            CharacterRigRefs refs = _charScratch[i];

            if (refs?.VisualEffect == null)
                continue;

            if (!IsDescendantOf(refs.RigRoot, _contentRoot))
                continue;

            _bindings.Add(new Binding(refs.VisualEffect, refs.VisualEffect.BlurAmount));
        }

        _bgScratch.Clear();
        _stage.backgroundRigs.CollectAliveRigs(_bgScratch);

        for (int i = 0; i < _bgScratch.Count; i++)
        {
            BackgroundRigRefs refs = _bgScratch[i];

            if (refs?.VisualEffect == null)
                continue;

            if (!IsDescendantOf(refs.RigRoot, _contentRoot))
                continue;

            _bindings.Add(new Binding(refs.VisualEffect, refs.VisualEffect.BlurAmount));
        }
    }

    protected override void ClaimTarget(CommandRunScope scope)
    {
        // 트윈 타깃은 커맨드 인스턴스가 아니라 레이어의 content root다 —
        // this로 잡으면 같은 레이어에 screen_blur를 다시 걸 때 이전 트윈이 안 죽어 둘이 싸운다.
        DOTween.Kill(_contentRoot, false);

        CollectBindings(scope);

        _destAmount = Mathf.Clamp01(_spec.amount);
    }

    protected override Tween CreateTween(float duration)
        => DOTween
            .To(
                () => 0f,
                ApplyAt,
                1f,
                duration)
            .SetEase(_spec.ease)
            .SetTarget(_contentRoot);

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
        DOTween.Kill(_contentRoot, false);

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
                Mathf.Lerp(binding.FromValue, _destAmount, t));
        }
    }

    // 가장 멀리 가야 하는 리그를 기준으로 잰다 — 하나라도 남아 있으면 아직 안 끝난 것이다.
    private float ClaimedDistance()
    {
        float max = 0f;

        for (int i = 0; i < _bindings.Count; i++)
            max = Mathf.Max(max, Mathf.Abs(_destAmount - _bindings[i].FromValue));

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

            max = Mathf.Max(max, Mathf.Abs(_destAmount - binding.Controller.BlurAmount));
        }

        return max;
    }

    private static bool IsDescendantOf(Transform child, Transform parent)
    {
        if (child == null || parent == null)
            return false;

        Transform t = child;

        while (t != null)
        {
            if (t == parent)
                return true;

            t = t.parent;
        }

        return false;
    }
}
