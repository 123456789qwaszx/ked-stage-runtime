using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Ked.Presentation.Core.Tests.UnityParity
{
    /// <summary>
    /// 전용 모션 툴(ked-motion-tool)이 내보낸 motions.json을 **런타임 길**로 읽으면 툴 미리보기와 같은 포즈가 나오는가 (M6).
    ///
    /// 견본 둘은 ToolExport~/에 있다(~ 폴더라 Unity가 가져오지 않는다). 원본은 툴 저장소
    /// tests/Mt.Authoring.Tests/Fixtures/stage-parity/ @ ea3d82d — 거기서 바뀌면 그대로 복사한다.
    ///   motions.json : 툴 산출물. sway(restore · 회전 z만 · 탄젠트 있음) · step(commit · 위치 x만 · 첫 키가 중립값이 아님)
    ///                  · grow(commit · 배율 x·y)
    ///   poses.json   : 0이 아닌 시작값 위에 relative·absolute로 얹은 툴 미리보기 포즈 34칸. 길이를 지난 6칸이 정지 프레임이다.
    ///
    /// 툴 쪽(StageParityTests, CI)은 같은 표를 툴 미리보기·반입 코어 리듀서로 지킨다. 여기서만 볼 수 있는 것은
    /// 런타임 로더(MotionLibrary — JsonUtility)와 런타임 리그 스키마(CharacterRigSchema)다.
    /// 시간에 따른 재생(MotionCommandCharR · DOTween)은 플레이모드라 여기서 보지 않는다.
    /// </summary>
    public sealed class ToolExportMotionParityTests
    {
        private const float Eps = 1e-3f;
        private const string SlotKey = "c1";

        [Serializable]
        public sealed class PosesFileDto
        {
            public string schema;
            public List<PoseDto> start;
            public List<PoseDto> samples;
        }

        [Serializable]
        public sealed class PoseDto
        {
            public string motion;
            public string mode;
            public float t;
            public string node;
            public string kind;
            public float x;
            public float y;
            public float z;
        }

        private static string FixturePath(string name)
            => Path.Combine(Application.dataPath, "Ked.Presentation", "Core", "Tests", "EditMode", "UnityParity", "ToolExport~", name);

        private static MotionLibrary LoadLibrary(List<string> logs)
        {
            void Collect(string message, string stackTrace, LogType type) => logs.Add($"{type}: {message}");

            Application.logMessageReceived += Collect;

            try
            {
                return MotionLibrary.Parse(File.ReadAllText(FixturePath("motions.json")), "ToolExport~/motions.json", CharacterRigSchema.NodeIds());
            }
            finally
            {
                Application.logMessageReceived -= Collect;
            }
        }

        private static PosesFileDto LoadPoses()
        {
            PosesFileDto poses = JsonUtility.FromJson<PosesFileDto>(File.ReadAllText(FixturePath("poses.json")));

            Assert.That(poses.schema, Is.EqualTo("motion-poses/1"));
            Assert.That(poses.start, Has.Count.EqualTo(3));
            Assert.That(poses.samples, Has.Count.EqualTo(34));
            return poses;
        }

        // ── 로더 ─────────────────────────────────────────────────────

        [Test]
        public void 런타임_로더가_툴_산출물을_실제_리그_스키마로_경고_없이_전부_읽는다()
        {
            List<string> logs = new();
            MotionLibrary library = LoadLibrary(logs);

            Assert.That(logs, Is.Empty, "툴이 낸 파일에서 로더가 소리를 냈다 — 툴에서는 되는데 재생에서 사라지는 모션이다");
            Assert.That(library.Count, Is.EqualTo(3));

            Assert.IsTrue(library.TryGet("sway", out MotionClip sway));
            Assert.That(sway.End, Is.EqualTo(MotionEnd.Restore));
            Assert.That(sway.Channels[0].Kind, Is.EqualTo(MotionChannelKind.Euler));
            Assert.IsFalse(sway.Channels[0].HasX, "생략한 성분은 없음이다");
            Assert.That(sway.Channels[0].Z[0].OutTangent, Is.EqualTo(40f));
            Assert.That(sway.Channels[0].Z[2].InTangent, Is.EqualTo(-40f));

            Assert.IsTrue(library.TryGet("step", out MotionClip step));
            Assert.That(step.End, Is.EqualTo(MotionEnd.Commit));
            Assert.That(step.Duration, Is.EqualTo(0.4f));
            Assert.IsFalse(step.Channels[0].HasY);

            Assert.IsTrue(library.TryGet("grow", out MotionClip grow));
            Assert.That(grow.Channels[0].Kind, Is.EqualTo(MotionChannelKind.Scale));
            Assert.IsFalse(grow.Channels[0].HasZ);
            Assert.That(grow.Channels[0].Y, Has.Length.EqualTo(3));
        }

        // ── 재생 중 포즈 ─────────────────────────────────────────────

        [Test]
        public void 로더가_읽은_클립으로_합성한_중간_포즈가_툴_미리보기_표와_같다()
        {
            MotionLibrary library = LoadLibrary(new List<string>());
            PosesFileDto poses = LoadPoses();
            int checkedCount = 0;

            foreach (PoseDto sample in poses.samples)
            {
                Assert.IsTrue(library.TryGet(sample.motion, out MotionClip clip), sample.motion);

                if (sample.t > clip.Duration)
                    continue;

                MotionChannel channel = ChannelOf(clip, sample);
                Assert.IsTrue(MotionModeToken.TryParse(sample.mode, out MotionChannelMode mode), sample.mode);

                Vec3 actual = MotionMath.Compose(channel, mode, StartOf(poses, sample), sample.t);
                AssertNear(sample, actual);
                checkedCount++;
            }

            Assert.That(checkedCount, Is.EqualTo(28));
        }

        // ── 정지 프레임 ──────────────────────────────────────────────

        [Test]
        public void 리듀서가_접은_끝_상태가_툴_미리보기의_끝_포즈와_같다()
        {
            string dumpPath = Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "ExportedTuning", "rig-schemas.json");

            if (!File.Exists(dumpPath))
                Assert.Inconclusive($"리그 스키마 덤프가 없다: {dumpPath}");

            MotionLibrary library = LoadLibrary(new List<string>());
            PosesFileDto poses = LoadPoses();

            StageReducerTuning tuning = new()
            {
                RigSchemas = JsonUtility.FromJson<RigSchemasFileDto>(File.ReadAllText(dumpPath)),
                ReferenceStageWidth = 1920f,
                BaseResolution = new Vec2(1920f, 1080f),
                RoleAnchors = new RoleAnchorTuningBodyDto(),
                Motions = library.Clips,
            };

            StageState spawned = StageReducer.Apply(
                StageReducer.CreateInitialState(tuning),
                new StageCommand("slot", new[] { SlotKey }, "tool-export:1"),
                tuning);

            Assert.That(spawned.Unhandled, Is.Empty);

            foreach (PoseDto start in poses.start)
            {
                string key = StageState.NodeKeyOf(SlotKey, start.node);
                spawned.Nodes.SetState(key, WithValue(KindOf(start.kind), spawned.Nodes.GetState(key), VecOf(start)));
            }

            List<PoseDto> ends = poses.samples.Where(s => s.t > library.Clips[s.motion].Duration).ToList();
            Assert.That(ends, Has.Count.EqualTo(6));

            foreach (PoseDto sample in ends)
            {
                StageState folded = StageReducer.Apply(
                    spawned,
                    new StageCommand("motion", new[] { SlotKey, sample.motion, sample.mode }, "tool-export:2"),
                    tuning);

                Assert.That(folded.Unhandled, Is.Empty, $"{sample.motion} {sample.mode}");

                RectNodeState state = folded.Nodes.GetState(StageState.NodeKeyOf(SlotKey, sample.node));
                AssertNear(sample, ValueOf(KindOf(sample.kind), state));
            }
        }

        // ── 도우미 ───────────────────────────────────────────────────

        private static MotionChannel ChannelOf(MotionClip clip, PoseDto sample)
        {
            MotionChannelKind kind = KindOf(sample.kind);

            foreach (MotionChannel channel in clip.Channels)
            {
                if (channel.Node == sample.node && channel.Kind == kind)
                    return channel;
            }

            Assert.Fail($"{sample.motion}: 채널 {sample.node} {sample.kind}가 없다");
            return default;
        }

        private static Vec3 StartOf(PosesFileDto poses, PoseDto sample)
        {
            PoseDto start = poses.start.FirstOrDefault(s => s.node == sample.node && s.kind == sample.kind);
            Assert.IsNotNull(start, $"시작값 표에 {sample.node} {sample.kind}가 없다");
            return VecOf(start);
        }

        private static Vec3 VecOf(PoseDto pose) => new(pose.x, pose.y, pose.z);

        private static MotionChannelKind KindOf(string token) => token switch
        {
            "position" => MotionChannelKind.Position,
            "scale" => MotionChannelKind.Scale,
            "euler" => MotionChannelKind.Euler,
            _ => throw new ArgumentOutOfRangeException(nameof(token), token, "poses.json: 모르는 kind"),
        };

        /// <summary>채널 종류가 노드 상태의 어느 자리인가 — MotionMath.StartOf·ClaimFor와 같은 대응(배율 z는 노드 것을 지킨다).</summary>
        private static RectNodeState WithValue(MotionChannelKind kind, in RectNodeState state, Vec3 value) => kind switch
        {
            MotionChannelKind.Position => state.WithAnchoredPosition(value.XY),
            MotionChannelKind.Scale => state.WithLocalScale(new Vec3(value.X, value.Y, state.LocalScale.Z)),
            _ => state.WithLocalEuler(value),
        };

        private static Vec3 ValueOf(MotionChannelKind kind, in RectNodeState state) => kind switch
        {
            MotionChannelKind.Position => new Vec3(state.AnchoredPosition, 0f),
            MotionChannelKind.Scale => state.LocalScale,
            _ => state.LocalEulerAngles,
        };

        private static void AssertNear(PoseDto sample, Vec3 actual)
        {
            string where = $"{sample.motion} {sample.mode} t={sample.t} {sample.node}";

            Assert.That(actual.X, Is.EqualTo(sample.x).Within(Eps), where + " x");
            Assert.That(actual.Y, Is.EqualTo(sample.y).Within(Eps), where + " y");
            Assert.That(actual.Z, Is.EqualTo(sample.z).Within(Eps), where + " z");
        }
    }
}
