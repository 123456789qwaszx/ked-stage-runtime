using System;
using System.Collections.Generic;
using Ked.Presentation.Core;
using UnityEngine;

/// <summary>한 깊이 층. Rect에 샷 반응(위치 · 배율)을 쓰고, Content에 리그를 붙인다.</summary>
public sealed class StageLayerRefs
{
    public readonly float Depth;
    public readonly RectTransform Rect;
    public readonly RectTransform Content;

    public StageLayerRefs(float depth, RectTransform rect, RectTransform content)
    {
        Depth = depth;
        Rect = rect;
        Content = content;
    }
}

/// <summary>
/// 무대 하나의 노드 참조. 빌더가 만들고 보장한다 — null 검사하지 않는다.
/// 샷이 쓰는 노드(카메라 둘 · 층 전부)의 소유 목록이 곧 이 객체다: IsShotOwned.
/// </summary>
public sealed class StageRigRefs
{
    public string StageKey { get; }

    /// <summary>본 무대만 샷을 따른다. 덮는 무대의 카메라 · 층은 빌더가 세운 항등 그대로다.</summary>
    public StageRole Role => StageRoles.Of(StageKey);

    public RectTransform Root { get; }
    public RectTransform MaskRoot { get; internal set; }
    public RectTransform MaskEdgeRoot { get; internal set; }
    public RectTransform CameraPan { get; internal set; }
    public RectTransform CameraZoom { get; internal set; }
    public StageMaskSlot Mask { get; internal set; }

    // d 오름차순 = 멀리서 가까이로 = 형제 순서(먼 층이 뒤에 그려진다).
    private readonly SortedDictionary<float, StageLayerRefs> _layers = new();

    // 층 깊이 배열 — 샷 시스템이 매 프레임 Solve에 넘긴다. 층이 늘 때만 다시 만든다.
    private float[] _depths = Array.Empty<float>();
    private StageLayerRefs[] _layerArray = Array.Empty<StageLayerRefs>();

    public StageRigRefs(string stageKey, RectTransform root)
    {
        StageKey = stageKey;
        Root = root;
    }

    public IReadOnlyList<StageLayerRefs> Layers => _layerArray;

    /// <summary>Layers와 같은 순서의 d.</summary>
    public ReadOnlySpan<float> LayerDepths => _depths;

    public bool TryGetLayer(float depth, out StageLayerRefs layer) => _layers.TryGetValue(depth, out layer);

    public StageLayerRefs GetLayer(float depth)
        => _layers.TryGetValue(depth, out StageLayerRefs layer)
            ? layer
            : throw new InvalidOperationException($"[StageRigRefs] '{StageKey}'에 깊이 {depth}인 층이 없다. StageRigBuilder.EnsureLayer로 먼저 세운다.");

    internal void AddLayer(StageLayerRefs layer)
    {
        _layers[layer.Depth] = layer;

        _depths = new float[_layers.Count];
        _layerArray = new StageLayerRefs[_layers.Count];

        int i = 0;
        foreach (KeyValuePair<float, StageLayerRefs> pair in _layers)
        {
            _depths[i] = pair.Key;
            _layerArray[i] = pair.Value;
            i++;
        }
    }

    /// <summary>샷이 쓰는 노드인가 — 카메라 둘 또는 어느 층의 Rect. 중립값 조회(IStageViewNeutral)의 근거다.</summary>
    public bool IsShotOwned(RectTransform rect, out bool isCameraPan, out bool isCameraZoom, out StageLayerRefs layer)
    {
        isCameraPan = rect == CameraPan;
        isCameraZoom = rect == CameraZoom;
        layer = null;

        if (isCameraPan || isCameraZoom)
            return true;

        for (int i = 0; i < _layerArray.Length; i++)
        {
            if (_layerArray[i].Rect == rect)
            {
                layer = _layerArray[i];
                return true;
            }
        }

        return false;
    }
}
