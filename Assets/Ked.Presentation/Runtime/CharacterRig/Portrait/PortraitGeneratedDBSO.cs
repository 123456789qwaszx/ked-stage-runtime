using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//[CreateAssetMenu(menuName = "CPS/Portraits/Generated DB", fileName = "PortraitGeneratedDB")]
public sealed class PortraitGeneratedDBSO : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public string characterId;
        public string variantKey;
        public string emotionKey;
        public Sprite sprite;

        [Tooltip("원본 에셋 경로 (디버깅용)")]
        public string assetPath;

        public override string ToString()
            => $"{characterId}|{variantKey}|{emotionKey} -> {(sprite ? sprite.name : "NULL")}";
    }

    [Header("Generated (Read-only)")]
    public List<Entry> entries = new List<Entry>();

    [Header("Build Info")]
    public long generatedTicksUtc;
    public string generatedTimeReadable;

    public int TotalEntries => entries != null ? entries.Count : 0;

    public int TotalCharacters
    {
        get
        {
            if (entries == null || entries.Count == 0) return 0;
            return entries.Select(e => e.characterId).Distinct().Count();
        }
    }

    public int TotalVariants
    {
        get
        {
            if (entries == null || entries.Count == 0) return 0;
            var set = new HashSet<string>();
            foreach (var e in entries)
                set.Add(e.characterId + "|" + e.variantKey);
            return set.Count;
        }
    }

    public int TotalUniqueEmotions
    {
        get
        {
            if (entries == null || entries.Count == 0) return 0;
            return entries.Select(e => e.emotionKey).Distinct().Count();
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Print Stats")]
    private void PrintStats()
    {
        Debug.Log(
            $"[PortraitDb Stats]\n" +
            $"  Entries: {TotalEntries}\n" +
            $"  Characters: {TotalCharacters}\n" +
            $"  Variants: {TotalVariants}\n" +
            $"  Unique Emotions: {TotalUniqueEmotions}\n" +
            $"  Generated: {generatedTimeReadable}",
            this
        );
    }

    [ContextMenu("Print All Characters")]
    private void PrintAllCharacters()
    {
        if (entries == null || entries.Count == 0)
        {
            Debug.Log("Db is empty.", this);
            return;
        }

        var chars = entries.Select(e => e.characterId).Distinct().OrderBy(c => c).ToList();
        Debug.Log($"[PortraitDb] Characters ({chars.Count}):\n  " + string.Join(", ", chars), this);
    }

    [ContextMenu("Find Missing Sprites")]
    private void FindMissingSprites()
    {
        if (entries == null || entries.Count == 0)
        {
            Debug.Log("Db is empty.", this);
            return;
        }

        var missing = entries.Where(e => e.sprite == null).ToList();
        if (missing.Count == 0)
        {
            Debug.Log("No missing sprites!", this);
            return;
        }

        Debug.LogWarning(
            $"[PortraitDb] {missing.Count} missing sprites:\n" +
            string.Join("\n", missing.Select(e => $"  {e}")),
            this
        );
    }

    [ContextMenu("Find Duplicates")]
    private void FindDuplicates()
    {
        if (entries == null || entries.Count == 0)
        {
            Debug.Log("Db is empty.", this);
            return;
        }

        var seen = new HashSet<string>();
        var duplicates = new List<Entry>();

        foreach (var e in entries)
        {
            var key = e.characterId + "|" + e.variantKey + "|" + e.emotionKey;
            if (!seen.Add(key))
                duplicates.Add(e);
        }

        if (duplicates.Count == 0)
        {
            Debug.Log("No duplicates!", this);
            return;
        }

        Debug.LogWarning(
            $"[PortraitDb] {duplicates.Count} duplicates:\n" +
            string.Join("\n", duplicates.Select(e => $"  {e}")),
            this
        );
    }
#endif
}