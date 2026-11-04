using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public struct PresentationTargetResponse
{
    public Vector2 anchoredPosition;
    public Vector2 scale;
}

public readonly struct PresentationResponseMeasure
{
    // focus-side 판정용. MeasureRect 기준으로 잰 rig-space 위치.
    // (대상이 focusPoint의 좌/우, 위/아래 어느 쪽인지 부호를 보는 데만 사용.)
    public readonly Vector2 basePositionInRigSpace;

    // 적용 기준. PositionRect의 중립(bind 시점) anchoredPosition (부모 로컬 공간).
    // 실제 위치 적용은, 이 값에 offset을 더하는 방식. 
    public readonly Vector2 baseAnchoredPosition;

    // ScaleRect의 중립(bind 시점) localScale.
    public readonly Vector2 baseLocalScale;

    public PresentationResponseMeasure(
        Vector2 basePositionInRigSpace,
        Vector2 baseAnchoredPosition,
        Vector2 baseLocalScale)
    {
        this.basePositionInRigSpace = basePositionInRigSpace;
        this.baseAnchoredPosition = baseAnchoredPosition;
        this.baseLocalScale = baseLocalScale;
    }
}

// 샷은 장면 전역 의도다 — 커맨드가 의도(Intent)를 쓰고, 화면(Presented)은 이 시스템이 따라간다.
// 전환 트윈은 시스템이 하나만 갖는다. 새 의도가 오면 지금 보이는 값에서 다시 조준한다(앞 전환을 완주시키지 않는다).
public sealed class PresentationShotResponseSystem
{
    private sealed class RuntimeBinding
    {
        public string key;
        public IResponseTarget target;
        public PresentationResponseProfile profile;
        public PresentationResponseMeasure baseMeasure;

        public bool IsAlive =>
            target != null &&
            target.MeasureRect != null &&
            target.PositionRect != null &&
            target.ScaleRect != null &&
            profile != null;
    }

    private readonly List<RuntimeBinding> _bindings = new();
    private readonly PresentationCameraRootApplier _cameraRootApplier;
    private readonly PresentationResponseCoordinateMapper _coordinateMapper;
    private readonly StageDepthLayerBinder _stageDepthLayerBinder;

    private PresentationIntentState _intent = PresentationIntentState.Default;
    private PresentationIntentState _presented = PresentationIntentState.Default;

    private Tween _transition;
    private PresentationIntentState _transitionFrom;
    private float _transitionDuration;
    private Ease _transitionEase;
    private Ked.Presentation.Core.CurveKey[] _transitionCurveKeys;
    private bool _transitionAccelerated;

    // 저작된 의도 — 다음 샷 커맨드의 출발점이고, 폴드가 접는 값이다.
    public PresentationIntentState Intent => _intent;

    // 지금 화면에 적용된 값. 전환 중에만 Intent와 다르다.
    public PresentationIntentState Presented => _presented;

    public bool IsTransitioning => _transition != null;

    public PresentationShotResponseSystem(IShotResponseStageProvider stageProvider)
    {
        _cameraRootApplier = new PresentationCameraRootApplier(stageProvider);
        _coordinateMapper = new PresentationResponseCoordinateMapper(stageProvider);
        _stageDepthLayerBinder = new StageDepthLayerBinder(stageProvider);
    }

    public void RegisterRuntimeBinding(
        string bindingKey,
        IResponseTarget target,
        PresentationResponseProfile presetProfile)
    {
        if (presetProfile == null)
            return;

        RuntimeBinding binding = new()
        {
            key = bindingKey,
            target = target,
            profile = presetProfile,

            // 등록 시점에만 캡처.
            baseMeasure = _coordinateMapper.CaptureBaseMeasure(target),
        };

        AddOrReplaceBinding(bindingKey, binding);
    }

    public void Follow(
        in PresentationIntentState intent,
        float duration,
        Ease ease,
        Ked.Presentation.Core.CurveKey[] curveKeys)
    {
        _intent = intent;
        KillTransition();

        if (duration <= 0f || PresentationShotIntentMath.ApproximatelyEqual(_presented, intent))
        {
            ApplyToAllBindings(intent);
            return;
        }

        _transitionDuration = duration;
        _transitionEase = ease;
        _transitionCurveKeys = curveKeys;
        _transitionAccelerated = false;

        StartTransition(duration);
    }

    // 압축(빨리 넘기기·시크) — 의도로 바로 맞춘다.
    public void Snap(in PresentationIntentState intent)
    {
        _intent = intent;
        KillTransition();
        ApplyToAllBindings(intent);
    }

