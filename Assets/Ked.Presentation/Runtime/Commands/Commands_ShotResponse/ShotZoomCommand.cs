using System;
using UnityEngine;

[Serializable]
public sealed class ShotZoomCommandSpec : ShotIntentCommandSpecBase
{
    [Header("Zoom")]
    [Tooltip("목표 zoom intent 값")]
    [Range(-10f, 10f)]
    public float zoom = 0f;
}

public sealed class ShotZoomCommand : ShotIntentCommandBase<ShotZoomCommandSpec>
{
    public ShotZoomCommand(PresentationShotResponseSystem rig, ShotZoomCommandSpec spec)
        : base(rig, spec)
    { }

    protected override PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope)
    {
        return Ked.Presentation.Core.ShotZoomReduction
            .Reduce(from.ToCore(), spec.zoom)
            .ToUnity();
    }
}