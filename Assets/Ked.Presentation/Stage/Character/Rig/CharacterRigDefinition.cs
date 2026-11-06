using System;
using UnityEngine;
using UnityEngine.UI;

// ─────────────────────────────────────────────────────────────────────
// 리그 계층의 유일한 소유자. 프리팹이 아니라 이 배열이 진실이다 —
// CharacterRigBuilder가 프리팹을 검사해서 여기와 다르면 부수고 다시 세운다.
//
// 아래 Nodes의 **순서는 취향이 아니라 규약이다.** 한 노드 = 한 축이고,
// 자리가 그 노드의 뜻을 정한다:
//   · 동시에 돌 수 있는 축은 반드시 다른 노드다 — 같은 프로퍼티를 두 트윈이
//     잡으면 서로를 덮는다 (Track_X / Track_Y가 따로인 이유).
//   · 스케일이 셋, 회전이 둘인 것은 중복이 아니다. 무엇에 곱해져야 하는가가
//     달라서 자리가 다르다 — DepthScale은 이동량까지 줄여야 원근이 되고
//     (최상단), ActingScale은 이동량을 건드리면 안 된다(최하단).
//
// 순서를 바꾸거나 노드를 합치면 컴파일도 되고 예외도 안 나지만 연출이
// 조용히 망가진다. 고치기 전에 반드시 읽을 것:
//   Documentation~/rig-axis-order.md
// ─────────────────────────────────────────────────────────────────────
public static class CharacterRigSchema
{
    public enum Refs
    {
        // Slot axis - stage placement
        CharSlot_Track_Focus,
        CharSlot_DepthY,
        CharSlot_DepthScale,
        CharSlot_Track_Idle,
        CharSlot_Track,
        CharSlot_Track_X,
        CharSlot_Track_Y,
        CharSlot_Rotation,
        CharSlot_SwayPivot,
        CharSlot_Scale,

        // Character casting axis - per-character defaults
        CharacterPortrait_VisualOffset,

        // Portrait acting axis
        CharacterPortrait_Track,
        CharacterPortrait_Rotation,
        CharacterPortrait_Track_Move,
        CharacterPortrait_Track_Move_X,
        CharacterPortrait_Track_Move_Y,
        CharacterPortrait_SwayPivot,
        CharacterPortrait_Shake,
        CharacterPortrait_ActingScale,
        CharacterPortrait_ActingScale_X,
        CharacterPortrait_ActingScale_Y,

        // Portrait sprite
        CharacterPortraitSprite_Root,
        CharacterPortraitSprite_Image,

        // Portrait sprite overlay
        CharacterPortraitSpriteOverlay_Root,
        CharacterPortraitSpriteOverlay_Image,
    }

    public sealed class NodeDef
    {
        public Refs  Id;
        public Refs? Parent;

        public bool  NeedsImage;
        public bool  NeedsCanvasGroup;
        public bool  NeedsBottomPivot;

        public float InitialCanvasGroupAlpha = 1f;
    }

    /// <summary>스키마 노드 id 전부(순서 그대로). 모션 로더가 채널 노드를 이걸로 검증한다.</summary>
    public static string[] NodeIds() => Array.ConvertAll(Nodes, n => n.Id.ToString());

    public static readonly NodeDef[] Nodes =
    {
        // Slot axis - stage placement
        new() { Id = Refs.CharSlot_Track_Focus, Parent = null },
        new() { Id = Refs.CharSlot_DepthY, Parent = Refs.CharSlot_Track_Focus },
        new() { Id = Refs.CharSlot_DepthScale, Parent = Refs.CharSlot_DepthY, NeedsBottomPivot = true },
        new() { Id = Refs.CharSlot_Track_Idle, Parent = Refs.CharSlot_DepthScale },
        new() { Id = Refs.CharSlot_Track, Parent = Refs.CharSlot_Track_Idle },
        new() { Id = Refs.CharSlot_Track_X, Parent = Refs.CharSlot_Track },
        new() { Id = Refs.CharSlot_Track_Y, Parent = Refs.CharSlot_Track_X },
        new() { Id = Refs.CharSlot_Rotation, Parent = Refs.CharSlot_Track_Y },
        new() { Id = Refs.CharSlot_SwayPivot, Parent = Refs.CharSlot_Rotation, NeedsBottomPivot = true },
        new() { Id = Refs.CharSlot_Scale, Parent = Refs.CharSlot_SwayPivot, NeedsBottomPivot = true },

        // Character casting axis - per-character defaults
        new() { Id = Refs.CharacterPortrait_VisualOffset, Parent = Refs.CharSlot_Scale, NeedsBottomPivot = true},

        // Portrait acting axis
        new() { Id = Refs.CharacterPortrait_Track, Parent = Refs.CharacterPortrait_VisualOffset },
        new() { Id = Refs.CharacterPortrait_Rotation, Parent = Refs.CharacterPortrait_Track },
        new() { Id = Refs.CharacterPortrait_Track_Move, Parent = Refs.CharacterPortrait_Rotation },
        new() { Id = Refs.CharacterPortrait_Track_Move_X, Parent = Refs.CharacterPortrait_Track_Move },
        new() { Id = Refs.CharacterPortrait_Track_Move_Y, Parent = Refs.CharacterPortrait_Track_Move_X },
        new() { Id = Refs.CharacterPortrait_SwayPivot, Parent = Refs.CharacterPortrait_Track_Move_Y, NeedsBottomPivot = true },
        new() { Id = Refs.CharacterPortrait_Shake, Parent = Refs.CharacterPortrait_SwayPivot },
        new() { Id = Refs.CharacterPortrait_ActingScale, Parent = Refs.CharacterPortrait_Shake },
        new() { Id = Refs.CharacterPortrait_ActingScale_X, Parent = Refs.CharacterPortrait_ActingScale },
        new() { Id = Refs.CharacterPortrait_ActingScale_Y, Parent = Refs.CharacterPortrait_ActingScale_X },

        // Portrait sprite
        new() { Id = Refs.CharacterPortraitSprite_Root, Parent = Refs.CharacterPortrait_ActingScale_Y, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.CharacterPortraitSprite_Image, Parent = Refs.CharacterPortraitSprite_Root, NeedsImage = true },

        // Portrait sprite overlay
        new() { Id = Refs.CharacterPortraitSpriteOverlay_Root, Parent = Refs.CharacterPortrait_ActingScale_Y, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.CharacterPortraitSpriteOverlay_Image, Parent = Refs.CharacterPortraitSpriteOverlay_Root, NeedsImage = true },
    };
}

