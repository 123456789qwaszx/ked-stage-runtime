using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ked.Presentation.Core.Tests.UnityParity
{
    /// <summary>
    /// 실제 RectTransform 계층을 조립해 코어 수학과 대조한다.
    ///
    /// 순수 테스트(RectChainMathTests)와 역할이 다르다:
    ///   순수 테스트 실패 = 내 산수가 틀렸다
    ///   여기만 실패     = 유니티 규약을 잘못 알았다 (앵커 해석·회전 순서·pivot 규약)
    /// 두 실패를 갈라 보려고 나눠 둔다.
    ///
    /// 대조 방법: 코어의 "월드"는 rootSpace 로컬이므로 유니티 쪽은
    ///   root.InverseTransformPoint(leaf.TransformPoint(p))
    /// 로 같은 공간에 맞춘다. 그래서 루트를 옮기거나 돌려도 결과가 같아야 한다
    /// (루트를_옮기고_돌려도_코어_결과는_같다가 그걸 판정한다).
    ///
    /// ε = 0.01px — 체인이 얕아(≤ 6단) 잡음 상한이 그 정도다.
    /// 공식이 미묘하게 틀렸을 때 더 일찍 잡으려고 U14 판정(0.1px)보다 조인다.
    /// 근거는 Documentation~/transform-math-and-epsilon.md.
    /// </summary>
    public sealed class RectChainMathUnityParityTests
    {
        private const float Eps = 0.01f;

        private readonly List<GameObject> _spawned = new();

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

        // ── 조립 ─────────────────────────────────────────────────────

        private RectTransform CreateRect(string name, RectTransform parent)
        {
            GameObject go = new(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;

            if (parent == null)
                _spawned.Add(go);
            else
                rect.SetParent(parent, false);

            return rect;
        }

        /// <summary>
        /// 상태값을 rect에 입힌다. 순서가 중요하다 — 앵커·pivot·크기가 정해진 뒤라야
        /// anchoredPosition이 의도한 localPosition으로 풀린다.
        /// </summary>
        private static void Apply(RectTransform rect, in RectNodeState node)
        {
            rect.anchorMin = new Vector2(node.AnchorMin.X, node.AnchorMin.Y);
            rect.anchorMax = new Vector2(node.AnchorMax.X, node.AnchorMax.Y);
            rect.pivot = new Vector2(node.Pivot.X, node.Pivot.Y);
            rect.sizeDelta = new Vector2(node.SizeDelta.X, node.SizeDelta.Y);
            rect.anchoredPosition = new Vector2(node.AnchoredPosition.X, node.AnchoredPosition.Y);
            rect.localScale = new Vector3(node.LocalScale.X, node.LocalScale.Y, node.LocalScale.Z);
            rect.localEulerAngles = new Vector3(
                node.LocalEulerAngles.X, node.LocalEulerAngles.Y, node.LocalEulerAngles.Z);
        }

        /// <summary>
        /// 루트 + 체인을 세운다. 부모를 완전히 구성한 뒤 자식을 만든다 —
        /// 부모 rect가 나중에 바뀌면 자식의 localPosition 재계산이 레이아웃 시점으로
        /// 미뤄질 수 있어서, 그 창을 아예 만들지 않는다.
        /// </summary>
        private RectTransform BuildChain(Vec2 rootSize, Vec2 rootPivot, RectNodeState[] chain)
        {
            RectTransform root = CreateRect("__ParityRoot", null);

            // 루트는 고정 앵커 + sizeDelta로 크기를 못 박는다(부모가 없으므로 스트레치는 0이 된다).
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(rootPivot.X, rootPivot.Y);
            root.sizeDelta = new Vector2(rootSize.X, rootSize.Y);
            root.anchoredPosition = Vector2.zero;

            RectTransform current = root;

            for (int i = 0; i < chain.Length; i++)
            {
                RectTransform child = CreateRect($"n{i}", current);
                Apply(child, chain[i]);
                current = child;
            }

            return current; // leaf
        }

        // ── 대조 ─────────────────────────────────────────────────────

        private void AssertParity(
            Vec2 rootSize, Vec2 rootPivot, RectNodeState[] chain, params Vec3[] localPoints)
        {
            RectTransform leaf = BuildChain(rootSize, rootPivot, chain);
            RectTransform root = leaf.root as RectTransform;

            RectSpace space = new(rootSize, rootPivot);

            // 루트 rect가 의도한 크기·pivot인지 먼저 확인한다 —
            // 여기가 어긋나면 아래 비교는 다른 두 계산을 견주는 셈이 된다.
            Assert.That(root.rect.size.x, Is.EqualTo(rootSize.X).Within(Eps), "루트 폭");
            Assert.That(root.rect.size.y, Is.EqualTo(rootSize.Y).Within(Eps), "루트 높이");

            foreach (Vec3 p in localPoints)
            {
                Vector3 unityLocal = new(p.X, p.Y, p.Z);

                // 정변환: leaf 로컬 → 루트 로컬.
                Vector3 expected = root.InverseTransformPoint(leaf.TransformPoint(unityLocal));
                Vec3 actual = RectChainMath.TransformPoint(chain, space, p);

                AssertVec3(actual, expected, $"TransformPoint({p})");

                // 역변환: 루트 로컬 → leaf 로컬. 유니티가 낸 값을 그대로 입력으로 준다
                // (코어 정변환 결과를 넣으면 코어끼리의 왕복만 보게 된다).
                Vector3 expectedBack = leaf.InverseTransformPoint(root.TransformPoint(expected));
                Vec3 actualBack = RectChainMath.InverseTransformPoint(
                    chain, space, new Vec3(expected.x, expected.y, expected.z));

                AssertVec3(actualBack, expectedBack, $"InverseTransformPoint({p})");
            }
        }

        private static void AssertVec3(Vec3 actual, Vector3 expected, string what)
        {
            Assert.That(actual.X, Is.EqualTo(expected.x).Within(Eps), $"{what} X — 코어={actual} 유니티={expected}");
            Assert.That(actual.Y, Is.EqualTo(expected.y).Within(Eps), $"{what} Y — 코어={actual} 유니티={expected}");
            Assert.That(actual.Z, Is.EqualTo(expected.z).Within(Eps), $"{what} Z — 코어={actual} 유니티={expected}");
        }

        // ── 전제 ─────────────────────────────────────────────────────

        [Test]
        public void 유니티_rect가_상태값을_그대로_담는다()
        {
            // 이 하네스 전체가 딛고 선 전제다. 유니티가 대입값을 조정해 버리면
            // 아래 대조는 "코어 상태"와 "다른 유니티 상태"를 비교하게 된다.
            RectNodeState node = RectNodeState.StretchFull
                .WithAnchors(new Vec2(0.25f, 0f), new Vec2(0.75f, 1f))
                .WithPivot(new Vec2(0.5f, 0f))
                .WithSizeDelta(new Vec2(-40f, 20f))
                .WithAnchoredPosition(new Vec2(30f, -20f))
                .WithLocalScale(new Vec3(1.4f, 0.8f, 1f))
                .WithLocalEuler(new Vec3(0f, 0f, 25f));

            RectTransform leaf = BuildChain(new Vec2(1920f, 1080f), Vec2.Half, new[] { node });

            Assert.That(leaf.anchorMin, Is.EqualTo(new Vector2(0.25f, 0f)));
            Assert.That(leaf.anchorMax, Is.EqualTo(new Vector2(0.75f, 1f)));
            Assert.That(leaf.pivot, Is.EqualTo(new Vector2(0.5f, 0f)));
            Assert.That(leaf.sizeDelta, Is.EqualTo(new Vector2(-40f, 20f)));
            Assert.That(leaf.anchoredPosition, Is.EqualTo(new Vector2(30f, -20f)));

            // 코어 RectSize가 유니티 rect 크기와 같은가 — 앵커 해석의 직접 대조.
            Vec2 coreSize = RectChainMath.RectSize(new Vec2(1920f, 1080f), node);
            Assert.That(leaf.rect.size.x, Is.EqualTo(coreSize.X).Within(Eps));
            Assert.That(leaf.rect.size.y, Is.EqualTo(coreSize.Y).Within(Eps));
        }

        // ── 케이스 ───────────────────────────────────────────────────

        [Test]
        public void 스트레치_풀_3단_anchoredPosition()
        {
            RectNodeState[] chain =
            {
                RectNodeState.StretchFull.WithAnchoredPosition(new Vec2(10f, 0f)),
                RectNodeState.StretchFull.WithAnchoredPosition(new Vec2(20f, -15f)),
                RectNodeState.StretchFull.WithAnchoredPosition(new Vec2(5f, 5f)),
            };

            AssertParity(new Vec2(1920f, 1080f), Vec2.Half, chain,
                Vec3.Zero, new Vec3(100f, -60f, 0f));
        }

        [Test]
        public void 리그_모양_바닥_pivot과_스케일()
        {
            // CharSlot_DepthScale / SwayPivot / Scale 계열의 모양:
            // 바닥 pivot 노드가 연달아 오고 그 위에 스케일이 걸린다.
            RectNodeState[] chain =
            {
                RectNodeState.StretchFull.WithPivot(new Vec2(0.5f, 0f)),
                RectNodeState.StretchFull
                    .WithPivot(new Vec2(0.5f, 0f))
                    .WithLocalScale(new Vec3(0.82f, 0.82f, 1f)),
                RectNodeState.StretchFull
                    .WithPivot(new Vec2(0.5f, 0f))
                    .WithAnchoredPosition(new Vec2(0f, 24f)),
            };

            AssertParity(new Vec2(1920f, 1080f), Vec2.Half, chain,
                Vec3.Zero, new Vec3(0f, 300f, 0f), new Vec3(-120f, 40f, 0f));
        }

        [Test]
        public void Z_회전과_스케일_중첩()
        {
            RectNodeState[] chain =
            {
                RectNodeState.StretchFull
                    .WithLocalEuler(new Vec3(0f, 0f, 18f))
                    .WithLocalScale(new Vec3(1.3f, 0.7f, 1f)),
                RectNodeState.StretchFull
                    .WithPivot(new Vec2(0.5f, 0f))
                    .WithLocalEuler(new Vec3(0f, 0f, -35f)),
            };

            AssertParity(new Vec2(1000f, 600f), Vec2.Half, chain,
                Vec3.Zero, new Vec3(80f, 0f, 0f), new Vec3(0f, 200f, 0f));
        }

        [Test]
        public void 고정_앵커와_sizeDelta_초상화_이미지_모양()
        {
            // CharacterPortraitSprite_Image: 고정 앵커 + 폭만 sizeDelta로 준다.
            RectNodeState[] chain =
            {
                RectNodeState.StretchFull.WithPivot(new Vec2(0.5f, 0f)),
                RectNodeState.StretchFull
                    .WithAnchors(Vec2.Half, Vec2.Half)
                    .WithPivot(new Vec2(0.5f, 0f))
                    .WithSizeDelta(new Vec2(620.46f, 0f)),
            };

            AssertParity(new Vec2(1920f, 1080f), Vec2.Half, chain,
                Vec3.Zero, new Vec3(310f, 540f, 0f));
        }

        [Test]
        public void 부분_스트레치와_sizeDelta_오버레이_모양()
        {
            RectNodeState[] chain =
            {
                RectNodeState.StretchFull
                    .WithAnchors(new Vec2(0.1f, 0.2f), new Vec2(0.9f, 0.6f))
                    .WithSizeDelta(new Vec2(-64f, 32f))
                    .WithAnchoredPosition(new Vec2(12f, -8f)),
                RectNodeState.StretchFull.WithPivot(new Vec2(0f, 1f)),
            };

            AssertParity(new Vec2(1920f, 1080f), Vec2.Half, chain,
                Vec3.Zero, new Vec3(200f, -100f, 0f));
        }

        [Test]
        public void 오일러_3축_순서()
        {
            // 순수 테스트는 "구현 안에서 일관한가"까지만 본다.
            // 유니티 Quaternion.Euler의 실제 순서(Z→X→Y)와 같은지는 여기가 판정한다.
            RectNodeState[] chain =
            {
                RectNodeState.StretchFull.WithLocalEuler(new Vec3(30f, 40f, 50f)),
            };

            AssertParity(new Vec2(1000f, 600f), Vec2.Half, chain,
                new Vec3(100f, 0f, 0f), new Vec3(0f, 100f, 0f), new Vec3(40f, -70f, 25f));
        }

        [Test]
        public void 전_요소_혼합_5단()
        {
            RectNodeState[] chain =
            {
                RectNodeState.StretchFull
                    .WithAnchoredPosition(new Vec2(15f, -10f))
                    .WithLocalScale(new Vec3(1.2f, 1.2f, 1f)),
                RectNodeState.StretchFull
                    .WithAnchors(new Vec2(0.2f, 0.1f), new Vec2(0.8f, 0.9f))
                    .WithSizeDelta(new Vec2(-30f, 45f))
                    .WithPivot(new Vec2(0.5f, 0f)),
                RectNodeState.StretchFull
                    .WithLocalEuler(new Vec3(0f, 0f, 22f))
                    .WithAnchoredPosition(new Vec2(-40f, 60f)),
                RectNodeState.StretchFull
                    .WithAnchors(Vec2.Half, Vec2.Half)
                    .WithSizeDelta(new Vec2(320f, 480f))
                    .WithPivot(new Vec2(1f, 0.25f)),
                RectNodeState.StretchFull
                    .WithLocalScale(new Vec3(0.65f, 1.15f, 1f))
                    .WithLocalEuler(new Vec3(12f, -8f, 5f)),
            };

            AssertParity(new Vec2(1920f, 1080f), Vec2.Half, chain,
                Vec3.Zero, new Vec3(75f, -125f, 0f), new Vec3(-30f, 90f, 18f));
        }

        [Test]
        public void 루트_pivot이_가운데가_아니어도_같다()
        {
            RectNodeState[] chain =
            {
                RectNodeState.StretchFull.WithAnchoredPosition(new Vec2(40f, 25f)),
                RectNodeState.StretchFull
                    .WithAnchors(Vec2.One, Vec2.One)
                    .WithSizeDelta(new Vec2(200f, 100f)),
            };

            // 좌하단 pivot과 비대칭 pivot 둘 다. AssertParity가 매번 새 계층을 세우므로
            // 두 벌이 나란히 있어도 서로 간섭하지 않는다.
            AssertParity(new Vec2(1280f, 720f), new Vec2(0f, 0f), chain,
                Vec3.Zero, new Vec3(50f, -50f, 0f));

            AssertParity(new Vec2(1280f, 720f), new Vec2(0.25f, 0.75f), chain,
                Vec3.Zero, new Vec3(50f, -50f, 0f));
        }

        [Test]
        public void 루트를_옮기고_돌려도_코어_결과는_같다()
        {
            // 코어의 "월드"는 절대 월드가 아니라 rootSpace 로컬이다.
            // 호스트가 리그를 화면 어디에 놓든 코어 계산은 영향받지 않아야 한다.
            RectNodeState[] chain =
            {
                RectNodeState.StretchFull
                    .WithPivot(new Vec2(0.5f, 0f))
                    .WithAnchoredPosition(new Vec2(30f, 12f)),
                RectNodeState.StretchFull.WithLocalScale(new Vec3(1.5f, 1.5f, 1f)),
            };

            RectSpace space = new(new Vec2(1920f, 1080f), Vec2.Half);
            Vec3 point = new(60f, -45f, 0f);

            Vec3 core = RectChainMath.TransformPoint(chain, space, point);

            RectTransform leaf = BuildChain(new Vec2(1920f, 1080f), Vec2.Half, chain);
            RectTransform root = leaf.root as RectTransform;

            // 루트를 옮기고·돌리고·키운다.
            root.localPosition = new Vector3(300f, -220f, 15f);
            root.localEulerAngles = new Vector3(0f, 0f, 37f);
            root.localScale = new Vector3(1.7f, 1.7f, 1f);

            Vector3 expected = root.InverseTransformPoint(leaf.TransformPoint(
                new Vector3(point.X, point.Y, point.Z)));

            AssertVec3(core, expected, "루트 이동·회전·스케일 후");
        }
    }
}
