using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
public sealed class FadeCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Node")]
    public CharacterRigNode node = CharacterRigNode.RigRoot;

    [Tooltip("목표 alpha [0,1]. fade_in은 1, fade_out은 0(코어 NodeTargets.FadeIn · NodeTargets.FadeOut의 값)이고, 그 사이는 반투명까지의 페이드다.")]
    [Range(0f, 1f)]
    public float targetAlpha = 1f;

    [Tooltip("페이드 시간(초). 0 이하이면 즉시 스냅합니다.")]
    public float duration = 0.4f;

    public Ease ease = Ease.OutCubic;
}

/// <summary>
/// 리그 노드의 CanvasGroup을 목표 alpha까지 페이드한다 — fade_in · fade_out · bg_fade_in/out · show가 모두 이것 하나다.
/// 확정하면 보이는 동안만 입력을 받는다(alpha가 0이면 interactable · blocksRaycasts를 끈다).
/// </summary>
public sealed class FadeCommandCharR : ClaimTweenCommandBase
{
    private readonly PresentationStage _stage;
    private readonly FadeCommandSpecCharR _spec;

    private CanvasGroup _group;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    public FadeCommandCharR(PresentationStage stage, FadeCommandSpecCharR spec)
    {
        _stage = stage;
        _spec = spec;
    }

    protected override void ResolveRefs()
        => _group = CharacterRigResolver.ResolveRig(_stage, _spec.roleKey)
            .GetRect(_spec.node)
            .GetComponent<CanvasGroup>();

    protected override void ClaimTarget()
    {
        _group.DOKill(true);
    }

    protected override Tween CreateTween(float duration)
        => _group
            .DOFade(_spec.targetAlpha, duration)
            .SetEase(_spec.ease)
            .SetTarget(_group);

    protected override void OnCommitFinalState()
    {
        bool visible = _spec.targetAlpha > 0f;

        _group.alpha = _spec.targetAlpha;
        _group.interactable = visible;
        _group.blocksRaycasts = visible;
    }

}
