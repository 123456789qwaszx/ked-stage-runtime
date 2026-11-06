using Ked.Presentation.Core;
using UnityEngine;

public static class PresentationShotIntentMath
{
    // zoom -> 배율 환산 규약은 코어가 가짐(ShotIntentMath.ZoomToScaleFactor = 0.05).
    public static float EvaluateCameraScale(float zoom)
        => ShotIntentMath.EvaluateCameraScale(zoom);

    // 저작된 의도 값을 보간하는 것이지 최종 Transform 값을 보간하는 게 아님.
    // 매 프레임 보간된 의도에서 최종 response를 다시 푼다.
    public static PresentationIntentState Interpolate(
        in PresentationIntentState from,
        in PresentationIntentState to,
        float t)
        => new()
        {
            zoom = Mathf.Lerp(from.zoom, to.zoom, t),
            panInRigSpace = Vector2.Lerp(from.panInRigSpace, to.panInRigSpace, t),
            focusPointInRigSpace = Vector2.Lerp(from.focusPointInRigSpace, to.focusPointInRigSpace, t),
        };

    // 불필요한 트윈을 건너뛰기 위한 근사 비교.
    public static bool ApproximatelyEqual(
        in PresentationIntentState a,
        in PresentationIntentState b)
        => Mathf.Abs(a.zoom - b.zoom) <= 0.0001f &&
           Vector2.SqrMagnitude(a.panInRigSpace - b.panInRigSpace) <= 0.0001f &&
           Vector2.SqrMagnitude(a.focusPointInRigSpace - b.focusPointInRigSpace) <= 0.0001f;
}