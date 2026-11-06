using UnityEngine;
using UnityEngine.Audio;

public sealed class AudioVolumeSettings
{
    private const string KeyBgm   = "Volume_BGM";
    private const string KeyVoice = "Volume_Voice";
    private const string KeySfx   = "Volume_SFX";

    private const float DefaultVolume = 0.8f;

    private readonly AudioMixer _mixer;

    public float BgmVolume   { get; private set; }
    public float VoiceVolume { get; private set; }
    public float SfxVolume   { get; private set; }

    public AudioVolumeSettings(AudioMixer mixer)
    {
        _mixer = mixer;
    }

    // Restore saved values and apply them to the mixer.
    public void Load()
    {
        BgmVolume   = PlayerPrefs.GetFloat(KeyBgm,   DefaultVolume);
        VoiceVolume = PlayerPrefs.GetFloat(KeyVoice, DefaultVolume);
        SfxVolume   = PlayerPrefs.GetFloat(KeySfx,   DefaultVolume);

        ApplyToMixer(BgmVolume,   "BGMVolume");
        ApplyToMixer(VoiceVolume, "VoiceVolume");
        ApplyToMixer(SfxVolume,   "SFXVolume");
    }

    public void SetBgmVolume(float value)
    {
        BgmVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KeyBgm, BgmVolume);
        ApplyToMixer(BgmVolume, "BGMVolume");
    }

    public void SetVoiceVolume(float value)
    {
        VoiceVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KeyVoice, VoiceVolume);
        ApplyToMixer(VoiceVolume, "VoiceVolume");
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KeySfx, SfxVolume);
        ApplyToMixer(SfxVolume, "SFXVolume");
    }

    private void ApplyToMixer(float linearValue, string exposedParam)
    {
        float dB = linearValue <= 0.0001f
            ? -80f
            : Mathf.Log10(linearValue) * 20f;

        _mixer.SetFloat(exposedParam, dB);
    }
}