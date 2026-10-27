using System;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using Ked.Presentation.Core;
using NUnit.Framework;

// ─────────────────────────────────────────────────────────────────────────────
// W66b — 코어 EaseFunctions ↔ DOTween EaseManager.Evaluate 등가.
//
// 여기가 등가의 심판이다: DOTween을 직접 참조할 수 있는 유일한 자리.
// VnTool 쪽은 ease-golden.json 대조만 한다(그쪽엔 DOTween이 없다).
// 샘플 격자는 골든 덤프와 같다: t = i/256, i = 0..256.
// ─────────────────────────────────────────────────────────────────────────────
public class EaseFunctionsDOTweenParityTests
{
    private const int SampleCount = 257;
    private const float Tolerance = 1e-4f;

    private static List<Ease> StandardEases()
    {
        List<Ease> eases = new();

        foreach (Ease ease in Enum.GetValues(typeof(Ease)))
        {
            if (ease == Ease.Unset) continue;
            if (ease.ToString().StartsWith("INTERNAL_", StringComparison.Ordinal)) continue;
            eases.Add(ease);
        }

        return eases;
    }

    [Test]
    public void 표준_Ease와_EaseKind가_이름으로_일대일이다()
    {
        List<Ease> standard = StandardEases();

        // DOTween → 코어: 표준 항목마다 같은 이름의 EaseKind가 있어야 한다.
        foreach (Ease ease in standard)
        {
            Assert.IsTrue(
                Enum.TryParse(ease.ToString(), out EaseKind _),
                $"EaseKind에 '{ease}'가 없다 — DOTween 표준 항목과 어긋났다.");
        }

        // 코어 → DOTween: EaseKind에 표준 밖 항목이 생기면 안 된다.
        Assert.AreEqual(
            standard.Count, Enum.GetValues(typeof(EaseKind)).Length,
            "EaseKind 항목 수가 DOTween 표준 항목 수와 다르다.");
    }

    [Test]
    public void 전_항목_전_샘플에서_DOTween과_오차가_한계_미만이다()
    {
        // SetEase(Ease)만 쓴 트윈과 같은 조건 — DOTween 라이브 기본값.
        float overshootOrAmplitude = DOTween.defaultEaseOvershootOrAmplitude;
        float period = DOTween.defaultEasePeriod;

        foreach (Ease ease in StandardEases())
        {
            EaseKind kind = (EaseKind)Enum.Parse(typeof(EaseKind), ease.ToString());

            for (int i = 0; i < SampleCount; i++)
            {
                float t = i / 256f;

                float expected = EaseManager.Evaluate(
                    ease, null, t, 1f, overshootOrAmplitude, period);
                float actual = EaseFunctions.Evaluate(
                    kind, t, 1f, overshootOrAmplitude, period);

                Assert.AreEqual(
                    expected, actual, Tolerance,
                    $"{ease} @ t={t}: DOTween={expected}, Core={actual}");
            }
        }
    }

    [Test]
    public void 기본_상수가_DOTween_기본값과_같다()
    {
        Assert.AreEqual(
            DOTween.defaultEaseOvershootOrAmplitude,
            EaseFunctions.DefaultOvershootOrAmplitude,
            0f,
            "코어 DefaultOvershootOrAmplitude가 DOTween 기본값과 다르다.");

        Assert.AreEqual(
            DOTween.defaultEasePeriod,
            EaseFunctions.DefaultPeriod,
            0f,
            "코어 DefaultPeriod가 DOTween 기본값과 다르다.");
    }
}
