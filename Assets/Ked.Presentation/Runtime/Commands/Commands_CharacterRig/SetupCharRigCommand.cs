using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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

        // 슬롯 키가 캐스팅된 캐릭터 키와 같으면, 그 키로 부르는 명령은 캐릭터 쪽 슬롯으로 풀린다
        // (CharacterRigTargetResolver — 캐릭터 키가 먼저). 이 리그에는 닿지 않는다. 조용히 두지 않는다.
        if (_stage.castRegistry.TryGetSlotKey(rigKey, out string characterSlot) &&
            !string.Equals(characterSlot, rigKey, StringComparison.Ordinal))
        {
            Debug.LogWarning(
                $"[SetupCharRigCommand] 슬롯 키 '{rigKey}'가 캐스팅된 캐릭터 키와 같다(그 캐릭터는 슬롯 '{characterSlot}'). " +
                $"'{rigKey}'로 부르는 명령은 슬롯 '{characterSlot}'으로 간다 — 이 리그를 움직이려면 다른 키를 쓸 것.");
        }

        // 재선언 = 새 리그(옛 리그는 Register가 부순다). 새 리그는 좌우 반전 전이다 — 배역의 방향도 되돌린다.
        // 그대로 두면 바인딩은 Left인데 그림은 Right라, 다음 mirror 토글이 아무것도 바꾸지 않는다.
        if (_stage.characterRigs.TryPeekRig(rigKey, out _) &&
            _stage.castRegistry.TryPeekFacing(rigKey, out CharacterFacing previousFacing) &&
            previousFacing != CharacterFacing.Right)
        {
            _stage.castRegistry.SetFacing(rigKey, CharacterFacing.Right);
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

        // 배경은 화면 전체를 덮는다 — 리그 안의 그래픽 전부(본 그림·위 겹)가 클릭을 받지 않게 한다.
        // 위 겹(CharacterPortraitSpriteOverlay_Image)은 스프라이트 없이 화면 크기로 늘어나 있어 그대로면 레이캐스트를 받는다.
        if (spec.asBackground)
        {
            foreach (Graphic graphic in rigRoot.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        }

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