using System;

[Serializable]
public sealed class ShotResetCommandSpec : ShotIntentCommandSpecBase 
{ }

public sealed class ShotResetCommand : ShotIntentCommandBase<ShotResetCommandSpec>
{
    public ShotResetCommand(PresentationShotResponseSystem rig, ShotResetCommandSpec spec) 
        : base(rig, spec) 
    { }

    protected override PresentationIntentState BuildTargetState(in PresentationIntentState from)
    {
        return Ked.Presentation.Core.ShotIntents.Reset().ToUnity();
    }
}