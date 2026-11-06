using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
public sealed class StageLayerBlurCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public string stage = StageKeyParser.DefaultStage;
    public float layerDepth = Ked.Presentation.Core.StageLayerLabels.Far;

    [Tooltip("true면 무대 · 층 전부의 레이어 블러를 걷는다(screen_blur_reset). stage · layerDepth는 무시.")]
    public bool clearAll;

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
/// 배경도 캐릭터 리그다(캐스팅하지 않은 슬롯) — 그 레이어에 있으면 같이 흐려진다.
///
/// 레이어 블러는 장면 전역 의도다 — 의도만 RigBlurIntentSystem에 넘기고, 줄 머리에서 다시 맞추는 것은 시스템이 한다.
/// 그래서 뒤늦게 그 레이어에 붙은 리그도 흐려진다.
/// 모양(char_visual)·초점(focus_on) 블러와는 리그 컨트롤러에서 가장 큰 값으로 합쳐진다.
/// </summary>
public sealed class StageLayerBlurCommand : CommandBase
{
    // 화면의 큰 면적이 한꺼번에 초점을 되찾으면 눈에 띄게 튄다 — 훨씬 완만하게 붙인다.
    private const float StepFinishSpeedUpMultiplier = 1.5f;

    private readonly StageLayerBlurCommandSpec _spec;
    private readonly RigBlurIntentSystem _blur;

    public override bool WaitForCompletion => _spec.wait;

    public StageLayerBlurCommand(RigBlurIntentSystem blur, StageLayerBlurCommandSpec spec)
    {
        _blur = blur;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Apply(snap: false);

        if (!_spec.wait)
            yield break;

        while (_blur.IsLayerTransitioning)
            yield return null;
    }

    protected override void OnSkip() => Apply(snap: true);

    protected override void OnStepLifetimeFinished()
        => _blur.AccelerateLayer(StepFinishSpeedUpMultiplier);

    private void Apply(bool snap)
    {
        if (_spec.clearAll)
            _blur.ClearLayerBlur(_spec.duration, _spec.ease, snap);
        else
            _blur.SetLayerBlur(_spec.stage, _spec.layerDepth, _spec.amount, _spec.duration, _spec.ease, snap);
    }
}
