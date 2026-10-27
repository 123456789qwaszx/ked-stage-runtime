using System;
using System.Collections;
using UnityEngine;

[Serializable]
public sealed class CastCharacterCommandSpec : CommandSpecBase
{
    [Header("Slot / Role")]
    [Tooltip("캐릭터를 바인딩할 slotKey / roleKey.")]
    public string slotKey;

    [Header("Character Identity")]
    [Tooltip("이 slotKey에 바인딩할 캐릭터 키.")]
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
        _stage.castRegistry.CastCharRig(_spec.slotKey, _spec.characterKey);
        yield break;
    }
}