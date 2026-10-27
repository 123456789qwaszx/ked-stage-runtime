using UnityEngine;

public sealed class PresentationResponseCoordinateMapper
{
    private readonly IShotResponseStageProvider _stageRootProvider;

    public PresentationResponseCoordinateMapper(IShotResponseStageProvider stageRootProvider)
    {
        _stageRootProvider = stageRootProvider;
    }

    public PresentationResponseMeasure CaptureBaseMeasure(IResponseTarget target)
    {
        RectTransform rigSpaceRoot = GetRigSpaceRoot();

        Vector2 basePositionInRigSpace =
            PresentationCoordinateMath.CaptureNeutralPivotInRigSpace(
                target.MeasureRect,
                rigSpaceRoot);

        // 적용 기준은 PositionRect 자신의 중립 anchoredPosition.
        // 이 중립값에 offset만 더함.
        Vector2 baseAnchoredPosition = target.PositionRect.anchoredPosition;
        Vector2 baseLocalScale = new(target.ScaleRect.localScale.x, target.ScaleRect.localScale.y);

        return new PresentationResponseMeasure(
            basePositionInRigSpace,
            baseAnchoredPosition,
            baseLocalScale);
    }

    // rig-space "오프셋(벡터)"을 target PositionRect 부모 공간의 벡터로 변환한다.
    // 점(point)이 아니라 벡터라 translation이 빠진다 -> rigSpaceRoot와 targetParent가
    // 같은 카메라 아래 있으면 카메라 zoom/pan이 상쇄되어 변환이 흔들리지 않는다.
    public Vector2 ConvertOffsetFromRigSpaceToTargetParentSpace(
        Vector2 offsetInRigSpace,
        IResponseTarget target)
    {
        return PresentationCoordinateMath.ConvertVectorFromRigSpaceToTargetPositionParentSpace(
            offsetInRigSpace,
            GetRigSpaceRoot(),
            target.PositionRect.parent as RectTransform);
    }

    private RectTransform GetRigSpaceRoot()
    {
        return _stageRootProvider.RigSpaceRoot;
    }
}

// “계산 좌표계”와 “실제 적용 좌표계” 연결 어댑터
// rigSpaceRoot = PresentationResponseMath가 결과를 계산하는 공통 기준 좌표계
// targetParent = 실제 Target.PositionRect.anchoredPosition을 적용할 때 필요한 부모 좌표계
// neutralPivotSource = target의 현재 중립 기준 위치를 캡처하기 위한 측정용 RectTransform
public static class PresentationCoordinateMath
{
    public static Vector2 CaptureNeutralPivotInRigSpace(
        RectTransform neutralPivotSource,
        RectTransform rigSpaceRoot)
    {
        Vector3 worldPivot = neutralPivotSource.TransformPoint(Vector3.zero);
        Vector3 localPivot = rigSpaceRoot.InverseTransformPoint(worldPivot);

        return new Vector2(localPivot.x, localPivot.y);
    }

    public static Vector2 ConvertPointFromRigSpaceToTargetPositionParentSpace(
        Vector2 pointInRigSpace,
        RectTransform rigSpaceRoot,
        RectTransform targetParent)
    {
        Vector3 worldPosition = rigSpaceRoot.TransformPoint(new Vector3(pointInRigSpace.x, pointInRigSpace.y, 0f));
        Vector3 positionInParentSpace = targetParent.InverseTransformPoint(worldPosition);

        return new Vector2(positionInParentSpace.x, positionInParentSpace.y);
    }

    // 벡터(오프셋) 변환. origin을 빼서 translation을 제거.
    // rigSpaceRoot 간의 targetParent의 상대 스케일/회전만 반영되므로 공통 카메라는 상쇄됨.
    public static Vector2 ConvertVectorFromRigSpaceToTargetPositionParentSpace(
        Vector2 vectorInRigSpace,
        RectTransform rigSpaceRoot,
        RectTransform targetParent)
    {
        Vector3 worldOrigin = rigSpaceRoot.TransformPoint(Vector3.zero);
        Vector3 worldTip = rigSpaceRoot.TransformPoint(new Vector3(vectorInRigSpace.x, vectorInRigSpace.y, 0f));

        Vector3 parentOrigin = targetParent.InverseTransformPoint(worldOrigin);
        Vector3 parentTip = targetParent.InverseTransformPoint(worldTip);

        Vector3 delta = parentTip - parentOrigin;
        return new Vector2(delta.x, delta.y);
    }
}