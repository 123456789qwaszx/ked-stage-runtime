using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

[Serializable]
public sealed class SetAnchorCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Node (Anchor only)")]
    public CharacterRigNode node = CharacterRigNode.CharacterPortrait_VisualOffset;

    [Header("Reset")]
    [Tooltip("체크하면 Anchor 설정 후 CharSlot_Track / Move / X / Y / Rotation / Scale 축을 기본값으로 초기화합니다.")]
    public bool resetSlotPos = true;

    [Tooltip("체크하면 Anchor 설정 후 CharacterPortrait_Track / Move / X / Y / Rotation / SwayPivot / Shake / ActingScale 축을 기본값으로 초기화합니다.")]
    public bool resetCharacterPos = true;
}

public sealed class SetAnchorCommandCharR : CommandBase
{
    private readonly SetAnchorCommandSpecCharR _spec;
    private readonly RoleAnchorTuningDBSO _roleTuningDb;

    private CharacterRigRefs _rigRefs;
    private RectTransform _rect;

    private bool _resolveAttempted;

    // 리셋 대상 rect 목록. 목표 계산에 넘기는 키 목록과 같은 순서로 만들어 zip으로 적용한다.
    private readonly List<RectTransform> _resetPositionRects = new(16);
    private readonly List<RectTransform> _resetEulerRects = new(8);
    private readonly List<RectTransform> _resetScaleRects = new(8);

    private readonly PresentationStage _stage;

    public SetAnchorCommandCharR(
        PresentationStage stage,
        SetAnchorCommandSpecCharR spec,
        RoleAnchorTuningDBSO roleTuningDb)
    {
        _stage = stage;
        _spec = spec;
        _roleTuningDb = roleTuningDb;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        Apply();
        yield break;
    }

    protected override void OnSkip()
    {
        if (!_resolveAttempted)
            ResolveRefs();

        Apply();
    }

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        _rigRefs = CharacterRigResolver.ResolveRig(
            _stage,
            _spec.roleKey);

        _rect = _rigRefs.GetRect(_spec.node);
    }

    private void Apply()
    {
        // SetAnchor는 즉시 확정 command다.
        // 이전 PlaceTo tween/ledger가 남아 있으면 새 anchor 값과 싸우므로 먼저 정리.
        KillTweenAndClearPlacementTarget(_rect);

        CollectResetTargets();

        string tuningKey =
            CharacterRigResolver.ResolveCharacterKey(_stage, _spec.roleKey);

        // ── 코어: 목표 계산(스펙 + 튜닝) ──
        NodeTarget[] targets = NodeTargets.SetAnchor(
            _rect.name,
            ResolveRoleAnchorTuning(tuningKey),
            NamesOf(_resetPositionRects),
            NamesOf(_resetEulerRects),
            NamesOf(_resetScaleRects));

        // ── 호스트: 타깃을 같은 순서로 rect에 적용 ──
        int i = 0;

        foreach (RectTransform rect in _resetPositionRects)
            ApplyTarget(rect, targets[i++]);

        foreach (RectTransform rect in _resetEulerRects)
            ApplyTarget(rect, targets[i++]);

        foreach (RectTransform rect in _resetScaleRects)
            ApplyTarget(rect, targets[i++]);

        ApplyTarget(_rect, targets[i++]);   // 앵커 위치
        ApplyTarget(_rect, targets[i]);     // 앵커 스케일

        // 즉시 적용이므로 Publish하지 않는다.
        // live transform이 이미 settled target.
        ClearPlacementTarget(_rect);
    }

    /// <summary>
    /// 리셋 대상을 축별로 모은다. 순서가 목표 계산의 타깃 순서와 대응하므로 바꾸지 말 것.
    /// </summary>
    private void CollectResetTargets()
    {
        _resetPositionRects.Clear();
        _resetEulerRects.Clear();
        _resetScaleRects.Clear();

        if (_spec.resetSlotPos)
        {
            _resetPositionRects.Add(_rigRefs.CharSlot_Track);
            _resetPositionRects.Add(_rigRefs.CharSlot_Track_X);
            _resetPositionRects.Add(_rigRefs.CharSlot_Track_Y);

            _resetEulerRects.Add(_rigRefs.CharSlot_Rotation);

            _resetScaleRects.Add(_rigRefs.CharSlot_Scale);
        }

        if (_spec.resetCharacterPos)
        {
            _resetPositionRects.Add(_rigRefs.CharacterPortrait_Track);
            _resetPositionRects.Add(_rigRefs.CharacterPortrait_Track_Move);
            _resetPositionRects.Add(_rigRefs.CharacterPortrait_Track_Move_X);
            _resetPositionRects.Add(_rigRefs.CharacterPortrait_Track_Move_Y);
            _resetPositionRects.Add(_rigRefs.CharacterPortrait_SwayPivot);
            _resetPositionRects.Add(_rigRefs.CharacterPortrait_Shake);

            _resetEulerRects.Add(_rigRefs.CharacterPortrait_Rotation);
            _resetEulerRects.Add(_rigRefs.CharacterPortrait_SwayPivot);
            _resetEulerRects.Add(_rigRefs.CharacterPortrait_Shake);

            _resetScaleRects.Add(_rigRefs.CharacterPortrait_SwayPivot);
            _resetScaleRects.Add(_rigRefs.CharacterPortrait_Shake);
            _resetScaleRects.Add(_rigRefs.CharacterPortrait_ActingScale);
            _resetScaleRects.Add(_rigRefs.CharacterPortrait_ActingScale_X);
            _resetScaleRects.Add(_rigRefs.CharacterPortrait_ActingScale_Y);
        }
    }

    private NodeTargets.RoleAnchorTuning ResolveRoleAnchorTuning(string tuningKey)
    {
        // 하한 클램프는 목표 계산이 한다 — 규약이 사는 자리를 하나로 둔다.
        if (_roleTuningDb.TryGet(tuningKey, out var entry))
            return new NodeTargets.RoleAnchorTuning(entry.offset.ToCore(), entry.visualScale);

        return NodeTargets.RoleAnchorTuning.Default;
    }

    private void ApplyTarget(RectTransform rect, in NodeTarget target)
    {
        KillTweenAndClearPlacementTarget(rect);

        switch (target.Kind)
        {
            case NodeTargetKind.AnchoredPosition:
                rect.anchoredPosition = target.Value.XY.ToUnity();
                break;

            case NodeTargetKind.LocalEulerAngles:
                rect.localEulerAngles = target.Value.ToUnity();
                break;

            case NodeTargetKind.LocalScaleXY:
                // z는 1로 확정한다 — 타깃의 z 보존 규약(트윈 종점용)과 다르다.
                // set_anchor는 리셋 커맨드라 "알려진 상태로 되돌린다"가 종전 동작이다.
                rect.localScale = new Vector3(target.Value.X, target.Value.Y, 1f);
                break;

            default:
                throw new InvalidOperationException(
                    $"[SetAnchorCommandCharR] set_anchor가 낼 수 없는 타깃 종류: {target.Kind}");
        }
    }

    private static string[] NamesOf(List<RectTransform> rects)
    {
        string[] names = new string[rects.Count];

        for (int i = 0; i < rects.Count; i++)
            names[i] = rects[i].name;

        return names;
    }

    private void KillTweenAndClearPlacementTarget(RectTransform rect)
    {
        rect.DOKill(true);

        ClearPlacementTarget(rect);
    }

    private void ClearPlacementTarget(RectTransform rect)
        => _rigRefs.PlacementTargets.Clear(rect);
}