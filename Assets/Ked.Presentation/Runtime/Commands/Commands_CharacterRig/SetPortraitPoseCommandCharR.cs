using System;
using System.Collections;
using UnityEngine;

[Serializable]
public sealed class SetPortraitPoseCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Pose / Variant")]
    [Tooltip("의상/자세/바디 variant 키. 예: a, b.")]
    public string variantKey = PortraitResolver.DefaultVariant;
}

// pose는 cast의 변형만 갈아 끼우고 스프라이트는 건드리지 않는다.
// 화면의 초상 폭(sizeDelta)은 다음 show/face/face_swap에서야 바뀐다 —
// 코어의 PortraitSizingReduction이 pose를 접지 않는 근거가 이것이다
// (Ked.Presentation/Core/Documentation~/reduction-boundary.md 초상 절).
public sealed class SetPortraitPoseCommandCharR : CommandBase
{
    private readonly SetPortraitPoseCommandSpecCharR _spec;

    private readonly PresentationStage _stage;

    public SetPortraitPoseCommandCharR(PresentationStage stage, SetPortraitPoseCommandSpecCharR spec)
    {
        _stage = stage;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Apply(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope) => Apply(scope);

    private void Apply(CommandRunScope scope)
    {
        string resolvedSlotKey =
            CharacterRigTargetResolver.ResolveRigKeyByPolicy(_stage, _spec.slotKey);

        string variantKey = string.IsNullOrWhiteSpace(_spec.variantKey)
            ? PortraitResolver.DefaultVariant
            : _spec.variantKey;

        _stage.castRegistry.SetVariant(resolvedSlotKey, variantKey);
    }
}