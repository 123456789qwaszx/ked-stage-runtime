// Voice policy: single channel; no overlap; newest request wins.
// 보이스는 줄 소속 이벤트다 — 줄이 바뀌면 재생 쪽이 FadeOut으로 끝낸다. 음량 설정은 믹서가 맡으니 source.volume은 페이드에만 쓴다.
using System.Collections;
using UnityEngine;

public sealed class VoicePlayer
{
    private readonly AudioSource _source;
    private readonly MonoBehaviour _host;
    private readonly float _baseVolume = 1f;

    private Coroutine _fadeRoutine;

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

    public VoicePlayer(AudioSource source, MonoBehaviour host)
    {
        _source = source;
        _host = host;

        if (_source == null)
        {
            Debug.LogWarning("[VoicePlayer] source is null.");
            return;
        }

        _source.loop = false;
        _baseVolume = _source.volume;
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

        StopFade();
        _source.Stop();
        _source.volume = _baseVolume;
        _source.clip = clip;
        _source.Play();
    }

    public void Stop()
    {
        if (_source == null)
            return;

        StopFade();
        _source.Stop();
        _source.clip = null;
        _source.volume = _baseVolume;
    }

    // 짧게 줄이며 끝낸다. 재생 중이 아니면 Stop과 같다.
    public void FadeOut(float duration)
    {
        if (_source == null)
            return;

        if (!_source.isPlaying || duration <= 0f || _host == null)
        {
            Stop();
            return;
        }

        StopFade();
        _fadeRoutine = _host.StartCoroutine(FadeOutRoutine(duration));
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        float elapsed     = 0f;
        float startVolume = _source.volume;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _source.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        _fadeRoutine = null;
        Stop();
    }

    private void StopFade()
    {
        if (_fadeRoutine == null)
            return;

        _host.StopCoroutine(_fadeRoutine);
        _fadeRoutine = null;
    }
}
