using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class ScreenEffectRigBuilder
{
    public RectTransform BuildRigRoot(
        RectTransform rigPrefab = null,
        string rigRootName = "ScreenEffectRig")
    {
        RectTransform rigRoot;

        if (rigPrefab != null)
        {
            rigRoot = Object.Instantiate(rigPrefab);
            rigRoot.name = rigRootName;
        }
        else
        {
            GameObject rootGo = new(rigRootName, typeof(RectTransform));
            rigRoot = (RectTransform)rootGo.transform;
        }

        StretchFull(rigRoot);
        EnsureGraph(rigRoot);

        return rigRoot;
    }

    public void BindRefsFromRoot(
        RectTransform rigRoot,
        out ScreenEffectRigRefs refs)
    {
        if (rigRoot == null)
        {
            Debug.LogWarning("[ScreenEffectRigBuilder] rigRoot is null.");
            refs = null;
            return;
        }

        EnsureGraph(rigRoot);

        Dictionary<ScreenEffectRigSchema.Refs, RectTransform> map =
            CollectRefMap(rigRoot);

        EnsureCompleteMap(rigRoot, map);

        refs = BuildRefs(rigRoot, map);
    }

    private void EnsureGraph(RectTransform root)
    {
        foreach (ScreenEffectRigSchema.NodeDef node in ScreenEffectRigSchema.Nodes)
            EnsureNode(root, node);

        NormalizeSiblingOrder(root);
    }

    private void EnsureNode(
        RectTransform root,
        ScreenEffectRigSchema.NodeDef node)
    {
        RectTransform parentRt = node.Parent.HasValue
            ? FindByName(root, node.Parent.Value.ToString()) as RectTransform
            : root;

        if (parentRt == null)
        {
            Debug.LogWarning(
                $"[ScreenEffectRigBuilder] Parent not found. " +
                $"node='{node.Id}', parent='{node.Parent}'. Attach to root.",
                root);

            parentRt = root;
        }

        RectTransform rt = EnsureRect(root, parentRt, node.Id.ToString());

        if (node.StretchFull)
            StretchFull(rt);

        Image image = null;
        Material sourceMaterial = LoadMaterial(node.MaterialResourcesPath);

        if (node.NeedsImage)
        {
            image = EnsureImage(rt, node);

            if (sourceMaterial != null)
                image.material = sourceMaterial;
        }

        EnsureController(rt.gameObject, image, sourceMaterial, node.Controller);
    }

    private Image EnsureImage(
        RectTransform rt,
        ScreenEffectRigSchema.NodeDef node)
    {
        if (!rt.TryGetComponent(out Image image))
            image = rt.gameObject.AddComponent<Image>();

        image.color = node.InitialImageColor;
        image.raycastTarget = node.RaycastTarget;

        return image;
    }

    private void EnsureController(
        GameObject go,
        Image image,
        Material sourceMaterial,
        ScreenEffectRigSchema.ControllerKind kind)
    {
        switch (kind)
        {
            case ScreenEffectRigSchema.ControllerKind.Vignette:
                EnsureEffectController<ScreenVignetteEffectController>(go, image, sourceMaterial);
                break;

            case ScreenEffectRigSchema.ControllerKind.Noise:
                EnsureEffectController<ScreenNoiseEffectController>(go, image, sourceMaterial);
                break;

            case ScreenEffectRigSchema.ControllerKind.Flash:
                EnsureEffectController<ScreenFlashEffectController>(go, image, sourceMaterial);
                break;

            case ScreenEffectRigSchema.ControllerKind.None:
                break;
        }
    }

    private void EnsureEffectController<T>(
        GameObject go,
        Image image,
        Material sourceMaterial)
        where T : Component, IScreenEffectController
    {
        if (!go.TryGetComponent(out T controller))
            controller = go.AddComponent<T>();

        controller.Bind(image, sourceMaterial);
    }

    private static Material LoadMaterial(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        Material material = Resources.Load<Material>(path);

        if (material == null)
        {
            Debug.LogWarning(
                $"[ScreenEffectRigBuilder] Material not found at Resources path. path='{path}'.");
        }

        return material;
    }

    private RectTransform EnsureRect(
        RectTransform root,
        RectTransform parent,
        string name)
    {
        RectTransform existing = FindByName(root, name) as RectTransform;

        if (existing == null)
        {
            GameObject go = new(name, typeof(RectTransform));
            existing = (RectTransform)go.transform;
        }

        if (existing.parent != parent)
            existing.SetParent(parent, false);

        StretchFull(existing);

        return existing;
    }

    private Dictionary<ScreenEffectRigSchema.Refs, RectTransform> CollectRefMap(
        RectTransform rigRoot)
    {
        Dictionary<ScreenEffectRigSchema.Refs, RectTransform> map = new();

        foreach (ScreenEffectRigSchema.Refs id in
                 Enum.GetValues(typeof(ScreenEffectRigSchema.Refs)))
        {
            Transform t = FindByName(rigRoot, id.ToString());

            if (t is RectTransform rt)
                map[id] = rt;
        }

        return map;
    }

    private void EnsureCompleteMap(
        RectTransform rigRoot,
        Dictionary<ScreenEffectRigSchema.Refs, RectTransform> map)
    {
        int expectedCount =
            Enum.GetValues(typeof(ScreenEffectRigSchema.Refs)).Length;

        if (map.Count >= expectedCount)
            return;

        Debug.LogWarning(
            $"[ScreenEffectRigBuilder] Incomplete rig graph after EnsureGraph. " +
            $"expected={expectedCount}, actual={map.Count}, rigRoot='{rigRoot.name}'.",
            rigRoot);
    }

    private ScreenEffectRigRefs BuildRefs(
        RectTransform rigRoot,
        Dictionary<ScreenEffectRigSchema.Refs, RectTransform> map)
    {
        ScreenEffectRigRefs refs = new(rigRoot);

        RectTransform GetRt(ScreenEffectRigSchema.Refs key)
        {
            if (!map.TryGetValue(key, out RectTransform rt) || rt == null)
            {
                Debug.LogWarning($"[ScreenEffectRigBuilder] Missing bound ref '{key}'.");
                return null;
            }

            return rt;
        }

        Image GetImg(ScreenEffectRigSchema.Refs key)
        {
            RectTransform rt = GetRt(key);
            return rt != null ? rt.GetComponent<Image>() : null;
        }

        refs.ScreenOverlay_Root = GetRt(ScreenEffectRigSchema.Refs.ScreenOverlay_Root);

        refs.Vignette_Image = GetImg(ScreenEffectRigSchema.Refs.Vignette_Image);
        refs.Noise_Image = GetImg(ScreenEffectRigSchema.Refs.Noise_Image);
        refs.Flash_Image = GetImg(ScreenEffectRigSchema.Refs.Flash_Image);

        refs.Vignette = refs.Vignette_Image != null
            ? refs.Vignette_Image.GetComponent<ScreenVignetteEffectController>()
            : null;

        refs.Noise = refs.Noise_Image != null
            ? refs.Noise_Image.GetComponent<ScreenNoiseEffectController>()
            : null;

        refs.Flash = refs.Flash_Image != null
            ? refs.Flash_Image.GetComponent<ScreenFlashEffectController>()
            : null;

        return refs;
    }

    private void NormalizeSiblingOrder(RectTransform root)
    {
        Dictionary<Transform, int> nextIndexByParent = new();

        foreach (ScreenEffectRigSchema.NodeDef node in ScreenEffectRigSchema.Nodes)
        {
            RectTransform rt = FindByName(root, node.Id.ToString()) as RectTransform;

            if (rt == null)
                continue;

            RectTransform parent = node.Parent.HasValue
                ? FindByName(root, node.Parent.Value.ToString()) as RectTransform
                : root;

            if (parent == null)
                continue;

            int index = nextIndexByParent.TryGetValue(parent, out int current)
                ? current
                : 0;

            if (rt.parent == parent)
                rt.SetSiblingIndex(index);

            nextIndexByParent[parent] = index + 1;
        }
    }

    private Transform FindByName(Transform root, string name)
    {
        if (root == null)
            return null;

        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindByName(root.GetChild(i), name);

            if (found != null)
                return found;
        }

        return null;
    }

    private static void StretchFull(RectTransform rt)
    {
        if (rt == null)
            return;

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }
}