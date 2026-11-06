using System.Collections.Generic;
using NUnit.Framework;
using Ked.Presentation.Core;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Ked.Presentation.Tests.UnityParity
{
    /// <summary>
    /// 무대 리그는 스키마가 소유한다 — 씬이 깨져도 빌더가 다시 세운다(캐릭터 리그 b470fc88과 같은 시험).
    /// </summary>
    public sealed class StageRigBuilderTests
    {
        private readonly List<GameObject> _spawned = new();
        private RectTransform _mount;
        private StageRigBuilder _builder;

        [SetUp]
        public void SetUp()
        {
            GameObject go = new("__Mount", typeof(RectTransform));
            _spawned.Add(go);
            _mount = (RectTransform)go.transform;
            _mount.sizeDelta = new Vector2(1920f, 1080f);
            _builder = new StageRigBuilder();
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

        [Test]
        public void 스키마대로_선다_카메라는_무대_아래_층은_줌_아래()
        {
            StageRigRefs refs = _builder.Build(_mount, "stage00", StageLayerLabels.Depths);

            Assert.That(refs.Root.parent, Is.EqualTo(_mount));
            Assert.That(refs.MaskRoot.parent, Is.EqualTo(refs.Root));
            Assert.That(refs.MaskEdgeRoot.parent, Is.EqualTo(refs.Root));
            Assert.That(refs.CameraPan.parent, Is.EqualTo(refs.MaskRoot));
            Assert.That(refs.CameraZoom.parent, Is.EqualTo(refs.CameraPan));

            Assert.That(refs.Layers.Count, Is.EqualTo(5));

            for (int i = 0; i < refs.Layers.Count; i++)
            {
                StageLayerRefs layer = refs.Layers[i];
                Assert.That(layer.Rect.parent, Is.EqualTo(refs.CameraZoom));
                Assert.That(layer.Content.parent, Is.EqualTo(layer.Rect));
                Assert.That(layer.Rect.GetSiblingIndex(), Is.EqualTo(i), "먼 층이 앞 형제 = 뒤에 그려진다");
                Assert.That(layer.Depth, Is.EqualTo(StageLayerLabels.Depths[i]));
            }

            Assert.That(refs.Layers[0].Rect.name, Is.EqualTo("stage00_Layer_far"));
            Assert.That(refs.Layers[0].Content.name, Is.EqualTo("stage00_Layer_far_Content"));

            Assert.That(refs.Mask.HasMask, Is.True);
            Assert.That(refs.MaskRoot.GetComponent<Mask>().showMaskGraphic, Is.False);
            Assert.That(refs.MaskEdgeRoot.GetComponent<StageMaskEdgeGraphic>(), Is.Not.Null);
        }

        [Test]
        public void 모든_노드는_항등이다()
        {
            StageRigRefs refs = _builder.Build(_mount, "stage00", StageLayerLabels.Depths);

            foreach (RectTransform rect in refs.Root.GetComponentsInChildren<RectTransform>(true))
            {
                Assert.That(rect.anchoredPosition, Is.EqualTo(Vector2.zero), rect.name);
                Assert.That(rect.localScale, Is.EqualTo(Vector3.one), rect.name);
                Assert.That(rect.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)), rect.name);
            }
        }

        [Test]
        public void 층은_참조될_때_서고_깊이_순서로_끼어든다()
        {
            StageRigRefs refs = _builder.Build(_mount, "stage00", new[] { StageLayerLabels.Far, StageLayerLabels.Close });

            StageLayerRefs mid = _builder.EnsureLayer(refs, StageLayerLabels.Mid);

            Assert.That(refs.Layers.Count, Is.EqualTo(3));
            Assert.That(refs.Layers[1], Is.SameAs(mid));
            Assert.That(mid.Rect.GetSiblingIndex(), Is.EqualTo(1));
            Assert.That(_builder.EnsureLayer(refs, StageLayerLabels.Mid), Is.SameAs(mid), "같은 d는 같은 층");

            StageLayerRefs odd = _builder.EnsureLayer(refs, 0.25f);
            Assert.That(odd.Rect.name, Is.EqualTo("stage00_Layer_0.25"));
            Assert.That(odd.Rect.GetSiblingIndex(), Is.EqualTo(2), "mid(0)와 close(1) 사이");
        }

        [Test]
        public void 있는_계층은_그대로_묶고_깨진_계층은_다시_세운다()
        {
            StageRigRefs first = _builder.Build(_mount, "stage00", StageLayerLabels.Depths);
            RectTransform pan = first.CameraPan;

            // 멀쩡한 계층 — 같은 노드를 묶는다.
            StageRigRefs again = _builder.BindRefsFromRoot(first.Root, "stage00");
            Assert.That(again.CameraPan, Is.SameAs(pan));
            Assert.That(again.Layers.Count, Is.EqualTo(5));

            // 층 하나를 줌 밖으로 옮긴다 — 계층이 깨졌다.
            first.Layers[2].Rect.SetParent(first.Root, false);

            StageRigRefs rebuilt = _builder.BindRefsFromRoot(first.Root, "stage00");
            Assert.That(rebuilt.CameraPan, Is.Not.SameAs(pan), "부수고 새로 세웠다");
            Assert.That(rebuilt.Layers.Count, Is.EqualTo(0), "층은 참조될 때 다시 선다");
            Assert.That(rebuilt.Mask.HasMask, Is.True);
        }

        [Test]
        public void 샷_소유는_카메라_둘과_층의_Rect뿐이다()
        {
            StageRigRefs refs = _builder.Build(_mount, "stage00", StageLayerLabels.Depths);

            Assert.That(refs.IsShotOwned(refs.CameraPan, out bool pan, out _, out _) && pan);
            Assert.That(refs.IsShotOwned(refs.CameraZoom, out _, out bool zoom, out _) && zoom);
            Assert.That(refs.IsShotOwned(refs.Layers[3].Rect, out _, out _, out StageLayerRefs layer) && layer == refs.Layers[3]);
            Assert.That(refs.IsShotOwned(refs.Layers[3].Content, out _, out _, out _), Is.False);
            Assert.That(refs.IsShotOwned(refs.Root, out _, out _, out _), Is.False);
            Assert.That(refs.IsShotOwned(refs.MaskRoot, out _, out _, out _), Is.False);
        }
    }
}
