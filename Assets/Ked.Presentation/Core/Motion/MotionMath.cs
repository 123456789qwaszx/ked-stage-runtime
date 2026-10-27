namespace Ked.Presentation.Core
{
    // ─────────────────────────────────────────────────────────────────
    // 모션 샘플링과 합성 — 정본은 여기다.
    //
    // 재생(호스트의 샘플러)과 툴 프리뷰와 정지 프레임(리듀서)이 전부 이 함수를 지난다.
    // 성분마다 CurveFunctions.Evaluate를 부르므로 AnimationCurve.Evaluate와 같은 값이다
    // (CurveFunctionsAnimationCurveParityTests).
    //
    // 규칙(EaseFunctions·CurveFunctions와 같다): 순수 함수, UnityEngine 타입 금지.
    // ─────────────────────────────────────────────────────────────────
    public static class MotionMath
    {
        /// <summary>
        /// 채널의 원시 값. 없는 성분은 0이다 — 합성(Compose)이 "건드리지 않는다"로 읽는다.
        /// seconds는 키 범위 밖이면 끝값으로 클램프된다(CurveFunctions 규칙).
        /// </summary>
        public static Vec3 Sample(in MotionChannel channel, float seconds)
            => new(
                channel.HasX ? CurveFunctions.Evaluate(channel.X, seconds) : 0f,
                channel.HasY ? CurveFunctions.Evaluate(channel.Y, seconds) : 0f,
                channel.HasZ ? CurveFunctions.Evaluate(channel.Z, seconds) : 0f);

        /// <summary>
        /// 시작값 위에 채널을 얹은 실제 값. 한 함수가 모션의 모양을 전부 정한다.
        ///   Offset   : start + sample (없는 성분은 +0)
        ///   Absolute : sample, 없는 성분은 start 유지
        /// </summary>
        public static Vec3 Compose(in MotionChannel channel, Vec3 start, float seconds)
        {
            Vec3 v = Sample(channel, seconds);

            if (channel.Mode == MotionChannelMode.Offset)
                return start + v;

            return new Vec3(
                channel.HasX ? v.X : start.X,
                channel.HasY ? v.Y : start.Y,
                channel.HasZ ? v.Z : start.Z);
        }

        /// <summary>
        /// 채널의 종점 — 마지막 키를 합성한 값. 규칙이 "마지막 키 Time = 길이"를 보장하므로
        /// Compose(duration)과 같지만, 길이를 몰라도 되게 키에서 직접 읽는다.
        /// </summary>
        public static Vec3 EndValue(in MotionChannel channel, Vec3 start)
        {
            Vec3 last = new(
                channel.HasX ? channel.X[channel.X.Length - 1].Value : 0f,
                channel.HasY ? channel.Y[channel.Y.Length - 1].Value : 0f,
                channel.HasZ ? channel.Z[channel.Z.Length - 1].Value : 0f);

            if (channel.Mode == MotionChannelMode.Offset)
                return start + last;

            return new Vec3(
                channel.HasX ? last.X : start.X,
                channel.HasY ? last.Y : start.Y,
                channel.HasZ ? last.Z : start.Z);
        }

        /// <summary>
        /// 채널의 클레임 — 장부 게시·트윈 종점·상태 폴드 세 갈래가 같은 값을 본다.
        /// 종류가 StageNodeClaimKind로 가는 자리는 여기 하나다.
        /// </summary>
        public static StageNodeClaim ClaimFor(in MotionChannel channel, string nodeKey, Vec3 start)
        {
            Vec3 end = EndValue(channel, start);

            return channel.Kind switch
            {
                MotionChannelKind.Position => StageNodeClaim.AnchoredPosition(nodeKey, end.XY),
                MotionChannelKind.Scale => StageNodeClaim.LocalScaleXY(nodeKey, end.XY),
                MotionChannelKind.Euler => StageNodeClaim.LocalEuler(nodeKey, end),
                _ => throw new System.ArgumentOutOfRangeException(nameof(channel), channel.Kind, "모르는 채널 종류"),
            };
        }

        /// <summary>채널 종류에 맞는 시작값을 노드 상태에서 읽는다 — Position은 z=0, Scale은 z를 그대로 든다.</summary>
        public static Vec3 StartOf(in MotionChannel channel, in RectNodeState state)
            => channel.Kind switch
            {
                MotionChannelKind.Position => new Vec3(state.AnchoredPosition, 0f),
                MotionChannelKind.Scale => state.LocalScale,
                MotionChannelKind.Euler => state.LocalEulerAngles,
                _ => throw new System.ArgumentOutOfRangeException(nameof(channel), channel.Kind, "모르는 채널 종류"),
            };
    }
}
