using System;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

/// <summary>
/// 유니티 벡터 ↔ 코어 값 타입 변환.
/// </summary>
public static class PresentationCoreConversions
{
    public static Vec2 ToCore(this Vector2 v) => new(v.x, v.y);

    public static Vec3 ToCore(this Vector3 v) => new(v.x, v.y, v.z);

    public static Vector2 ToUnity(this Vec2 v) => new(v.X, v.Y);

    public static Vector3 ToUnity(this Vec3 v) => new(v.X, v.Y, v.Z);

    /// <summary>코어 이징 → DOTween. 둘은 이름이 일대일이다(EaseFunctionsDOTweenParityTests).</summary>
    public static Ease ToDOTween(this EaseKind ease) => (Ease)Enum.Parse(typeof(Ease), ease.ToString());
}