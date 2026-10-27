using UnityEngine;

public sealed class PresentationCameraRootApplier
{
    private readonly IShotResponseStageProvider _cameraRootProvider;

    public PresentationCameraRootApplier(IShotResponseStageProvider cameraRootProvider)
    {
        _cameraRootProvider = cameraRootProvider;
    }

    public void Apply(in PresentationIntentState state)
    {
        float scale = PresentationShotIntentMath.EvaluateCameraScale(state.zoom);
        
        _cameraRootProvider.StageZoomRoot.localScale = new Vector3(scale, scale, 1f);
        _cameraRootProvider.StagePanRoot.anchoredPosition = state.panInRigSpace;
    }
}