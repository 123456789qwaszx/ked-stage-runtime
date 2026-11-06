using System;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

[Serializable]
public class ScaleToCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Node")]
    public CharacterRigNode node = CharacterRigNode.CharSlot_Scale;

    [Header("Scale (XY)")]
    public Vector2 toScale = Vector2.one;

    [Header("Mode")]
    [Tooltip("false면 toScale을 절대 scale로 사용합니다. true면 현재 scale에 toScale을 곱한 값을 목표로 사용합니다.")]
    public bool relativeToCurrent = false;

    [Header("From")]
    public bool overrideFromScale = false;
    public Vector2 fromScale = Vector2.one;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;
}

public sealed class ScaleToCommandCharR : ClaimTweenCommandBase
{
    private readonly PresentationStage _stage;
    private CharacterRigRefs _rig;
    private readonly ScaleToCommandSpecCharR _spec;

    private RectTransform _rect;

    private Vector2 _startScale;
    private Vector2 _targetScale;
    private Vector3 _endScale;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    public ScaleToCommandCharR(PresentationStage stage, ScaleToCommandSpecCharR spec)
    {
        _stage = stage;
        _spec = spec;
    }

    protected override void ResolveRefs()
    {
        _rig = CharacterRigResolver.ResolveRig(_stage, _spec.roleKey);
        _rect = _rig.GetRect(_spec.node);
    }

    protected override void ClaimTarget()
    {
        _rect.DOKill(true);

        if (_spec.overrideFromScale)
            ApplyScaleXY(_rect, _spec.fromScale);

        Vector3 currentScale = _rect.localScale;
        _startScale = new Vector2(currentScale.x, currentScale.y);

        // ── 코어: 목표 계산. z는 타깃이 건드리지 않는다 (라이브 z 유지 규약) ──
        NodeTarget target = RigTargets.ScaleTo(_rect, _spec, _startScale);

        _targetScale = target.Value.XY.ToUnity();

        _endScale = currentScale;
        _endScale.x = _targetScale.x;
        _endScale.y = _targetScale.y;

        _rig.PlacementTargets.Reserve(_rect, target, Releases);
    }

    protected override Tween CreateTween(float duration)
        => _rect
            .DOScale(_endScale, duration)
            .SetEase(_spec.ease)
            .SetTarget(_rect);

    protected override void OnCommitFinalState()
        => ApplyScaleXY(_rect, _targetScale);

    private static void ApplyScaleXY(RectTransform rect, Vector2 targetXY)
    {
        Vector3 scale = rect.localScale;
        scale.x = targetXY.x;
        scale.y = targetXY.y;
        rect.localScale = scale;
    }
}