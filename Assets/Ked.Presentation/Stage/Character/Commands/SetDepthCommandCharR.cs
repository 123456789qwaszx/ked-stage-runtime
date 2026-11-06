using System;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

[Serializable]
public sealed class SetDepthCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Depth")]
    public CharacterDepthKey preset = CharacterDepthKey.Mid;

    public CharacterFocusPreset focusPreset = CharacterFocusPreset.Bust;
    
    [Tooltip("true이면 preset 대신 level을 사용합니다.")]
    public bool useLevel = false;
    public float level = 5f;

    [Tooltip("preserve focus point에 추가로 더할 offset.")]
    public Vector2 focusOffset = Vector2.zero;

    [Header("Nodes")]
    public CharacterRigNode depthYNode = CharacterRigNode.CharSlot_DepthY;
    public CharacterRigNode depthScaleNode = CharacterRigNode.CharSlot_DepthScale;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;
}

public sealed class SetDepthCommandCharR : ClaimTweenCommandBase
{
    private readonly SetDepthCommandSpecCharR _spec;
    private readonly CharacterDepthTuningSO _globalTuning;
    private readonly CharacterFocusTuningDBSO _focusTuningDb;

    private RectTransform _depthYRect;
    private RectTransform _depthScaleRect;

    private Vector2 _destRawDepthY;
    private Vector2 _destFinalDepthY;
    private Vector2 _destDepthScale;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    private readonly PresentationStage _stage;
    private CharacterRigRefs _rig;

    public SetDepthCommandCharR(
        PresentationStage stage,
        SetDepthCommandSpecCharR spec,
        CharacterDepthTuningSO globalTuning,
        CharacterFocusTuningDBSO focusTuningDb)
    {
        _stage = stage;
        _spec = spec;
        _globalTuning = globalTuning;
        _focusTuningDb = focusTuningDb;
    }

    protected override void ResolveRefs()
    {
        _rig = CharacterRigResolver.ResolveRig(_stage, _spec.roleKey);

        _depthYRect = _rig.GetRect(_spec.depthYNode);
        _depthScaleRect = _rig.GetRect(_spec.depthScaleNode);
    }

    protected override void ClaimTarget()
    {
        _depthYRect.DOKill(true);
        _depthScaleRect.DOKill(true);

        CharacterDepthResolver.ResolveRawDepth(
            _spec.preset,
            _spec.useLevel,
            _spec.level,
            _globalTuning,
            _spec.focusPreset,
            _spec.focusOffset,
            out CharacterDepthResult rawDepth);

        _destRawDepthY = rawDepth.RawDepthYAnchoredPosition;
        _destDepthScale = rawDepth.DepthScale;

        // 착란원 계산이 읽는 정지 프레임 값. 트윈 시작 시점에 확정된다.
        _rig.SettledDepthScale = _destDepthScale.x;
        
        CharacterDepthResolver.CalculateDepthYThatPreservesCurrentFocus(
            _stage,
            _spec.roleKey,
            _depthYRect,
            _depthScaleRect,
            _destRawDepthY,
            _destDepthScale,
            rawDepth.PreserveFocusPreset,
            rawDepth.PreserveFocusOffset,
            _focusTuningDb,
            out Vector2 destFinalDepthY);
        
        // 초점 보존이 실패해도 움직인다 — raw Y라는 의미 있는 대체값이 있어서다(크기는 바뀌고 초점만 보정 안 됨).
        // 실패는 리졸버가 경로마다 경고한다. place는 대체값이 없어 움직이지 않는 것과 대비된다.
        _destFinalDepthY = destFinalDepthY;

        _rig.PlacementTargets.Reserve(_depthYRect, NodeTarget.AnchoredPosition(_depthYRect.name, _destFinalDepthY.ToCore()), Releases);
        _rig.PlacementTargets.Reserve(_depthScaleRect, NodeTarget.LocalScaleXY(_depthScaleRect.name, _destDepthScale.ToCore()), Releases);
    }

    /// <summary>깊이는 Y와 배율이 함께 움직여야 한 몸이라, 두 트윈을 한 시퀀스로 묶는다.</summary>
    protected override Tween CreateTween(float duration)
    {
        Sequence sequence = DOTween.Sequence()
            .SetTarget(_depthYRect);

        sequence.Join(
            _depthYRect
                .DOAnchorPos(_destFinalDepthY, duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(_depthYRect));

        sequence.Join(
            _depthScaleRect
                .DOScale(new Vector3(_destDepthScale.x, _destDepthScale.y, 1f), duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(_depthScaleRect));

        return sequence;
    }

    protected override void OnCommitFinalState()
    {
        _depthYRect.anchoredPosition = _destFinalDepthY;
        _depthScaleRect.localScale = new Vector3(_destDepthScale.x, _destDepthScale.y, 1f);
    }

}