using System;
using System.Collections;

[Serializable]
public sealed class StopVoiceCommandSpec : CommandSpecBase { }

// StopVoiceCommand -> ExecuteEvenIfSkipping
// skip 중에도 반드시 실행돼야 해. 이전 씬의 보이스가 다음 씬까지 흘러나오는 걸 막아야 하기 때문.
public sealed class StopVoiceCommand : CommandBase
{
    private readonly AudioSystem _audio;

    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    public StopVoiceCommand(AudioSystem audio)
    {
        _audio = audio;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        _audio.Voice.Stop();
        yield break;
    }
}