using System;
using System.Collections.Generic;
using UnityEngine;

public enum CharacterFacing
{
    Right = 1,
    Left = -1,
}

public static class CharacterFacingExtensions
{
    public static int Sign(this CharacterFacing facing)
    {
        return facing == CharacterFacing.Left ? -1 : 1;
    }

    public static CharacterFacing Opposite(this CharacterFacing facing)
    {
        return facing == CharacterFacing.Left
            ? CharacterFacing.Right
            : CharacterFacing.Left;
    }

    public static Vector2 MirrorX(this CharacterFacing facing, Vector2 value)
    {
        return facing == CharacterFacing.Left
            ? new Vector2(-value.x, value.y)
            : value;
    }
}

public sealed class CastRegistry
{
    private readonly struct CastBinding
    {
        public readonly string character;
        public readonly string variant;
        public readonly CharacterFacing facing;

        public CastBinding(
            string character,
            string variant,
            CharacterFacing facing = CharacterFacing.Right)
        {
            this.character = character;
            this.variant = variant;
            this.facing = facing;
        }

        public CastBinding WithVariant(string newVariant)
        {
            return new CastBinding(character, newVariant, facing);
        }

        public CastBinding WithFacing(CharacterFacing newFacing)
        {
            return new CastBinding(character, variant, newFacing);
        }
    }

    private readonly Dictionary<string, CastBinding> _slotToBinding = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _characterToSlot = new(StringComparer.Ordinal);

    public void CastCharRig(string slotKey, string characterKey)
    {
        slotKey = (slotKey ?? "").Trim();
        characterKey = PresentationKeyNormalizer.NormalizeCharacterKey(characterKey);

        if (string.IsNullOrEmpty(slotKey) || string.IsNullOrEmpty(characterKey))
            return;

        CharacterFacing previousFacing = CharacterFacing.Right;

        if (IsCast(slotKey))
        {
            if (TryGetFacing(slotKey, out CharacterFacing existingFacing))
                previousFacing = existingFacing;

            UncastCharRig(slotKey);
        }

        _slotToBinding[slotKey] = new CastBinding(characterKey, "", previousFacing);
        _characterToSlot[characterKey] = slotKey;
    }

    public bool SetVariant(string slotKey, string variantKey)
    {
        slotKey = (slotKey ?? "").Trim();
        variantKey = PresentationKeyNormalizer.NormalizeVariantKey(variantKey);

        if (!_slotToBinding.TryGetValue(slotKey, out CastBinding binding))
        {
            Debug.LogWarning(
                $"[CastRegistry] SetVariant failed. Binding not found. " +
                $"slotKey='{slotKey}', variantKey='{variantKey}'. " +
                $"Expected order: SetupCommand -> CastCommand -> PoseCommand.");

            return false;
        }

        _slotToBinding[slotKey] = binding.WithVariant(variantKey);
        return true;
    }

    public bool SetFacing(string targetKey, CharacterFacing facing)
    {
        if (!TryResolveSlotKey(targetKey, out string slotKey))
        {
            Debug.LogWarning(
                $"[CastRegistry] SetFacing failed. Binding not found. " +
                $"targetKey='{targetKey}'. Expected slot key or cast character key.");

            return false;
        }

        _slotToBinding[slotKey] = _slotToBinding[slotKey].WithFacing(facing);
        return true;
    }

    public bool ToggleFacing(string targetKey, out CharacterFacing newFacing)
    {
        newFacing = CharacterFacing.Right;

        if (!TryGetFacing(targetKey, out CharacterFacing currentFacing))
            return false;

        newFacing = currentFacing.Opposite();
        return SetFacing(targetKey, newFacing);
    }

    public bool TryGetFacing(string targetKey, out CharacterFacing facing)
    {
        facing = CharacterFacing.Right;

        if (!TryResolveSlotKey(targetKey, out string slotKey))
            return false;

        facing = _slotToBinding[slotKey].facing;
        return true;
    }

    public bool TryPeekFacing(string targetKey, out CharacterFacing facing)
    {
        facing = CharacterFacing.Right;

        if (!TryResolveSlotKey(targetKey, out string slotKey))
            return false;

        facing = _slotToBinding[slotKey].facing;
        return true;
    }

    public bool UncastCharRig(string slotKey)
    {
        slotKey = (slotKey ?? "").Trim();

        if (!_slotToBinding.Remove(slotKey, out CastBinding binding))
        {
            Debug.LogWarning($"[CastRegistry] Uncast failed. Binding not found. slotKey='{slotKey}'.");
            return false;
        }

        _characterToSlot.Remove(binding.character);
        return true;
    }

    public bool TryGetCharacter(string slotKey, out string characterKey)
    {
        slotKey = (slotKey ?? "").Trim();

        if (!_slotToBinding.TryGetValue(slotKey, out CastBinding binding))
        {
            Debug.LogWarning(
                $"[CastRegistry] Binding not found. slotKey='{slotKey}'. " +
                $"Expected order: SetupCommand -> CastCommand -> PortraitCommand.");

            characterKey = null;
            return false;
        }

        characterKey = binding.character;
        return true;
    }

    public bool TryGetVariant(string slotKey, out string variantKey)
    {
        slotKey = (slotKey ?? "").Trim();

        if (!_slotToBinding.TryGetValue(slotKey, out CastBinding binding))
        {
            Debug.LogWarning(
                $"[CastRegistry] Binding not found. slotKey='{slotKey}'. " +
                $"Expected order: SetupCommand -> CastCommand -> PoseCommand.");

            variantKey = null;
            return false;
        }

        variantKey = binding.variant;
        return true;
    }

    public bool TryGetSlotKey(string characterKey, out string slotKey)
    {
        characterKey = PresentationKeyNormalizer.NormalizeCharacterKey(characterKey);
        return _characterToSlot.TryGetValue(characterKey, out slotKey);
    }

    public void Clear()
    {
        _slotToBinding.Clear();
        _characterToSlot.Clear();
    }
    
    public bool TryPeekCharacter(string slotKey, out string characterKey)
    {
        if (!_slotToBinding.TryGetValue(slotKey, out CastBinding binding))
        {
            characterKey = null;
            return false;
        }

        characterKey = binding.character;
        return true;
    }

    private bool IsCast(string targetKey)
    {
        targetKey = (targetKey ?? "").Trim();

        if (_slotToBinding.ContainsKey(targetKey))
            return true;

        string characterKey = PresentationKeyNormalizer.NormalizeCharacterKey(targetKey);
        return _characterToSlot.ContainsKey(characterKey);
    }

    private bool TryResolveSlotKey(string targetKey, out string slotKey)
    {
        slotKey = (targetKey ?? "").Trim();

        if (string.IsNullOrEmpty(slotKey))
            return false;

        if (_slotToBinding.ContainsKey(slotKey))
            return true;

        string characterKey = PresentationKeyNormalizer.NormalizeCharacterKey(slotKey);

        if (_characterToSlot.TryGetValue(characterKey, out string resolvedSlotKey))
        {
            slotKey = resolvedSlotKey;
            return true;
        }

        slotKey = null;
        return false;
    }
}
