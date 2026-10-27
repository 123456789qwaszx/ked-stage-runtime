using System;
using UnityEngine;

[Serializable]
public abstract class BackgroundRigCommandSpecBase : CommandSpecBase
{
    [Header("Background Rig")]
    [Tooltip("BackgroundRig registration key.")]
    public string rigKey;
}