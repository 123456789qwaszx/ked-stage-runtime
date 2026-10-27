using UnityEngine;

public static class CharacterRigTargetResolver
{
    public static CharacterRigRefs ResolveCharRigFromTargetKey(PresentationStage stage, string targetKey)
    {
        string resolvedRigKey = ResolveRigKeyByPolicy(stage, targetKey);
        
        if (!stage.characterRigs.TryGetRig(resolvedRigKey, out CharacterRigRefs rig))
        {
            Debug.LogWarning(
                $"[CharacterRigTargetResolver] Failed to resolve CharacterRigRefs. " +
                $"targetKey='{targetKey}', resolvedRigKey='{resolvedRigKey}'.");
        }

        return rig;
    }
    
    public static string ResolveRigKeyByPolicy(PresentationStage stage, string targetKey)
    {
        string resolvedTargetKey = stage.characterTargetAliases.Resolve(targetKey);

        if (stage.castRegistry.TryGetSlotKey(resolvedTargetKey, out string characterSlotKey))
            return characterSlotKey;

        return resolvedTargetKey;
    }
    
    public static string ResolveCharacterKeyFromTargetKey(PresentationStage stage, string targetKey)
    {
        string resolvedTargetKey = stage.characterTargetAliases.Resolve(targetKey);
        string resolvedRigKey = ResolveRigKeyByPolicy(stage, resolvedTargetKey);

        if (!stage.castRegistry.TryGetCharacter(resolvedRigKey, out string characterKey))
        {
            Debug.LogWarning(
                $"[CharacterRigTargetResolver] Failed to get characterKey. return resolved targetKey as fallback. " +
                $"targetKey='{targetKey}', resolvedTargetKey='{resolvedTargetKey}', resolvedRigKey='{resolvedRigKey}'.");

            return resolvedTargetKey;
        }
        
        return characterKey;
    }
}