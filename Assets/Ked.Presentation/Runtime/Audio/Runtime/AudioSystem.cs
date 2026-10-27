using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public sealed class AudioSystem : MonoBehaviour
{
    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;

    [Header("BGM")]
    [SerializeField] private AudioSource bgmSourceA;
    [SerializeField] private AudioSource bgmSourceB;

    [Header("Voice")]
    [SerializeField] private AudioSource voiceSource;

    // SFX:
    // - Assign at least one SFX AudioSource in the inspector
    // - The pool may grow at runtime up to _sfxMaxSize.
    [Header("SFX (assign at least 1 source)")]
    [SerializeField] private List<AudioSource> sfxSources;
    [SerializeField] private int sfxMaxSize = 10;

    public BgmPlayer           Bgm     { get; private set; }
    public VoicePlayer         Voice   { get; private set; }
    public SfxPool             Sfx     { get; private set; }
    public AudioVolumeSettings Volume  { get; private set; }

    public void Initialize()
    {
        EnsureRuntimeSources();
        
        Volume = new AudioVolumeSettings(mixer);
        Volume.Load();

        Bgm   = new BgmPlayer(bgmSourceA, bgmSourceB, this);
        Voice = new VoicePlayer(voiceSource);
        Sfx   = new SfxPool(sfxSources, sfxMaxSize);
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            PlayerPrefs.Save();
    }
    
    #region auto_create_fallback
    
    private void EnsureRuntimeSources()
    {
        if (bgmSourceA == null)
        {
            bgmSourceA = CreateAudioSource("BGM Source A", loop: true);
            WarnAutoCreated("BGM Source A",
                "BGM sources should be assigned in the inspector when possible.");
        }

        if (bgmSourceB == null)
        {
            bgmSourceB = CreateAudioSource("BGM Source B", loop: true);
            CopyAudioSourceRouting(bgmSourceA, bgmSourceB);
            WarnAutoCreated("BGM Source B",
                "BGM sources should be assigned in the inspector when possible.");
        }

        if (voiceSource == null)
        {
            voiceSource = CreateAudioSource("Voice Source", loop: false);
            WarnAutoCreated("Voice Source",
                "Voice source should be assigned in the inspector when possible.");
        }

        if (sfxSources == null)
            sfxSources = new List<AudioSource>();

        RemoveNullSfxEntries();

        if (sfxSources.Count == 0)
        {
            AudioSource template = CreateAudioSource("SFX Source 0", loop: false);
            sfxSources.Add(template);

            WarnAutoCreated("SFX Source 0",
                "At least one SFX AudioSource template is recommended in the inspector.");
        }
    }

    private void RemoveNullSfxEntries()
    {
        for (int i = sfxSources.Count - 1; i >= 0; i--)
        {
            if (sfxSources[i] == null)
                sfxSources.RemoveAt(i);
        }
    }

    private AudioSource CreateAudioSource(string childName, bool loop)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(transform, false);

        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.clip = null;

        return source;
    }

    private void CopyAudioSourceRouting(AudioSource from, AudioSource to)
    {
        if (from == null || to == null)
            return;

        to.outputAudioMixerGroup = from.outputAudioMixerGroup;
        to.spatialBlend = from.spatialBlend;
        to.volume = from.volume;
        to.priority = from.priority;
        to.pitch = from.pitch;
        to.panStereo = from.panStereo;
        to.reverbZoneMix = from.reverbZoneMix;
        to.dopplerLevel = from.dopplerLevel;
        to.spread = from.spread;
        to.minDistance = from.minDistance;
        to.maxDistance = from.maxDistance;
        to.rolloffMode = from.rolloffMode;
    }

    private void WarnAutoCreated(string sourceName, string recommendation)
    {
        Debug.LogWarning(
            $"[AudioSystem] '{sourceName}' was missing and has been auto-created at runtime. " +
            $"{recommendation} Runtime auto-creation is a fallback path, not the preferred setup.",
            this);
    }
    
    #endregion
}