using System;
using UnityEngine;

public enum CharRigDirection
{
    Left = 0,
    Right,
    Up,
    Down,
}

[Serializable]
public abstract class CharacterRigCommandSpecBase : CommandSpecBase
{
    [Header("Role")]
    [Tooltip("대본이 캐릭터를 부르는 이름 — 슬롯 키 · 캐릭터 키 · @별칭. CharacterRigResolver가 슬롯으로 풉니다(캐릭터 키가 먼저).")]
    public string roleKey;
}