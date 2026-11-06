using System.Collections.Generic;
using NUnit.Framework;
using Ked.Presentation.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ked.Presentation.Tests.UnityParity
{
    /// <summary>
    /// 무대 리그 리팩터의 오라클 — 런타임 샷 시스템이 무대에 쓰는 트랜스폼 전부(무대마다 카메라 2 + 층 5)가
    /// 코어 ShotLayoutMath.Solve의 출력과 같아야 한다. 본 무대는 Solve(intent), 덮는 무대는 Solve(Default)다(2026-11-06 역할).
    ///
    /// P1에서는 종전 런타임 식(PresentationResponseMath)과 코어 식이 같음을 고정했고,
    /// P2/P3(런타임이 코어 식을 쓰고 무대를 스키마가 세운 뒤)에도 같은 판정이 그대로 통과한다 — 동작 불변의 증명이다.
    ///
    /// 무대는 StageRigBuilder가 세운다(씬과 같은 경로). 세 무대 × 층 다섯.
    /// </summary>
    public sealed class ShotLayoutUnityParityTests
    {
        private const float Eps = 1e-4f;

        private static readonly string[] StageKeys = { "stage00", "stage01", "stage02" };

        private readonly List<GameObject> _spawned = new();

        private RectTransform _mount;
        private StageRigRegistry _stages;
        private PresentationShotResponseSystem _system;

        [SetUp]
        public void SetUp()
        {
            _mount = NewRoot("__StageViewportLayer");

            _stages = new StageRigRegistry(new StageRigBuilder());

            foreach (string key in StageKeys)
                _stages.Build(_mount, key, StageLayerLabels.Depths);

            _system = new PresentationShotResponseSystem(_stages, LayerProfileCurve.Default);
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

        private static readonly ShotIntentState[] Intents =
        {
            ShotIntentState.Default,
            new(6f, Vec2.Zero, Vec2.Zero),                               // 줌만
            new(0f, new Vec2(250f, -30f), Vec2.Zero),                    // 팬만
            new(6f, Vec2.Zero, new Vec2(100f, 0f)),                      // 줌 + 초점 오른쪽
            new(6f, new Vec2(250f, -30f), new Vec2(-100f, 40f)),         // 전부 + 초점 왼쪽 위
            new(-4f, new Vec2(-120f, 60f), new Vec2(0f, -200f)),         // 줌 아웃 + 초점 아래
        };

        [Test]
        public void 런타임이_무대에_쓰는_트랜스폼_전부가_코어_Solve와_같다()
        {
            foreach (ShotIntentState intent in Intents)
            {
                _system.Snap(intent.ToUnity());

                string tag = $"intent={{zoom {intent.Zoom}, pan {intent.PanInRigSpace}, focus {intent.FocusPointInRigSpace}}}";

                foreach (StageRigRefs stage in _stages.All)
                {
                    ShotIntentState followed = stage.Role == StageRole.Main ? intent : ShotIntentState.Default;
                    ShotLayout layout = ShotLayoutMath.Solve(in followed, stage.LayerDepths, LayerProfileCurve.Default);

                    Assert.That(stage.CameraZoom.localScale.x, Is.EqualTo(layout.CameraScale).Within(Eps), $"{tag} {stage.StageKey} 줌 배율 x");
                    Assert.That(stage.CameraZoom.localScale.y, Is.EqualTo(layout.CameraScale).Within(Eps), $"{tag} {stage.StageKey} 줌 배율 y");
                    Assert.That(stage.CameraPan.anchoredPosition.x, Is.EqualTo(layout.Pan.X).Within(Eps), $"{tag} {stage.StageKey} 팬 x");
                    Assert.That(stage.CameraPan.anchoredPosition.y, Is.EqualTo(layout.Pan.Y).Within(Eps), $"{tag} {stage.StageKey} 팬 y");

                    // 층 오프셋은 rig space 벡터를 층 부모(줌 루트) 공간으로 옮긴 값이다. 줌 루트 배율이 cameraScale이고
                    // 그 밖은 항등이므로 부모 공간 벡터 = rig space 벡터 / cameraScale.
                    for (int l = 0; l < stage.Layers.Count; l++)
                    {
                        StageLayerRefs layer = stage.Layers[l];
                        LayerResponse expected = layout.Layers[l];
                        string where = $"{tag} {stage.StageKey}/{StageLayerLabels.NameOf(layer.Depth)}";

                        Assert.That(layer.Rect.anchoredPosition.x, Is.EqualTo(expected.OffsetInRigSpace.X / layout.CameraScale).Within(Eps), $"{where} 위치 x");
                        Assert.That(layer.Rect.anchoredPosition.y, Is.EqualTo(expected.OffsetInRigSpace.Y / layout.CameraScale).Within(Eps), $"{where} 위치 y");
                        Assert.That(layer.Rect.localScale.x, Is.EqualTo(expected.Scale.X).Within(Eps), $"{where} 배율 x");
                        Assert.That(layer.Rect.localScale.y, Is.EqualTo(expected.Scale.Y).Within(Eps), $"{where} 배율 y");
                    }
                }
            }
        }

        [Test]
        public void 샷은_본_무대만_움직이고_덮는_무대는_중립에_고정이다()
        {
            _system.Snap(Intents[4].ToUnity());

            Assert.That(_stages.Get(StageRoles.MainStageKey).CameraZoom.localScale.x, Is.Not.EqualTo(1f), "본 무대는 따른다");

            foreach (StageRigRefs stage in _stages.All)
            {
                if (stage.Role == StageRole.Main)
                    continue;

                Assert.That(stage.CameraZoom.localScale, Is.EqualTo(Vector3.one), $"{stage.StageKey} 줌");
                Assert.That(stage.CameraPan.anchoredPosition, Is.EqualTo(Vector2.zero), $"{stage.StageKey} 팬");

                foreach (StageLayerRefs layer in stage.Layers)
                {
                    Assert.That(layer.Rect.anchoredPosition, Is.EqualTo(Vector2.zero), $"{stage.StageKey}/{StageLayerLabels.NameOf(layer.Depth)} 위치");
                    Assert.That(layer.Rect.localScale, Is.EqualTo(Vector3.one), $"{stage.StageKey}/{StageLayerLabels.NameOf(layer.Depth)} 배율");
                }
            }
        }

        [Test]
        public void 역할은_무대_키로_정한다()
        {
            Assert.That(_stages.Get("stage00").Role, Is.EqualTo(StageRole.Main));
            Assert.That(_stages.Get("stage01").Role, Is.EqualTo(StageRole.Overlay));
            Assert.That(_stages.Get("stage02").Role, Is.EqualTo(StageRole.Overlay));
        }

        [Test]
        public void 중립은_Solve_Default다()
        {
            // 샷을 한 번 움직인 뒤에도 중립은 Default의 값이어야 한다.
            _system.Snap(Intents[4].ToUnity());

            StageRigRefs stage = _stages.Get("stage01");
            ShotLayout neutral = ShotLayoutMath.Solve(ShotIntentState.Default, stage.LayerDepths, LayerProfileCurve.Default);

            Assert.That(_system.TryGetNeutral(stage.CameraPan, out Vector2 panPos, out _), Is.True);
            Assert.That(panPos.x, Is.EqualTo(neutral.Pan.X).Within(Eps));

            Assert.That(_system.TryGetNeutral(stage.CameraZoom, out _, out Vector3 zoomScale), Is.True);
            Assert.That(zoomScale.x, Is.EqualTo(neutral.CameraScale).Within(Eps));

            for (int l = 0; l < stage.Layers.Count; l++)
            {
                StageLayerRefs layer = stage.Layers[l];

                Assert.That(_system.TryGetNeutral(layer.Rect, out Vector2 pos, out Vector3 scale), Is.True, StageLayerLabels.NameOf(layer.Depth));
                Assert.That(pos.x, Is.EqualTo(neutral.Layers[l].OffsetInRigSpace.X).Within(Eps));
                Assert.That(scale.x, Is.EqualTo(neutral.Layers[l].Scale.X).Within(Eps));
            }

            // 무대 루트 · 마스크 · Content · 무대 밖은 샷 소유가 아니다.
            Assert.That(_system.TryGetNeutral(stage.Root, out _, out _), Is.False);
            Assert.That(_system.TryGetNeutral(stage.MaskRoot, out _, out _), Is.False);
            Assert.That(_system.TryGetNeutral(stage.Layers[0].Content, out _, out _), Is.False);
            Assert.That(_system.TryGetNeutral(_mount, out _, out _), Is.False);
        }

        // ── 하네스 경로 — 캡처한 무대 뷰가 접힌 샷과 등가다 ─────────────

        [Test]
        public void 캡처한_무대_뷰는_같은_의도를_코어로_푼_값과_등가다()
        {
            foreach (ShotIntentState intent in Intents)
            {
                _system.Snap(intent.ToUnity());

                StageViewSnapshot view = StageStateCapture.CaptureStageView(_stages);

                StageStateComparer.Result result = new();
                StageStateComparer.CompareStageView(in intent, view, LayerProfileCurve.Default, result);

                Assert.That(result.IsEquivalent, Is.True,
                    $"zoom {intent.Zoom}, pan {intent.PanInRigSpace}, focus {intent.FocusPointInRigSpace}\n" +
                    string.Join("\n", result.Mismatches));
                Assert.That(result.ComparedLayers, Is.EqualTo(StageKeys.Length * StageLayerLabels.Depths.Length));
            }
        }

        [Test]
        public void 층을_손으로_흔들면_하네스가_잡는다()
        {
            ShotIntentState intent = Intents[4];
            _system.Snap(intent.ToUnity());

            _stages.Get("stage01").GetLayer(StageLayerLabels.Close).Rect.anchoredPosition += new Vector2(5f, 0f);

            StageStateComparer.Result result = new();
            StageStateComparer.CompareStageView(in intent, StageStateCapture.CaptureStageView(_stages), LayerProfileCurve.Default, result);

            Assert.That(result.IsEquivalent, Is.False);
            Assert.That(result.Mismatches.Count, Is.EqualTo(1), string.Join("\n", result.Mismatches));
            Assert.That(result.Mismatches[0], Does.StartWith("stage:stage01/layer:1.center"));
        }

        [Test]
        public void 층_안의_점이_보이는_자리는_코어_합성식과_같다()
        {
            // 리그가 Content 아래에 서 있을 때 그 점이 무대 루트의 어디에 보이는가 — 툴 프리뷰가 쓰는 식.
            ShotIntentState intent = Intents[4];
            _system.Snap(intent.ToUnity());

            StageRigRefs stage = _stages.Get("stage00");

            foreach (StageLayerRefs layer in stage.Layers)
            {
                Vector3 p = new(120f, -260f, 0f);
                Vector3 seen = stage.Root.InverseTransformPoint(layer.Content.TransformPoint(p));

                LayerResponse response = ShotLayoutMath.SolveLayer(in intent, layer.Depth, LayerProfileCurve.Default);
                ShotLayoutMath.SolveCamera(in intent, out float cameraScale, out Vec2 pan);
                Vec2 expected = ShotLayoutMath.ProjectToRigSpace(new Vec2(p.x, p.y), cameraScale, pan, in response);

                Assert.That(seen.x, Is.EqualTo(expected.X).Within(1e-3f), StageLayerLabels.NameOf(layer.Depth));
                Assert.That(seen.y, Is.EqualTo(expected.Y).Within(1e-3f), StageLayerLabels.NameOf(layer.Depth));
            }
        }

        [Test]
        public void 나중에_선_층도_다음_적용부터_샷을_받는다()
        {
            ShotIntentState intent = Intents[4];
            _system.Snap(intent.ToUnity());

            StageLayerRefs late = _stages.EnsureLayer("stage00", 0.25f);

            Assert.That(late.Rect.localScale.x, Is.EqualTo(1f).Within(Eps), "선 직후는 항등");

            _system.Snap(intent.ToUnity());

            LayerResponse expected = ShotLayoutMath.SolveLayer(in intent, 0.25f, LayerProfileCurve.Default);
            Assert.That(late.Rect.localScale.x, Is.EqualTo(expected.Scale.X).Within(Eps));
            Assert.That(_system.TryGetNeutral(late.Rect, out _, out _), Is.True, "소유 목록에 들어갔다");
        }
    }
}
