using System;
using System.Collections;
using UnityEngine;

[Serializable]
public sealed class PlayBgmCommandSpec : CommandSpecBase
{
    [Header("Source")]
    public string clipKey;
    public AudioClip directClip;

    [Header("Playback")]
    public float fadeDuration = 1f;
}

// PlayBgmCommand / StopBgmCommand -> CompleteImmediately
// skip 시 BGM은 즉시 스냅으로 전환. 무시하면 BGM이 안 바뀐 채로 씬이 넘어감.
// OnSkip에서 isSkipping: true를 넘겨서 BgmPlayer가 페이드 없이 즉시 처리.
public sealed class PlayBgmCommand : CommandBase
{
    private readonly AudioSystem _audio;
    private readonly AudioClip   _clip;
    private readonly float       _fadeDuration;

    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public PlayBgmCommand(AudioSystem audio, AudioClip clip, float fadeDuration = 1f)
    {
        _audio        = audio;
        _clip         = clip;
        _fadeDuration = fadeDuration;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        _audio.Bgm.Play(_clip, _fadeDuration, isSkipping: false);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        _audio.Bgm.Play(_clip, _fadeDuration, isSkipping: true);
    }
}