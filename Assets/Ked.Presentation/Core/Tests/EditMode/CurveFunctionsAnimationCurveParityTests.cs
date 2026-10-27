using Ked.Presentation.Core;
using NUnit.Framework;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// 커스텀 곡선 — 코어 CurveFunctions ↔ Unity AnimationCurve.Evaluate 등가.
//
// 여기가 등가의 심판이다(ease-golden과 같은 이중 구조): 유니티 쪽 대조 상대를
// 참조할 수 있는 유일한 자리. VnTool 쪽은 대표 커브 픽스처 대조만 한다.
// 샘플 격자는 이징 골든 덤프와 같다: t = i/256, i = 0..256.
// ─────────────────────────────────────────────────────────────────────────────
public class CurveFunctionsAnimationCurveParityTests
{
    private const int SampleCount = 257;
    private const float Tolerance = 1e-4f;

    private static readonly (string name, CurveKey[] keys)[] RepresentativeCurves =
    {
        // 항등: (0,0)→(1,1) 탄젠트 1 — Evaluate(t) == t 여야 한다.
        ("linear", new[]
        {
            new CurveKey(0f, 0f, 1f, 1f),
            new CurveKey(1f, 1f, 1f, 1f),
        }),

        // 요청서 §3의 견본 커브.
        ("hop_snappy", new[]
        {
            new CurveKey(0f, 0f, 0f, 2.6f),
            new CurveKey(0.4f, 0.9f, 0.8f, 0.3f),
            new CurveKey(1f, 1f, 0.1f, 0f),
        }),

        // 오버슈트: 값이 1을 넘었다 돌아온다 (Back 계열의 커스텀판).
        ("overshoot", new[]
        {
            new CurveKey(0f, 0f, 0f, 3f),
            new CurveKey(0.6f, 1.15f, 0f, 0f),
            new CurveKey(1f, 1f, 0f, 0f),
        }),

        // 다키 지그재그: 구간 탐색·경계 처리를 두들긴다.
        ("zigzag", new[]
        {
            new CurveKey(0f, 0f, 0f, 4f),
            new CurveKey(0.25f, 0.8f, -1f, -1f),
            new CurveKey(0.5f, 0.3f, 0f, 0f),
            new CurveKey(0.75f, 0.9f, 2f, 2f),
            new CurveKey(1f, 1f, 0f, 0f),
        }),

        // 무한 탄젠트 = 계단 구간 — AnimationCurve와 같은 규칙인지.
        ("stepped", new[]
        {
            new CurveKey(0f, 0f, 0f, float.PositiveInfinity),
            new CurveKey(0.5f, 0.7f, float.PositiveInfinity, 0f),
            new CurveKey(1f, 1f, 0f, 0f),
        }),
    };

    private static AnimationCurve ToAnimationCurve(CurveKey[] keys)
    {
        Keyframe[] frames = new Keyframe[keys.Length];

        for (int i = 0; i < keys.Length; i++)
            frames[i] = new Keyframe(keys[i].Time, keys[i].Value, keys[i].InTangent, keys[i].OutTangent);

        return new AnimationCurve(frames);
    }

    [Test]
    public void 대표_곡선_전_샘플에서_AnimationCurve와_오차가_한계_미만이다()
    {
        foreach ((string name, CurveKey[] keys) in RepresentativeCurves)
        {
            AnimationCurve unity = ToAnimationCurve(keys);

            for (int i = 0; i < SampleCount; i++)
            {
                float t = i / 256f;

                float expected = unity.Evaluate(t);
                float actual = CurveFunctions.Evaluate(keys, t);

                Assert.AreEqual(
                    expected, actual, Tolerance,
                    $"{name} @ t={t}: AnimationCurve={expected}, Core={actual}");
            }
        }
    }

    [Test]
    public void 범위_밖은_끝값으로_클램프한다()
    {
        CurveKey[] keys = RepresentativeCurves[1].keys; // hop_snappy

        Assert.AreEqual(keys[0].Value, CurveFunctions.Evaluate(keys, -0.5f), 0f);
        Assert.AreEqual(keys[keys.Length - 1].Value, CurveFunctions.Evaluate(keys, 1.5f), 0f);
    }

    [Test]
    public void 빈_커브와_단일_키는_안전하게_퇴화한다()
    {
        Assert.AreEqual(0f, CurveFunctions.Evaluate(null, 0.5f), 0f);
        Assert.AreEqual(0f, CurveFunctions.Evaluate(new CurveKey[0], 0.5f), 0f);
        Assert.AreEqual(0.3f, CurveFunctions.Evaluate(new[] { new CurveKey(0f, 0.3f, 0f, 0f) }, 0.9f), 0f);
    }
}
