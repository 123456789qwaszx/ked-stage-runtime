using System;
using DG.Tweening;
using UnityEngine;

public static class YarnEaseParser
{
    // "OutCubic" / "linear" / "InOutBack" (대소문자 무시).
    // 빈 토큰 = 인자 생략 = fallback(스펙 기본값 OutCubic) — 기존 대본 불변의 축.
    public static Ease Parse(string token, Ease fallback = Ease.OutCubic)
    {
        if (string.IsNullOrWhiteSpace(token))
            return fallback;

        // 숫자 토큰은 Enum.TryParse가 임의 정수로도 성공한다 — 이름만 받는다.
        if (!char.IsDigit(token[0]) && token[0] != '-' && token[0] != '+'
            && Enum.TryParse(token, ignoreCase: true, out Ease parsed)
            && IsPlayableEase(parsed))
            return parsed;

        // 침묵 금지 — 오류는 소리 내되 재생은 계속한다.
        Debug.LogError(
            $"[YarnEaseParser] Invalid ease token '{token}'. " +
            $"Expected a DOTween ease name (e.g. Linear, OutCubic, InOutBack). " +
            $"Fallback to {fallback}.");

        return fallback;
    }

    private static bool IsPlayableEase(Ease ease)
        => ease != Ease.Unset
           && ease != Ease.INTERNAL_Zero
           && ease != Ease.INTERNAL_Custom;
}
