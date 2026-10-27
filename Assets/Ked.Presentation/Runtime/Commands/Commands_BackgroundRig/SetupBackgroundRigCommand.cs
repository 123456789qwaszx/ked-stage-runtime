using System;
using System.Collections;
using UnityEngine;

[Serializable]
public sealed class SetupBackgroundRigCommandSpec : CommandSpecBase
{
    [Header("Rig / Slot")]
    [Tooltip("Rig registration key. Also used as prefix: 'city' -> 'city_'.")]
    public string rigKey;

    [Tooltip("BackgroundRig prefab used for command presentation. " +
             "Empty fields bake a complete rig from BackgroundRigSchema at runtime. " +
             "Prefab the baked result when you need performance setup, external systems, response targets, or shot helpers.")]
    public RectTransform rigPrefab;

    public PresentationStageKey stage = PresentationStageKey.Stage00;
    public PresentationDepthLayerKey layer = PresentationDepthLayerKey.Far;

    [Tooltip("Base root name. Final name is '{rolePrefix}{rigRootName}'.")]
    public string rigRootName = "BackgroundRig";

    public string ResolvedRolePrefix
    {
        get
        {
            if (string.IsNullOrEmpty(rigKey))
                return "";

            return rigKey.EndsWith("_", StringComparison.Ordinal)
                ? rigKey
                : $"{rigKey}_";
        }
    }
}

public sealed class SetupBackgroundRigCommand : CommandBase
{
    // 캐릭터와 같은 머티리얼을 쓴다 — 리그마다 Instantiate되므로 공유해도 서로 간섭하지 않는다.
    private const string VisualEffectMaterialPath = "VisualEffects/M_UICharacterVisual";

    private readonly BackgroundRigBuilder _rigBuilder;
    private readonly BackgroundRigSlotResolver _slotResolver;
    private readonly SetupBackgroundRigCommandSpec _spec;

    private readonly PresentationStage _stage;

    public SetupBackgroundRigCommand(
        PresentationStage stage,
    SetupBackgroundRigCommandSpec spec,
        BackgroundRigBuilder rigBuilder,
        BackgroundRigSlotResolver slotResolver)
    {
        _stage = stage;
        _spec = spec;
        _rigBuilder = rigBuilder;
        _slotResolver = slotResolver;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Apply(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope) => Apply(scope);
    
    private void Apply(CommandRunScope scope)
    {
        SetupBackgroundRigCommandSpec spec = _spec;

        string rigKey = spec.rigKey;
        string rolePrefix = spec.ResolvedRolePrefix;

        RectTransform rigRoot = _rigBuilder.BuildBackgroundRigRoot(
            spec.rigPrefab,
            rolePrefix,
            spec.rigRootName);
        
        if (_slotResolver.TryResolve(_spec.stage, _spec.layer, out RectTransform parent))
            rigRoot.SetParent(parent, false);

        _rigBuilder.BindRefsFromRoot(rigRoot, rolePrefix, out BackgroundRigRefs refs);

        Material sourceMaterial = Resources.Load<Material>(VisualEffectMaterialPath);
        refs.VisualEffect = new RigVisualEffectController(
            refs.BackgroundSprite_Image,
            sourceMaterial);

        _stage.backgroundRigs.Register(rigKey, refs);

        // Optional bake helper:
        // Enable after refs registration when saving the generated rig as a reusable prefab.
        //StripRolePrefixForBake(rigRoot, rolePrefix, spec.rigRootName);
    }

    #region Helpers
    private static void StripRolePrefixForBake(RectTransform rigRoot, string rolePrefix, string rigRootName)
    {
        if (rigRoot == null)
            return;

        if (string.IsNullOrEmpty(rolePrefix))
            return;

        StripPrefixRecursive(rigRoot, rolePrefix);

        rigRoot.name = rigRootName;
    }

    private static void StripPrefixRecursive(Transform root, string rolePrefix)
    {
        if (root.name.StartsWith(rolePrefix, StringComparison.Ordinal))
            root.name = root.name.Substring(rolePrefix.Length);

        for (int i = 0; i < root.childCount; i++)
            StripPrefixRecursive(root.GetChild(i), rolePrefix);
    }
    #endregion
}