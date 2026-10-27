using System.Collections.Generic;
using UnityEngine;

public sealed class AudioCommandFactory : INodeCommandFactory
{
    private readonly AudioSystem _audio;

    // 없는 음원 키는 소리 없이 넘어가되, 조용히 삼키지는 않는다 — 키마다 한 번 경고한다(규율 1, 침묵 금지).
    private readonly HashSet<string> _warnedMissingKeys = new();

    public AudioCommandFactory(AudioSystem audio)
    {
        _audio = audio;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            PlayBgmCommandSpec s => new PlayBgmCommand(_audio, ResolveClip(s.directClip, s.clipKey), s.fadeDuration),
            StopBgmCommandSpec s => new StopBgmCommand(_audio, s.fadeDuration),

            PlaySfxCommandSpec s => new PlaySfxCommand(_audio, ResolveClip(s.directClip, s.clipKey)),
            StopAllSfxCommandSpec _ => new StopAllSfxCommand(_audio),
            
            PlayVoiceCommandSpec s => new PlayVoiceCommand(_audio, ResolveClip(s.directClip, s.clipKey)),
            StopVoiceCommandSpec _ => new StopVoiceCommand(_audio),

            _ => null
        };

        return command != null;
    }

    private AudioClip ResolveClip(AudioClip directClip, string clipKey)
    {
        if (directClip != null)
            return directClip;

        if (ResourcesAudioClipResolver.TryResolve(clipKey, out AudioClip resolved))
            return resolved;

        string key = string.IsNullOrWhiteSpace(clipKey) ? "(빈 키)" : clipKey;

        if (_warnedMissingKeys.Add(key))
            Debug.LogWarning($"[오디오] 음원 키 '{key}'에 해당하는 클립이 Resources에 없다 — 소리 없이 넘어간다. 키는 Resources 아래 경로다(예: audio/파일이름).");

        return null;
    }
}