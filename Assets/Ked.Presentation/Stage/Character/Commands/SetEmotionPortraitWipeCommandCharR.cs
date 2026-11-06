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
//
// 스텝 경계는 비워 둔다 — 밑장에 쓰는 커맨드가 모두 CompleteInFlight를 거치므로 끝까지 돌아도 나중 쓰기가 이긴다.
// 초상 α는 fade_in/out의 축이라 쓰지 않는다.
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

    private RectTransform _overlayRoot;
    private Image _portraitImage;
    private Image _overlayImage;
    private CanvasGroup _portraitCanvasGroup;
    private CanvasGroup _overlayCanvasGroup;

    private Sprite _targetSprite;
    private Sequence _seq;

    private bool _resolveAttempted;
    private bool _hasResolvedTargetSprite;

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

    // 밑장(스프라이트·사이징)에 쓰는 커맨드는 쓰기 전에 부른다.
    // 진행 중인 face_swap을 완주시켜(OnComplete가 제 표정을 확정) 나중 쓰기가 이기게 한다.
    public static void CompleteInFlight(CharacterRigRefs rig)
        => CompleteInFlight(rig.CharacterPortraitSpriteOverlay_Root);

    private static void CompleteInFlight(RectTransform overlayRoot)
        => overlayRoot.DOKill(true);

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        ResolveTargetSprite();

        ClaimTarget();
        PrepareTransitionState();

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _seq = DOTween.Sequence()
            .SetUpdate(true)
            .SetTarget(_overlayRoot);

        _seq.Append(CreateOverlayTween(0f, 1f));

        _seq.AppendCallback(() =>
        {
            _portraitImage.sprite = _targetSprite;
            ApplySizing(_portraitImage, _targetSprite);
        });

        _seq.Append(CreateOverlayTween(1f, 0f));

        _seq.OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return _seq.WaitForCompletion();
    }

    protected override void OnSkip()
    {
        if (!_resolveAttempted)
            ResolveRefs();

        ResolveTargetSprite();

        // OnSkip은 ExecuteInner 전에만 불린다 — 아직 잡은 것이 없다.
        ClaimTarget();
        CommitFinalState();
    }

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        CharacterRigRefs rigRefs = CharacterRigResolver.ResolveRig(_stage, _spec.roleKey);

        _overlayRoot = rigRefs.CharacterPortraitSpriteOverlay_Root;
        _portraitImage = rigRefs.CharacterPortraitSprite_Image;
        _overlayImage = rigRefs.CharacterPortraitSpriteOverlay_Image;

        _portraitCanvasGroup = rigRefs.CharacterPortraitSprite_Root.GetComponent<CanvasGroup>();
        _overlayCanvasGroup = _overlayRoot.GetComponent<CanvasGroup>();
    }

    private void ResolveTargetSprite()
    {
        if (_hasResolvedTargetSprite)
            return;

        _targetSprite = _resolver.Resolve(
            _stage,
            _spec.roleKey,
            _spec.portrait,
            nameof(SetEmotionPortraitWipeCommandCharR));

        _hasResolvedTargetSprite = true;
    }

    private void ClaimTarget() => CompleteInFlight(_overlayRoot);

    private void PrepareTransitionState()
    {
        _overlayCanvasGroup.alpha = 0f;

        _overlayImage.enabled = true;
        _overlayImage.sprite = _targetSprite;
        ApplySizing(_overlayImage, _targetSprite);
    }

    // 위 겹은 밑장의 형제라 초상 α를 물려받지 않는다 — 덮은 정도에 초상 α를 곱해 따라간다.
    private Tween CreateOverlayTween(float from, float to)
        => DOTween
            .To(() => from, ApplyOverlayCoverage, to, _spec.duration)
            .SetEase(_spec.ease);

    private void ApplyOverlayCoverage(float coverage)
        => _overlayCanvasGroup.alpha = coverage * _portraitCanvasGroup.alpha;

    private void CommitFinalState()
    {
        _portraitImage.sprite = _targetSprite;
        ApplySizing(_portraitImage, _targetSprite);

        _overlayCanvasGroup.alpha = 0f;
        _overlayImage.enabled = false;
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
