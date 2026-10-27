using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

[Serializable]
public sealed class SetAnchorCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target (Anchor only)")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_VisualOffset;

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

    // 리셋 대상 rect 목록. 리덕션에 넘기는 키 목록과 같은 순서로 만들어 zip으로 적용한다.
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
        if (!TryResolveRefs(scope))
            yield break;

        Apply(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!TryResolveRefs(scope))
            return;

        Apply(scope);
    }

    private bool TryResolveRefs(CommandRunScope scope)
    {
        if (_resolveAttempted)
            return _rigRefs != null && _rect != null;

        _resolveAttempted = true;

        _rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(
            _stage,
            _spec.slotKey);

        if (_rigRefs == null)
            return false;

        _rect = _rigRefs.GetRect(_spec.target);

        return _rect != null;
    }

    private void Apply(CommandRunScope scope)
    {
        // SetAnchor는 즉시 확정 command다.
        // 이전 PlaceTo tween/ledger가 남아 있으면 새 anchor 값과 싸우므로 먼저 정리.
        KillTweenAndClearPlacementTarget(_rect);

        CollectResetTargets();

        string tuningKey =
            CharacterRigTargetResolver.ResolveCharacterKeyFromTargetKey(_stage, _spec.slotKey);

        // ── 코어: 스펙 + 튜닝 → 목표 상태 ──
        StageNodeClaim[] claims = SetAnchorReduction.Reduce(
            _rect.name,
            ResolveRoleAnchorTuning(tuningKey),
            NamesOf(_resetPositionRects),
            NamesOf(_resetEulerRects),
            NamesOf(_resetScaleRects));

        // ── 호스트: 클레임을 같은 순서로 rect에 적용 ──
        int i = 0;

        foreach (RectTransform rect in _resetPositionRects)
            ApplyClaim(rect, claims[i++]);

        foreach (RectTransform rect in _resetEulerRects)
            ApplyClaim(rect, claims[i++]);

        foreach (RectTransform rect in _resetScaleRects)
            ApplyClaim(rect, claims[i++]);

        ApplyClaim(_rect, claims[i++]);   // 앵커 위치
        ApplyClaim(_rect, claims[i]);     // 앵커 스케일

        // 즉시 적용이므로 Publish하지 않는다.
        // live transform이 이미 settled target.
        ClearPlacementTarget(_rect);
    }

    /// <summary>
    /// 리셋 대상을 축별로 모은다. 순서가 리덕션의 클레임 순서와 대응하므로 바꾸지 말 것.
    /// 없는 노드(null)는 목록에 넣지 않는다 — 키 목록과 rect 목록의 길이가 어긋나면 안 된다.
    /// </summary>
    private void CollectResetTargets()
    {
        _resetPositionRects.Clear();
        _resetEulerRects.Clear();
        _resetScaleRects.Clear();

        if (_spec.resetSlotPos)
        {
            AddIfPresent(_resetPositionRects, _rigRefs.CharSlot_Track);
            AddIfPresent(_resetPositionRects, _rigRefs.CharSlot_Track_X);
            AddIfPresent(_resetPositionRects, _rigRefs.CharSlot_Track_Y);

            AddIfPresent(_resetEulerRects, _rigRefs.CharSlot_Rotation);

            AddIfPresent(_resetScaleRects, _rigRefs.CharSlot_Scale);
        }

        if (_spec.resetCharacterPos)
        {
            AddIfPresent(_resetPositionRects, _rigRefs.CharacterPortrait_Track);
            AddIfPresent(_resetPositionRects, _rigRefs.CharacterPortrait_Track_Move);
            AddIfPresent(_resetPositionRects, _rigRefs.CharacterPortrait_Track_Move_X);
            AddIfPresent(_resetPositionRects, _rigRefs.CharacterPortrait_Track_Move_Y);
            AddIfPresent(_resetPositionRects, _rigRefs.CharacterPortrait_SwayPivot);
            AddIfPresent(_resetPositionRects, _rigRefs.CharacterPortrait_Shake);

            AddIfPresent(_resetEulerRects, _rigRefs.CharacterPortrait_Rotation);
            AddIfPresent(_resetEulerRects, _rigRefs.CharacterPortrait_SwayPivot);
            AddIfPresent(_resetEulerRects, _rigRefs.CharacterPortrait_Shake);

            AddIfPresent(_resetScaleRects, _rigRefs.CharacterPortrait_SwayPivot);
            AddIfPresent(_resetScaleRects, _rigRefs.CharacterPortrait_Shake);
            AddIfPresent(_resetScaleRects, _rigRefs.CharacterPortrait_ActingScale);
            AddIfPresent(_resetScaleRects, _rigRefs.CharacterPortrait_ActingScale_X);
            AddIfPresent(_resetScaleRects, _rigRefs.CharacterPortrait_ActingScale_Y);
        }
    }

    private SetAnchorReduction.RoleAnchorTuning ResolveRoleAnchorTuning(string tuningKey)
    {
        // 하한 클램프는 리덕션이 한다 — 규약이 사는 자리를 하나로 둔다.
        if (_roleTuningDb != null && _roleTuningDb.TryGet(tuningKey, out var entry))
            return new SetAnchorReduction.RoleAnchorTuning(entry.offset.ToCore(), entry.visualScale);

        return SetAnchorReduction.RoleAnchorTuning.Default;
    }

    private void ApplyClaim(RectTransform rect, in StageNodeClaim claim)
    {
        KillTweenAndClearPlacementTarget(rect);

        switch (claim.Kind)
        {
            case StageNodeClaimKind.AnchoredPosition:
                rect.anchoredPosition = claim.Value.XY.ToUnity();
                break;

            case StageNodeClaimKind.LocalEulerAngles:
                rect.localEulerAngles = claim.Value.ToUnity();
                break;

            case StageNodeClaimKind.LocalScaleXY:
                // z는 1로 확정한다 — 클레임의 z 보존 규약(트윈 종점용)과 다르다.
                // set_anchor는 리셋 커맨드라 "알려진 상태로 되돌린다"가 종전 동작이다.
                rect.localScale = new Vector3(claim.Value.X, claim.Value.Y, 1f);
                break;

            default:
                throw new InvalidOperationException(
                    $"[SetAnchorCommandCharR] set_anchor가 낼 수 없는 클레임 종류: {claim.Kind}");
        }
    }

    private static void AddIfPresent(List<RectTransform> list, RectTransform rect)
    {
        if (rect != null)
            list.Add(rect);
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
        if (rect == null)
            return;

        rect.DOKill(true);

        ClearPlacementTarget(rect);
    }

    private void ClearPlacementTarget(RectTransform rect)
    {
        if (_rigRefs == null || rect == null)
            return;

        _rigRefs.PlacementTargets.Clear(rect);
    }
}