using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// 무대 리그를 스키마대로 세우고, 있는 것은 검증·복구한다. CharacterRigBuilder와 같은 규율이다.
/// 씬에 무대가 남아 있어도 된다 — 이름·부모·컴포넌트·항등이 하나라도 어긋나면 부수고 다시 세운다.
/// 파괴 전에 SetParent(null)로 떼는 줄은 장식이 아니다 — 같은 프레임의 FindByName이 삭제 예정 노드를 다시 잡는다(b470fc88).
/// </summary>
public sealed class StageRigBuilder
{
    /// <summary>mount 아래에 무대 하나를 세운다. 층은 initialDepths만큼 미리 세운다(없어도 된다 — 참조될 때 선다).</summary>
    public StageRigRefs Build(RectTransform mount, string stageKey, ReadOnlySpan<float> initialDepths)
    {
        string rootName = StageRigSchema.NodeName(stageKey, StageRigSchema.Refs.Stage_Root);

        RectTransform root = FindByName(mount, rootName) as RectTransform;

        if (root == null)
        {
            GameObject go = new(rootName, typeof(RectTransform));
            root = (RectTransform)go.transform;
            root.SetParent(mount, false);
            StretchFull(root);
        }

        StageRigRefs refs = BindRefsFromRoot(root, stageKey);

        for (int i = 0; i < initialDepths.Length; i++)
            EnsureLayer(refs, initialDepths[i]);

        return refs;
    }

    /// <summary>있는 루트에서 참조를 묶는다. 계층이 스키마와 다르면 자식을 전부 부수고 다시 세운다.</summary>
    public StageRigRefs BindRefsFromRoot(RectTransform root, string stageKey)
    {
        Dictionary<StageRigSchema.Refs, RectTransform> map = CollectRefMap(root, stageKey);

        // 빈 루트는 새로 세울 자리다 — 검증할 것이 없다. 무언가 있는데 스키마와 다르면 부수고 다시 세운다.
        if (root.childCount > 0 && !IsValidGraph(root, stageKey, map))
        {
            Debug.LogWarning(
                $"[StageRigBuilder] 무대 계층이 스키마와 다르다. StageRigSchema로 다시 세운다. stage='{stageKey}', root='{root.name}'.",
                root);

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                child.SetParent(null, false);

                if (Application.isPlaying)
                    Object.Destroy(child.gameObject);
                else
                    Object.DestroyImmediate(child.gameObject);
            }

            map = CollectRefMap(root, stageKey);
        }

        EnsureGraph(root, stageKey, map);

        StageRigRefs refs = new(stageKey, root)
        {
            MaskRoot = map[StageRigSchema.Refs.Mask_Root],
            MaskEdgeRoot = map[StageRigSchema.Refs.MaskEdge_Root],
            CameraPan = map[StageRigSchema.Refs.Camera_Pan],
            CameraZoom = map[StageRigSchema.Refs.Camera_Zoom],
        };

        refs.Mask = EnsureMask(refs.MaskRoot, refs.MaskEdgeRoot);

        // 이미 서 있는 층을 거둔다(검증 통과한 계층이거나, 방금 다시 세워 비어 있다).
        RectTransform zoom = refs.CameraZoom;

        for (int i = 0; i < zoom.childCount; i++)
        {
            RectTransform child = (RectTransform)zoom.GetChild(i);

            if (!StageRigSchema.TryParseLayerName(stageKey, child.name, out float depth))
                continue;

            RectTransform content = EnsureRect(child, StageRigSchema.ContentName(stageKey, depth));
            refs.AddLayer(new StageLayerRefs(depth, child, content));
        }

        SortLayers(refs);

