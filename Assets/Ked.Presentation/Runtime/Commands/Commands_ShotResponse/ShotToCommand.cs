using System;
using UnityEngine;

[Serializable]
public sealed class ShotToCommandSpec : ShotIntentCommandSpecBase
{
    [Header("Pan")]
    public Vector2 pan = Vector2.zero;

    [Header("Zoom")]
    [Range(-10f, 10f)]
    public float zoom = 0f;
}

public sealed class ShotToCommand : ShotIntentCommandBase<ShotToCommandSpec>
{
    public ShotToCommand(PresentationShotResponseSystem rig, ShotToCommandSpec spec)
        : base(rig, spec)
    { }

    protected override PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope)
    {
        return Ked.Presentation.Core.ShotToReduction
            .Reduce(from.ToCore(), spec.zoom, spec.pan.ToCore())
            .ToUnity();
    }
}