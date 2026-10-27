using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// 표정 교체(face_swap). Spine 이관 대상이지만 저작 콘텐츠에서 가장 많이 쓰이는 커맨드라
// 마이그레이션이 끝날 때까지 스프라이트 경로를 유지한다.
//
// 겹 구조: CharacterPortraitSpriteOverlay_Root가 초상 위에 얹혀 새 표정을 먼저 띄우고,
// 완전히 덮은 순간 밑장(CharacterPortraitSprite_Image)을 갈아끼운 뒤 위 겹을 걷는다.
// 정지 프레임에는 밑장만 남으므로 코어 리듀서는 face와 같은 자리(ApplyFace)에서 접는다.
[Serializable]
public sealed class SetEmotionPortraitWipeCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Portrait Identity")]
    public PortraitIdentity portrait;

    [Header("Tween")]
    [Range(0f, 2f)]
    public float duration = 0.38f;

    public Ease ease = Ease.OutCubic;

    [Header("Sizing Policy")]
    public CharRigImageSizingMode sizingMode = CharRigImageSizingMode.HeightFitPreserveAspect;

    public CharRigImageSizingPolicy.HorizontalAlign horizontalAlign =
        CharRigImageSizingPolicy.HorizontalAlign.Center;
}

public sealed class SetEmotionPortraitWipeCommandCharR : CommandBase
{
    private readonly SetEmotionPortraitWipeCommandSpecCharR _spec;
    private readonly PortraitResolver _resolver;

    private RectTransform _portraitRoot;
    private RectTransform _overlayRoot;
    private Image _portraitImage;
    private Image _overlayImage;
    private CanvasGroup _portraitCanvasGroup;
    private CanvasGroup _overlayCanvasGroup;

    private Sprite _targetSprite;
    private Sequence _seq;

    private bool _resolveAttempted;
    private bool _hasResolvedTargetSprite;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    private readonly PresentationStage _stage;

    public SetEmotionPortraitWipeCommandCharR(
        PresentationStage stage,
        SetEmotionPortraitWipeCommandSpecCharR spec,
        PortraitResolver resolver)
    {
        _stage = stage;
        _spec = spec;
        _resolver = resolver;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ResolveTargetSprite(scope);

        ClaimTarget();
        PrepareTransitionState();

        if (!HasOverlayLayer || _spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _seq = DOTween.Sequence()
            .SetUpdate(true)
            .SetTarget(_overlayRoot);

        _seq.Append(
            _overlayCanvasGroup
                .DOFade(1f, _spec.duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(_overlayCanvasGroup));

        _seq.AppendCallback(() =>
        {
            _portraitImage.sprite = _targetSprite;
            ApplySizing(_portraitImage, _targetSprite);
            _portraitCanvasGroup.alpha = 1f;
        });

        _seq.Append(
            _overlayCanvasGroup
                .DOFade(0f, _spec.duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(_overlayCanvasGroup));

        _seq.OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return _seq.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ResolveTargetSprite(scope);

        if (!HasClaimedTarget)
            ClaimTarget();
        else
            KillCurrentTween();

        CommitFinalState();
    }

    // 위 겹이 없는 리그(구 프리팹)에서는 밑장 즉시 교체로 굴러간다 — 연출만 빠진다.
    private bool HasOverlayLayer =>
        _overlayRoot != null && _overlayImage != null && _overlayCanvasGroup != null;

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(_stage, _spec.slotKey);

        _portraitRoot = rigRefs.CharacterPortraitSprite_Root;
        _overlayRoot = rigRefs.CharacterPortraitSpriteOverlay_Root;
        _portraitImage = rigRefs.CharacterPortraitSprite_Image;
        _overlayImage = rigRefs.CharacterPortraitSpriteOverlay_Image;

        _portraitCanvasGroup = _portraitRoot != null ? _portraitRoot.GetComponent<CanvasGroup>() : null;
        _overlayCanvasGroup = _overlayRoot != null ? _overlayRoot.GetComponent<CanvasGroup>() : null;

        if (_overlayRoot == null || _overlayImage == null)
        {
            Debug.LogWarning(
                $"[{nameof(SetEmotionPortraitWipeCommandCharR)}] Missing portrait overlay layer on " +
                $"slotKey='{_spec.slotKey}'. Falling back to an instant sprite swap.");
        }
    }

    private void ResolveTargetSprite(CommandRunScope scope)
    {
        if (_hasResolvedTargetSprite)
            return;

        _targetSprite = _resolver.Resolve(
            _stage,
            _spec.slotKey,
            _spec.portrait,
            nameof(SetEmotionPortraitWipeCommandCharR));

        _hasResolvedTargetSprite = true;
    }

    private void ClaimTarget()
    {
        KillPreviousTween();

        HasClaimedTarget = true;
    }

    private void PrepareTransitionState()
    {
        if (_portraitCanvasGroup != null)
            _portraitCanvasGroup.alpha = 1f;

        if (!HasOverlayLayer)
            return;

        _overlayCanvasGroup.alpha = 0f;

        _overlayImage.enabled = true;
        _overlayImage.sprite = _targetSprite;
        ApplySizing(_overlayImage, _targetSprite);
    }

    private void CommitFinalState()
    {
        _portraitImage.sprite = _targetSprite;
        ApplySizing(_portraitImage, _targetSprite);

        if (_portraitCanvasGroup != null)
            _portraitCanvasGroup.alpha = 1f;

        if (HasOverlayLayer)
        {
            _overlayCanvasGroup.alpha = 0f;
            _overlayImage.enabled = false;
        }

        HasClaimedTarget = false;
    }

    private void KillPreviousTween() => KillTweens(true);

    private void KillCurrentTween() => KillTweens(false);

    private void KillTweens(bool complete)
    {
        _seq?.Kill(false);
        _seq = null;

        if (_overlayCanvasGroup != null)
            _overlayCanvasGroup.DOKill(complete);

        if (_portraitCanvasGroup != null)
            _portraitCanvasGroup.DOKill(complete);

        if (_overlayRoot != null)
            _overlayRoot.DOKill(complete);

        if (_portraitRoot != null)
            _portraitRoot.DOKill(complete);
    }

    private void ApplySizing(Image image, Sprite sprite)
    {
        CharRigImageSizingPolicy.Apply(
            image,
            sprite,
            _spec.sizingMode,
            _spec.horizontalAlign);
    }
}
