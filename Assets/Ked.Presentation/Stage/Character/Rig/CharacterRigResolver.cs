using System;
using UnityEngine;

public static class CharacterRigResolver
{
    public static CharacterRigRefs ResolveRig(PresentationStage stage, string roleKey)
    {
        string resolvedRigKey = ResolveSlotKey(stage, roleKey);

        if (!stage.characterRigs.TryGetRig(resolvedRigKey, out CharacterRigRefs rig))
        {
            throw new InvalidOperationException(
                $"[CharacterRigResolver] Failed to resolve CharacterRigRefs. " +
                $"roleKey='{roleKey}', resolvedRigKey='{resolvedRigKey}'.");
        }

        return rig;
    }
    
    public static string ResolveSlotKey(PresentationStage stage, string roleKey)
    {
        string resolvedRoleKey = stage.roleAliases.Resolve(roleKey);

        if (stage.castRegistry.TryGetSlotKey(resolvedRoleKey, out string characterSlotKey))
            return characterSlotKey;

        return resolvedRoleKey;
    }
    
    public static string ResolveCharacterKey(PresentationStage stage, string roleKey)
    {
        string resolvedRoleKey = stage.roleAliases.Resolve(roleKey);
        string resolvedRigKey = ResolveSlotKey(stage, resolvedRoleKey);

        if (!stage.castRegistry.TryGetCharacter(resolvedRigKey, out string characterKey))
        {
            Debug.LogWarning(
                $"[CharacterRigResolver] Failed to get characterKey. return resolved roleKey as fallback. " +
                $"roleKey='{roleKey}', resolvedRoleKey='{resolvedRoleKey}', resolvedRigKey='{resolvedRigKey}'.");

            return resolvedRoleKey;
        }
        
        return characterKey;
    }
}