using System.Collections;
using UnityEngine;

// BGM policy: single active track; crossfade on replace; snap while skipping.
public sealed class BgmPlayer
{
    private readonly AudioSource _sourceA;
    private readonly AudioSource _sourceB;
    private readonly MonoBehaviour _host;

    private AudioSource _current;
    private AudioSource _next;

    private Coroutine _fadeRoutine;

    public AudioClip CurrentClip => _current?.clip;
    public bool IsPlaying => _current != null && _current.isPlaying;

    public BgmPlayer(AudioSource sourceA, AudioSource sourceB, MonoBehaviour host)
    {
        _sourceA = sourceA;
        _sourceB = sourceB;
        _host    = host;

        _sourceA.loop   = true;
        _sourceB.loop   = true;
        _sourceA.volume = 0f;
        _sourceB.volume = 0f;

        _current = _sourceA;
        _next    = _sourceB;
    }

    // Replace policy.
    // If another BGM is already playing, transition to the new clip.
    // While skipping, apply the final state immediately without fade.
    public void Play(AudioClip clip, float fadeDuration, bool isSkipping)
    {
        if (clip == null)
        {
            return;
        }

        if (_current.clip == clip && _current.isPlaying)
            return;

        StopFade();

        if (isSkipping || fadeDuration <= 0f)
        {
            SnapTo(clip);
            return;
        }

        _fadeRoutine = _host.StartCoroutine(CrossFade(clip, fadeDuration));
    }

    public void Stop(float fadeDuration, bool isSkipping)
    {
        StopFade();

        if (isSkipping || fadeDuration <= 0f)
        {
            _current.Stop();
            _current.clip   = null;
            _current.volume = 0f;
            return;
        }

        _fadeRoutine = _host.StartCoroutine(FadeOut(fadeDuration));
    }

    private void SnapTo(AudioClip clip)
    {
        _current.Stop();
        _next.Stop();

        _current.volume = 1f;
        _current.clip   = clip;
        _current.Play();

        _next.volume = 0f;
        _next.clip   = null;
    }

    private IEnumerator CrossFade(AudioClip clip, float duration)
    {
        _next.clip   = clip;
        _next.volume = 0f;
        _next.Play();

        float elapsed = 0f;
        float startVolume = _current.volume;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            _current.volume = Mathf.Lerp(startVolume, 0f, t);
            _next.volume    = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        _current.Stop();
        _current.clip   = null;
        _current.volume = 0f;

        (_current, _next) = (_next, _current);

        _fadeRoutine = null;
    }

    private IEnumerator FadeOut(float duration)
    {
        float elapsed     = 0f;
        float startVolume = _current.volume;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _current.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        _current.Stop();
        _current.clip   = null;
        _current.volume = 0f;

        _fadeRoutine = null;
    }

    private void StopFade()
    {
        if (_fadeRoutine == null) return;
        _host.StopCoroutine(_fadeRoutine);
        _fadeRoutine = null;
    }
}