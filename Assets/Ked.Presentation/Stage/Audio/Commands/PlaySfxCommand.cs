using System;
using System.Collections;
using UnityEngine;

[Serializable]
public sealed class PlaySfxCommandSpec : CommandSpecBase
{
    [Header("Source")]
    public string clipKey;
    public AudioClip directClip;
}

// PlaySfxCommand -> Ignore
// skip 중 효과음은 의미가 없음. 연출용 SFX가 skip 구간에서 쏟아지면 오히려 이상함.
public sealed class PlaySfxCommand : CommandBase
{
    private readonly AudioSystem _audio;
    private readonly AudioClip   _clip;

    protected override SkipPolicy SkipPolicy => SkipPolicy.Ignore;

    public PlaySfxCommand(AudioSystem audio, AudioClip clip)
    {
        _audio = audio;
        _clip  = clip;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        _audio.Sfx.Play(_clip);
        yield break;
    }
}