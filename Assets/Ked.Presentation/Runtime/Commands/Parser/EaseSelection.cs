using DG.Tweening;
using Ked.Presentation.Core;

/// <summary>
/// 다섯째(또는 마지막) 이징 인자가 고른 것 — 표준 이징 하나이거나 커스텀 곡선 하나다.
/// 스펙에 두 필드로 실린다: ease · customCurveKeys.
/// </summary>
public readonly struct EaseSelection
{
    public readonly Ease Ease;
    public readonly CurveKey[] CurveKeys;

    public EaseSelection(Ease ease, CurveKey[] curveKeys = null)
    {
        Ease = ease;
        CurveKeys = curveKeys;
    }
}
