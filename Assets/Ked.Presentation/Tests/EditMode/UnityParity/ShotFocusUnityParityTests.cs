using System.Collections.Generic;
using NUnit.Framework;
using Ked.Presentation.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ked.Presentation.Tests.UnityParity
{
    /// <summary>
    /// shot_focus_to의 명중을 실제 계층에서 잰다. 측정(정착 사슬, 샷 노드는 중립) → 코어 목표 계산 →
    /// 샷 시스템 적용 → Unity 트랜스폼으로 본 그 점의 자리가 화면 지점이어야 한다. 리그가 어느 층에 서도.
    ///
    /// 무대는 StageRigBuilder, 샷은 PresentationShotResponseSystem, 리그는 CharacterRigBuilder — 조립과 같은 셋이다.
    /// 커맨드(ShotZoomFocusCommand)는 이 사슬을 잇기만 한다(측정 · 화면 지점 해석 · 목표 계산 · Follow).
    /// </summary>
    public sealed class ShotFocusUnityParityTests
    {
        private const float Eps = 0.01f;

        /// <summary>bust 상당 (ExportedTuning/presets/focus-tuning.json의 실값).</summary>
        private static readonly Vector3 BustOffset = new(0f, 820f, 0f);

        /// <summary>place_left 상당 자리 — 1920 × 0.24 왼쪽.</summary>
        private static readonly Vector2 LeftTrack = new(-460.8f, -520f);

        private readonly List<GameObject> _spawned = new();

        private StageRigRefs _stage;
        private PresentationShotResponseSystem _system;

        [SetUp]
        public void SetUp()
        {
            RectTransform mount = NewRoot("__StageViewportLayer");

            StageRigRegistry stages = new(new StageRigBuilder());
            _stage = stages.Build(mount, "stage00", StageLayerLabels.Depths);

            _system = new PresentationShotResponseSystem(stages, LayerProfileCurve.Default);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _spawned.Clear();
        }

        private RectTransform NewRoot(string name)
        {
            GameObject go = new(name, typeof(RectTransform));
            _spawned.Add(go);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1920f, 1080f);
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        // SetupCharRigCommand와 같은 배선: 층의 Content 아래에 세우고 무대 · 층을 적는다.
        private CharacterRigRefs BuildRigOn(StageLayerRefs layer, Vector2 trackPosition)
        {
            CharacterRigBuilder builder = new();
            RectTransform rigRoot = builder.BuildCharacterRigRoot();
            rigRoot.SetParent(layer.Content, false);

            builder.BindRefsFromRoot(rigRoot, "", out CharacterRigRefs refs);
            refs.StandOn(_stage, layer);

            refs.CharSlot_Track_Focus.anchoredPosition = trackPosition;
            return refs;
        }

        // CharacterFocusPointResolver(shot_focus_to)와 같은 측정 — 정착 월드 점을 무대 루트 로컬로.
        private Vector2 MeasureInStageSpace(CharacterRigRefs refs)
        {
            Vector3 world = refs.PlacementTargets.MeasureSettledWorldPoint(
                refs.CharacterPortrait_VisualOffset, BustOffset, refs.Stage.Root, _system);

            Vector3 local = refs.Stage.Root.InverseTransformPoint(world);
            return new Vector2(local.x, local.y);
        }

        // 지금 화면에 보이는 자리 — 샷이 쓴 계층을 Unity가 합성한 결과.
        private static Vector2 SeenNow(CharacterRigRefs refs)
        {
            Vector3 world = refs.CharacterPortrait_VisualOffset.TransformPoint(BustOffset);
            Vector3 local = refs.Stage.Root.InverseTransformPoint(world);
            return new Vector2(local.x, local.y);
        }

        // ShotZoomFocusCommand.BuildTargetState의 목표 계산 부분.
        private ShotIntentState FocusTo(CharacterRigRefs refs, Vector2 desired, float zoom)
            => ShotIntents.FocusTo(
                zoom,
                MeasureInStageSpace(refs).ToCore(),
                desired.ToCore(),
                refs.Layer.Depth,
                _system.LayerProfile);

        private static void AssertClose(Vector2 expected, Vector2 actual, string what)
        {
            Assert.That(Vector2.Distance(expected, actual), Is.LessThan(Eps),
                $"{what}: expected {expected}, actual {actual}");
        }

        [Test]
        public void 어느_층에_선_리그도_화면_지점에_명중한다()
        {
            Vector2[] desireds = { Vector2.zero, new(-460.8f, 0f), new(300f, 200f) };

            foreach (StageLayerRefs layer in _stage.Layers)
            {
                CharacterRigRefs refs = BuildRigOn(layer, LeftTrack);

                foreach (Vector2 desired in desireds)
                foreach (float zoom in new[] { 2.5f, 6f, -4f })
                {
                    _system.Snap(FocusTo(refs, desired, zoom).ToUnity());

                    AssertClose(desired, SeenNow(refs), $"{StageLayerLabels.NameOf(layer.Depth)} zoom={zoom} desired={desired}");
                }

                Object.DestroyImmediate(refs.RigRoot.gameObject);
            }
        }

        [Test]
        public void 카메라가_이미_움직였어도_같은_샷을_푼다()
        {
            // 일반 재생은 앞 샷이 끝난 카메라를, 압축 재생은 중립에서 시작한 카메라를 본다 — 같은 답이어야 한다.
            CharacterRigRefs refs = BuildRigOn(_stage.GetLayer(StageLayerLabels.Close), LeftTrack);

            ShotIntentState atNeutral = FocusTo(refs, Vector2.zero, 2.5f);

            _system.Snap(new ShotIntentState(6f, new Vec2(250f, -30f), new Vec2(-100f, 40f)).ToUnity());
            ShotIntentState atMoved = FocusTo(refs, Vector2.zero, 2.5f);

            Assert.That(atMoved.PanInRigSpace.X, Is.EqualTo(atNeutral.PanInRigSpace.X).Within(Eps));
            Assert.That(atMoved.PanInRigSpace.Y, Is.EqualTo(atNeutral.PanInRigSpace.Y).Within(Eps));
            Assert.That(atMoved.FocusPointInRigSpace.X, Is.EqualTo(atNeutral.FocusPointInRigSpace.X).Within(Eps));
            Assert.That(atMoved.FocusPointInRigSpace.Y, Is.EqualTo(atNeutral.FocusPointInRigSpace.Y).Within(Eps));
        }

        [Test]
        public void 카메라만_보고_푼_팬은_close에서_빗나간다()
        {
            // 종전 식(pan = desired − p × cameraScale)이 층 반응을 빼서 생긴 어긋남 — 이 목표 계산이 층을 보는 이유.
            CharacterRigRefs refs = BuildRigOn(_stage.GetLayer(StageLayerLabels.Close), LeftTrack);

            Vector2 p = MeasureInStageSpace(refs);
            float cameraScale = ShotIntentMath.EvaluateCameraScale(2.5f);

            _system.Snap(new ShotIntentState(2.5f, (Vector2.zero - p * cameraScale).ToCore(), p.ToCore()).ToUnity());

            Assert.That(Vector2.Distance(Vector2.zero, SeenNow(refs)), Is.GreaterThan(50f), $"seen={SeenNow(refs)}");
        }
    }
}
