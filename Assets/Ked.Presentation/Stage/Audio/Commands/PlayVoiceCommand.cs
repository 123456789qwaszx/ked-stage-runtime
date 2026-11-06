using System;
using System.Collections;
using UnityEngine;

[Serializable]
public sealed class PlayVoiceCommandSpec : CommandSpecBase
{
    [Header("Source")]
    public string clipKey;
    public AudioClip directClip;
}

// PlayVoiceCommand -> Ignore
// skip 중에는 대사를 재생할 필요가 없음. 텍스트도 건너뛰는데 보이스만 재생하면 어색하기 때문.
public sealed class PlayVoiceCommand : CommandBase
{
    private readonly AudioSystem _audio;
    private readonly AudioClip   _clip;

    protected override SkipPolicy SkipPolicy => SkipPolicy.Ignore;

    public PlayVoiceCommand(AudioSystem audio, AudioClip clip)
    {
        _audio = audio;
        _clip  = clip;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        _audio.Voice.Play(_clip);
        yield break;
    }
}