        return refs;
    }

    /// <summary>깊이 d인 층을 보장한다. 없으면 세우고, 먼 층이 뒤에 그려지도록 형제 순서를 d 오름차순으로 맞춘다.</summary>
    public StageLayerRefs EnsureLayer(StageRigRefs refs, float depth)
    {
        if (refs.TryGetLayer(depth, out StageLayerRefs existing))
            return existing;

        RectTransform rect = EnsureRect(refs.CameraZoom, StageRigSchema.LayerName(refs.StageKey, depth));
        RectTransform content = EnsureRect(rect, StageRigSchema.ContentName(refs.StageKey, depth));

        StageLayerRefs layer = new(depth, rect, content);
        refs.AddLayer(layer);

        SortLayers(refs);

        return layer;
    }

    private static void SortLayers(StageRigRefs refs)
    {
        IReadOnlyList<StageLayerRefs> layers = refs.Layers;

        for (int i = 0; i < layers.Count; i++)
            layers[i].Rect.SetSiblingIndex(i);
    }

    // ── 검증 ─────────────────────────────────────────────────────────

    private static bool IsValidGraph(RectTransform root, string stageKey, Dictionary<StageRigSchema.Refs, RectTransform> map)
    {
        foreach (StageRigSchema.NodeDef node in StageRigSchema.Nodes)
        {
            if (!map.TryGetValue(node.Id, out RectTransform rect))
                return false;

            Transform expectedParent = node.Parent.HasValue
                ? (map.TryGetValue(node.Parent.Value, out RectTransform parentRect) ? parentRect : null)
                : root.parent;

            if (expectedParent == null || rect.parent != expectedParent)
                return false;

            if (node.Id != StageRigSchema.Refs.Stage_Root && !IsIdentityStretch(rect))
                return false;
        }

        // 자식 집합도 스키마 그대로여야 한다 — 엉뚱한 자리에 선 노드(옮겨진 층, 남은 옛 노드)는 어긋남이다.
        if (!ChildrenAreExactly(root, map[StageRigSchema.Refs.Mask_Root], map[StageRigSchema.Refs.MaskEdge_Root]))
            return false;

        if (!ChildrenAreExactly(map[StageRigSchema.Refs.Mask_Root], map[StageRigSchema.Refs.Camera_Pan]))
            return false;

        if (!ChildrenAreExactly(map[StageRigSchema.Refs.Camera_Pan], map[StageRigSchema.Refs.Camera_Zoom]))
            return false;

        if (map[StageRigSchema.Refs.MaskEdge_Root].childCount != 0)
            return false;

        RectTransform maskRoot = map[StageRigSchema.Refs.Mask_Root];

        if (!maskRoot.TryGetComponent(out StageMaskGraphic _) || !maskRoot.TryGetComponent(out Mask _) || !maskRoot.TryGetComponent(out StageMaskSlot _))
            return false;

        if (!map[StageRigSchema.Refs.MaskEdge_Root].TryGetComponent(out StageMaskEdgeGraphic _))
            return false;

        // 줌 루트의 자식은 전부 층이어야 하고, 층은 Content 하나를 가진다.
        RectTransform zoom = map[StageRigSchema.Refs.Camera_Zoom];

        for (int i = 0; i < zoom.childCount; i++)
        {
            RectTransform child = (RectTransform)zoom.GetChild(i);

            if (!StageRigSchema.TryParseLayerName(stageKey, child.name, out float depth))
                return false;

            if (!IsIdentityStretch(child))
                return false;

            RectTransform content = FindDirectChild(child, StageRigSchema.ContentName(stageKey, depth));

            if (content == null || !IsIdentityStretch(content) || !ChildrenAreExactly(child, content))
                return false;
        }

        return true;
    }

    private static bool ChildrenAreExactly(Transform parent, params Transform[] expected)
    {
        if (parent.childCount != expected.Length)
            return false;

        for (int i = 0; i < expected.Length; i++)
        {
            bool found = false;

            for (int c = 0; c < parent.childCount; c++)
            {
                if (parent.GetChild(c) == expected[i])
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                return false;
        }

        return true;
    }

    private static bool IsIdentityStretch(RectTransform rect)
        => rect.anchorMin == Vector2.zero
           && rect.anchorMax == Vector2.one
           && rect.pivot == new Vector2(0.5f, 0.5f)
           && rect.anchoredPosition == Vector2.zero
           && rect.sizeDelta == Vector2.zero
           && rect.localScale == Vector3.one
           && rect.localEulerAngles == Vector3.zero;

    // ── 생성 ─────────────────────────────────────────────────────────

    private static void EnsureGraph(RectTransform root, string stageKey, Dictionary<StageRigSchema.Refs, RectTransform> map)
    {
        foreach (StageRigSchema.NodeDef node in StageRigSchema.Nodes)
        {
            if (node.Id == StageRigSchema.Refs.Stage_Root)
            {
                map[node.Id] = root;
                continue;
            }

            RectTransform parent = map[node.Parent.Value];
            map[node.Id] = EnsureRect(parent, StageRigSchema.NodeName(stageKey, node.Id));
        }
    }

    // 마스크 컴포넌트는 코드 기본값으로 선다 — 씬 값이 세 무대 모두 기본값과 같았다(2026-11-05 확인).
    private static StageMaskSlot EnsureMask(RectTransform maskRoot, RectTransform edgeRoot)
    {
        if (!maskRoot.TryGetComponent(out StageMaskGraphic graphic))
        {
            graphic = maskRoot.gameObject.AddComponent<StageMaskGraphic>();
            graphic.raycastTarget = false;
        }

        if (!maskRoot.TryGetComponent(out Mask mask))
        {
            mask = maskRoot.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
        }

        if (!edgeRoot.TryGetComponent(out StageMaskEdgeGraphic edge))
        {
            edge = edgeRoot.gameObject.AddComponent<StageMaskEdgeGraphic>();
            edge.raycastTarget = false;
        }

        if (!maskRoot.TryGetComponent(out StageMaskSlot slot))
            slot = maskRoot.gameObject.AddComponent<StageMaskSlot>();

        slot.BindFromBuilder(graphic, mask, edge);

        return slot;
    }

    private static RectTransform EnsureRect(RectTransform parent, string name)
    {
        RectTransform existing = FindDirectChild(parent, name);

        if (existing != null)
            return existing;

        GameObject go = new(name, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        StretchFull(rt);

        return rt;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localEulerAngles = Vector3.zero;
    }

    // ── 조회 ─────────────────────────────────────────────────────────

    private static Dictionary<StageRigSchema.Refs, RectTransform> CollectRefMap(RectTransform root, string stageKey)
    {
        Dictionary<StageRigSchema.Refs, RectTransform> map = new();

        foreach (StageRigSchema.Refs id in Enum.GetValues(typeof(StageRigSchema.Refs)))
        {
            RectTransform t = FindByName(root, StageRigSchema.NodeName(stageKey, id)) as RectTransform;

            if (t != null)
                map[id] = t;
        }

        return map;
    }

    private static RectTransform FindDirectChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name == name)
                return (RectTransform)child;
        }

        return null;
    }

    private static Transform FindByName(Transform root, string name)
    {
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
}
