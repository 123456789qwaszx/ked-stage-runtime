using System.Collections.Generic;
using Ked.Presentation.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ked.Presentation.Core.Tests.UnityParity
{
    /// <summary>
    /// 내보내기의 등가 — Animation 창이 보여 주는 모양(AnimationClip.SampleAnimation) = 재생이 타는 모양(MotionMath.Compose).
    /// 여기가 심판이다: 실제 리그를 세우고 실제 클립을 샘플링해 코어 합성과 대조한다.
    /// </summary>
    public sealed class MotionClipExporterUnityParityTests
    {
        private const float Tolerance = 1e-3f;

        private readonly List<Object> _spawned = new();
        private RectTransform _rigRoot;
        private CharacterRigRefs _refs;

        [SetUp]
        public void SetUp()
        {
            GameObject stageGo = new("__MotionParityStage", typeof(RectTransform));
            _spawned.Add(stageGo);

            CharacterRigBuilder builder = new();
            _rigRoot = builder.BuildCharacterRigRoot(null, "");
            _rigRoot.SetParent((RectTransform)stageGo.transform, false);
            builder.BindRefsFromRoot(_rigRoot, "", out _refs);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _spawned)
            {
                if (o != null)
                    Object.DestroyImmediate(o);
            }

            _spawned.Clear();
        }

        private AnimationClip NewClip()
        {
            AnimationClip clip = new();
            _spawned.Add(clip);
            return clip;
        }

        private string PathTo(RectTransform node) => AnimationUtility.CalculateTransformPath(node, _rigRoot);

        private static MotionChannelMode AllAbsolute(string node, MotionChannelKind kind) => MotionChannelMode.Absolute;

        // 내보내기는 리그를 rig-schemas.json으로 안다 — 테스트도 같은 길을 쓴다(실제 덤프).
        private static ICollection<string> CharacterNodeIds()
        {
            Assert.IsTrue(MotionClipExporter.TryLoadRigNodeIds("ExportedTuning/rig-schemas.json", out var rigs, out string error), error);
            Assert.IsTrue(rigs.ContainsKey("character"));
            return rigs["character"];
        }

        [Test]
        public void 클립의_키가_채널_성분으로_그대로_나오고_길이는_클립_길이다()
        {
            AnimationClip clip = NewClip();
            AnimationCurve y = new(new Keyframe(0f, 0f, 0f, -120f), new Keyframe(0.3f, -18f, 0f, 0f), new Keyframe(0.6f, 0f, 60f, 0f));
            AnimationCurve z = new(new Keyframe(0f, 3f), new Keyframe(0.6f, 0f));

            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(PathTo(_refs.CharacterPortrait_Track_Move_Y), typeof(RectTransform), "m_AnchoredPosition.y"), y);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(PathTo(_refs.CharacterPortrait_SwayPivot), typeof(Transform), "localEulerAnglesRaw.z"), z);

            MotionChannelMode Modes(string node, MotionChannelKind kind)
                => kind == MotionChannelKind.Position ? MotionChannelMode.Relative : MotionChannelMode.Absolute;

            Assert.IsTrue(MotionClipExporter.TryConvert(clip, "nod", "character", MotionEnd.Commit, Modes, CharacterNodeIds(), out MotionClip motion, out List<string> errors),
                string.Join("\n", errors));

            Assert.AreEqual(0.6f, motion.Duration, 1e-5f);
            Assert.AreEqual(MotionEnd.Commit, motion.End);
            Assert.AreEqual(2, motion.Channels.Length);

            MotionChannel pos = motion.Channels[0];
            Assert.AreEqual("CharacterPortrait_Track_Move_Y", pos.Node);
            Assert.AreEqual(MotionChannelKind.Position, pos.Kind);
            Assert.AreEqual(MotionChannelMode.Relative, pos.Mode);
            Assert.IsFalse(pos.HasX);
            Assert.AreEqual(3, pos.Y.Length);
            Assert.AreEqual(-120f, pos.Y[0].OutTangent, 0f);
            Assert.AreEqual(-18f, pos.Y[1].Value, 0f);

            MotionChannel rot = motion.Channels[1];
            Assert.AreEqual("CharacterPortrait_SwayPivot", rot.Node);
            Assert.AreEqual(MotionChannelKind.Euler, rot.Kind);
            Assert.IsTrue(rot.HasZ);
        }

        [Test]
        public void Animation_창이_보여_주는_모양과_코어_합성이_같다()
        {
            AnimationClip clip = NewClip();
            RectTransform node = _refs.CharacterPortrait_Track_Move;

            AnimationCurve x = new(new Keyframe(0f, 20f, 0f, 90f), new Keyframe(0.25f, 35f, -10f, -10f), new Keyframe(0.5f, 20f, 0f, 0f));
            AnimationCurve y = new(new Keyframe(0f, 0f), new Keyframe(0.5f, -12f));
            AnimationCurve sx = new(new Keyframe(0f, 1f), new Keyframe(0.5f, 1.3f));

            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(PathTo(node), typeof(RectTransform), "m_AnchoredPosition.x"), x);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(PathTo(node), typeof(RectTransform), "m_AnchoredPosition.y"), y);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(PathTo(node), typeof(Transform), "m_LocalScale.x"), sx);

            Assert.IsTrue(MotionClipExporter.TryConvert(clip, "sway", "character", MotionEnd.Commit, AllAbsolute, CharacterNodeIds(), out MotionClip motion, out List<string> errors),
                string.Join("\n", errors));

            MotionChannel pos = motion.Channels[0];
            MotionChannel scale = motion.Channels[1];
            Vec3 posStart = new(node.anchoredPosition.x, node.anchoredPosition.y, 0f);
            Vec3 scaleStart = new(node.localScale.x, node.localScale.y, node.localScale.z);

            for (int i = 0; i <= 32; i++)
            {
                float t = clip.length * i / 32f;
                clip.SampleAnimation(_rigRoot.gameObject, t);

                Vec3 expectPos = MotionMath.Compose(pos, posStart, t);
                Vec3 expectScale = MotionMath.Compose(scale, scaleStart, t);

                Assert.AreEqual(expectPos.X, node.anchoredPosition.x, Tolerance, $"x t={t}");
                Assert.AreEqual(expectPos.Y, node.anchoredPosition.y, Tolerance, $"y t={t}");
                Assert.AreEqual(expectScale.X, node.localScale.x, Tolerance, $"scale.x t={t}");
                Assert.AreEqual(scaleStart.Y, node.localScale.y, Tolerance, "키잉 안 한 성분은 그대로");
            }
        }

        [Test]
        public void 거부_사유를_전부_모아_돌려준다()
        {
            AnimationClip clip = NewClip();
            RectTransform node = _refs.CharSlot_Track_X;

            AnimationCurve plain = new(new Keyframe(0f, 0f), new Keyframe(0.5f, 10f));
            AnimationCurve weighted = new(new Keyframe(0f, 0f, 0f, 0f, 0f, 0.5f) { weightedMode = WeightedMode.Out }, new Keyframe(0.5f, 10f));

            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(PathTo(node), typeof(Transform), "m_LocalPosition.x"), plain);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(PathTo(_refs.CharSlot_Track_Focus), typeof(RectTransform), "m_AnchoredPosition.x"), plain);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(PathTo(_refs.CharSlot_Track_Y), typeof(RectTransform), "m_AnchoredPosition.y"), weighted);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(PathTo(_refs.CharSlot_Scale), typeof(Transform), "m_LocalScale.z"), plain);

            Assert.IsFalse(MotionClipExporter.TryConvert(clip, "bad", "character", MotionEnd.Commit, AllAbsolute, CharacterNodeIds(), out _, out List<string> errors));

            string all = string.Join("\n", errors);
            StringAssert.Contains("anchoredPosition", all);
            StringAssert.Contains("가중 탄젠트", all);
            StringAssert.Contains("z 성분", all);
            Assert.GreaterOrEqual(errors.Count, 3);

            // Track_Focus는 변환은 되지만 규칙에서 걸린다 — 다른 오류가 없을 때 드러난다.
            AnimationClip focusOnly = NewClip();
            AnimationUtility.SetEditorCurve(focusOnly, EditorCurveBinding.FloatCurve(PathTo(_refs.CharSlot_Track_Focus), typeof(RectTransform), "m_AnchoredPosition.x"), plain);
            Assert.IsFalse(MotionClipExporter.TryConvert(focusOnly, "focus", "character", MotionEnd.Commit, AllAbsolute, CharacterNodeIds(), out _, out errors));
            StringAssert.Contains("Track_Focus", string.Join("\n", errors));

            // restore인데 마지막 키가 남는다 — 선언과 키가 어긋나면 거부.
            AnimationClip stays = NewClip();
            AnimationUtility.SetEditorCurve(stays, EditorCurveBinding.FloatCurve(PathTo(node), typeof(RectTransform), "m_AnchoredPosition.x"), plain);
            Assert.IsFalse(MotionClipExporter.TryConvert(stays, "stays", "character", MotionEnd.Restore, (n, k) => MotionChannelMode.Relative, CharacterNodeIds(), out _, out errors));
            StringAssert.Contains("restore", string.Join("\n", errors));
        }

        [Test]
        public void 쓴_JSON을_로더가_같은_개수로_되읽는다()
        {
            AnimationClip clip = NewClip();
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(PathTo(_refs.CharSlot_Track_X), typeof(RectTransform), "m_AnchoredPosition.x"),
                new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 80f)));

            MotionChannelMode Relative(string n, MotionChannelKind k) => MotionChannelMode.Relative;

            Assert.IsTrue(MotionClipExporter.TryConvert(clip, "step", "character", MotionEnd.Commit, Relative, CharacterNodeIds(), out MotionClip motion, out List<string> errors), string.Join("\n", errors));

            string json = MotionClipExporter.ToJson(new[] { motion });
            MotionLibrary library = MotionLibrary.Parse(json, "test", CharacterRigSchema.NodeIds());

            Assert.AreEqual(1, library.Count);
            Assert.IsTrue(library.TryGet("step", out MotionClip back));
            Assert.AreEqual(motion.Duration, back.Duration, 1e-6f);
            Assert.AreEqual(MotionEnd.Commit, back.End);
            Assert.AreEqual(80f, back.Channels[0].X[1].Value, 1e-6f);
            Assert.AreEqual(MotionChannelMode.Relative, back.Channels[0].Mode);
        }
    }
}
