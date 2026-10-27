// Voice policy: single channel; no overlap; newest request wins.
using UnityEngine;

public sealed class VoicePlayer
{
    private readonly AudioSource _source;

    public AudioClip CurrentClip
    {
        get
        {
            if (_source == null)
                return null;

            return _source.clip;
        }
    }

    public bool IsPlaying
    {
        get
        {
            if (_source == null)
                return false;

            return _source.isPlaying;
        }
    }

    public VoicePlayer(AudioSource source)
    {
        _source = source;

        if (_source == null)
        {
            Debug.LogWarning("[VoicePlayer] source is null.");
            return;
        }

        _source.loop = false;
    }

    // Interrupt-and-replace policy.
    // A new voice clip stops the previous one immediately.
    public void Play(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[VoicePlayer] clip is null.");
            return;
        }

        if (_source == null)
            return;

        _source.Stop();
        _source.clip = clip;
        _source.Play();
    }

    public void Stop()
    {
        if (_source == null)
            return;

        _source.Stop();
        _source.clip = null;
    }
}