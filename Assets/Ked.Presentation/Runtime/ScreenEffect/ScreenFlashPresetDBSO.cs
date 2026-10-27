using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(
    menuName = "CPS/ScreenEffect/Flash Preset DB",
    fileName = "ScreenFlashPresetDB")]
public sealed class ScreenFlashPresetDBSO : ScriptableObject
{
    public const string DefaultPresetKey = "default";

    [Serializable]
    public struct Entry
    {
        [Tooltip("Yarn/Command에서 사용할 preset key. ex) clear, default, soft, hit, camera")]
        public string key;

        public Color color;
        [Range(0f, 1f)] public float amount;
        [Min(0f)] public float attackDuration;
        [Min(0f)] public float holdDuration;
        [Min(0f)] public float releaseDuration;
        public Ease attackEase;
        public Ease releaseEase;
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<string, Entry> _map;

    public bool TryGet(string key, out Entry entry)
    {
        if (_map == null)
            Build();

        return _map.TryGetValue(NormalizeKey(key), out entry);
    }

    public static string NormalizeKey(string key)
    {
        key = (key ?? "").Trim();

        if (string.IsNullOrEmpty(key))
            return DefaultPresetKey;

        key = key.ToLowerInvariant();
        key = key.Replace(" ", "_");
        key = key.Replace("-", "_");

        if (key == "normal" || key == "white")
            return DefaultPresetKey;

        if (key == "damage" || key == "red")
            return "hit";

        if (key == "photo")
            return "camera";

        return key;
    }

    private void OnEnable() => _map = null;
    private void OnValidate() => _map = null;

    private void Build()
    {
        _map = new Dictionary<string, Entry>(StringComparer.Ordinal);

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            string key = NormalizeKey(entry.key);

            if (string.IsNullOrEmpty(key))
                continue;

            entry.key = key;
            _map[key] = entry;
        }
    }

    private void Reset()
    {
        entries = new List<Entry>
        {
            new()
            {
                key = "clear",
                color = Color.white,
                amount = 0f,
                attackDuration = 0f,
                holdDuration = 0f,
                releaseDuration = 0f,
                attackEase = Ease.OutCubic,
                releaseEase = Ease.OutCubic
            },
            new()
            {
                key = "default",
                color = Color.white,
                amount = 1f,
                attackDuration = 0.02f,
                holdDuration = 0.01f,
                releaseDuration = 0.16f,
                attackEase = Ease.OutCubic,
                releaseEase = Ease.OutCubic
            },
            new()
            {
                key = "soft",
                color = Color.white,
                amount = 0.5f,
                attackDuration = 0.06f,
                holdDuration = 0.02f,
                releaseDuration = 0.30f,
                attackEase = Ease.OutCubic,
                releaseEase = Ease.OutCubic
            },
            new()
            {
                key = "hit",
                color = new Color(1f, 0.16f, 0.10f, 1f),
                amount = 0.45f,
                attackDuration = 0.015f,
                holdDuration = 0.015f,
                releaseDuration = 0.18f,
                attackEase = Ease.OutCubic,
                releaseEase = Ease.OutCubic
            },
            new()
            {
                key = "camera",
                color = Color.white,
                amount = 1f,
                attackDuration = 0.01f,
                holdDuration = 0f,
                releaseDuration = 0.10f,
                attackEase = Ease.OutQuad,
                releaseEase = Ease.OutCubic
            },
        };
    }
}