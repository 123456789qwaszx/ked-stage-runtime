using Ked.Presentation.Core;

// PresentationIntentState <-> 코어 ShotIntentState. 필드 1:1 변환.
public static class PresentationIntentStateCoreBridge
{
    public static ShotIntentState ToCore(this in PresentationIntentState state)
        => new(state.zoom, state.panInRigSpace.ToCore(), state.focusPointInRigSpace.ToCore());

    public static PresentationIntentState ToUnity(this in ShotIntentState state)
        => new()
        {
            zoom = state.Zoom,
            panInRigSpace = state.PanInRigSpace.ToUnity(),
            focusPointInRigSpace = state.FocusPointInRigSpace.ToUnity(),
        };
}