public enum CharacterRigNode
{
    RigRoot,
    
    // Slot axis - stage placement
    CharSlot_Track_Focus,
    CharSlot_DepthY,
    CharSlot_DepthScale,
    CharSlot_Track_Idle,
    CharSlot_Track,
    CharSlot_Track_X,
    CharSlot_Track_Y,
    CharSlot_Rotation,
    CharSlot_SwayPivot,
    CharSlot_Scale,

    // Character casting axis - per-character defaults
    CharacterPortrait_VisualOffset,

    // Portrait acting axis
    CharacterPortrait_Track,
    CharacterPortrait_Rotation,
    CharacterPortrait_Track_Move,
    CharacterPortrait_Track_Move_X,
    CharacterPortrait_Track_Move_Y,
    CharacterPortrait_SwayPivot,
    CharacterPortrait_Shake,
    CharacterPortrait_ActingScale,
    CharacterPortrait_ActingScale_X,
    CharacterPortrait_ActingScale_Y,

    // Portrait sprite
    CharacterPortraitSprite_Root,
    CharacterPortraitSprite_Image,

    // Portrait sprite overlay
    CharacterPortraitSpriteOverlay_Root,
    CharacterPortraitSpriteOverlay_Image,
}

public sealed class CharacterRigRefs
{
    public RectTransform RigRoot { get; private set; }
    public CharacterRigRefs(RectTransform rigRoot) => RigRoot = rigRoot;

    // Visual effect: CharacterPortraitSprite_Image에 바인딩된 runtime material 소유자.
    // SetupCharRigCommand가 생성, CharacterRigRegistry.DestroyRig가 Dispose.
    public RigVisualEffectController VisualEffect;

    public CharacterPlacementTargetLedger PlacementTargets { get; } = new();

    // 이 리그가 선 무대와 층. 리그 좌표 계산의 경계(RigSpaceRoot)는 Stage.Root이고, 리그 루트의 부모는 Layer.Content다.
    // slot(SetupCharRig)이 한 번 정한다. 리그는 무대 · 층을 옮기지 않는다 — 다른 자리에 서려면 fade_out 뒤 새 slot
    // (char_to는 2026-11-06에 걷었다. 무대마다 카메라가 다를 수 있어서 옮기는 순간 화면에서 튄다).
    public StageRigRefs Stage { get; private set; }
    public StageLayerRefs Layer { get; private set; }

    public void StandOn(StageRigRefs stage, StageLayerRefs layer)
    {
        Stage = stage;
        Layer = layer;
    }

    // 정지 프레임의 깊이 스케일. SetDepthCommandCharR가 끝값을 확정할 때 기록한다.
    // 트윈이 도는 중에도 "끝나면 어디인가"를 들고 있어야 착란원 계산이 흔들리지 않는다.
    // 열거형(CharacterDepthKey)이 아니라 스케일 값인 이유: useLevel=true 경로는 프리셋 키가 없고,
    // CoC 공식이 필요로 하는 것도 스케일이다. Far 0.68 ~ Close 1.38로 단조증가한다.
    public float SettledDepthScale = 1f;

