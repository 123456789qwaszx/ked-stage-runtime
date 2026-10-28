using System;
using System.Collections;
using UnityEngine;

[Serializable]
public sealed class SetupCharRigCommandSpec : CommandSpecBase
{
    [Header("Role / Slot")]
    [Tooltip("Rig registration key. Also used as prefix: 'hill' -> 'hill_'.")]
    public string roleKey;

    [Tooltip("CharacterRig prefab used for command presentation. " +
             "Empty fields bake a complete rig from CharacterRigSchema at runtime. " +
             "Prefab the baked result when you need performance setup, external systems, response targets, or shot helpers.")]
    public RectTransform rigPrefab;

    [Header("Stage Depth Slot")]
    public PresentationStageKey stage = PresentationStageKey.Stage00;
    public PresentationDepthLayerKey layer = PresentationDepthLayerKey.Mid;

    [Tooltip("Base root name. Final name is '{rolePrefix}{rigRootName}'.")]
    public string rigRootName = "CharacterRig";

    [Header("Background")]
    [Tooltip("배경으로 세운다(bg_spawn·bg_slotNN). 배경은 캐스팅하지 않은 슬롯이다 — " +
             "배역 있는 슬롯을 덮으면 배역을 풀고 경고한다.")]
    public bool asBackground;

    public string ResolvedRolePrefix
    {
        get
        {
            if (string.IsNullOrEmpty(roleKey))
                return "";

            return roleKey.EndsWith("_", StringComparison.Ordinal)
                ? roleKey
                : $"{roleKey}_";
        }
    }
}

public sealed class SetupCharRigCommand : CommandBase
{
    private const string VisualEffectMaterialPath = "VisualEffects/M_UICharacterVisual";

    private readonly CharRigSlotResolver _slotResolver;
    private readonly CharacterRigBuilder _rigBuilder;
    private readonly SetupCharRigCommandSpec _spec;

    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    private readonly PresentationStage _stage;

    public SetupCharRigCommand(
        PresentationStage stage,
        CharRigSlotResolver slotResolver,
        CharacterRigBuilder rigBuilder,
        SetupCharRigCommandSpec spec)
    {
        _stage = stage;
        _slotResolver = slotResolver;
        _rigBuilder = rigBuilder;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        SetupCharRigCommandSpec spec = _spec;

        string rigKey = spec.roleKey;
        string rolePrefix = spec.ResolvedRolePrefix;

        // 배경 = 캐스팅하지 않은 슬롯. 배역이 남으면 `fade_in harin`이 배경을 페이드하고
        // focus_on이 배경을 초점면에 넣는다. 코어 StageReducer.Background도 같은 자리에서 배역을 푼다.
        if (spec.asBackground && _stage.castRegistry.TryPeekCharacter(rigKey, out string castCharacter))
        {
            Debug.LogWarning(
                $"[SetupCharRigCommand] 배경 '{rigKey}'가 배역 있는 슬롯(캐릭터 '{castCharacter}')을 덮는다 — 배역을 푼다. " +
                "배경과 캐릭터는 같은 키 공간이다. 다른 키를 쓰려던 것이 아닌지 확인할 것.");

            _stage.castRegistry.UncastCharRig(rigKey);
        }

        RectTransform rigRoot = _rigBuilder.BuildCharacterRigRoot(
            spec.rigPrefab,
            rolePrefix,
            spec.rigRootName);

        bool resolved = _slotResolver.TryResolve(
            spec.stage,
            spec.layer,
            out RectTransform parent);

        if (resolved)
            rigRoot.SetParent(parent, false);

        _rigBuilder.BindRefsFromRoot(rigRoot, rolePrefix, out CharacterRigRefs refs);

        Material sourceMaterial = Resources.Load<Material>(VisualEffectMaterialPath);
        refs.VisualEffect = new RigVisualEffectController(
            refs.CharacterPortraitSprite_Image,
            sourceMaterial,
            refs.CharacterPortraitSpriteOverlay_Image);

        _stage.characterRigs.Register(rigKey, refs);

        //StripRolePrefixForBake(rigRoot, rolePrefix, spec.rigRootName);
        yield break;
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