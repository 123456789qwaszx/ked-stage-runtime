using UnityEngine;

public static class PresentationResponseMath
{
    public static PresentationTargetResponse CalculateTargetTransformResponseFromShotIntent(
        in PresentationIntentState state,
        PresentationResponseProfile profile,
        in PresentationResponseMeasure measure)
    {
        float cameraScale = PresentationShotIntentMath.EvaluateCameraScale(state.zoom);
        float zoomAmount = cameraScale - 1f;

        Vector2 focusSpreadOffset =
            CalculateAxisSeparatedFocusSpreadOffset(
                measure.basePositionInRigSpace,
                state.focusPointInRigSpace,
                zoomAmount,
                profile.focusSpreadPixelsPerZoom);

        Vector2 panOffset =
            CalculateAxisSeparatedPanOffset(
                state.panInRigSpace,
                profile.panResponse);

        Vector2 destPos =
            measure.basePositionInRigSpace +
            focusSpreadOffset +
            panOffset;

        float responseScaleMultiplier = Mathf.Pow(cameraScale, profile.zoomScaleResponse);
        Vector2 destScale = measure.baseLocalScale * responseScaleMultiplier;

        return new PresentationTargetResponse
        {
            anchoredPosition = destPos,
            scale = destScale,
        };
    }

    private static Vector2 CalculateAxisSeparatedFocusSpreadOffset(
        Vector2 basePositionInRigSpace,
        Vector2 focusPointInRigSpace,
        float zoomAmount,
        Vector2 focusSpreadPixelsPerZoom)
    {
        Vector2 fromFocus = basePositionInRigSpace - focusPointInRigSpace;

        float signX = Mathf.Approximately(fromFocus.x, 0f)
            ? 0f
            : Mathf.Sign(fromFocus.x);

        float signY = Mathf.Approximately(fromFocus.y, 0f)
            ? 0f
            : Mathf.Sign(fromFocus.y);

        return new Vector2(
            signX * focusSpreadPixelsPerZoom.x * zoomAmount,
            signY * focusSpreadPixelsPerZoom.y * zoomAmount);
    }

    private static Vector2 CalculateAxisSeparatedPanOffset(
        Vector2 panInRigSpace,
        Vector2 panResponse)
    {
        return new Vector2(
            panInRigSpace.x * panResponse.x,
            panInRigSpace.y * panResponse.y);
    }
}