using System.Collections.Generic;
using UnityEngine;

// SFX policy: overlap allowed; reuse idle sources; grow to cap; steal oldest on overflow.
public sealed class SfxPool
{
    private readonly List<AudioSource> _pool;
    private readonly int _maxSize;

    public SfxPool(List<AudioSource> sources, int maxSize)
    {
        _pool    = sources;
        _maxSize = maxSize;
    }
    
    // Fire-and-forget playback.
    // SFX does not reserve a persistent channel.
    public void Play(AudioClip clip, float gain = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[SfxPool] clip is null.");
            return;
        }

        AudioSource source = GetAvailable();
        source.clip = clip;
        source.volume = Mathf.Max(0f, gain);
        source.Play();
    }

    public void StopAll()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            _pool[i].Stop();
            _pool[i].clip = null;
            _pool[i].volume = 1f;
        }
    }
    
    // Source selection policy:
    // idle first, expand if allowed, otherwise recycle the oldest playing source.
    private AudioSource GetAvailable()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            if (!_pool[i].isPlaying)
                return _pool[i];
        }

        if (_pool.Count < _maxSize)
        {
            AudioSource newSource = CreateSource();
            _pool.Add(newSource);
            return newSource;
        }

        return GetOldest();
    }

    private AudioSource GetOldest() 
    {
        AudioSource oldest  = _pool[0];
        float       minTime = _pool[0].time;

        for (int i = 1; i < _pool.Count; i++)
        {
            if (_pool[i].time < minTime)
            {
                minTime = _pool[i].time;
                oldest  = _pool[i];
            }
        }

        oldest.Stop();
        oldest.volume = 1f;
        return oldest;
    }

    // Create a pooled SFX source using the same routing as the template source.
    private AudioSource CreateSource()
    {
        AudioSource newSource = _pool[0].gameObject.AddComponent<AudioSource>();
        newSource.loop        = false;
        newSource.playOnAwake = false;
        
        newSource.outputAudioMixerGroup = _pool[0].outputAudioMixerGroup;
        
        return newSource;
    }
}