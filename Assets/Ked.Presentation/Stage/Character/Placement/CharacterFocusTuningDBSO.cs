using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "CPS/CharRig/Tuning/Character Focus Tuning DB",
    fileName = "CharacterFocusTuningDB")]
public sealed class CharacterFocusTuningDBSO : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string key;

        [Header("Default Offset")]
        [Tooltip("이 캐릭터/포즈에 항상 적용되는 기본 focus 보정값입니다.")]
        public Vector2 defaultOffset = Vector2.zero;

        [Header("Preset Offsets")]
        [Tooltip("Feet/Body/Bust/Face 등 프리셋별 캐릭터 보정값입니다.")]
        public CharacterFocusOffsetSet offsets;
    }

    [Header("Global Base Offsets")]
    [Tooltip("캐릭터별 보정이 적용되기 전의 기본 focus preset 위치입니다.")]
    public CharacterFocusOffsetSet baseOffsets = CharacterFocusOffsetSet.Default;

    [Header("Character / Pose Entries")]
    public List<Entry> entries = new();

    private Dictionary<string, Entry> _map;

    public Vector2 ResolveOffset(
        string tuningKey,
        CharacterFocusPreset preset,
        Vector2 commandOffset)
    {
        Vector2 offset = Vector2.zero;

        offset += baseOffsets.Get(preset);

        if (TryGet(tuningKey, out Entry entry))
        {
            offset += entry.defaultOffset;
            offset += entry.offsets.Get(preset);
        }

        offset += commandOffset;

        return offset;
    }

    public bool TryGet(string key, out Entry entry)
    {
        if (_map == null)
            Build();

        key = (key ?? "").Trim();

        if (string.IsNullOrEmpty(key))
        {
            entry = null;
            return false;
        }

        return _map.TryGetValue(key, out entry) && entry != null;
    }

    private void OnEnable() => _map = null;
    private void OnValidate() => _map = null;

    private void Build()
    {
        // 캐릭터 키는 대소문자를 구분하지 않는다.
        // 예: "Willow", "willow", "WILLOW"는 같은 key로 취급한다.
        _map = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];

            if (entry == null)
                continue;

            string key = (entry.key ?? "").Trim();

            if (string.IsNullOrEmpty(key))
                continue;

#if UNITY_EDITOR
            if (_map.TryGetValue(key, out Entry prev) &&
                prev != null &&
                !ReferenceEquals(prev, entry))
            {
                Debug.LogWarning(
                    $"[CharacterFocusTuningDB] Duplicate key ignoring case: '{key}'",
                    this);
            }
#endif

            _map[key] = entry;
        }
    }
}