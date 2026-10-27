using System;
using UnityEngine;

[Serializable]
public sealed class ShotTrackCommandSpec : ShotIntentCommandSpecBase
{
    [Header("Pan")]
    [Tooltip("현재 pan에 더할 값")]
    public Vector2 pan = Vector2.zero;
}

public sealed class ShotTrackCommand : ShotIntentCommandBase<ShotTrackCommandSpec>
{
    public ShotTrackCommand(PresentationShotResponseSystem rig, ShotTrackCommandSpec spec)
        : base(rig, spec)
    { }

    protected override PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope)
    {
        return Ked.Presentation.Core.ShotTrackReduction
            .Reduce(from.ToCore(), spec.pan.ToCore())
            .ToUnity();
    }
}