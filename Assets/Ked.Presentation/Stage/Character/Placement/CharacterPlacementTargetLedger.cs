using System.Collections.Generic;
using Ked.Presentation.Core;
using UnityEngine;

// "트윈이 다 끝났다면 어디에 있을 것인가"의 예약 장부 — 코어 PlacementTargetLedger의 어댑터.
//
// 라이브 체인을 값으로 떠서 예약을 입히고
// RectChainMath로 직접 계산.
//
// 이제 하는 일은 유니티 경계뿐이다:
// - RectTransform → 논리 키 대응 (참조 기반. 이름은 읽기 좋으라고 쓴다)
// - 라이브 체인 캡처 (+ 샷이 움직이는 무대 노드는 중립값으로 치환)
// - stopRoot 로컬 ↔ 월드 변환
//
// 게시는 NodeTarget으로만 받는다 — 클레임-트윈 커맨드는 Reserve로(확정 때 저절로 풀린다),
// 자기 수명을 직접 도는 MotionCommandCharR은 Publish · Clear로.

// stopRoot는 측정 대상 leaf의 상위 RectTransform 중, Core가 변환 체인을 계산할 범위를 끊는 경계
// 실질적으로 stopRoot의 로컬 좌표계 = Core가 말하는 rig space
// stopRoot 위로는 유니티의 UI같이 Core가 알 필요 없는 값.
public sealed class CharacterPlacementTargetLedger : ICommitRelease
{
    private readonly PlacementTargetLedger _core = new();

    // 참조 → 키. 장부는 리그마다 하나이고 리그 노드 이름은 스키마가 정하므로 키는 리그 안에서 유일하다.
    private readonly Dictionary<RectTransform, string> _keyByRect = new();

    private readonly List<RectTransform> _scratchRects = new(48);
    private readonly List<RectNodeState> _scratchChain = new(48);

    // ── 게시 ─────────────────────────────────────────────────────────

    /// <summary>
    /// 목표 계산 출력을 그대로 예약으로 받는다. 키는 타깃의 NodeKey가 아니라 참조에서 정한다 —
    /// 호스트에서 노드의 정체는 참조이고 NodeKey는 진단용 이름이다(target-boundary.md).
    /// 장부 대상이 아닌 종류(SizeDelta · CanvasAlpha)는 코어가 거부한다.
    /// </summary>
    public void Publish(RectTransform node, in NodeTarget target)
    {
        _core.Publish(target.WithNodeKey(KeyOf(node)));
    }

    public void Clear(RectTransform node)
    {
        if (_keyByRect.TryGetValue(node, out string key))
            _core.Clear(key);
    }

    /// <summary>
    /// 클레임-트윈 커맨드의 게시. 예약을 걸고, 확정할 때 풀리도록 releases에 등록한다.
    /// 커맨드는 이것 하나만 부른다 — 게시와 해제를 따로 챙길 일이 없다.
    /// 값을 돌려주지 않는다 — 장부는 기록만 하고, 트윈 종점은 커맨드가 타깃에서 직접 읽는다.
    /// </summary>
    public void Reserve(RectTransform node, in NodeTarget target, CommitReleases releases)
    {
        Publish(node, target);
        releases.Add(this, node);
    }

    void ICommitRelease.Release(Object target) => Clear((RectTransform)target);

    // ── 정착 측정  ─────────────────────────────