    // 스텝 경계 — 남은 비율만큼만 빨리 마저 간다. 한 전환은 한 번만 가속한다.
    public void AccelerateTransition(float speedUpMultiplier)
    {
        if (_transition == null || _transitionAccelerated)
            return;

        float remainingRatio = MeasureRemainingRatio();
        KillTransition();

        if (remainingRatio <= 0f)
        {
            ApplyToAllBindings(_intent);
            return;
        }

        _transitionAccelerated = true;
        StartTransition(Mathf.Max(0.01f, _transitionDuration * remainingRatio / speedUpMultiplier));
    }

    // 장면 경계 — 전환을 확정 없이 멈추고 의도·화면을 기본 샷으로.
    public void Clear()
    {
        Snap(PresentationIntentState.Default);
    }

    private void StartTransition(float duration)
    {
        PresentationIntentState from = _presented;
        PresentationIntentState to = _intent;

        _transitionFrom = from;

        _transition = DOTween
            .To(
                () => 0f,
                t => ApplyToAllBindings(PresentationShotIntentMath.Interpolate(from, to, t)),
                1f,
                duration)
            .ApplyEase(_transitionEase, _transitionCurveKeys)
            .SetUpdate(true)
            .SetTarget(this)
            .OnComplete(() =>
            {
                _transition = null;
                ApplyToAllBindings(to);
            });
    }

    private void KillTransition()
    {
        _transition?.Kill(false);
        _transition = null;
    }

    // 가장 늦게 도착하는 축이 기준.
    private float MeasureRemainingRatio()
        => Mathf.Max(
            RemainingRatio(_transitionFrom.zoom, _intent.zoom, _presented.zoom),
            RemainingRatio(_transitionFrom.panInRigSpace, _intent.panInRigSpace, _presented.panInRigSpace),
            RemainingRatio(_transitionFrom.focusPointInRigSpace, _intent.focusPointInRigSpace, _presented.focusPointInRigSpace));

    private static float RemainingRatio(float from, float to, float current)
    {
        float originalDistance = Mathf.Abs(to - from);
        float remainingDistance = Mathf.Abs(to - current);

        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        return Mathf.Clamp01(remainingDistance / originalDistance);
    }

    private static float RemainingRatio(Vector2 from, Vector2 to, Vector2 current)
    {
        float originalDistance = Vector2.Distance(from, to);
        float remainingDistance = Vector2.Distance(current, to);

        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        return Mathf.Clamp01(remainingDistance / originalDistance);
    }

    private void ApplyToAllBindings(in PresentationIntentState state)
    {
        _presented = state;

        // 최초 1회와 Clear() 직후에만 실제로 바인딩한다. 그 외에는 bool 체크로 끝난다.
        _stageDepthLayerBinder.EnsureBindings(this);

        _cameraRootApplier.Apply(in state);

        for (int i = _bindings.Count - 1; i >= 0; i--)
        {
            RuntimeBinding binding = _bindings[i];

            if (binding == null || !binding.IsAlive)
            {
                _bindings.RemoveAt(i);
                continue;
            }

            PresentationTargetResponse responseForTarget =
                BuildTargetSpaceResponse(in state, binding);

            binding.target.ApplyResponse(in responseForTarget);
        }
    }

    private PresentationTargetResponse BuildTargetSpaceResponse(
        in PresentationIntentState state,
        RuntimeBinding binding)
    {
        // bind 시점에 저장해둔 baseMeasure만 기준으로 계산.
        PresentationTargetResponse responseInRigSpace =
            PresentationResponseMath.CalculateTargetTransformResponseFromShotIntent(
                in state,
                binding.profile,
                in binding.baseMeasure);

        // 계산 결과는 rig space상의 "목표점" 형태로 나오지만, 직접 사용하는 대신,
        // basePosition에서 얼마나 벗어났는지 offset만 추출해서 target parent space로 변환.
        Vector2 offsetInRigSpace =
            responseInRigSpace.anchoredPosition -
            binding.baseMeasure.basePositionInRigSpace;

        Vector2 offsetInParentSpace =
            _coordinateMapper.ConvertOffsetFromRigSpaceToTargetParentSpace(
                offsetInRigSpace,
                binding.target);

        responseInRigSpace.anchoredPosition =
            binding.baseMeasure.baseAnchoredPosition + offsetInParentSpace;

        return responseInRigSpace;
    }

    private void AddOrReplaceBinding(string key, RuntimeBinding newBinding)
    {
        for (int i = 0; i < _bindings.Count; i++)
        {
            RuntimeBinding binding = _bindings[i];

            if (binding == null)
                continue;

            if (string.Equals(binding.key, key, StringComparison.Ordinal))
            {
                _bindings[i] = newBinding;
                return;
            }
        }

        _bindings.Add(newBinding);
    }
}