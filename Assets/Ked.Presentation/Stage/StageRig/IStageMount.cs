using UnityEngine;

/// <summary>
/// 무대가 서는 자리를 내주는 쪽 — 씬에 놓인 무대 캔버스(예제에서는 PresentationUIRoot).
/// 조립이 이 아래에 StageRigBuilder로 무대를 세우고, 에디터 도구(튜닝 덤프)가 이걸로 무대 캔버스를 찾는다.
/// 본체는 예제 타입을 모르므로 이 계약 하나로만 만난다.
/// </summary>
public interface IStageMount
{
    RectTransform StageMount { get; }
}
