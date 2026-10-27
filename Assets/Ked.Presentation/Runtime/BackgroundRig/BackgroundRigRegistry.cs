using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class BackgroundRigRegistry
{
    private sealed class ExternalChildRecord
    {
        public RectTransform ChildRoot;
        public RectTransform RestoreParent;

        public ExternalChildRecord(RectTransform childRoot, RectTransform restoreParent)
        {
            ChildRoot = childRoot;
            RestoreParent = restoreParent;
        }
    }

    private readonly Dictionary<string, BackgroundRigRefs> _rigs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<ExternalChildRecord>> _externalChildrenByRigKey = new(StringComparer.Ordinal);

    public void Register(string rigKey, BackgroundRigRefs rigRefs)
    {
        if (_rigs.TryGetValue(rigKey, out BackgroundRigRefs existingRig))
        {
            DetachExternalChildren(rigKey);
            DestroyRig(existingRig);
        }

        _rigs[rigKey] = rigRefs;
    }

    public bool TryGetRig(string rigKey, out BackgroundRigRefs rigRefs)
    {
        if (!_rigs.TryGetValue(rigKey, out rigRefs))
        {
            Debug.LogWarning($"[BackgroundRigRegistry] Rig not found. rigKey='{rigKey}'.");
            return false;
        }

        return true;
    }
    
    public void Clear()
    {
        List<string> rigKeys = new List<string>(_rigs.Keys);

        for (int i = 0; i < rigKeys.Count; i++)
        {
            string rigKey = rigKeys[i];

            if (!_rigs.TryGetValue(rigKey, out BackgroundRigRefs rigRefs))
                continue;

            DetachExternalChildren(rigKey);
            DestroyRig(rigRefs);
        }

        _rigs.Clear();
        _externalChildrenByRigKey.Clear();
    }
    
    public void CollectAliveRigs(List<BackgroundRigRefs> results)
    {
        if (results == null)
            return;

        foreach (BackgroundRigRefs rigRefs in _rigs.Values)
        {
            if (rigRefs == null)
                continue;

            if (rigRefs.RigRoot == null)
                continue;

            results.Add(rigRefs);
        }
    }

    public void RegisterExternalChild(string rigKey, RectTransform childRoot, RectTransform restoreParent)
    {
        if (!_rigs.ContainsKey(rigKey))
        {
            Debug.LogWarning($"[BackgroundRigRegistry] RegisterExternalChild failed. Background rig not found. rigKey='{rigKey}'.");
            return;
        }

        RectTransform preservedRestoreParent = RemoveExternalChildRecord(childRoot);

        if (preservedRestoreParent != null)
            restoreParent = preservedRestoreParent;

        if (!_externalChildrenByRigKey.TryGetValue(rigKey, out List<ExternalChildRecord> records))
        {
            records = new List<ExternalChildRecord>();
            _externalChildrenByRigKey[rigKey] = records;
        }

        records.Add(new ExternalChildRecord(childRoot, restoreParent));
    }
    

    private RectTransform RemoveExternalChildRecord(RectTransform childRoot)
    {
        RectTransform preservedRestoreParent = null;
        List<string> emptyKeys = null;

        foreach (KeyValuePair<string, List<ExternalChildRecord>> pair in _externalChildrenByRigKey)
        {
            List<ExternalChildRecord> records = pair.Value;

            for (int i = records.Count - 1; i >= 0; i--)
            {
                ExternalChildRecord record = records[i];

                if (record == null || record.ChildRoot == null)
                {
                    records.RemoveAt(i);
                    continue;
                }

                if (record.ChildRoot != childRoot)
                    continue;

                if (preservedRestoreParent == null)
                    preservedRestoreParent = record.RestoreParent;

                records.RemoveAt(i);
            }

            if (records.Count == 0)
            {
                emptyKeys ??= new List<string>();
                emptyKeys.Add(pair.Key);
            }
        }

        if (emptyKeys != null)
        {
            for (int i = 0; i < emptyKeys.Count; i++)
                _externalChildrenByRigKey.Remove(emptyKeys[i]);
        }

        return preservedRestoreParent;
    }

    private void DetachExternalChildren(string rigKey)
    {
        if (!_externalChildrenByRigKey.Remove(rigKey, out List<ExternalChildRecord> records))
            return;

        for (int i = 0; i < records.Count; i++)
        {
            ExternalChildRecord record = records[i];

            if (record == null || record.ChildRoot == null)
                continue;

            RectTransform restoreParent = record.RestoreParent;

            if (restoreParent != null)
                record.ChildRoot.SetParent(restoreParent, false);
            else
                record.ChildRoot.SetParent(null, false);
        }
    }

    private static void DestroyRig(BackgroundRigRefs rigRefs)
    {
        if (rigRefs == null)
            return;

        rigRefs.VisualEffect?.Dispose();
        rigRefs.VisualEffect = null;

        if (rigRefs.RigRoot == null)
            return;

        KillTweenOnHierarchy(rigRefs.RigRoot);
        Object.Destroy(rigRefs.RigRoot.gameObject);
    }

    private static void KillTweenOnHierarchy(Transform root)
    {
        if (root == null)
            return;

        RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            if (rects[i] != null)
                rects[i].DOKill(false);
        }

        CanvasGroup[] canvasGroups = root.GetComponentsInChildren<CanvasGroup>(true);
        for (int i = 0; i < canvasGroups.Length; i++)
        {
            if (canvasGroups[i] != null)
                canvasGroups[i].DOKill(false);
        }

        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].DOKill(false);
        }

        DOTween.Kill(root, false);
        DOTween.Kill(root.gameObject, false);
    }
}