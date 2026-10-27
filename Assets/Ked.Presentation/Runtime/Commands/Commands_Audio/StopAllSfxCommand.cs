using System;
using System.Collections;

[Serializable]
public sealed class StopAllSfxCommandSpec : CommandSpecBase
{ }

// StopAllSfxCommand -> ExecuteEvenIfSkipping
// skip / scene transition / cut 중에도 반드시 남아있는 효과음을 정리해야 할 때 사용.
// 특히 UI loop SFX, transition 잔재, 연속 효과음 정리에 유용하다.
public sealed class StopAllSfxCommand : CommandBase
{
    private readonly AudioSystem _audio;

    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    public StopAllSfxCommand(AudioSystem audio)
    {
        _audio = audio;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        _audio.Sfx.StopAll();
        yield break;
    }
}