using System;
using UnityEngine;

public enum StageMaskKind
{
    FullRect = 0,

    Slanted = 1,
    HorizontalStrip = 2,

    // Extra VN-useful masks.
    VerticalStrip = 3,
    DiagonalBand = 4,
    CircleIris = 5,
}

[Flags]
public enum StageMaskEdgeMode
{
    None = 0,
    Leading = 1 << 0,
    Trailing = 1 << 1,
    Both = Leading | Trailing,

    // Draws the whole outline where possible.
    Outline = 1 << 2,
}

public enum StageMaskRubberMode
{
    None = 0,
    OvershootEnd = 1,
    PullStart = 2,
}

public readonly struct StageMaskLineSegment
{
    public readonly Vector2 A;
    public readonly Vector2 B;

    public StageMaskLineSegment(Vector2 a, Vector2 b)
    {
        A = a;
        B = b;
    }
}