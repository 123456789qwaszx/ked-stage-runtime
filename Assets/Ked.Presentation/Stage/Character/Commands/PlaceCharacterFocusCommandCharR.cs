using System;
using DG.Tweening;
using Ked.Presentation.Core;
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

    [Header("Placement Node")]
    [Tooltip("Focus placement 전용 축. 보통 CharSlot_Track_Focus를 사용합니다.")]
    public CharacterRigNode moveNode = CharacterRigNode.CharSlot_Track_Focus;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;
}

/// <summary>
/// place — 캐릭터의 초점(얼굴 · 가슴 · 몸)을 **무대 지점**에 두도록 CharSlot_Track_Focus를 옮긴다.
/// 무대 지점은 카메라 중립의 화면 지점과 겹치는 무대 위 자리다 — 화면 구도는 샷이 맡는다.
/// 같은 줄의 예약(size · move_by 등)은 끝값으로, 샷이 움직이는 무대 노드는 중립값으로 풀어서
/// 일반 재생 · 롤백 · 코어 폴드가 같은 자리를 낸다(CharacterFocusPlacementSolver).
/// </summary>
public sealed class PlaceCharacterFocusCommandCharR : ClaimTweenCommandBase
{
    private readonly PlaceCharacterFocusCommandSpecCharR _spec;
    private readonly CharacterFocusTuningDBSO _focusTuningDb;

    private RectTransform _moveRect;

    private Vector2 _destination;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    private readonly PresentationStage _stage;
    private CharacterRigRefs _rig;

    public PlaceCharacterFocusCommandCharR(
        PresentationStage stage,
        PlaceCharacterFocusCommandSpecCharR spec,
        CharacterFocusTuningDBSO focusTuningDb)
    {
        _stage = stage;
        _spec = spec;
        _focusTuningDb = focusTuningDb;
    }

    protected override void ResolveRefs()
    {
        _rig = CharacterRigResolver.ResolveRig(_stage, _spec.roleKey);
        _moveRect = _rig.GetRect(_spec.moveNode);
    }

    protected override void ClaimTarget()
    {
        _moveRect.DOKill(true);

        _destination = CharacterFocusPlacementSolver.CalculateFocusPlacement(
            _stage,
            _spec.roleKey,
            _moveRect,
            _spec.focusPreset,
            _spec.focusOffset,
            _focusTuningDb,
            _spec.screenPoint,
            _spec.screenOffset);

        _rig.PlacementTargets.Reserve(_moveRect, NodeTarget.AnchoredPosition(_moveRect.name, _destination.ToCore()), Releases);
    }

    protected override Tween CreateTween(float duration)
        => _moveRect
            .DOAnchorPos(_destination, duration)
            .SetEase(_spec.ease)
            .SetTarget(_moveRect);

    protected override void OnCommitFinalState()
        => _moveRect.anchoredPosition = _destination;

}