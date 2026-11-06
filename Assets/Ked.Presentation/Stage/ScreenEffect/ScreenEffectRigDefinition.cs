using UnityEngine;
using UnityEngine.UI;

public static class ScreenEffectRigSchema
{
    public enum Refs
    {
        ScreenOverlay_Root,

        Vignette_Image,
        Noise_Image,
        Flash_Image,
    }

    public enum ControllerKind
    {
        None,
        Vignette,
        Noise,
        Flash,
    }

    public sealed class NodeDef
    {
        public Refs Id;
        public Refs? Parent;

        public bool NeedsImage;
        public bool StretchFull = true;

        public ControllerKind Controller;
        public string MaterialResourcesPath;

        public Color InitialImageColor = Color.white;
        public bool RaycastTarget = false;
    }

    public static readonly NodeDef[] Nodes =
    {
        new()
        {
            Id = Refs.ScreenOverlay_Root,
            Parent = null,
        },

        // Node array order is the layer order under the same parent.
        // Bottom -> top.
        new()
        {
            Id = Refs.Vignette_Image,
            Parent = Refs.ScreenOverlay_Root,
            NeedsImage = true,
            Controller = ControllerKind.Vignette,
            MaterialResourcesPath = "VisualEffects/M_UIScreenVignette",
            RaycastTarget = false,
        },

        new()
        {
            Id = Refs.Noise_Image,
            Parent = Refs.ScreenOverlay_Root,
            NeedsImage = true,
            Controller = ControllerKind.Noise,
            MaterialResourcesPath = "VisualEffects/M_UIScreenNoise",
            RaycastTarget = false,
        },

        new()
        {
            Id = Refs.Flash_Image,
            Parent = Refs.ScreenOverlay_Root,
            NeedsImage = true,
            Controller = ControllerKind.Flash,
            MaterialResourcesPath = "VisualEffects/M_UIScreenFlash",
            RaycastTarget = false,
        },
    };
}

public sealed class ScreenEffectRigRefs
{
    public RectTransform RigRoot { get; }

    public RectTransform ScreenOverlay_Root;

    public Image Vignette_Image;
    public Image Noise_Image;
    public Image Flash_Image;

    public ScreenVignetteEffectController Vignette;
    public ScreenNoiseEffectController Noise;
    public ScreenFlashEffectController Flash;

    public ScreenEffectRigRefs(RectTransform rigRoot)
    {
        RigRoot = rigRoot;
    }
}