using UnityEngine;

/// <summary>
/// 호스트가 실제로 쓰는 리그 프리팹을 내준다. 배경은 캐스팅하지 않은 캐릭터 슬롯이라 캐릭터 리그 하나다.
///
/// 튜닝 덤프 도구(PresentationTuningExporter)가 씬에서 이 계약을 찾아, 런타임과 같은 프리팹으로
/// 리그를 세우고 노드 트리를 읽는다. 본체 도구는 예제 타입을 모르므로 이 계약으로만 찾는다.
/// 런타임 경로는 이 계약을 쓰지 않는다 — 조립기는 프리팹을 직접 받는다.
/// </summary>
public interface IRigPrefabSource
{
    RectTransform CharacterRigPrefab { get; }
}
