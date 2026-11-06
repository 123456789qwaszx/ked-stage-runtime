using System;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

[Serializable]
public class RotateToCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Node")]
    public CharacterRigNode node = CharacterRigNode.CharSlot_Rotation;

    [Header("Rotation (localEulerAngles)")]
    public Vector3 toEuler = Vector3.zero;

    [Header("Mode")]
    [Tooltip("false면 toEuler를 절대 localEulerAngles로 사용합니다. true면 현재 localEulerAngles에 toEuler를 더합니다.")]
    public bool relativeToCurrent = false;

    [Header("From")]
    public bool overrideFromEuler = false;
    public Vector3 fromEuler = Vector3.zero;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;
}

public sealed class RotateToCommandCharR : ClaimTweenCommandBase
{
    private readonly PresentationStage _stage;
    private CharacterRigRefs _rig;
    private readonly RotateToCommandSpecCharR _spec;

    private RectTransform _rect;

    private Vector3 _startEuler;
    private Vector3 _targetEuler;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    public RotateToCommandCharR(PresentationStage stage, RotateToCommandSpecCharR spec)
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

        if (_spec.overrideFromEuler)
            _rect.localEulerAngles = _spec.fromEuler;

        _startEuler = _rect.localEulerAngles;

        // ── 코어: 목표 계산 ──
        // 회전도 게시한다. SwayPivot은 측정 체인 위라 RectChainMath가 euler를 읽는다 —
        // 같은 줄의 place가 회전 중간값을 보면 일반 재생과 롤백이 다른 자리를 낸다.
        NodeTarget target = RigTargets.RotateTo(_rect, _spec, _startEuler);
        _rig.PlacementTargets.Reserve(_rect, target, Releases);
        _targetEuler = target.Value.ToUnity();
    }

    protected override Tween CreateTween(float duration)
        => _rect
            .DOLocalRotate(_targetEuler, duration, RotateMode.Fast)
            .SetEase(_spec.ease)
            .SetTarget(_rect);

    protected override void OnCommitFinalState()
        => _rect.localEulerAngles = _targetEuler;

}
