using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class CharacterRigRegistry
{
    private readonly Dictionary<string, CharacterRigRefs> _rigs = new(StringComparer.Ordinal);

    public void Register(string rigKey, CharacterRigRefs rigRefs)
    {
        if (_rigs.TryGetValue(rigKey, out CharacterRigRefs existingRig))
            DestroyRig(existingRig);

        _rigs[rigKey] = rigRefs;
    }

    public bool TryGetRig(string rigKey, out CharacterRigRefs rigRefs)
    {
        if (!_rigs.TryGetValue(rigKey, out rigRefs))
        {
            Debug.LogWarning($"[CharacterRigRegistry] Rig not found. rigKey='{rigKey}'.");
            return false;
        }

        if (rigRefs?.RigRoot == null)
        {
            Debug.LogWarning($"[CharacterRigRegistry] Rig is registered but invalid or destroyed. rigKey='{rigKey}'.");
            rigRefs = null;
            return false;
        }

        return true;
    }

    public void Clear()
    {
        foreach (CharacterRigRefs rigRefs in _rigs.Values)
            DestroyRig(rigRefs);

        _rigs.Clear();
    }
    
    public bool TryPeekRig(string rigKey, out CharacterRigRefs rigRefs)
    {
        if (!_rigs.TryGetValue(rigKey, out rigRefs))
            return false;

        return true;
    }
    
    public void CollectAliveRigs(List<CharacterRigRefs> results)
    {
        if (results == null)
            return;

        foreach (CharacterRigRefs rigRefs in _rigs.Values)
        {
            if (rigRefs == null)
                continue;

            if (rigRefs.RigRoot == null)
                continue;

            results.Add(rigRefs);
        }
    }

    /// <summary>
    /// 키와 함께 살아 있는 리그를 모은다 — 등가성 하네스의 캡처용.
    /// 슬롯 키가 코어 노드 키의 prefix가 되므로 키가 같이 나와야 한다.
    /// </summary>
    public void CollectAliveRigEntries(List<KeyValuePair<string, CharacterRigRefs>> results)
    {
        if (results == null)
            return;

        foreach (KeyValuePair<string, CharacterRigRefs> pair in _rigs)
        {
            if (pair.Value == null || pair.Value.RigRoot == null)
                continue;

            results.Add(pair);
        }
    }

    private static void DestroyRig(CharacterRigRefs rigRefs)
    {
        if (rigRefs == null)
            return;

        // runtime material 명시적 회수. RigRoot가 이미 파괴됐어도 material 누수를 막기 위해 먼저 처리.
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