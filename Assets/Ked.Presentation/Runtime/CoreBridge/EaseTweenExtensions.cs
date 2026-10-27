using DG.Tweening;
using Ked.Presentation.Core;

/// <summary>
/// 트윈에 이징을 거는 한 자리 — 표준 EaseKind와 커스텀 곡선(@이름)의 갈림길.
///
/// 커스텀 곡선은 DOTween 커스텀 이즈 델리게이트로 나간다: 트윈 경로 구조는
/// 그대로이고 모양만 바뀐다. 프리뷰(VnTool)와 재생이 같은 CurveFunctions를
/// 지나므로 "선택기가 보여주는 모양 = 재생이 타는 모양"이 여기서 갈린다.
/// </summary>
public static class EaseTweenExtensions
{
    public static T ApplyEase<T>(this T tween, Ease ease, CurveKey[] curveKeys)
        where T : Tween
    {
        if (curveKeys is { Length: > 0 })
            return tween.SetEase((time, duration, _, _) =>
                CurveFunctions.Evaluate(curveKeys, duration <= 0f ? 1f : time / duration));

        return tween.SetEase(ease);
    }
}