    // measureRect 로컬의 특정 점이,
    // 모든 예약 트윈이 끝났다고 가정하면 월드 좌표 상 어디에 위치하는 지 측정.
    // stageView를 주면 샷이 움직이는 무대 노드는 중립값으로 잰다 — 결과는 카메라 중립의 무대 좌표다.
    public Vector3 MeasureSettledWorldPoint(
        RectTransform measureRect,
        Vector3 localOffset,
        RectTransform stopRoot,
        IStageViewNeutral stageView = null)
    {
        if (_core.IsEmpty && stageView == null)
            return measureRect.TransformPoint(localOffset);

        RectNodeState[] chain = CaptureSettledChain(measureRect, stopRoot, null, stageView);

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
        if (_core.IsEmpty)
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
    /// 예약된 타깃이 있는 곳에는 그것을 덮어씌움.
    ///
    /// chainRects를 주면 같은 순서의 rect 목록을 채운다 — solver가 특정 노드의
    /// 체인 인덱스를 찾는 용도다(SettledFocusMath의 moveNodeIndex / depthYIndex).
    ///
    /// stageView를 주면 예약이 없는 노드 중 샷이 움직이는 무대 노드(카메라 루트 · 깊이 층 프레이밍)는
    /// 라이브 대신 중립값을 쓴다. 값의 우선순위: 리그 예약 끝값 → 무대 중립값 → 라이브.
    /// 배치는 무대 좌표라서다 — 라이브 카메라를 읽으면 결과가 샷의 진행도에 따라 갈린다.
    ///
    /// stopRoot가 조상이 아니거나 체인에 RectTransform이 아닌 노드가 있으면
    /// 조용히 어긋나는 대신 예외다 — 실경로에서는 발생하지 않는 방어선이다.
    /// </summary>
    public RectNodeState[] CaptureSettledChain(
        RectTransform leaf,
        RectTransform stopRoot,
        List<RectTransform> chainRects = null,
        IStageViewNeutral stageView = null)
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

            // 소유를 차례로 묻는다: 장부(리그 노드 — 트윈 끝값) → 샷(무대 노드 — 중립값) → 아무도 아니면 라이브.
            // 한 노드를 둘이 소유하는 상태는 없다 — 장부는 리그 노드만, 샷은 무대 노드만 안다. 겹치면 구조가 깨진 것이라 예외.
            Vector2 neutralPosition = default;
            Vector3 neutralScale = default;

            bool inLedger = _keyByRect.TryGetValue(rect, out string key);
            bool inShot = stageView != null && stageView.TryGetNeutral(rect, out neutralPosition, out neutralScale);

            if (inLedger && inShot)
            {
                throw new System.InvalidOperationException(
                    $"[CharacterPlacementTargetLedger] '{rect.name}'을 장부와 샷이 둘 다 소유한다. 리그 노드와 무대 노드가 겹쳤다.");
            }

            if (inLedger)
                _scratchChain.Add(_core.ApplyTo(key, live));
            else if (inShot)
                _scratchChain.Add(WithView(live, neutralPosition, neutralScale));
            else
                _scratchChain.Add(live);

            chainRects?.Add(rect);
        }

        return _scratchChain.ToArray();
    }

    private static RectNodeState WithView(in RectNodeState live, Vector2 anchoredPosition, Vector3 localScale)
    {
        return new RectNodeState(
            anchoredPosition: anchoredPosition.ToCore(),
            anchorMin: live.AnchorMin,
            anchorMax: live.AnchorMax,
            pivot: live.Pivot,
            sizeDelta: live.SizeDelta,
            localScale: localScale.ToCore(),
            localEulerAngles: live.LocalEulerAngles);
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
    /// 참조 → 논리 키. 이름이 키다. 같은 이름의 다른 참조가 들어오면 스키마의 이름 계약이 깨진 것이라 예외다 —
    /// 종전엔 접미사를 붙여 넘겼는데, 도달 불가 상태를 방어하면 깨졌을 때 조용히 지나간다.
    /// </summary>
    private string KeyOf(RectTransform rect)
    {
        if (_keyByRect.TryGetValue(rect, out string existing))
            return existing;

        string key = rect.name;

        foreach (KeyValuePair<RectTransform, string> pair in _keyByRect)
        {
            if (pair.Value == key && pair.Key != rect)
            {
                throw new System.InvalidOperationException(
                    $"[CharacterPlacementTargetLedger] 한 리그 안에 이름이 겹치는 노드가 있다: '{key}'. " +
                    "리그 노드 이름은 스키마가 유일하게 정한다.");
            }
        }

        _keyByRect[rect] = key;

        return key;
    }
}