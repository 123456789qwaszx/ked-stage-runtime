using System;
using UnityEngine;

[Serializable]
public struct CharacterDepthPresetValue
{
    [Header("Depth Transform")]
    public Vector2 depthY;

    public float depthScale;

    [Header("Focus-Preserving Scale Pivot")]
    public CharacterFocusPreset preserveFocusPreset;

    public Vector2 preserveFocusOffset;

    public static CharacterDepthPresetValue Far => new()
    {
        depthY = new Vector2(0f, 480f),
        depthScale = 0.68f,
        preserveFocusPreset = CharacterFocusPreset.Feet,
        preserveFocusOffset = Vector2.zero,
    };

    public static CharacterDepthPresetValue Back => new()
    {
        depthY = new Vector2(0f, 240f),
        depthScale = 0.86f,
        preserveFocusPreset = CharacterFocusPreset.Bust,
        preserveFocusOffset = Vector2.zero,
    };

    public static CharacterDepthPresetValue Mid => new()
    {
        depthY = Vector2.zero,
        depthScale = 1f,
        preserveFocusPreset = CharacterFocusPreset.Bust,
        preserveFocusOffset = Vector2.zero,
    };

    public static CharacterDepthPresetValue Front => new()
    {
        depthY = new Vector2(0f, -320f),
        depthScale = 1.18f,
        preserveFocusPreset = CharacterFocusPreset.Bust,
        preserveFocusOffset = Vector2.zero,
    };

    public static CharacterDepthPresetValue Close => new()
    {
        depthY = new Vector2(0f, 440f),
        depthScale = 1.38f,
        preserveFocusPreset = CharacterFocusPreset.Face,
        preserveFocusOffset = Vector2.zero,
    };
}

[Serializable]
public struct CharacterDepthTuningSet
{
    public CharacterDepthPresetValue far;
    public CharacterDepthPresetValue back;
    public CharacterDepthPresetValue mid;
    public CharacterDepthPresetValue front;
    public CharacterDepthPresetValue close;

    public CharacterDepthPresetValue exp1;
    public CharacterDepthPresetValue exp2;

    public CharacterDepthPresetValue Get(CharacterDepthKey preset) => preset switch
    {
        CharacterDepthKey.None => mid,

        CharacterDepthKey.Far => far,
        CharacterDepthKey.Back => back,
        CharacterDepthKey.Mid => mid,
        CharacterDepthKey.Front => front,
        CharacterDepthKey.Close => close,

        CharacterDepthKey.Exp1 => exp1,
        CharacterDepthKey.Exp2 => exp2,

        _ => mid,
    };

    public static CharacterDepthTuningSet Default => new()
    {
        far = CharacterDepthPresetValue.Far,
        back = CharacterDepthPresetValue.Back,
        mid = CharacterDepthPresetValue.Mid,
        front = CharacterDepthPresetValue.Front,
        close = CharacterDepthPresetValue.Close,

        exp1 = CharacterDepthPresetValue.Mid,
        exp2 = CharacterDepthPresetValue.Mid,
    };
}

// 연속 numeric level(설계 구간 [0,20] 커브) 입력 모델. 라벨(far·mid…)도 이 커브 위의
// 눈금이므로(코어 DepthLevelLabels) 모든 depth가 이 한 장을 지난다.
// 구간 밖 수치도 받는다 — 끝 두 키의 할선으로 외삽한다.
[Serializable]
public struct CharacterDepthLevelTuningSet
{
    [Header("Level Curves")]
    [Tooltip("depth level에 대한 Y 커브입니다. 키 범위 밖의 값은 끝 키의 기울기로 외삽됩니다.")]
    public AnimationCurve yCurve;

    [Tooltip("depth level에 대한 Scale 커브입니다. 키 범위 밖의 값은 끝 키의 기울기로 외삽됩니다.")]
    public AnimationCurve scaleCurve;

    [Header("Focus Pivot")]
    public CharacterFocusPreset farPreserveFocus;
    public CharacterFocusPreset midPreserveFocus;
    public CharacterFocusPreset closePreserveFocus;
    public CharacterFocusPreset frontPreserveFocus;

