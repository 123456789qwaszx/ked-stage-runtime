using System;
using System.Collections;
using UnityEngine;

[Serializable]
public sealed class SetOriginSizeCommandSpecBgR : BackgroundRigCommandSpecBase
{
    [Header("Target")]
    public BackgroundRigTarget target = BackgroundRigTarget.Background_Anchor;

    [Header("Scale")]
    public float scale = 1f;

    [Header("Options")]
    [Tooltip("true면 X/Y/Z 모두 같은 값으로 적용합니다.")]
    public bool uniformScale = true;

    [Tooltip("uniformScale=false일 때 X에 곱할 배율입니다.")]
    public float xMultiplier = 1f;

    [Tooltip("uniformScale=false일 때 Y에 곱할 배율입니다.")]
    public float yMultiplier = 1f;

    [Tooltip("uniformScale=false일 때 Z에 곱할 배율입니다. UI RectTransform이면 보통 1을 유지합니다.")]
    public float zMultiplier = 1f;

    [Header("Override")]
    [Tooltip("체크하면 scale 계산을 무시하고 scaleOverride를 직접 적용합니다.")]
    public bool overrideScale = false;

    public Vector3 scaleOverride = Vector3.one;
}

public sealed class SetOriginSizeCommandBgR : CommandBase
{
    private readonly SetOriginSizeCommandSpecBgR _spec;

    private RectTransform _rect;
    private bool _resolveAttempted;

    private readonly PresentationStage _stage;

    public SetOriginSizeCommandBgR(PresentationStage stage, SetOriginSizeCommandSpecBgR spec)
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
        if (_spec.overrideScale)
        {
            _rect.localScale = _spec.scaleOverride;
            return;
        }

        float scale = _spec.scale;

        if (_spec.uniformScale)
        {
            _rect.localScale = new Vector3(scale, scale, scale);
            return;
        }
        
        _rect.localScale = new Vector3(
            scale * SafeMultiplier(_spec.xMultiplier),
            scale * SafeMultiplier(_spec.yMultiplier),
            scale * SafeMultiplier(_spec.zMultiplier));
    }

    private static float SafeMultiplier(float value) => value == 0f ? 1f : value;

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;
        
        _stage.backgroundRigs.TryGetRig(_spec.rigKey, out BackgroundRigRefs rig);
        _rect = rig.GetRect(_spec.target);
    }
}