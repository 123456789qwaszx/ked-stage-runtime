using System;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

[Serializable]
public class RotateToCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharSlot_Rotation;

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

    [Tooltip("커스텀 이징 곡선 키(@이름 인자에서). null/빈 배열이면 ease를 쓴다.")]
    public CurveKey[] customCurveKeys;
}

public sealed class RotateToCommandCharR : ClaimTweenCommandBase
{
    private readonly RotateToCommandSpecCharR _spec;

    private RectTransform _rect;

    private Vector3 _startEuler;
    private Vector3 _targetEuler;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    private readonly PresentationStage _stage;

    public RotateToCommandCharR(PresentationStage stage, RotateToCommandSpecCharR spec)
    {
        _stage = stage;
        _spec = spec;
    }

    protected override void ResolveTargets(CommandRunScope scope)
    {
        CharacterRigRefs rig = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(_stage, _spec.slotKey);
        _rect = rig?.GetRect(_spec.target);
    }

    protected override void ClaimTarget(CommandRunScope scope)
    {
        _rect.DOKill(true);

        if (_spec.overrideFromEuler)
            _rect.localEulerAngles = _spec.fromEuler;

        _startEuler = _rect.localEulerAngles;

        // ── 코어: 스펙 → 목표 상태 ──
        // 회전은 장부에 게시하지 않는다 — 종전에도 하지 않았다
        // (오일러를 게시하는 실호출부가 없다는 조사 결과와 일치).
        StageNodeClaim claim = RotateToReduction.Reduce(
            _rect.name,
            new RotateToReduction.Args(_spec.relativeToCurrent, _spec.toEuler.ToCore()),
            _startEuler.ToCore());

        _targetEuler = claim.Value.ToUnity();
    }

    protected override Tween CreateTween(float duration)
        => _rect
            .DOLocalRotate(_targetEuler, duration, RotateMode.Fast)
            .ApplyEase(_spec.ease, _spec.customCurveKeys)
            .SetTarget(_rect);

    protected override void OnCommitFinalState()
    {
        _rect.localEulerAngles = _targetEuler;
    }

    protected override float MeasureRemainingRatio()
        => RemainingRatio(
            EulerDistance(_startEuler, _targetEuler),
            EulerDistance(_rect.localEulerAngles, _targetEuler));

    private static float EulerDistance(Vector3 from, Vector3 to)
    {
        float x = Mathf.DeltaAngle(from.x, to.x);
        float y = Mathf.DeltaAngle(from.y, to.y);
        float z = Mathf.DeltaAngle(from.z, to.z);

        return new Vector3(x, y, z).magnitude;
    }
}
