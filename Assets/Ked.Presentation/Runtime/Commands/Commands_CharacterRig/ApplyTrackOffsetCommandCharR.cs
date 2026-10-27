using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
public class ApplyTrackOffsetCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    [Tooltip("offset를 실제로 적용할 대상.")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Track;

    [Header("Offset")]
    [Tooltip("현재 anchoredPosition 기준으로 더해질 오프셋(픽셀 단위).")]
    public Vector2 offset = Vector2.zero;

    [Header("Reset Target Before Apply")]
    [Tooltip("체크하면 target의 위치를 먼저 (0,0)으로 맞춘 뒤 offset을 적용합니다. 상위 Anchor의 위치는 유지됩니다.")]
    public bool applyFromZero = true;
    
    [Header("Track Layer Reset")]
    [Tooltip("체크하면 Character_Track / Move / X / Y 를 전부 (0,0)으로 초기화합니다.")]
    public bool resetAllTrackLayers = true;

    [Tooltip("Char_Track 을 (0,0)으로 초기화.")]
    public bool resetCharTrack = false;

    [Tooltip("Char_Track_X 를 (0,0)으로 초기화.")]
    public bool resetCharTrackX = false;

    [Tooltip("Char_Track_Y 를 (0,0)으로 초기화.")]
    public bool resetCharTrackY = false;
}

public sealed class ApplyTrackOffsetCommandCharR : CommandBase
{
    private readonly ApplyTrackOffsetCommandSpecCharR _spec;

    private CharacterRigRefs _rigRefs;
    private RectTransform _rect;

    private bool _resolveAttempted;

    private readonly PresentationStage _stage;

    public ApplyTrackOffsetCommandCharR(PresentationStage stage, ApplyTrackOffsetCommandSpecCharR spec)
    {
        _stage = stage;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ApplyFinalState();
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ApplyFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;
        
        _rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(_stage, _spec.slotKey);
        _rect = _rigRefs.GetRect(_spec.target);
    }

    private void ApplyFinalState()
    {
        if (_spec.resetAllTrackLayers || _spec.resetCharTrack)
            ResetRect(_rigRefs.CharSlot_Track);

        if (_spec.resetAllTrackLayers || _spec.resetCharTrackX)
            ResetRect(_rigRefs.CharSlot_Track_X);

        if (_spec.resetAllTrackLayers || _spec.resetCharTrackY)
            ResetRect(_rigRefs.CharSlot_Track_Y);

        _rect.DOKill(true);

        if (_spec.applyFromZero)
            _rect.anchoredPosition = Vector2.zero;

        _rect.anchoredPosition += _spec.offset;
    }
    
    private static void ResetRect(RectTransform rect)
    {
        rect.DOKill(true);
        rect.anchoredPosition = Vector2.zero;
    }
}