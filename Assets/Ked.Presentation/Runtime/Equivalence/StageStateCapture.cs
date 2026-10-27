using System;
using System.Collections.Generic;
using Ked.Presentation.Core;
using UnityEngine;

/// <summary>
/// 등가성 하네스의 캡처 절반 — 살아 있는 무대에서 StageState를 읽어낸다.
///
/// 새로 발명한 것이 없다: 노드 상태 캡처는 Ledger 어댑터의 라이브 캡처와 같은 필드 읽기다.
///
/// **노드 열거는 이름 파싱이 아니라 타입 접근(GetRect)이다.** 실제 오브젝트 이름에는
/// role prefix가 붙지만 GetRect는 필드로 가져오므로, 이름과 무관하게 스키마 id로 키가 잡힌다.
/// 키 규약은 리듀서와 동일: "{slotKey}/{스키마 id}", 리그 루트는 "{slotKey}/__root".
///
/// 아무것도 쓰지 않는다 — 읽기 전용이다.
/// </summary>
public static class StageStateCapture
{
    public static StageState Capture(
        CharacterRigRegistry characterRigs,
        PresentationShotResponseSystem shotSystem,
        Vec2 baseResolution)
    {
        if (characterRigs == null)
            throw new ArgumentNullException(nameof(characterRigs));

        StageState state = new(new RectSpace(baseResolution, Vec2.Half));

        List<KeyValuePair<string, CharacterRigRefs>> rigs = new();
        characterRigs.CollectAliveRigEntries(rigs);

        foreach (KeyValuePair<string, CharacterRigRefs> pair in rigs)
            CaptureRig(state, pair.Key, pair.Value);

        // 샷 축은 "저작된 의도"를 그대로 읽는다 — 카메라 트랜스폼이 아니라.
        if (shotSystem != null)
            state.Shot = shotSystem.CurrentState.ToCore();

        return state;
    }

    private static void CaptureRig(StageState state, string slotKey, CharacterRigRefs refs)
    {
        state.RegisterSlot(slotKey);

        string rootKey = StageState.NodeKeyOf(slotKey, RigSchemaLoader.RootKey);
        state.Nodes.Add(rootKey, null, CaptureNode(refs.RigRoot));
        CaptureAlpha(state, rootKey, refs.RigRoot);

        // 스키마 선언 순서(부모 먼저)를 그대로 탄다 — 트리 Add의 부모 선행 조건과 맞는다.
        foreach (CharacterRigSchema.NodeDef def in CharacterRigSchema.Nodes)
        {
            if (!TryToTarget(def.Id, out CharacterRigTarget target))
                continue;

            RectTransform rect = refs.GetRect(target);

            if (rect == null)
                continue; // 없는 노드는 캡처가 안 된다 — 비교기가 "접힘 전용"으로 센다

            string key = StageState.NodeKeyOf(slotKey, def.Id.ToString());

            string parentKey = def.Parent.HasValue
                ? StageState.NodeKeyOf(slotKey, def.Parent.Value.ToString())
                : rootKey;

            if (!state.Nodes.Contains(parentKey))
                continue; // 부모가 캡처 안 됐으면 자식도 잇지 않는다 — 비교기가 잡는다

            state.Nodes.Add(key, parentKey, CaptureNode(rect));
            CaptureAlpha(state, key, rect);
        }
    }

    private static RectNodeState CaptureNode(RectTransform rect)
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

    private static void CaptureAlpha(StageState state, string key, RectTransform rect)
    {
        // CanvasGroup이 없는 노드는 기록하지 않는다 — 상태의 기본값(1)이 곧 답이다.
        if (rect.TryGetComponent(out CanvasGroup group))
            state.SetAlpha(key, group.alpha);
    }

    private static bool TryToTarget(CharacterRigSchema.Refs id, out CharacterRigTarget target)
    {
        // 두 enum은 이름이 1:1이다 (CharacterRigTarget이 RigRoot 하나를 더 가질 뿐).
        return Enum.TryParse(id.ToString(), out target);
    }
}