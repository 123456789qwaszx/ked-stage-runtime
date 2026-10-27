using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
public sealed class PlaceCharacterFocusCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Focus")]
    public CharacterFocusPreset focusPreset = CharacterFocusPreset.Face;

    [Tooltip("선택한 focus preset에 추가로 더할 command-time offset입니다.")]
    public Vector2 focusOffset = Vector2.zero;

    [Header("Screen Point")]
    public ScreenFocusPoint screenPoint = ScreenFocusPoint.Center;

    [Tooltip("ScreenFocusPoint에 추가로 더할 오프셋. Stage/RigSpace local 기준입니다.")]
    public Vector2 screenOffset = Vector2.zero;

    [Header("Placement Target")]
    [Tooltip("Focus placement 전용 축. 보통 CharSlot_Track_Focus를 사용합니다.")]
    public CharacterRigTarget moveTarget = CharacterRigTarget.CharSlot_Track_Focus;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;

    [Tooltip("커스텀 이징 곡선 키(@이름 인자에서). null/빈 배열이면 ease를 쓴다.")]
    public Ked.Presentation.Core.CurveKey[] customCurveKeys;
}

public sealed class PlaceCharacterFocusCommandCharR : ClaimTweenCommandBase
{
    private readonly PlaceCharacterFocusCommandSpecCharR _spec;
    private readonly CharacterFocusTuningDBSO _focusTuningDb;
    private readonly IShotResponseStageProvider _stageProvider;

    private CharacterRigRefs _rigRefs;
    private RectTransform _moveRect;

    private Vector2 _startPosition;
    private Vector2 _destination;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    private readonly PresentationStage _stage;

    public PlaceCharacterFocusCommandCharR(
        PresentationStage stage,
        PlaceCharacterFocusCommandSpecCharR spec,
        CharacterFocusTuningDBSO focusTuningDb,
        IShotResponseStageProvider stageProvider)
    {
        _stage = stage;
        _spec = spec;
        _focusTuningDb = focusTuningDb;
        _stageProvider = stageProvider;
    }

    protected override void ResolveTargets(CommandRunScope scope)
    {
        _rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(_stage, _spec.slotKey);
        _moveRect = _rigRefs?.GetRect(_spec.moveTarget);
    }

    protected override void ClaimTarget(CommandRunScope scope)
    {
        _moveRect.DOKill(true);

        _startPosition = _moveRect.anchoredPosition;

        CharacterFocusPlacementSolver.TryCalculateFocusPlacement(
            _stage,
            _stageProvider,
            _spec.slotKey,
            _moveRect,
            _spec.focusPreset,
            _spec.focusOffset,
            _focusTuningDb,
            _spec.screenPoint,
            _spec.screenOffset,
            out Vector2 destination);

        _destination = destination;

        _rigRefs.PlacementTargets.PublishAnchoredPosition(_moveRect, _destination);
    }

    protected override Tween CreateTween(float duration)
        => _moveRect
            .DOAnchorPos(_destination, duration)
            .ApplyEase(_spec.ease, _spec.customCurveKeys)
            .SetTarget(_moveRect);

    protected override void OnCommitFinalState()
    {
        _moveRect.anchoredPosition = _destination;
        _rigRefs.PlacementTargets.Clear(_moveRect);
    }

    protected override float MeasureRemainingRatio()
        => RemainingRatio(
            Vector2.Distance(_startPosition, _destination),
            Vector2.Distance(_moveRect.anchoredPosition, _destination));
}