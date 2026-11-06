using System;
using UnityEngine;

public enum CharacterFocusPreset
{
    Feet = 0,

    Body = 10,
    Bust = 20,
    Face = 30,
    
    FaceAura = 31,

    HandLeft = 40,
    HandRight = 41,
}


[Serializable]
public struct CharacterFocusOffsetSet
{
    [Header("Standard Focus Offsets")]
    public Vector2 feet;
    public Vector2 body;
    public Vector2 bust;
    public Vector2 face;
    
    [Header("Expression / Emoji Focus Offsets")]
    public Vector2 faceAura;

    [Header("Extra Focus Offsets")]
    public Vector2 handLeft;
    public Vector2 handRight;

    public Vector2 Get(CharacterFocusPreset preset)
    {
        switch (preset)
        {
            case CharacterFocusPreset.Feet:
                return feet;

            case CharacterFocusPreset.Body:
                return body;

            case CharacterFocusPreset.Bust:
                return bust;

            case CharacterFocusPreset.Face:
                return face;
            
            case CharacterFocusPreset.FaceAura:
                return faceAura;

            case CharacterFocusPreset.HandLeft:
                return handLeft;

            case CharacterFocusPreset.HandRight:
                return handRight;

            default:
                return Vector2.zero;
        }
    }

    public static CharacterFocusOffsetSet Default => new()
    {
        feet = Vector2.zero,
        body = new Vector2(0f, 400f),
        bust = new Vector2(0f, 600f),
        face = new Vector2(0f, 850f),
        
        faceAura = new Vector2(0f, 1000f),

        handLeft = new Vector2(-220f, 520f),
        handRight = new Vector2(220f, 520f),
    };
}