using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "CPS/ScreenEffect/Vignette Preset DB",
    fileName = "ScreenVignettePresetDB")]
public sealed class ScreenVignettePresetDBSO : ScriptableObject
{
    public const string DefaultPresetKey = "focus";

    [Serializable]
    public struct Entry
    {
        [Tooltip("Yarn/Command에서 사용할 preset key. ex) clear, focus, horror, dream, letterbox")]
        public string key;

        [Range(0f, 1f)] public float amount;
        public Color color;
        [Range(0f, 1f)] public float radius;
        [Range(0.001f, 1f)] public float softness;
        [Min(0f)] public float aspect;
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

        if (key == "default" || key == "default_focus")
            return DefaultPresetKey;

        if (key == "lb" || key == "letter_box")
            return "letterbox";

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
                color = Color.black,
                radius = 0.45f,
                softness = 0.35f,
                aspect = 1.777f
            },
            new()
            {
                key = "focus",
                amount = 0.35f,
                color = Color.black,
                radius = 0.25f,
                softness = 0.10f,
                aspect = 1.2f
            },
            new()
            {
                key = "tension",
                amount = 0.55f,
                color = Color.black,
                radius = 0.15f,
                softness = 0.22f,
                aspect = 1.2f
            },
            new()
            {
                key = "horror",
                amount = 0.78f,
                color = new Color(0.02f, 0.015f, 0.018f, 1f),
                radius = 0.14f,
                softness = 0.36f,
                aspect = 1.2f
            },
            new()
            {
                key = "danger",
                amount = 0.58f,
                color = new Color(0.35f, 0.02f, 0.015f, 1f),
                radius = 0.14f,
                softness = 0.34f,
                aspect = 1.2f
            },
            new()
            {
                key = "memory",
                amount = 0.36f,
                color = new Color(0.34f, 0.38f, 0.48f, 1f),
                radius = 0.10f,
                softness = 0.36f,
                aspect = 1.2f
            },
            new()
            {
                key = "dream",
                amount = 0.32f,
                color = new Color(0.38f, 0.32f, 0.52f, 1f),
                radius = 0.34f,
                softness = 0.12f,
                aspect = 1.2f
            },
            new()
            {
                key = "letterbox",
                amount = 1f,
                color = Color.black,
                radius = 0.288f,
                softness = 0.001f,
                aspect = 0f
            },
        };
    }
}