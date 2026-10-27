using System;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

[Serializable]
public class ScaleToCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharSlot_Scale;

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

    [Tooltip(
        "커스텀 이징 곡선 키(@이름 인자에서). null/빈 배열이면 ease를 쓴다. " +
        "종점(목표 scale)에는 관여하지 않는다 — 모양만 바꾼다.")]
    public CurveKey[] customCurveKeys;
}

public sealed class ScaleToCommandCharR : ClaimTweenCommandBase
{
    private readonly ScaleToCommandSpecCharR _spec;

    private CharacterRigRefs _rigRefs;
    private RectTransform _rect;

    private Vector2 _startScale;
    private Vector2 _targetScale;
    private Vector3 _endScale;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    private readonly PresentationStage _stage;

    public ScaleToCommandCharR(PresentationStage stage, ScaleToCommandSpecCharR spec)
    {
        _stage = stage;
        _spec = spec;
    }

    protected override void ResolveTargets(CommandRunScope scope)
    {
        _rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(_stage, _spec.slotKey);
        _rect = _rigRefs?.GetRect(_spec.target);
    }

    protected override void ClaimTarget(CommandRunScope scope)
    {
        _rect.DOKill(true);

        if (_spec.overrideFromScale)
            ApplyScaleXY(_rect, _spec.fromScale);

        Vector3 currentScale = _rect.localScale;
        _startScale = new Vector2(currentScale.x, currentScale.y);

        // ── 코어: 스펙 → 목표 상태. z는 클레임이 건드리지 않는다 (라이브 z 유지 규약) ──
        StageNodeClaim claim = ScaleToReduction.Reduce(
            _rect.name,
            new ScaleToReduction.Args(_spec.relativeToCurrent, _spec.toScale.ToCore()),
            _startScale.ToCore());

        _targetScale = claim.Value.XY.ToUnity();

        _endScale = currentScale;
        _endScale.x = _targetScale.x;
        _endScale.y = _targetScale.y;

        // 이 커맨드는 duration 동안 localScale을 바꾸는 placement(scale) writer다.
        // FocusPoint solver가 라이브 scale이 아니라 "정착 scale"을 알 수 있도록 게시한다.
        _rigRefs.PlacementTargets.PublishLocalScale(_rect, _targetScale);
    }

    protected override Tween CreateTween(float duration)
        => _rect
            .DOScale(_endScale, duration)
            .ApplyEase(_spec.ease, _spec.customCurveKeys)
            .SetTarget(_rect);

    protected override void OnCommitFinalState()
    {
        ApplyScaleXY(_rect, _targetScale);
        _rigRefs.PlacementTargets.Clear(_rect);
    }

    protected override float MeasureRemainingRatio()
    {
        Vector3 current = _rect.localScale;

        return RemainingRatio(
            Vector2.Distance(_startScale, _targetScale),
            Vector2.Distance(new Vector2(current.x, current.y), _targetScale));
    }

    private static void ApplyScaleXY(RectTransform rect, Vector2 targetXY)
    {
        Vector3 scale = rect.localScale;
        scale.x = targetXY.x;
        scale.y = targetXY.y;
        rect.localScale = scale;
    }
}