    public CharacterDepthPresetValue Resolve(float level)
    {
        CharacterFocusPreset preserveFocus = ResolvePreserveFocus(level);

        float depthY = EvaluateUnclamped(yCurve, level);
        float depthScale = EvaluateUnclamped(scaleCurve, level);

        return new CharacterDepthPresetValue
        {
            depthY = new Vector2(0f, depthY),
            depthScale = Mathf.Max(0.0001f, depthScale),

            preserveFocusPreset = preserveFocus,
            preserveFocusOffset = Vector2.zero,
        };
    }

    private CharacterFocusPreset ResolvePreserveFocus(float level)
    {
        if (level <= 2.5f)
            return farPreserveFocus;

        if (level <= 6.5f)
            return midPreserveFocus;

        if (level <= 8.5f)
            return closePreserveFocus;

        return frontPreserveFocus;
    }

    public static CharacterDepthLevelTuningSet Default => new()
    {
        // 설계 구간은 [0,20]이다.
        // y는 종전 기울기 그대로 직선(-56/레벨).
        // scale은 close(레벨 10)에 무릎이 있다: 0~10은 종전 기울기(+0.052/레벨)라
        // 라벨 값이 그대로고, 10~20은 더 가파르다(+0.082/레벨 → 상한 2.2).
        yCurve = AnimationCurve.Linear(0f, 120f, 20f, -1000f),
        scaleCurve = new AnimationCurve(
            new Keyframe(0f, 0.86f, 0f, 0.052f),
            new Keyframe(10f, 1.38f, 0.052f, 0.082f),
            new Keyframe(20f, 2.2f, 0.082f, 0f)),

        farPreserveFocus = CharacterFocusPreset.Feet,
        midPreserveFocus = CharacterFocusPreset.Bust,
        closePreserveFocus = CharacterFocusPreset.Bust,
        frontPreserveFocus = CharacterFocusPreset.Face,
    };

    #region Curve Math
    private static float EvaluateUnclamped(AnimationCurve curve, float time)
    {
        if (curve == null || curve.length <= 0)
            return 0f;

        if (curve.length == 1)
            return curve.keys[0].value;

        Keyframe first = curve.keys[0];
        Keyframe last = curve.keys[curve.length - 1];

        if (time < first.time)
        {
            Keyframe next = curve.keys[1];
            float slope = CalculateSlope(first, next);
            return first.value + slope * (time - first.time);
        }

        if (time > last.time)
        {
            Keyframe prev = curve.keys[curve.length - 2];
            float slope = CalculateSlope(prev, last);
            return last.value + slope * (time - last.time);
        }

        return curve.Evaluate(time);
    }

    private static float CalculateSlope(Keyframe a, Keyframe b)
    {
        float dt = b.time - a.time;

        if (Mathf.Abs(dt) <= 0.0001f)
            return 0f;

        return (b.value - a.value) / dt;
    }
    #endregion
}

[CreateAssetMenu(menuName = "CPS/CharRig/Tuning/Character Depth Tuning", fileName = "CharacterDepthTuning")]
public sealed class CharacterDepthTuningSO : ScriptableObject
{
    [Header("Preset (사장 데이터 — 읽지 않는다)")]
    [Tooltip(
        "라벨이 level 커브의 눈금이 된 뒤로 이 표는 읽지 않는다. 에셋 호환을 위해 필드만 남겼다. " +
        "깊이 값을 바꾸려면 아래 Numeric Level 커브를 고쳐라.")]
    public CharacterDepthTuningSet presets = CharacterDepthTuningSet.Default;

    [Header("Numeric Level")]
    public CharacterDepthLevelTuningSet level = CharacterDepthLevelTuningSet.Default;

    /// <summary>
    /// 라벨(far·mid…)은 독립 프리셋이 아니라 level 커브 위의 눈금이다 —
    /// 코어 DepthLevelLabels가 그 눈금표이고, 재생·폴드·툴 프리뷰가 같은 커브를 지난다.
    /// (presets 표는 더 이상 읽지 않는다. 필드 주석 참조.)
    /// </summary>
    public CharacterDepthPresetValue ResolvePreset(CharacterDepthKey preset)
    {
        Ked.Presentation.Core.DepthLevelLabels.TryGetLevel(preset.ToString(), out float levelValue);

        return ResolveLevel(levelValue);
    }

    public CharacterDepthPresetValue ResolveLevel(float levelValue)
    {
        return level.Resolve(levelValue);
    }
}