using UnityEngine;
using UnityEngine.UI;

public static class BackgroundRigSchema
{
    public enum Refs
    {
        // Background base axis - response-neutral placement / measurement
        Background_Root,

        // Background casting axis - per-background defaults
        Background_Anchor,

        // Background acting axis
        Background_Track_Move,
        Background_Track_X,
        Background_Track_Y,
        Background_Original_Rotation,
        Background_Rotation,
        Background_Shake,

        Background_Size,
        Background_Scale,
        Background_DepthScale,

        Background_Mask,

        Background_ActingScale,
        Background_ActingScale_X,
        Background_ActingScale_Y,

        // Background sprite
        BackgroundSprite_Root,
        BackgroundSprite_Image,

        // Object slots
        Background_ObjectSlotRoot,
    }

    public sealed class NodeDef
    {
        public Refs Id;
        public Refs? Parent;

        public bool NeedsCanvasGroup;
        public bool NeedsImage;
        public bool NeedsMask;

        public float InitialCanvasGroupAlpha = 1f;
    }

    public static readonly NodeDef[] Nodes =
    {
        // Background base axis - response-neutral placement / measurement
        new() { Id = Refs.Background_Root, Parent = null, NeedsCanvasGroup = true },

        // Background casting axis - per-background defaults
        new() { Id = Refs.Background_Anchor, Parent = Refs.Background_Root },

        // Background acting axis
        new() { Id = Refs.Background_Track_Move, Parent = Refs.Background_Anchor },
        new() { Id = Refs.Background_Track_X, Parent = Refs.Background_Track_Move },
        new() { Id = Refs.Background_Track_Y, Parent = Refs.Background_Track_X },
        new() { Id = Refs.Background_Original_Rotation, Parent = Refs.Background_Track_Y },
        new() { Id = Refs.Background_Rotation, Parent = Refs.Background_Original_Rotation },
        new() { Id = Refs.Background_Shake, Parent = Refs.Background_Rotation },

        new() { Id = Refs.Background_Size, Parent = Refs.Background_Shake },
        new() { Id = Refs.Background_Scale, Parent = Refs.Background_Size },
        new() { Id = Refs.Background_DepthScale, Parent = Refs.Background_Scale },

        // Mask boundary
        new() { Id = Refs.Background_Mask, Parent = Refs.Background_DepthScale, NeedsImage = true, NeedsMask = true },

        // Acting scale inside mask
        new() { Id = Refs.Background_ActingScale, Parent = Refs.Background_Mask },
        new() { Id = Refs.Background_ActingScale_X, Parent = Refs.Background_ActingScale },
        new() { Id = Refs.Background_ActingScale_Y, Parent = Refs.Background_ActingScale_X },

        // Background sprite
        new() { Id = Refs.BackgroundSprite_Root, Parent = Refs.Background_ActingScale_Y, NeedsCanvasGroup = true },
        new() { Id = Refs.BackgroundSprite_Image, Parent = Refs.BackgroundSprite_Root, NeedsImage = true },

        // Object slots
        new() { Id = Refs.Background_ObjectSlotRoot, Parent = Refs.Background_ActingScale, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
    };
}

public enum BackgroundRigTarget
{
    // Background base axis - response-neutral placement / measurement
    Background_Root,

    // Background casting axis - per-background defaults
    Background_Anchor,

    // Background acting axis
    Background_Track_Move,
    Background_Track_X,
    Background_Track_Y,
    Background_Original_Rotation,
    Background_Rotation,
    Background_Shake,

    Background_Size,
    Background_Scale,
    Background_DepthScale,

    Background_Mask,

    Background_ActingScale,
    Background_ActingScale_X,
    Background_ActingScale_Y,

    // Background sprite
    BackgroundSprite_Root,
    BackgroundSprite_Image,

    // Object slots
    Background_ObjectSlotRoot,
}

public sealed class BackgroundRigRefs
{
    public RectTransform RigRoot { get; private set; }

    public BackgroundRigRefs(RectTransform rigRoot)
    {
        RigRoot = rigRoot;
    }

    // Background base axis - response-neutral placement / measurement
    public RectTransform Background_Root;

    // Background casting axis - per-background defaults
    public RectTransform Background_Anchor;

    // Background acting axis
    public RectTransform Background_Track_Move;
    public RectTransform Background_Track_X;
    public RectTransform Background_Track_Y;
    public RectTransform Background_Original_Rotation;
    public RectTransform Background_Rotation;
    public RectTransform Background_Shake;

    public RectTransform Background_Size;
    public RectTransform Background_Scale;
    public RectTransform Background_DepthScale;

    public RectTransform Background_Mask;

    public RectTransform Background_ActingScale;
    public RectTransform Background_ActingScale_X;
    public RectTransform Background_ActingScale_Y;

    // Background sprite
    public RectTransform BackgroundSprite_Root;
    public Image BackgroundSprite_Image;

    // Object slots
    public RectTransform Background_ObjectSlotRoot;

    // 런타임 머티리얼 소유자. SetupBackgroundRigCommand가 만들고
    // BackgroundRigRegistry.DestroyRig가 Dispose한다.
    public RigVisualEffectController VisualEffect;
}

public static class BackgroundRigRefsExtensions
{
    public static RectTransform GetRect(this BackgroundRigRefs refs, BackgroundRigTarget target)
    {
        Component c = refs.GetComponent(target);

        if (c == null)
            return null;

        return c.transform as RectTransform;
    }

    private static Component GetComponent(this BackgroundRigRefs refs, BackgroundRigTarget target)
    {
        if (refs == null)
            return null;

        return target switch
        {
            // Background base axis - response-neutral placement / measurement
            BackgroundRigTarget.Background_Root => refs.Background_Root,

            // Background casting axis - per-background defaults
            BackgroundRigTarget.Background_Anchor => refs.Background_Anchor,

            // Background acting axis
            BackgroundRigTarget.Background_Track_Move => refs.Background_Track_Move,
            BackgroundRigTarget.Background_Track_X => refs.Background_Track_X,
            BackgroundRigTarget.Background_Track_Y => refs.Background_Track_Y,
            BackgroundRigTarget.Background_Original_Rotation => refs.Background_Original_Rotation,
            BackgroundRigTarget.Background_Rotation => refs.Background_Rotation,
            BackgroundRigTarget.Background_Shake => refs.Background_Shake,

            BackgroundRigTarget.Background_Size => refs.Background_Size,
            BackgroundRigTarget.Background_Scale => refs.Background_Scale,
            BackgroundRigTarget.Background_DepthScale => refs.Background_DepthScale,

            BackgroundRigTarget.Background_Mask => refs.Background_Mask,

            BackgroundRigTarget.Background_ActingScale => refs.Background_ActingScale,
            BackgroundRigTarget.Background_ActingScale_X => refs.Background_ActingScale_X,
            BackgroundRigTarget.Background_ActingScale_Y => refs.Background_ActingScale_Y,

            // Background sprite
            BackgroundRigTarget.BackgroundSprite_Root => refs.BackgroundSprite_Root,
            BackgroundRigTarget.BackgroundSprite_Image => refs.BackgroundSprite_Image,

            // Object slots
            BackgroundRigTarget.Background_ObjectSlotRoot => refs.Background_ObjectSlotRoot,

            _ => null
        };
    }
}