using System;
using System.Collections;
using UnityEngine;

public enum CharacterRigReparentSiblingMode
{
    Preserve = 0,
    Back = 1,
    Front = 2
}

[Serializable]
public sealed class MoveCharacterRigToStageLayerCommandSpecCharR
    : CharacterRigCommandSpecBase
{
    [Header("Stage Depth Slot")]
    public PresentationStageKey stage = PresentationStageKey.Stage00;
    public PresentationDepthLayerKey layer = PresentationDepthLayerKey.Mid;

    [Header("Sibling Order")]
    public CharacterRigReparentSiblingMode siblingMode = CharacterRigReparentSiblingMode.Front;
}

public sealed class MoveCharacterRigToStageLayerCommandCharR : CommandBase
{
    private readonly MoveCharacterRigToStageLayerCommandSpecCharR _spec;
    private readonly CharRigSlotResolver _slotResolver;

    private CharacterRigRefs _rigRefs;
    private RectTransform _parent;

    private bool _resolveAttempted;

    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    private readonly PresentationStage _stage;

    public MoveCharacterRigToStageLayerCommandCharR(
        PresentationStage stage,
        MoveCharacterRigToStageLayerCommandSpecCharR spec,
        CharRigSlotResolver slotResolver)
    {
        _stage = stage;
        _spec = spec;
        _slotResolver = slotResolver;
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

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        _rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(_stage, _spec.slotKey);
        _slotResolver.TryResolve(_spec.stage, _spec.layer, out RectTransform parent);
        _parent = parent;
    }

    private void Apply()
    {
        RectTransform rigRoot = _rigRefs.RigRoot;

        rigRoot.SetParent(_parent, false);

        switch (_spec.siblingMode)
        {
            case CharacterRigReparentSiblingMode.Back:
                rigRoot.SetAsFirstSibling();
                break;

            case CharacterRigReparentSiblingMode.Front:
                rigRoot.SetAsLastSibling();
                break;

            case CharacterRigReparentSiblingMode.Preserve:
                break;
        }
    }
}