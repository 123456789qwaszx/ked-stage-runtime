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
    
    public ShotZoomFocusCommand(
        PresentationStage stage,
        PresentationShotResponseSystem rig, 
        ShotZoomFocusCommandSpec spec, 
        CharacterFocusTuningDBSO focusTuningDB)
        : base(rig, spec)
    {
        _stage = stage;
        _focusTuningDB = focusTuningDB;
    }

    protected override PresentationIntentState BuildTargetState(in PresentationIntentState from)
    {
        // 정착 focus 측정과 화면 지점 해석. 측정은 무대 좌표(카메라 중립)로 한다 — 캐릭터의 무대 위 자리.
        CharacterFocusPointResult focus = CharacterFocusPointResolver.Resolve(
            _stage,
            spec.focusRoleKey,
            spec.focusPreset,
            spec.focusOffset,
            _focusTuningDB,
            useSettledPlacementTargets: true);

        // 샷은 본 무대만 움직인다. 덮는 무대에 선 리그로는 본 무대의 카메라를 풀 수 없다 — 대본의 잘못이다.
        // 코어 폴드도 같은 줄을 Unhandled로 남긴다.
        if (focus.Stage.Role != Ked.Presentation.Core.StageRole.Main)
        {
            Debug.LogWarning(
                $"[ShotZoomFocusCommand] '{spec.focusRoleKey}'는 덮는 무대 '{focus.Stage.StageKey}'에 서 있다. " +
                "샷은 본 무대만 움직이므로 건너뛴다.");

            return from;
        }

        Vector2 desiredPoint =
            ScreenFocusPointResolver.Resolve(focus.RigSpaceRoot, spec.screenPoint) + spec.screenOffset;

        // 목표 배율에서, 리그가 선 층의 반응까지 넣어 그 자리가 화면 지점에 보이는 pan을 푼다.
        // 코어 폴드(StageReducer.Shot)와 같은 목표 계산이고 같은 곡선(샷 시스템이 쓰는 것)을 본다.
        // 결과는 무대 위 자리와 층만으로 정해진다 — 지금 카메라 · 샷의 진행도와 무관.
        return Ked.Presentation.Core.ShotIntents
            .FocusTo(
                spec.zoom,
                focus.FocusPointInRigSpace.ToCore(),
                desiredPoint.ToCore(),
                focus.Layer.Depth,
                rig.LayerProfile)
            .ToUnity();
    }
}