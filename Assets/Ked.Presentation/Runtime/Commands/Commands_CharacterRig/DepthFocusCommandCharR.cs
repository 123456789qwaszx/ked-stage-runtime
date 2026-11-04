using System;
using System.Collections;
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
/// 깊이는 레이어가 아니라 <see cref="CharacterRigRefs.SettledDepthScale"/>에서 온다.
///
/// 배경은 대상이 아니다 — screen_blur가 레이어 단위로 처리한다. 배역이 있는 슬롯만 흐린다(focus_clear도 같다).
///
/// 초점은 장면 전역 의도다 — 의도만 RigBlurIntentSystem에 넘기고, 리그마다 블러를 정하고 줄 머리에서 다시 맞추는 것은 시스템이 한다.
/// 그래서 뒤에 등장한 캐릭터, size로 바뀐 깊이도 초점을 따라간다.
/// </summary>
public sealed class DepthFocusCommandCharR : CommandBase
{
    // 화면 전체의 초점이 한꺼번에 옮겨가면 눈에 띄게 튄다 — 훨씬 완만하게 붙인다.
    private const float StepFinishSpeedUpMultiplier = 1.5f;

    private readonly DepthFocusCommandSpecCharR _spec;
    private readonly RigBlurIntentSystem _blur;

    public override bool WaitForCompletion => _spec.wait;

    public DepthFocusCommandCharR(RigBlurIntentSystem blur, DepthFocusCommandSpecCharR spec)
    {
        _blur = blur;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Apply(snap: false);

        if (!_spec.wait)
            yield break;

        while (_blur.IsFocusTransitioning)
            yield return null;
    }

    protected override void OnSkip(CommandRunScope scope) => Apply(snap: true);

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
        => _blur.AccelerateFocus(StepFinishSpeedUpMultiplier);

    private void Apply(bool snap)
    {
        if (_spec.clear)
            _blur.ClearFocus(_spec.duration, _spec.ease, snap);
        else
            _blur.SetFocus(_spec.slotKey, _spec.falloff, _spec.maxBlur, _spec.duration, _spec.ease, snap);
    }
}
