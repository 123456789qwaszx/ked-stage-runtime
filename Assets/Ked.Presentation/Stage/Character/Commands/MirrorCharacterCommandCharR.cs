using System;
using DG.Tweening;
using Ked.Presentation.Core;
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

    [Header("Node")]
    public CharacterRigNode node = CharacterRigNode.CharacterPortrait_ActingScale_X;

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

    private readonly PresentationStage _stage;
    private CharacterRigRefs _rig;

    public MirrorCharacterCommandCharR(PresentationStage stage, MirrorCharacterCommandSpecCharR spec)
    {
        _stage = stage;
        _spec = spec;
    }

    /// <summary>
    /// 해석과 동시에 방향을 정해 대장에 기록한다 — 대장이 곧 다음 Toggle의 입력이라
    /// 커맨드가 여러 번 불려도 방향이 한 번만 뒤집히도록 여기(1회 호출)에 둔다.
    /// </summary>
    protected override void ResolveRefs()
    {
        _rig = CharacterRigResolver.ResolveRig(_stage, _spec.roleKey);

        string resolvedRigKey =
            CharacterRigResolver.ResolveSlotKey(_stage, _spec.roleKey);

        _rect = _rig.GetRect(_spec.node);

        CharacterFacing facing = ResolveTargetFacing(resolvedRigKey);

        _stage.castRegistry.SetFacing(resolvedRigKey, facing);

        Vector3 baseScale = _rect.localScale;

        _targetScale = baseScale;
        _targetScale.x = Mathf.Abs(baseScale.x) * facing.Sign();
    }

    private CharacterFacing ResolveTargetFacing(string resolvedRigKey)
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

    protected override void ClaimTarget()
    {
        _rect.DOKill(true);

        _rig.PlacementTargets.Reserve(_rect, NodeTarget.LocalScaleXY(_rect.name, new Vec2(_targetScale.x, _targetScale.y)), Releases);
    }

    protected override Tween CreateTween(float duration)
        => _rect
            .DOScale(_targetScale, duration)
            .SetEase(_spec.ease)
            .SetTarget(_rect);

    protected override void OnCommitFinalState()
        => _rect.localScale = _targetScale;

}
