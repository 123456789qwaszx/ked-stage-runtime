using System;
using System.Collections;
using UnityEngine;

[Serializable]
public sealed class SetAnchorCommandSpecBgR : BackgroundRigCommandSpecBase
{
    [Header("Target")]
    [Tooltip("Usually Background_CastTransform. This is the per-background default transform axis.")]
    public BackgroundRigTarget target = BackgroundRigTarget.Background_Anchor;

    [Header("Position")]
    public Vector2 anchoredPosition = Vector2.zero;

    [Header("Rotation")]
    public float rotationZ = 0f;

    [Header("Scale")]
    public Vector2 scale = Vector2.one;

    [Tooltip("체크하면 Anchor 설정 후 Background_Track / Move / X / Y / Rotation / Shake / ActingScale 축을 기본값으로 초기화합니다.")]
    public bool resetActing = true;
}

public sealed class SetAnchorCommandBgR : CommandBase
{
    private readonly SetAnchorCommandSpecBgR _spec;

    private BackgroundRigRefs _rigRefs;
    private RectTransform _rect;
    private bool _resolveAttempted;

    private readonly PresentationStage _stage;

    public SetAnchorCommandBgR(PresentationStage stage, SetAnchorCommandSpecBgR spec)
    {
        _stage = stage;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply();
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply();
    }

    private void Apply()
    {
        _rect.anchoredPosition = _spec.anchoredPosition;
        _rect.localEulerAngles = new Vector3(0f, 0f, _spec.rotationZ);
        _rect.localScale = new Vector3(_spec.scale.x, _spec.scale.y, 1f);

        if (_spec.resetActing)
            ResetActingLayers();
    }

    private void ResetActingLayers()
    {
        if (_rigRefs.Background_Track_Move != null)
            _rigRefs.Background_Track_Move.anchoredPosition = Vector2.zero;

        if (_rigRefs.Background_Track_X != null)
            _rigRefs.Background_Track_X.anchoredPosition = Vector2.zero;

        if (_rigRefs.Background_Track_Y != null)
            _rigRefs.Background_Track_Y.anchoredPosition = Vector2.zero;

        if (_rigRefs.Background_Rotation != null)
            _rigRefs.Background_Rotation.localEulerAngles = Vector3.zero;

        if (_rigRefs.Background_Shake != null)
        {
            _rigRefs.Background_Shake.anchoredPosition = Vector2.zero;
            _rigRefs.Background_Shake.localEulerAngles = Vector3.zero;
            _rigRefs.Background_Shake.localScale = Vector3.one;
        }

        if (_rigRefs.Background_Scale != null)
            _rigRefs.Background_Scale.localScale = Vector3.one;

        if (_rigRefs.Background_ActingScale_X != null)
            _rigRefs.Background_ActingScale_X.localScale = Vector3.one;

        if (_rigRefs.Background_ActingScale_Y != null)
            _rigRefs.Background_ActingScale_Y.localScale = Vector3.one;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;
        
        _stage.backgroundRigs.TryGetRig(_spec.rigKey, out BackgroundRigRefs rig);
        _rect = _rigRefs.GetRect(_spec.target);
    }
}