    // Slot axis - stage placement
    public RectTransform CharSlot_Track_Focus;
    public RectTransform CharSlot_DepthY;
    public RectTransform CharSlot_DepthScale;
    public RectTransform CharSlot_Track_Idle;
    public RectTransform CharSlot_Track;
    public RectTransform CharSlot_Track_X;
    public RectTransform CharSlot_Track_Y;
    public RectTransform CharSlot_Rotation;
    public RectTransform CharSlot_SwayPivot;
    public RectTransform CharSlot_Scale;

    // Character casting axis - per-character defaults
    public RectTransform CharacterPortrait_VisualOffset;

    // Portrait acting axis
    public RectTransform CharacterPortrait_Track;
    public RectTransform CharacterPortrait_Rotation;
    public RectTransform CharacterPortrait_Track_Move;
    public RectTransform CharacterPortrait_Track_Move_X;
    public RectTransform CharacterPortrait_Track_Move_Y;
    public RectTransform CharacterPortrait_SwayPivot;
    public RectTransform CharacterPortrait_Shake;
    public RectTransform CharacterPortrait_ActingScale;
    public RectTransform CharacterPortrait_ActingScale_X;
    public RectTransform CharacterPortrait_ActingScale_Y;

    // Portrait sprite
    public RectTransform CharacterPortraitSprite_Root;
    public Image         CharacterPortraitSprite_Image;

    // Portrait sprite overlay — face_swap이 페이드로 겹치는 위 층.
    // 정지 프레임에서는 alpha 0에 Image가 비활성이라 코어 폴드에 남지 않는다.
    public RectTransform CharacterPortraitSpriteOverlay_Root;
    public Image         CharacterPortraitSpriteOverlay_Image;
}

public static class CharacterRigRefsExtensions
{
    public static RectTransform GetRect(this CharacterRigRefs refs, CharacterRigNode node)
    {
        Component component = refs?.GetComponent(node);
        return component != null ? component.transform as RectTransform : null;
    }

    public static Image GetImage(this CharacterRigRefs refs, CharacterRigNode node)
    {
        return refs?.GetComponent(node) as Image;
    }

    private static Component GetComponent(this CharacterRigRefs refs, CharacterRigNode node)
    {
        if (refs == null)
            return null;

        return node switch
        {
            CharacterRigNode.RigRoot => refs.RigRoot,
            
            // Slot axis - stage placement
            CharacterRigNode.CharSlot_Track_Focus => refs.CharSlot_Track_Focus,
            CharacterRigNode.CharSlot_DepthY => refs.CharSlot_DepthY,
            CharacterRigNode.CharSlot_DepthScale => refs.CharSlot_DepthScale,
            CharacterRigNode.CharSlot_Track_Idle => refs.CharSlot_Track_Idle,
            CharacterRigNode.CharSlot_Track => refs.CharSlot_Track,
            CharacterRigNode.CharSlot_Track_X => refs.CharSlot_Track_X,
            CharacterRigNode.CharSlot_Track_Y => refs.CharSlot_Track_Y,
            CharacterRigNode.CharSlot_Rotation => refs.CharSlot_Rotation,
            CharacterRigNode.CharSlot_SwayPivot => refs.CharSlot_SwayPivot,
            CharacterRigNode.CharSlot_Scale => refs.CharSlot_Scale,

            // Character casting axis - per-character defaults
            CharacterRigNode.CharacterPortrait_VisualOffset => refs.CharacterPortrait_VisualOffset,

            // Portrait acting axis
            CharacterRigNode.CharacterPortrait_Track => refs.CharacterPortrait_Track,
            CharacterRigNode.CharacterPortrait_Rotation => refs.CharacterPortrait_Rotation,
            CharacterRigNode.CharacterPortrait_Track_Move => refs.CharacterPortrait_Track_Move,
            CharacterRigNode.CharacterPortrait_Track_Move_X => refs.CharacterPortrait_Track_Move_X,
            CharacterRigNode.CharacterPortrait_Track_Move_Y => refs.CharacterPortrait_Track_Move_Y,
            CharacterRigNode.CharacterPortrait_SwayPivot => refs.CharacterPortrait_SwayPivot,
            CharacterRigNode.CharacterPortrait_Shake => refs.CharacterPortrait_Shake,
            CharacterRigNode.CharacterPortrait_ActingScale => refs.CharacterPortrait_ActingScale,
            CharacterRigNode.CharacterPortrait_ActingScale_X => refs.CharacterPortrait_ActingScale_X,
            CharacterRigNode.CharacterPortrait_ActingScale_Y => refs.CharacterPortrait_ActingScale_Y,

            // Portrait sprite
            CharacterRigNode.CharacterPortraitSprite_Root => refs.CharacterPortraitSprite_Root,
            CharacterRigNode.CharacterPortraitSprite_Image => refs.CharacterPortraitSprite_Image,

            // Portrait sprite overlay
            CharacterRigNode.CharacterPortraitSpriteOverlay_Root => refs.CharacterPortraitSpriteOverlay_Root,
            CharacterRigNode.CharacterPortraitSpriteOverlay_Image => refs.CharacterPortraitSpriteOverlay_Image,

            _ => null
        };
    }
}