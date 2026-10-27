using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "CPS/CharRig/Visual Focus Preset DB",
    fileName = "CharacterVisualFocusPresetDB")]
public sealed class CharacterVisualFocusPresetDBSO : ScriptableObject
{
    public const string DefaultPresetKey = CharacterVisualFocusPresetKeyParser.DefaultPresetKey;

    [Serializable]
    public struct Entry
    {
        [Tooltip("Yarn/Command에서 사용할 preset key. ex) clear, focus, defocus, dim, silhouette, outer_rim")]
        public string key;

        [Header("Dim")]
        [Range(0f, 1f)] public float dim;
        public Color dimTintColor;

        [Header("Rim")]
        [Range(0f, 1f)] public float outerRim;
        [Range(0f, 1f)] public float innerRim;
        public Color outerRimColor;
        public Color innerRimColor;

        [Header("Blur")]
        [Tooltip("밉+5탭 디포커스. 0이면 _BLUR 키워드가 꺼져 블러 경로 자체가 실행되지 않는다.")]
        [Range(0f, 1f)] public float blur;
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<string, Entry> _map;

    public bool TryGet(string key, out Entry entry)
    {
        if (_map == null)
            Build();

        string parsedKey = CharacterVisualFocusPresetKeyParser.Parse(key);
        return _map.TryGetValue(parsedKey, out entry);
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

            string key = CharacterVisualFocusPresetKeyParser.Parse(entry.key);

            if (string.IsNullOrEmpty(key))
                continue;

            entry.key = key;
            _map[key] = entry;
        }
    }

    private void Reset()
    {
        Color defaultDimTint = new(0.45f, 0.48f, 0.55f, 1f);
        Color defaultInnerRim = new(1f, 0.96f, 0.86f, 1f);

        entries = new List<Entry>
        {
            new()
            {
                key = "clear",

                blur = 0f,

                dim = 0f,
                dimTintColor = defaultDimTint,

                outerRim = 0f,
                innerRim = 0f,

                outerRimColor = Color.white,
                innerRimColor = defaultInnerRim,
            },
            new()
            {
                key = "focus",

                blur = 0f,

                dim = 0f,
                dimTintColor = defaultDimTint,

                outerRim = 0.4f,
                innerRim = 0.09f,

                outerRimColor = Color.white,
                innerRimColor = defaultInnerRim,
            },
            new()
            {
                key = "defocus",

                blur = 0.5f,

                dim = 0.45f,
                dimTintColor = defaultDimTint,

                outerRim = 0f,
                innerRim = 0f,

                outerRimColor = Color.white,
                innerRimColor = defaultInnerRim,
            },
            new()
            {
                key = "dim",

                blur = 0f,

                dim = 0.55f,
                dimTintColor = defaultDimTint,

                outerRim = 0f,
                innerRim = 0f,

                outerRimColor = Color.white,
                innerRimColor = defaultInnerRim,
            },
            new()
            {
                key = "silhouette",

                blur = 0f,

                dim = 1f,
                dimTintColor = Color.black,

                outerRim = 0f,
                innerRim = 0f,

                outerRimColor = Color.white,
                innerRimColor = defaultInnerRim,
            },
            new()
            {
                key = "inner_rim",

                blur = 0f,

                dim = 0f,
                dimTintColor = defaultDimTint,

                outerRim = 0f,
                innerRim = 0.4f,

                outerRimColor = Color.white,
                innerRimColor = defaultInnerRim,
            },
            new()
            {
                key = "outer_rim",

                blur = 0f,

                dim = 0f,
                dimTintColor = defaultDimTint,

                outerRim = 0.4f,
                innerRim = 0f,

                outerRimColor = Color.white,
                innerRimColor = defaultInnerRim,
            },
        };
    }
}