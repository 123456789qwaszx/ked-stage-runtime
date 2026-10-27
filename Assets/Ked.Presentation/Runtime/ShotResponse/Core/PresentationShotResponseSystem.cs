using System;
using System.Collections.Generic;
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

    private PresentationIntentState _currentState = PresentationIntentState.Default;

    public PresentationIntentState CurrentState => _currentState;

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

    public void ApplyToAllBindings(in PresentationIntentState state)
    {
        _currentState = state;

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

    public void Clear()
    {
        _currentState = PresentationIntentState.Default;
        
        // ApplyToAllBindings가 camera root까지 default로 보정.
        ApplyToAllBindings(_currentState);
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