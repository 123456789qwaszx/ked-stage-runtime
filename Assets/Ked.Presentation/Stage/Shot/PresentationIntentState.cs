using System;
using UnityEngine;

// Authored shot intent state.
// This is not a final Transform state.
// Values are solved later into camera root transforms and per-target response transforms.
[Serializable]
public struct PresentationIntentState
{
    // Authored zoom intent, not final camera scale.
    public float zoom;

    // Camera pan offset in shared rig space.
    public Vector2 panInRigSpace;

    // Logical focus point in shared rig space.
    // Camera framing and target spread are solved relative to this point.
    public Vector2 focusPointInRigSpace;

    public static PresentationIntentState Default => new()
    {
        zoom = 0f,
        panInRigSpace = Vector2.zero,
        focusPointInRigSpace = Vector2.zero,
    };
}