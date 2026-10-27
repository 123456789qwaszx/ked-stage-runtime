using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class BackgroundRigBuilder
{
    public RectTransform BuildBackgroundRigRoot(
        RectTransform rigPrefab = null,
        string rolePrefix = "",
        string rigRootName = "BackgroundRig")
    {
        RectTransform rigRoot;

        if (rigPrefab != null)
        {
            rigRoot = Object.Instantiate(rigPrefab);
            rigRoot.name = WithRole(rolePrefix, rigRootName);

            if (!string.IsNullOrEmpty(rolePrefix))
                PrefixAllChildren(rigRoot.transform, rolePrefix);
        }
        else
        {
            GameObject rootGo = new(WithRole(rolePrefix, rigRootName), typeof(RectTransform));
            rigRoot = (RectTransform)rootGo.transform;

            StretchFull(rigRoot);
            EnsureGraph(rigRoot, rolePrefix);
        }

        return rigRoot;
    }

    public void BindRefsFromRoot(RectTransform rigRoot, string rolePrefix, out BackgroundRigRefs refs)
    {
        Dictionary<BackgroundRigSchema.Refs, RectTransform> map = CollectRefMap(rigRoot, rolePrefix);
        EnsureValidGraphMap(rigRoot, rolePrefix, ref map);

        refs = BuildRefs(rigRoot, map);
    }

    #region Graph Recovery
    private void EnsureValidGraphMap(
        RectTransform rigRoot,
        string rolePrefix,
        ref Dictionary<BackgroundRigSchema.Refs, RectTransform> map)
    {
        int expectedCount = Enum.GetValues(typeof(BackgroundRigSchema.Refs)).Length;

        if (map.Count >= expectedCount)
            return;

        Debug.LogWarning(
            $"[BackgroundRigBuilder] Invalid rig graph. " +
            $"Rebuilding from BackgroundRigSchema. " +
            $"Prefab may be broken, or saved with another role prefix. " +
            $"For reusable prefab baking, call StripRolePrefixForBake after refs registration before saving. " +
            $"rigRoot='{rigRoot.name}', rolePrefix='{rolePrefix}'.",
            rigRoot);

        for (int i = rigRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = rigRoot.GetChild(i);
            child.SetParent(null, false);
            Object.Destroy(child.gameObject);
        }

        EnsureGraph(rigRoot, rolePrefix);

        map = CollectRefMap(rigRoot, rolePrefix);
    }
    #endregion

    #region Auto Create Graph
    private void EnsureGraph(RectTransform root, string rolePrefix)
    {
        foreach (BackgroundRigSchema.NodeDef node in BackgroundRigSchema.Nodes)
            EnsureNode(root, rolePrefix, node);
    }

    private void EnsureNode(RectTransform root, string rolePrefix, BackgroundRigSchema.NodeDef node)
    {
        RectTransform parentRt = node.Parent.HasValue
            ? FindByName(root, WithRole(rolePrefix, node.Parent.Value.ToString())) as RectTransform
            : root;

        RectTransform rt = EnsureRect(parentRt, WithRole(rolePrefix, node.Id.ToString()));

        if (node.NeedsCanvasGroup)
        {
            if (!rt.TryGetComponent<CanvasGroup>(out CanvasGroup canvasGroup))
                canvasGroup = rt.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = node.InitialCanvasGroupAlpha;
        }

        if (node.NeedsImage && !rt.TryGetComponent<Image>(out _))
        {
            Image image = rt.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
        }

        if (node.NeedsMask && !rt.TryGetComponent<Mask>(out _))
            rt.gameObject.AddComponent<Mask>();
    }

    private RectTransform EnsureRect(RectTransform parent, string name)
    {
        RectTransform existing = FindByName(parent, name) as RectTransform;
        if (existing != null)
            return existing;

        GameObject go = new(name, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        StretchFull(rt);

        return rt;
    }
    #endregion

    #region Binding / Refs
    private Dictionary<BackgroundRigSchema.Refs, RectTransform> CollectRefMap(
        RectTransform rigRoot,
        string rolePrefix)
    {
        Dictionary<BackgroundRigSchema.Refs, RectTransform> map = new();

        foreach (BackgroundRigSchema.Refs id in Enum.GetValues(typeof(BackgroundRigSchema.Refs)))
        {
            string nodeName = WithRole(rolePrefix, id.ToString());
            RectTransform t = FindByName(rigRoot, nodeName) as RectTransform;

            if (t != null)
                map[id] = t;
        }

        return map;
    }

    private BackgroundRigRefs BuildRefs(
        RectTransform rigRoot,
        Dictionary<BackgroundRigSchema.Refs, RectTransform> map)
    {
        BackgroundRigRefs refs = new(rigRoot);

        RectTransform GetRt(BackgroundRigSchema.Refs key)
        {
            if (!map.TryGetValue(key, out RectTransform targetRect) || targetRect == null)
            {
                Debug.LogWarning($"[BackgroundRigBuilder] Missing bound ref '{key}'.");
                return null;
            }

            return targetRect;
        }

        Image GetImg(BackgroundRigSchema.Refs key)
        {
            RectTransform rt = GetRt(key);
            if (rt == null)
                return null;

            Image img = rt.GetComponent<Image>();
            if (img == null)
            {
                Debug.LogWarning($"[BackgroundRigBuilder] Missing Image on '{rt.name}'.");
                return null;
            }

            return img;
        }

        // Background base axis - response-neutral placement / measurement
        refs.Background_Root = GetRt(BackgroundRigSchema.Refs.Background_Root);

        // Background casting axis - per-background defaults
        refs.Background_Anchor = GetRt(BackgroundRigSchema.Refs.Background_Anchor);

        // Background acting axis
        refs.Background_Track_Move = GetRt(BackgroundRigSchema.Refs.Background_Track_Move);
        refs.Background_Track_X = GetRt(BackgroundRigSchema.Refs.Background_Track_X);
        refs.Background_Track_Y = GetRt(BackgroundRigSchema.Refs.Background_Track_Y);
        refs.Background_Original_Rotation = GetRt(BackgroundRigSchema.Refs.Background_Original_Rotation);
        refs.Background_Rotation = GetRt(BackgroundRigSchema.Refs.Background_Rotation);
        refs.Background_Shake = GetRt(BackgroundRigSchema.Refs.Background_Shake);

        refs.Background_Size = GetRt(BackgroundRigSchema.Refs.Background_Size);
        refs.Background_Scale = GetRt(BackgroundRigSchema.Refs.Background_Scale);
        refs.Background_DepthScale = GetRt(BackgroundRigSchema.Refs.Background_DepthScale);

        refs.Background_Mask = GetRt(BackgroundRigSchema.Refs.Background_Mask);

        refs.Background_ActingScale = GetRt(BackgroundRigSchema.Refs.Background_ActingScale);
        refs.Background_ActingScale_X = GetRt(BackgroundRigSchema.Refs.Background_ActingScale_X);
        refs.Background_ActingScale_Y = GetRt(BackgroundRigSchema.Refs.Background_ActingScale_Y);

        // Background sprite
        refs.BackgroundSprite_Root = GetRt(BackgroundRigSchema.Refs.BackgroundSprite_Root);
        refs.BackgroundSprite_Image = GetImg(BackgroundRigSchema.Refs.BackgroundSprite_Image);

        // Object slots
        refs.Background_ObjectSlotRoot = GetRt(BackgroundRigSchema.Refs.Background_ObjectSlotRoot);

        return refs;
    }
    #endregion

    #region Helper
    private Transform FindByName(Transform root, string name)
    {
        if (root == null)
            return null;

        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            Transform found = FindByName(child, name);

            if (found != null)
                return found;
        }

        return null;
    }

    private void PrefixAllChildren(Transform root, string rolePrefix)
    {
        if (root == null || string.IsNullOrEmpty(rolePrefix))
            return;

        void Walk(Transform t)
        {
            t.name = WithRole(rolePrefix, t.name);

            for (int i = 0; i < t.childCount; i++)
                Walk(t.GetChild(i));
        }

        Walk(root);
    }

    private void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private string WithRole(string rolePrefix, string baseName)
    {
        if (string.IsNullOrEmpty(rolePrefix))
            return baseName;

        if (baseName.StartsWith(rolePrefix, StringComparison.Ordinal))
            return baseName;

        return $"{rolePrefix}{baseName}";
    }
    #endregion
}