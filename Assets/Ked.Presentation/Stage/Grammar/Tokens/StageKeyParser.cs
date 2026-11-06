using Ked.Presentation.Core;
using UnityEngine;

/// <summary>
/// 대본의 무대 키 · 층 라벨. 둘 다 정본만 받는다 — 대본은 compositor가 내고, 별칭 표는 2026-11-05에 걷었다
/// (옛 PresentationStageKeyParser · PresentationCommandKeyParser에 서로 다른 별칭 집합이 둘 있었다).
///
/// 무대 키는 문자열 그대로다. 있는 무대인지는 StageRigRegistry가 꺼낼 때 판정한다(없으면 예외).
/// 층은 깊이 값 d다. 라벨 → d는 코어 StageLayerLabels의 눈금이라 폴드와 같은 표를 본다.
/// </summary>
public static class StageKeyParser
{
    // 무대를 생략하면 본 무대(StageRoles).
    public const string DefaultStage = StageRoles.MainStageKey;

    public static string ParseStage(string raw)
    {
        string s = (raw ?? string.Empty).Trim().ToLowerInvariant();

        return s.Length == 0 ? DefaultStage : s;
    }

    /// <summary>모르는 라벨은 데이터 오류다 — 경고하고 mid로 간다(종전과 같은 정책).</summary>
    public static float ParseLayerDepth(string raw)
    {
        if (StageLayerLabels.TryGetDepth(raw, out float depth))
            return depth;

        Debug.LogWarning($"[StageKeyParser] 모르는 층 라벨 '{raw}'. mid로 간다. 정본: {string.Join(", ", StageLayerLabels.Names)}");

        return StageLayerLabels.Mid;
    }
}
