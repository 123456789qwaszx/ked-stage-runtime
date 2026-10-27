using System;
using UnityEngine;

[Serializable]
public sealed class ShotZoomFocusCommandSpec : ShotIntentCommandSpecBase
{
    [Header("Character Focus")]
    [Tooltip("캐릭터 key 또는 slot key입니다.")]
    public string focusRoleKey = "";

    [Tooltip("캐릭터 focus 예상 프리셋입니다. Transform anchor가 아니라 Character_CastTransform 기준 offset입니다.")]
    public CharacterFocusPreset focusPreset = CharacterFocusPreset.Face;

    [Tooltip("선택한 focus preset에 추가로 더할 최종 command-time offset입니다.")]
    public Vector2 focusOffset = Vector2.zero;

    [Header("Screen Focus")]
    public ScreenFocusPoint screenPoint = ScreenFocusPoint.Center;

    [Tooltip("ScreenFocusPoint에 추가로 더할 오프셋. Stage local space 기준.")]
    public Vector2 screenOffset = Vector2.zero;

    [Header("Intent")]
    [Range(-10f, 10f)]
    public float zoom = 0f;
}

public sealed class ShotZoomFocusCommand : ShotIntentCommandBase<ShotZoomFocusCommandSpec>
{
    private readonly PresentationStage _stage;
    CharacterFocusTuningDBSO _focusTuningDB;
    private readonly IShotResponseStageProvider _stageProvider;
    
    public ShotZoomFocusCommand(
        PresentationStage stage,
        PresentationShotResponseSystem rig, 
        ShotZoomFocusCommandSpec spec, 
        CharacterFocusTuningDBSO focusTuningDB,
        IShotResponseStageProvider stageProvider)
        : base(rig, spec)
    {
        _stage = stage;
        _focusTuningDB = focusTuningDB;
        _stageProvider = stageProvider;
    }

    protected override PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope)
    {
        // 입력 수집
        // 정착 focus 측정("현재 카메라가 적용된 채로 보이는 위치")과 화면 지점 해석.
        CharacterFocusPointResolver.TryResolve(
            _stage,
            _stageProvider,
            spec.focusRoleKey,
            spec.focusPreset,
            spec.focusOffset,
            _focusTuningDB,
            useSettledPlacementTargets: true,
            out CharacterFocusPointResult focus);

        Vector2 desiredPoint =
            ScreenFocusPointResolver.Resolve(focus.RigSpaceRoot, spec.screenPoint) + spec.screenOffset;

        // 논리 focus 복원 -> 목표 배율에서 pan 역산
        return Ked.Presentation.Core.ShotZoomFocusReduction
            .Reduce(
                from.ToCore(),
                spec.zoom,
                focus.FocusPointInRigSpace.ToCore(),
                desiredPoint.ToCore())
            .ToUnity();
    }
}