using System;
using System.Collections;
using UnityEngine;

public enum CharacterRigSiblingOrderMode
{
    Back = 0,
    Front = 1
}

[Serializable]
public sealed class SetCharacterSiblingOrderCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Order")]
    public CharacterRigSiblingOrderMode mode = CharacterRigSiblingOrderMode.Front;
}

public sealed class SetCharacterSiblingOrderCommandCharR : CommandBase
{
    private readonly SetCharacterSiblingOrderCommandSpecCharR _spec;

    private RectTransform _rigRoot;
    private bool _resolveAttempted;

    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    private readonly PresentationStage _stage;

    public SetCharacterSiblingOrderCommandCharR(
        PresentationStage stage,
        SetCharacterSiblingOrderCommandSpecCharR spec)
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

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rigRefs =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(
                _stage,
                _spec.slotKey);

        _rigRoot = rigRefs.RigRoot;
    }

    private void Apply()
    {
        switch (_spec.mode)
        {
            case CharacterRigSiblingOrderMode.Front:
                _rigRoot.SetAsLastSibling();
                break;

            case CharacterRigSiblingOrderMode.Back:
                _rigRoot.SetAsFirstSibling();
                break;
        }
    }
}