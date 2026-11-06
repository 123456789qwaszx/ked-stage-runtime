using System;
using UnityEngine;

[Serializable]
public struct PortraitIdentity
{
    [Tooltip("캐릭터 ID (Amber)")]
    public string character;

    [Tooltip("표정 (2 / 02)")]
    public string emotion;

    [Tooltip("의상 / 변형")]
    public string variant;
}