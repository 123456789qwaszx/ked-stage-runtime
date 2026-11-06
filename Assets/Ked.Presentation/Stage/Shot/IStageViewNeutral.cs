using UnityEngine;

/// <summary>
/// 카메라 중립에서 본 무대 — 배치 계산이 카메라를 모르게 하는 계약.
///
/// 이 런타임에 카메라는 없다. 샷은 무대 컨테이너(카메라 팬·줌 루트, 깊이 층 프레이밍)를 움직여
/// 카메라처럼 보이게 한다. 배치(place · size의 초점 고정)는 무대 좌표로 정의되므로, 계산 사슬이
/// 이 노드들을 지날 때 그 순간의 값이 아니라 중립값을 써야 한다 — 그래야 결과가 샷의 진행도와
/// 무관하고, 일반 재생 · 압축 재생(롤백 · 이어하기) · 코어 폴드가 같은 자리를 낸다.
///
/// 중립 = 샷이 아무것도 하지 않은 무대: 카메라 루트는 기본 의도(<see cref="PresentationIntentState.Default"/>)의 값,
/// 깊이 층 프레이밍은 바인드 때 잰 기준값. 코어 노드 트리에 카메라 노드가 없는 것과 같은 뜻이다.
/// </summary>
public interface IStageViewNeutral
{
    /// <summary>
    /// <paramref name="rect"/>가 샷이 움직이는 무대 노드면 중립 위치 · 크기를 내고 true.
    /// 아니면 false — 호출자는 라이브 값을 쓴다.
    /// </summary>
    bool TryGetNeutral(RectTransform rect, out Vector2 anchoredPosition, out Vector3 localScale);
}
