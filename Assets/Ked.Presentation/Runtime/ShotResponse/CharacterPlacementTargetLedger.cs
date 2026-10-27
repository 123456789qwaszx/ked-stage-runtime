using System.Collections.Generic;
using Ked.Presentation.Core;
using UnityEngine;

// "트윈이 다 끝났다면 어디에 있을 것인가"의 예약 장부 — 코어 PlacementTargetLedger의 어댑터.
//
// 라이브 체인을 값으로 떠서 예약을 입히고
// RectChainMath로 직접 계산.
//
// 이제 하는 일은 유니티 경계뿐이다:
// - RectTransform ↔ 논리 키 대응 (참조 기반. 이름은 읽기 좋으라고 쓴다)
// - 라이브 체인 캡처
// - stopRoot 로컬 ↔ 월드 변환

// stopRoot는 측정 대상 leaf의 상위 RectTransform 중, Core가 변환 체인을 계산할 범위를 끊는 경계
// 실질적으로 stopRoot의 로컬 좌표계 = Core가 말하는 rig space
// stopRoot 위로는 유니티의 UI같이 Core가 알 필요 없는 값.
public sealed class CharacterPlacementTargetLedger
{
    private readonly PlacementTargetLedger _core = new();

    // 참조 → 키. 이름이 같은 rect가 둘 있어도 참조가 다르면 키를 나눈다.
    private readonly Dictionary<RectTransform, string> _keyByRect = new();
    private readonly Dictionary<string, RectTransform> _rectByKey = new();

    private readonly List<RectTransform> _scratchRects = new(48);
    private readonly List<RectNodeState> _scratchChain = new(48);

    // ── 게시 ─────────────────────────────────────────────────────────

    public void PublishAnchoredPosition(RectTransform node, Vector2 targetAnchoredPosition)
    {
        if (node == null)
            return;

        _core.PublishAnchoredPosition(KeyOf(node), targetAnchoredPosition.ToCore());
    }

    public void PublishLocalScale(RectTransform node, Vector2 targetLocalScaleXY)
    {
        if (node == null)
            return;

        _core.PublishLocalScale(KeyOf(node), targetLocalScaleXY.ToCore());
    }

    public void PublishLocalEuler(RectTransform node, Vector3 targetLocalEuler)
    {
        if (node == null)
            return;

        _core.PublishLocalEuler(KeyOf(node), targetLocalEuler.ToCore());
    }

    public void Clear(RectTransform node)
    {
        if (node == null)
            return;

        if (_keyByRect.TryGetValue(node, out string key))
            _core.Clear(key);
    }

    // ── 정착 측정  ─────────────────────────────

    // measureRect 로컬의 특정 점이,
    // 모든 예약 트윈이 끝났다고 가정하면 월드 좌표 상 어디에 위치하는 지 측정.
    public Vector3 MeasureSettledWorldPoint(
        RectTransform measureRect,
        Vector3 localOffset,
        RectTransform stopRoot)
    {
        if (measureRect == null)
            return Vector3.zero;

        if (_core.IsEmpty || stopRoot == null)
            return measureRect.TransformPoint(localOffset);

        // 예약이 있다면 체인 생성.
        RectNodeState[] chain = CaptureSettledChain(measureRect, stopRoot);

        // Core 수학으로 root-space 좌표를 계산
        Vec3 inRootSpace = RectChainMath.TransformPoint(chain, SpaceOf(stopRoot), localOffset.ToCore());

        // Unity 월드로 변환
        return stopRoot.TransformPoint(new Vector3(inRootSpace.X, inRootSpace.Y, inRootSpace.Z));
    }

    // 월드의 이 점은,
    // 모든 예약이 끝났다고 가정하면 parentRect의 로컬 좌표로 얼마인지 측정.
    public Vector2 WorldPointToSettledParentLocalPoint(
        RectTransform parentRect,
        Vector3 worldPoint,
        RectTransform stopRoot)
    {
        if (parentRect == null)
            return Vector2.zero;

        if (_core.IsEmpty || stopRoot == null)
        {
            Vector3 liveLocal = parentRect.InverseTransformPoint(worldPoint);
            return new Vector2(liveLocal.x, liveLocal.y);
        }

        RectNodeState[] chain = CaptureSettledChain(parentRect, stopRoot);

        Vector3 inRootSpace = stopRoot.InverseTransformPoint(worldPoint);

        Vec3 settledLocal = RectChainMath.InverseTransformPoint(
            chain, SpaceOf(stopRoot), inRootSpace.ToCore());

        return new Vector2(settledLocal.X, settledLocal.Y);
    }

