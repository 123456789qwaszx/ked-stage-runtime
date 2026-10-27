using System;
using DG.Tweening;
using UnityEngine;

public enum CharacterMirrorMode
{
    Toggle,
    Left,
    Right,
}

[Serializable]
public sealed class MirrorCharacterCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Mirror")]
    public CharacterMirrorMode mode = CharacterMirrorMode.Toggle;

    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_ActingScale_X;

    [Header("Tween")]
    public float duration = 0f;
    public Ease ease = Ease.OutCubic;
}

public sealed class MirrorCharacterCommandCharR : ClaimTweenCommandBase
{
    private readonly MirrorCharacterCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector3 _targetScale;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    // 좌우 반전은 0→1 진행이 아니라 배율 부호 뒤집기다 —
    // 스텝 경계에서는 가속할 것 없이 곧장 확정한다.
    protected override bool AccelerateOnStepFinish => false;

    private readonly PresentationStage _stage;

    public MirrorCharacterCommandCharR(PresentationStage stage, MirrorCharacterCommandSpecCharR spec)
    {
        _stage = stage;
        _spec = spec;
    }

    /// <summary>
    /// 해석과 동시에 방향을 정해 대장에 기록한다 — 대장이 곧 다음 Toggle의 입력이라
    /// 커맨드가 여러 번 불려도 방향이 한 번만 뒤집히도록 여기(1회 호출)에 둔다.
    /// </summary>
    protected override void ResolveTargets(CommandRunScope scope)
    {
        string resolvedRigKey =
            CharacterRigTargetResolver.ResolveRigKeyByPolicy(_stage, _spec.slotKey);

        CharacterRigRefs rigRefs =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(_stage, _spec.slotKey);

        _rect = rigRefs?.GetRect(_spec.target);

        CharacterFacing facing = ResolveTargetFacing(scope, resolvedRigKey);

        _stage.castRegistry.SetFacing(resolvedRigKey, facing);

        Vector3 baseScale = _rect.localScale;

        _targetScale = baseScale;
        _targetScale.x = Mathf.Abs(baseScale.x) * facing.Sign();
    }

    private CharacterFacing ResolveTargetFacing(
        CommandRunScope scope,
        string resolvedRigKey)
    {
        switch (_spec.mode)
        {
            case CharacterMirrorMode.Left:
                return CharacterFacing.Left;

            case CharacterMirrorMode.Right:
                return CharacterFacing.Right;

            case CharacterMirrorMode.Toggle:
            default:
            {
                if (_stage.castRegistry.TryGetFacing(resolvedRigKey, out CharacterFacing current))
                    return current.Opposite();

                return CharacterFacing.Left;
            }
        }
    }

    protected override void ClaimTarget(CommandRunScope scope)
    {
        _rect.DOKill(true);
    }

    protected override Tween CreateTween(float duration)
        => _rect
            .DOScale(_targetScale, duration)
            .SetEase(_spec.ease)
            .SetTarget(_rect);

    protected override void OnCommitFinalState()
    {
        _rect.localScale = _targetScale;
    }

    // AccelerateOnStepFinish = false라 불리지 않지만, 계약은 정직하게 채운다.
    protected override float MeasureRemainingRatio()
        => RemainingRatio(
            Mathf.Abs(_targetScale.x - _rect.localScale.x),
            0f);
}
