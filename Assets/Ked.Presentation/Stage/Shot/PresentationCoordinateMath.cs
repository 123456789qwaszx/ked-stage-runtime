using UnityEngine;

// “계산 좌표계”와 “실제 적용 좌표계” 연결 어댑터
// rigSpaceRoot = 무대 루트(StageRigRefs.Root). 코어 ShotLayoutMath · 배치 계산이 결과를 내는 공통 기준 좌표계
// targetParent = 실제 anchoredPosition을 쓰는 노드의 부모 좌표계(예: 깊이 층의 부모 = 카메라 줌 루트)
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