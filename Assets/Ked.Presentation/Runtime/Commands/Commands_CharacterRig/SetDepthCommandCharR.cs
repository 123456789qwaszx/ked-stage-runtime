using System;
using DG.Tweening;
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

    [Header("Targets")]
    public CharacterRigTarget depthYTarget = CharacterRigTarget.CharSlot_DepthY;
    public CharacterRigTarget depthScaleTarget = CharacterRigTarget.CharSlot_DepthScale;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;

    [Tooltip("커스텀 이징 곡선 키(@이름 인자에서). null/빈 배열이면 ease를 쓴다.")]
    public Ked.Presentation.Core.CurveKey[] customCurveKeys;
}

public sealed class SetDepthCommandCharR : ClaimTweenCommandBase
{
    private readonly SetDepthCommandSpecCharR _spec;
    private readonly CharacterDepthTuningSO _globalTuning;
    private readonly CharacterFocusTuningDBSO _focusTuningDb;
    private readonly IShotResponseStageProvider _stageProvider;

    private CharacterRigRefs _rigRefs;
    private RectTransform _depthYRect;
    private RectTransform _depthScaleRect;

    private Vector2 _startDepthY;
    private Vector2 _startDepthScale;

    private Vector2 _destRawDepthY;
    private Vector2 _destFinalDepthY;
    private Vector2 _destDepthScale;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    private readonly PresentationStage _stage;

    public SetDepthCommandCharR(
        PresentationStage stage,
        SetDepthCommandSpecCharR spec,
        CharacterDepthTuningSO globalTuning,
        CharacterFocusTuningDBSO focusTuningDb,
        IShotResponseStageProvider stageProvider)
    {
        _stage = stage;
        _spec = spec;
        _globalTuning = globalTuning;
        _focusTuningDb = focusTuningDb;
        _stageProvider = stageProvider;
    }

    protected override void ResolveTargets(CommandRunScope scope)
    {
        _rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(_stage, _spec.slotKey);

        _depthYRect = _rigRefs?.GetRect(_spec.depthYTarget);
        _depthScaleRect = _rigRefs?.GetRect(_spec.depthScaleTarget);
    }

    protected override void ClaimTarget(CommandRunScope scope)
    {
        _depthYRect.DOKill(true);
        _depthScaleRect.DOKill(true);

        _startDepthY = _depthYRect.anchoredPosition;
        _startDepthScale = new Vector2(_depthScaleRect.localScale.x, _depthScaleRect.localScale.y);
        
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
        if (_rigRefs != null)
            _rigRefs.SettledDepthScale = _destDepthScale.x;
        
        CharacterDepthResolver.CalculateDepthYThatPreservesCurrentFocus(
            _stage,
            _stageProvider,
            _spec.slotKey,
            _depthYRect,
            _depthScaleRect,
            _destRawDepthY,
            _destDepthScale,
            rawDepth.PreserveFocusPreset,
            rawDepth.PreserveFocusOffset,
            _focusTuningDb,
            out Vector2 destFinalDepthY);
        
        _destFinalDepthY = destFinalDepthY;

        _rigRefs.PlacementTargets.PublishAnchoredPosition(_depthYRect, _destFinalDepthY);
        _rigRefs.PlacementTargets.PublishLocalScale(_depthScaleRect, _destDepthScale);
    }

    /// <summary>깊이는 Y와 배율이 함께 움직여야 한 몸이라, 두 트윈을 한 시퀀스로 묶는다.</summary>
    protected override Tween CreateTween(float duration)
    {
        Sequence sequence = DOTween.Sequence()
            .SetTarget(_depthYRect);

        sequence.Join(
            _depthYRect
                .DOAnchorPos(_destFinalDepthY, duration)
                .ApplyEase(_spec.ease, _spec.customCurveKeys)
                .SetUpdate(true)
                .SetTarget(_depthYRect));

        sequence.Join(
            _depthScaleRect
                .DOScale(new Vector3(_destDepthScale.x, _destDepthScale.y, 1f), duration)
                .ApplyEase(_spec.ease, _spec.customCurveKeys)
                .SetUpdate(true)
                .SetTarget(_depthScaleRect));

        return sequence;
    }

    protected override void OnCommitFinalState()
    {
        _depthYRect.anchoredPosition = _destFinalDepthY;
        _depthScaleRect.localScale = new Vector3(_destDepthScale.x, _destDepthScale.y, 1f);

        _rigRefs.PlacementTargets.Clear(_depthYRect);
        _rigRefs.PlacementTargets.Clear(_depthScaleRect);
    }

    /// <summary>두 축 중 늦게 도착하는 쪽이 기준이다 — 먼저 붙은 축을 기다려주는 셈.</summary>
    protected override float MeasureRemainingRatio()
    {
        Vector3 currentScale = _depthScaleRect.localScale;

        float posRatio = RemainingRatio(
            Vector2.Distance(_startDepthY, _destFinalDepthY),
            Vector2.Distance(_depthYRect.anchoredPosition, _destFinalDepthY));

        float scaleRatio = RemainingRatio(
            Vector2.Distance(_startDepthScale, _destDepthScale),
            Vector2.Distance(new Vector2(currentScale.x, currentScale.y), _destDepthScale));

        return Mathf.Max(posRatio, scaleRatio);
    }
}