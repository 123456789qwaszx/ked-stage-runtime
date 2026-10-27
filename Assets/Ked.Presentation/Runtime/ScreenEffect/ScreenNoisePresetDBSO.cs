using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "CPS/ScreenEffect/Noise Preset DB",
    fileName = "ScreenNoisePresetDB")]
public sealed class ScreenNoisePresetDBSO : ScriptableObject
{
    public const string DefaultPresetKey = "default";

    [Serializable]
    public struct Entry
    {
        [Tooltip("Yarn/Command에서 사용할 preset key. ex) clear, default, memory, horror, broadcast")]
        public string key;

        [Range(0f, 1f)] public float amount;
        public Color color;
        [Min(0f)] public float scale;
        public float speedX;
        public float speedY;
        [Min(0f)] public float contrast;
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

        if (key == "normal" || key == "base")
            return DefaultPresetKey;

        if (key == "rain" || key == "rainmood")
            return "rain_mood";

        return key;
    }

    private void OnEnable() => _map = null;

    // 플레이 모드 인스펙터 수정 -> 캐시 무효화 -> 다음 발화에 반영.
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
                amount = 0f,
                color = Color.white,
                scale = 0.8f,
                speedX = 0.015f,
                speedY = 0.012f,
                contrast = 1f
            },
            new()
            {
                key = "default",
                amount = 1f,
                color = Color.white,
                scale = 0.8f,
                speedX = 0.015f,
                speedY = 0.012f,
                contrast = 1f
            },
            new()
            {
                key = "memory",
                amount = 0.32f,
                color = new Color(0.72f, 0.78f, 0.90f, 1f),
                scale = 0.85f,
                speedX = 0.010f,
                speedY = 0.008f,
                contrast = 1.15f
            },
            new()
            {
                key = "horror",
                amount = 0.55f,
                color = new Color(0.62f, 0.62f, 0.66f, 1f),
                scale = 1.25f,
                speedX = 0.030f,
                speedY = 0.045f,
                contrast = 1.8f
            },
            new()
            {
                key = "broadcast",
                amount = 0.50f,
                color = new Color(0.80f, 0.90f, 1f, 1f),
                scale = 1.8f,
                speedX = 0.080f,
                speedY = 0.012f,
                contrast = 2.2f
            },
            new()
            {
                key = "dream",
                amount = 0.22f,
                color = new Color(0.76f, 0.70f, 0.95f, 1f),
                scale = 0.65f,
                speedX = 0.006f,
                speedY = 0.010f,
                contrast = 0.9f
            },
            new()
            {
                key = "rain_mood",
                amount = 0.28f,
                color = new Color(0.70f, 0.78f, 0.90f, 1f),
                scale = 1.0f,
                speedX = 0.012f,
                speedY = 0.035f,
                contrast = 1.1f
            },
        };
    }
}