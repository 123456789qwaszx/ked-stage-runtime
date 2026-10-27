using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ked.Presentation.Core.Tests.UnityParity
{
    /// <summary>
    /// depth 보정과 배치 해의 신구 등가성.
    ///
    /// 종전 알고리즘 — 목표 depth를 실제 트랜스폼에 잠깐 쓰고 측정한 뒤 되돌리는 —
    /// 을 이 파일 안에 오라클로 재현해, 실제 캐릭터 리그 위에서 코어 계산과 대조한다.
    /// 프로덕션에서 그 트릭을 지웠으므로, 지우기 전의 기준을 여기 박아 둔다.
    ///
    /// 대조하는 것은 수학이다. 입력 수집(facing·튜닝 조회·체인 캡처)은 프로덕션
    /// 경로가 그대로 쓰고, 컴파일과 등가성 하네스가 나중에 덮는다.
    /// </summary>
    public sealed class DepthFocusUnityParityTests
    {
        private const float Eps = 0.01f;

        private readonly List<GameObject> _spawned = new();

        private RectTransform _rigSpaceRoot;
        private CharacterRigRefs _refs;

        [SetUp]
        public void SetUp()
        {
            GameObject rootGo = new("__DepthParityRigSpaceRoot", typeof(RectTransform));
            _spawned.Add(rootGo);

            _rigSpaceRoot = (RectTransform)rootGo.transform;
            _rigSpaceRoot.anchorMin = _rigSpaceRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _rigSpaceRoot.pivot = new Vector2(0.5f, 0.5f);
            _rigSpaceRoot.sizeDelta = new Vector2(1920f, 1080f);
            _rigSpaceRoot.anchoredPosition = Vector2.zero;

            CharacterRigBuilder builder = new();
            RectTransform rigRoot = builder.BuildCharacterRigRoot();
            rigRoot.SetParent(_rigSpaceRoot, false);
            builder.BindRefsFromRoot(rigRoot, "", out _refs);
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

        private RectTransform Rect(CharacterRigTarget target) => _refs.GetRect(target);
        private RectTransform MeasureRect => Rect(CharacterRigTarget.CharacterPortrait_VisualOffset);
        private RectTransform DepthY => Rect(CharacterRigTarget.CharSlot_DepthY);
        private RectTransform DepthScale => Rect(CharacterRigTarget.CharSlot_DepthScale);
        private RectTransform MoveRect => Rect(CharacterRigTarget.CharSlot_Track_Focus);

        /// <summary>bust 상당 (ExportedTuning/presets/focus-tuning.json의 실값).</summary>
        private static readonly Vector3 BustOffset = new(0f, 820f, 0f);

        // ── 종전 알고리즘 오라클 ─────────────────────────────────────
        //
        // CharacterDepthResolver / CharacterFocusPlacementSolver의 종전 본문 그대로.
        // 고치지 말 것 — 이것이 "종전과 같다"의 기준이다.

        private Vector2 LegacyFocusInRigSpace(
            CharacterPlacementTargetLedger ledger, Vector3 focusLocalOffset)
        {
            Vector3 world = ledger.MeasureSettledWorldPoint(MeasureRect, focusLocalOffset, _rigSpaceRoot);
            Vector3 inRig = _rigSpaceRoot.InverseTransformPoint(world);

            return new Vector2(inRig.x, inRig.y);
        }

        private Vector2 LegacySolveDepthY(
            CharacterPlacementTargetLedger ledger,
            Vector3 focusLocalOffset,
            Vector2 rawDepthY,
            Vector2 targetDepthScale)
        {
            Vector2 currentFocus = LegacyFocusInRigSpace(ledger, focusLocalOffset);

            // ⚠ 적용 → 측정 → 복원. 이 사이에 예외가 나면 리그가 더럽게 남는다.
            Vector2 savedDepthY = DepthY.anchoredPosition;
            Vector3 savedDepthScale = DepthScale.localScale;

            DepthY.anchoredPosition = rawDepthY;
            DepthScale.localScale = new Vector3(targetDepthScale.x, targetDepthScale.y, savedDepthScale.z);

            Vector2 targetFocus = LegacyFocusInRigSpace(ledger, focusLocalOffset);

            DepthScale.localScale = savedDepthScale;
            DepthY.anchoredPosition = savedDepthY;

            Vector2 compensationInStageSpace = currentFocus - targetFocus;

            Vector2 compensationInDepthYParentSpace =
                PresentationCoordinateMath.ConvertVectorFromRigSpaceToTargetPositionParentSpace(
                    compensationInStageSpace,
                    _rigSpaceRoot,
                    DepthY.parent as RectTransform);

            return rawDepthY + compensationInDepthYParentSpace;
        }

        private Vector2 LegacySolvePlacement(
            CharacterPlacementTargetLedger ledger,
            Vector3 focusLocalOffset,
            Vector2 desiredFocusInRigSpace)
        {
            RectTransform targetParent = MoveRect.parent as RectTransform;

            Vector2 currentFocus = LegacyFocusInRigSpace(ledger, focusLocalOffset);

            Vector2 currentInParent =
                PresentationCoordinateMath.ConvertPointFromRigSpaceToTargetPositionParentSpace(
                    currentFocus, _rigSpaceRoot, targetParent);

            Vector2 desiredInParent =
                PresentationCoordinateMath.ConvertPointFromRigSpaceToTargetPositionParentSpace(
                    desiredFocusInRigSpace, _rigSpaceRoot, targetParent);

            return MoveRect.anchoredPosition + (desiredInParent - currentInParent);
        }

        // ── 새 경로 (프로덕션이 쓰는 것과 같은 조합) ─────────────────

        private Vector2 CoreSolveDepthY(
            CharacterPlacementTargetLedger ledger,
            Vector3 focusLocalOffset,
            Vector2 rawDepthY,
            Vector2 targetDepthScale)
        {
            List<RectTransform> chainRects = new();
            RectNodeState[] chain = ledger.CaptureSettledChain(MeasureRect, _rigSpaceRoot, chainRects);

            int depthYIndex = chainRects.IndexOf(DepthY);
            int depthScaleIndex = chainRects.IndexOf(DepthScale);

            Assert.That(depthYIndex, Is.GreaterThanOrEqualTo(0), "depthY가 측정 체인에 있어야 한다");
            Assert.That(depthScaleIndex, Is.GreaterThanOrEqualTo(0), "depthScale이 측정 체인에 있어야 한다");

            Vec2 solved = SettledFocusMath.SolveDepthYPreservingFocus(
                chain,
                CharacterPlacementTargetLedger.SpaceOf(_rigSpaceRoot),
                depthYIndex,
                depthScaleIndex,
                new Vector2(focusLocalOffset.x, focusLocalOffset.y).ToCore(),
                rawDepthY.ToCore(),
                targetDepthScale.ToCore());

            return solved.ToUnity();
        }

        private Vector2 CoreSolvePlacement(
            CharacterPlacementTargetLedger ledger,
            Vector3 focusLocalOffset,
            Vector2 desiredFocusInRigSpace)
        {
            List<RectTransform> chainRects = new();
            RectNodeState[] chain = ledger.CaptureSettledChain(MeasureRect, _rigSpaceRoot, chainRects);

            int moveIndex = chainRects.IndexOf(MoveRect);

            Assert.That(moveIndex, Is.GreaterThanOrEqualTo(0), "이동 축이 측정 체인에 있어야 한다");

            Vec2 solved = SettledFocusMath.SolveFocusPlacement(
                chain,
                CharacterPlacementTargetLedger.SpaceOf(_rigSpaceRoot),
                moveIndex,
                new Vector2(focusLocalOffset.x, focusLocalOffset.y).ToCore(),
                desiredFocusInRigSpace.ToCore(),
                MoveRect.anchoredPosition.ToCore());

            return solved.ToUnity();
        }

        // ── depth 등가성 ─────────────────────────────────────────────

        [TestCase(240f, 1.14f, TestName = "back")]
        [TestCase(-320f, 1.38f, TestName = "front")]
        [TestCase(440f, 1.58f, TestName = "close")]
        [TestCase(480f, 1.00f, TestName = "far")]
        public void depth_보정이_종전과_같다(float rawY, float scale)
        {
            CharacterPlacementTargetLedger ledger = new();

            Vector2 raw = new(0f, rawY);
            Vector2 targetScale = new(scale, scale);

            Vector2 expected = LegacySolveDepthY(ledger, BustOffset, raw, targetScale);
            Vector2 actual = CoreSolveDepthY(ledger, BustOffset, raw, targetScale);

            AssertVector2(actual, expected, $"depth 보정 (rawY={rawY}, scale={scale})");
        }

        [Test]
        public void 정착_예약이_겹친_상태에서도_depth_보정이_종전과_같다()
        {
            // 실사용 조건: place_focus가 Track_Focus에 예약을 걸어 둔 채로 size가 온다.
            CharacterPlacementTargetLedger ledger = new();
            ledger.PublishAnchoredPosition(MoveRect, new Vector2(-460.8f, 60f));
            ledger.PublishLocalScale(Rect(CharacterRigTarget.CharSlot_Scale), new Vector2(1.2f, 1.2f));

            Vector2 raw = new(0f, -320f);
            Vector2 targetScale = new(1.38f, 1.38f);

            Vector2 expected = LegacySolveDepthY(ledger, BustOffset, raw, targetScale);
            Vector2 actual = CoreSolveDepthY(ledger, BustOffset, raw, targetScale);

            AssertVector2(actual, expected, "예약이 겹친 depth 보정");
        }

        [Test]
        public void 어질러진_라이브_상태에서도_depth_보정이_종전과_같다()
        {
            Rect(CharacterRigTarget.CharSlot_Track).anchoredPosition = new Vector2(120f, -30f);
            Rect(CharacterRigTarget.CharSlot_Scale).localScale = new Vector3(0.85f, 0.85f, 1f);
            DepthY.anchoredPosition = new Vector2(0f, 90f);
            DepthScale.localScale = new Vector3(1.05f, 1.05f, 1f);

            CharacterPlacementTargetLedger ledger = new();

            Vector2 raw = new(0f, 440f);
            Vector2 targetScale = new(1.58f, 1.58f);

            Vector2 expected = LegacySolveDepthY(ledger, BustOffset, raw, targetScale);
            Vector2 actual = CoreSolveDepthY(ledger, BustOffset, raw, targetScale);

            AssertVector2(actual, expected, "어질러진 라이브의 depth 보정");
        }

        // ── place 등가성 ─────────────────────────────────────────────

        [Test]
        public void 배치_해가_종전과_같다()
        {
            CharacterPlacementTargetLedger ledger = new();

            Vector2 desired = new(-460.8f, 172.8f);   // place_tl 상당

            Vector2 expected = LegacySolvePlacement(ledger, BustOffset, desired);
            Vector2 actual = CoreSolvePlacement(ledger, BustOffset, desired);

            AssertVector2(actual, expected, "배치 해");
        }

        [Test]
        public void 스케일_예약_아래에서도_배치_해가_종전과_같다()
        {
            CharacterPlacementTargetLedger ledger = new();
            ledger.PublishLocalScale(DepthScale, new Vector2(1.58f, 1.58f));

            Vector2 desired = new(268.8f, -172.8f);   // place_br 상당

            Vector2 expected = LegacySolvePlacement(ledger, BustOffset, desired);
            Vector2 actual = CoreSolvePlacement(ledger, BustOffset, desired);

            AssertVector2(actual, expected, "예약 아래 배치 해");
        }

        // ── 이 단계의 수확 ───────────────────────────────────────────

        [Test]
        public void 새_경로는_계산_중_무대를_건드리지_않는다()
        {
            CharacterPlacementTargetLedger ledger = new();

            foreach (RectTransform rect in _rigSpaceRoot.GetComponentsInChildren<RectTransform>(true))
                rect.hasChanged = false;

            CoreSolveDepthY(ledger, BustOffset, new Vector2(0f, -320f), new Vector2(1.38f, 1.38f));
            CoreSolvePlacement(ledger, BustOffset, new Vector2(300f, 0f));

            foreach (RectTransform rect in _rigSpaceRoot.GetComponentsInChildren<RectTransform>(true))
            {
                Assert.That(rect.hasChanged, Is.False,
                    $"'{rect.name}'에 썼다 — depth/place 계산은 아무것도 쓰지 않아야 한다");
            }
        }

        [Test]
        public void 종전_경로는_계산_중_무대를_건드렸다()
        {
            // 이 단계가 무엇을 없앴는지 못 박는다. 오라클은 목표 depth를 실제로 쓴다 —
            // 그 사이에 예외가 나면 리그가 더럽게 남았다.
            CharacterPlacementTargetLedger ledger = new();

            foreach (RectTransform rect in _rigSpaceRoot.GetComponentsInChildren<RectTransform>(true))
                rect.hasChanged = false;

            LegacySolveDepthY(ledger, BustOffset, new Vector2(0f, -320f), new Vector2(1.38f, 1.38f));

            Assert.That(DepthY.hasChanged || DepthScale.hasChanged, Is.True,
                "오라클(종전 구현)은 계산 도중 depth 축에 쓴다");
        }

        [Test]
        public void 계산_전후_리그_상태가_완전히_같다()
        {
            CharacterPlacementTargetLedger ledger = new();

            Dictionary<RectTransform, string> before = Snapshot();

            CoreSolveDepthY(ledger, BustOffset, new Vector2(0f, 440f), new Vector2(1.58f, 1.58f));
            CoreSolvePlacement(ledger, BustOffset, new Vector2(-200f, 100f));

            Dictionary<RectTransform, string> after = Snapshot();

            foreach (KeyValuePair<RectTransform, string> pair in before)
                Assert.That(after[pair.Key], Is.EqualTo(pair.Value), $"'{pair.Key.name}'의 상태가 바뀌었다");
        }

        // ── helper ───────────────────────────────────────────────────

        private Dictionary<RectTransform, string> Snapshot()
        {
            Dictionary<RectTransform, string> snapshot = new();

            foreach (RectTransform rect in _rigSpaceRoot.GetComponentsInChildren<RectTransform>(true))
            {
                snapshot[rect] =
                    $"{rect.anchoredPosition:F6}|{rect.sizeDelta:F6}|" +
                    $"{rect.localScale:F6}|{rect.localEulerAngles:F6}|{rect.pivot:F6}";
            }

            return snapshot;
        }

        private static void AssertVector2(Vector2 actual, Vector2 expected, string what)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(Eps), $"{what} x — 신={actual} 구={expected}");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(Eps), $"{what} y — 신={actual} 구={expected}");
        }
    }
}