    // ── 유니티 경계 ──────────────────────────────────────────────────

    /// <summary>
    /// Unity hierarchy에서 현재 상태를 읽어 Core 체인을 만들고,
    /// 예약된 목표값이 있는 곳에는 그것을 덮어씌움.
    ///
    /// chainRects를 주면 같은 순서의 rect 목록을 채운다 — solver가 특정 노드의
    /// 체인 인덱스를 찾는 용도다(SettledFocusMath의 moveNodeIndex / depthYIndex).
    ///
    /// stopRoot가 조상이 아니거나 체인에 RectTransform이 아닌 노드가 있으면
    /// 조용히 어긋나는 대신 예외다 — 실경로에서는 발생하지 않는 방어선이다.
    /// </summary>
    public RectNodeState[] CaptureSettledChain(
        RectTransform leaf,
        RectTransform stopRoot,
        List<RectTransform> chainRects = null)
    {
        _scratchRects.Clear();
        _scratchChain.Clear();

        Transform current = leaf;

        while (current != null && current != stopRoot)
        {
            if (!(current is RectTransform rect))
            {
                throw new System.InvalidOperationException(
                    $"[CharacterPlacementTargetLedger] '{leaf.name}'에서 '{stopRoot.name}'까지의 체인에 " +
                    $"RectTransform이 아닌 노드 '{current.name}'가 있다. 좌표 규약이 성립하지 않는다.");
            }

            _scratchRects.Add(rect);
            current = current.parent;
        }

        if (current == null)
        {
            throw new System.InvalidOperationException(
                $"[CharacterPlacementTargetLedger] '{stopRoot.name}'가 '{leaf.name}'의 조상이 아니다.");
        }

        chainRects?.Clear();

        // 루트에 가까운 쪽부터 — RectChainMath 규약(chain[0]의 부모가 rootSpace).
        for (int i = _scratchRects.Count - 1; i >= 0; i--)
        {
            RectTransform rect = _scratchRects[i];
            RectNodeState live = CaptureLive(rect);

            // 예약이 있으면 입힌다. 없으면 라이브 그대로 — 아무것도 쓰지 않는다.
            _scratchChain.Add(
                _keyByRect.TryGetValue(rect, out string key)
                    ? _core.ApplyTo(key, live)
                    : live);

            chainRects?.Add(rect);
        }

        return _scratchChain.ToArray();
    }

    private static RectNodeState CaptureLive(RectTransform rect)
    {
        return new RectNodeState(
            anchoredPosition: rect.anchoredPosition.ToCore(),
            anchorMin: rect.anchorMin.ToCore(),
            anchorMax: rect.anchorMax.ToCore(),
            pivot: rect.pivot.ToCore(),
            sizeDelta: rect.sizeDelta.ToCore(),
            localScale: rect.localScale.ToCore(),
            localEulerAngles: rect.localEulerAngles.ToCore());
    }

    /// <summary>
    /// 체인이 딛고 서는 공간. solver가 CaptureSettledChain과 짝으로 쓴다 —
    /// 체인과 공간이 어긋나면 스트레치 노드의 크기가 전부 어긋난다.
    /// </summary>
    public static RectSpace SpaceOf(RectTransform rect)
        => new(rect.rect.size.ToCore(), rect.pivot.ToCore());

    /// <summary>
    /// 참조 → 논리 키. 이름을 키로 쓰되 대응은 참조 기반이라, 이름이 겹쳐도 섞이지 않는다.
    /// </summary>
    private string KeyOf(RectTransform rect)
    {
        if (_keyByRect.TryGetValue(rect, out string existing))
            return existing;

        string key = rect.name;

        if (_rectByKey.TryGetValue(key, out RectTransform other) && other != null && other != rect)
        {
            // 같은 이름의 다른 rect. 종전 구현은 참조를 키로 써서 안 섞였다 —
            // 그 성질을 유지하되, 조용히 넘어가지 않고 알린다.
            key = $"{rect.name}#{rect.GetInstanceID()}";

            Debug.LogWarning(
                $"[CharacterPlacementTargetLedger] 이름이 겹치는 노드가 있다: '{rect.name}'. " +
                $"참조로 구분해 '{key}'로 다룬다.", rect);
        }

        _keyByRect[rect] = key;
        _rectByKey[key] = rect;

        return key;
    }
}