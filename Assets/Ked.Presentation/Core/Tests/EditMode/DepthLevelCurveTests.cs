using Ked.Presentation.Core;
using NUnit.Framework;
using UnityEngine;

// 숫자 레벨(size c1 5)의 커브 해석 — 코어 폴드 ↔ 런타임(CharacterDepthLevelTuningSet) 등가.
//
// 런타임은 AnimationCurve.Evaluate + 끝 두 키의 할선 외삽을 쓴다.
// 여기가 그 등가의 심판이다: AnimationCurve를 참조할 수 있는 유일한 자리.
public class DepthLevelCurveTests
{
    // 현행 덤프(ExportedTuning/presets/depth.json)의 level 커브와 같은 모양.
    private static CurveKey[] LinearY => new[]
    {
        new CurveKey(0f, 120f, 0f, -56f),
        new CurveKey(20f, -1000f, -56f, 0f),
    };

    // 끝 키 탄젠트와 할선이 어긋나는 커브 — 외삽 규칙을 실제로 가르는 경우다.
    private static CurveKey[] TangentUnlikeSecant => new[]
    {
        new CurveKey(0f, 100f, 0f, 0f),
        new CurveKey(5f, 50f, 0f, 0f),
        new CurveKey(10f, -200f, 0f, 0f),
    };

    private static AnimationCurve ToUnity(CurveKey[] keys)
    {
        Keyframe[] frames = new Keyframe[keys.Length];

        for (int i = 0; i < keys.Length; i++)
            frames[i] = new Keyframe(keys[i].Time, keys[i].Value, keys[i].InTangent, keys[i].OutTangent);

        return new AnimationCurve(frames);
    }

    /// <summary>런타임 CharacterDepthLevelTuningSet.EvaluateUnclamped와 같은 식.</summary>
    private static float RuntimeEvaluate(AnimationCurve curve, float time)
    {
        Keyframe first = curve.keys[0];
        Keyframe last = curve.keys[curve.length - 1];

        if (time < first.time)
        {
            Keyframe next = curve.keys[1];
            return first.value + (next.value - first.value) / (next.time - first.time) * (time - first.time);
        }

        if (time > last.time)
        {
            Keyframe prev = curve.keys[curve.length - 2];
            return last.value + (last.value - prev.value) / (last.time - prev.time) * (time - last.time);
        }

        return curve.Evaluate(time);
    }

    [TestCase(-5f)]
    [TestCase(0f)]
    [TestCase(2.5f)]
    [TestCase(5f)]
    [TestCase(7.5f)]
    [TestCase(10f)]
    [TestCase(15f)]
    public void 설계_구간_안팎_모두_런타임과_일치한다(float level)
    {
        foreach (CurveKey[] keys in new[] { LinearY, TangentUnlikeSecant })
        {
            AnimationCurve unity = ToUnity(keys);

            Assert.AreEqual(
                RuntimeEvaluate(unity, level),
                CurveFunctions.EvaluateUnclamped(keys, level),
                1e-3f,
                $"level={level}");
        }
    }

    [Test]
    public void 구간_밖은_탄젠트가_아니라_할선으로_외삽한다()
    {
        // 끝 두 키 (5,50) → (10,-200): 할선 -50/구간. 탄젠트는 0이다.
        // 탄젠트로 외삽하면 level 15에서 -200이 나오고, 할선이면 -450이다.
        float value = CurveFunctions.EvaluateUnclamped(TangentUnlikeSecant, 15f);

        Assert.AreEqual(-450f, value, 1e-3f, "할선 외삽이어야 한다");
        Assert.AreNotEqual(-200f, value, "끝 키 탄젠트로 외삽하면 안 된다");
    }

    [Test]
    public void 설계_구간_밖_레벨도_거부하지_않는다()
    {
        DepthLevelTuningDto level = new()
        {
            yCurve = Curve(LinearY),
            scaleCurve = Curve(new[]
            {
                new CurveKey(0f, 0.86f, 0f, 0.052f),
                new CurveKey(10f, 1.38f, 0.052f, 0f),
            }),
        };

        Assert.IsTrue(level.TryResolve(-3f, out float y, out float scale), "구간 밖 음수 레벨");
        Assert.AreEqual(120f + 56f * 3f, y, 1e-3f);
        Assert.Greater(scale, 0f);

        Assert.IsTrue(level.TryResolve(20f, out _, out float bigScale), "구간 밖 큰 레벨");
        Assert.AreEqual(0.86f + 0.052f * 20f, bigScale, 1e-3f);
    }

    [Test]
    public void 스케일은_런타임과_같은_하한을_갖는다()
    {
        // 런타임: Mathf.Max(0.0001f, depthScale).
        DepthLevelTuningDto level = new()
        {
            yCurve = Curve(LinearY),
            scaleCurve = Curve(new[]
            {
                new CurveKey(0f, 1f, 0f, -1f),
                new CurveKey(10f, 0f, -1f, 0f),
            }),
        };

        Assert.IsTrue(level.TryResolve(50f, out _, out float scale));
        Assert.AreEqual(0.0001f, scale, 0f, "음수 스케일은 하한으로 잘린다");
    }

    [Test]
    public void 커브가_없으면_지금처럼_거부한다()
    {
        Assert.IsFalse(new DepthLevelTuningDto().TryResolve(5f, out _, out _));
    }

    private static AnimationCurveDto Curve(CurveKey[] keys)
    {
        AnimationCurveDto dto = new();

        foreach (CurveKey k in keys)
            dto.m_Curve.Add(new KeyframeDto
            {
                time = k.Time, value = k.Value, inSlope = k.InTangent, outSlope = k.OutTangent,
            });

        return dto;
    }
}
