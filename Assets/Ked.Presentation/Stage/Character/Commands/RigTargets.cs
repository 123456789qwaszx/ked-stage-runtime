using Ked.Presentation.Core;
using UnityEngine;

// RigTargets — 리그 커맨드의 Unity → Core 경계.
// Core의 NodeTargets는 노드 키와 Core 값으로 계산하고, 여기서는 같은 계산을 rect와 스펙으로 부른다.
// 커맨드가 변환과 Args 조립을 들고 있지 않도록 이 한 곳에서만 한다.
//
// 시작값은 인자로 받는다 — 안에서 rect를 다시 읽으면 시작값을 먼저 잡아야 한다는 순서가 숨는다.
public static class RigTargets
{
    public static NodeTarget MoveBy(RectTransform rect, MoveByCommandSpecCharR spec, Vector2 start)
        => NodeTargets.MoveBy(
            rect.name,
            new NodeTargets.MoveByArgs(spec.useAbsolutePosition, spec.delta.ToCore()),
            start.ToCore());

    public static NodeTarget RotateTo(RectTransform rect, RotateToCommandSpecCharR spec, Vector3 start)
        => NodeTargets.RotateTo(
            rect.name,
            new NodeTargets.RotateToArgs(spec.relativeToCurrent, spec.toEuler.ToCore()),
            start.ToCore());

    public static NodeTarget ScaleTo(RectTransform rect, ScaleToCommandSpecCharR spec, Vector2 start)
        => NodeTargets.ScaleTo(
            rect.name,
            new NodeTargets.ScaleToArgs(spec.relativeToCurrent, spec.toScale.ToCore()),
            start.ToCore());
}
