using System;
using System.Collections;
using UnityEngine;

[Serializable]
public sealed class SetPresentationActorAliasCommandSpec : CommandSpecBase
{
    [Header("Alias")]
    public string aliasSymbol = "@";

    [Header("Role")]
    public string roleKey;
}

public sealed class SetPresentationActorAliasCommand : CommandBase
{
    private readonly SetPresentationActorAliasCommandSpec _spec;

    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    private readonly PresentationStage _stage;

    public SetPresentationActorAliasCommand(
        PresentationStage stage,
        SetPresentationActorAliasCommandSpec spec)
    {
        _stage = stage;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        _stage.roleAliases.Register(
            _spec.aliasSymbol,
            _spec.roleKey);
        
        yield break;
    }
}