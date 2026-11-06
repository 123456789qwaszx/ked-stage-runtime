using System;
using System.Collections;
using UnityEngine;

[Serializable]
public sealed class CastCharacterCommandSpec : CommandSpecBase
{
    [Header("Slot / Role")]
    [Tooltip("캐릭터를 바인딩할 역할(슬롯) 키.")]
    public string roleKey;

    [Header("Character Identity")]
    [Tooltip("이 역할에 바인딩할 캐릭터 키.")]
    public string characterKey;
}
public sealed class CastCharacterCommand : CommandBase
{
    private readonly CastCharacterCommandSpec _spec;
    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    private readonly PresentationStage _stage;

    public CastCharacterCommand(PresentationStage stage, CastCharacterCommandSpec spec)
    {
        _stage = stage;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        _stage.castRegistry.CastCharRig(_spec.roleKey, _spec.characterKey);
        yield break;
    }
}