using System.Collections.Generic;
using NUnit.Framework;
using Ked.Presentation.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ked.Presentation.Tests.UnityParity
{
    /// <summary>
    /// 배치는 무대 좌표다 — 샷이 움직이는 무대 노드(카메라 팬 · 줌, 깊이 층 프레이밍)가
    /// 어떤 상태여도 place의 해와 초점 측정이 같아야 한다.
    ///
    /// 실제 씬처럼 기준 공간(StageShot_Root)과 리그 사이에 팬 · 줌 · 프레이밍 노드를 끼운다.
    /// 일반 재생은 샷 트윈이 막 시작한 카메라를, 압축 재생(롤백 · 이어하기)은 끝난 카메라를 본다 —
    /// 두 경우가 같은 답을 내는지가 이 파일의 질문이다.
    /// 무대 중립값은 가짜 <see cref="IStageViewNeutral"/>가 낸다(샷 시스템과 같은 정의: 손대지 않은 무대).
    /// </summary>
    public sealed class StageViewNeutralUnityParityTests
    {
        private const float Eps = 0.01f;

        /// <summary>bust 상당 (ExportedTuning/presets/focus-tuning.json의 실값).</summary>
        private static readonly Vector3 BustOffset = new(0f, 820f, 0f);

        /// <summary>왼쪽 지점 — 1920 × 0.24.</summary>
        private static readonly Vector2 DesiredLeft = new(-460.8f, 0f);

        private readonly List<GameObject> _spawned = new();

        private RectTransform _shotRoot;
        private RectTransform _pan;
        private RectTransform _zoom;
        private RectTransform _framingTransform;
        private RectTransform _framingScale;
        private CharacterRigRefs _refs;

        private FakeStageView _view;

        private sealed class FakeStageView : IStageViewNeutral
        {
            private readonly Dictionary<RectTransform, (Vector2 position, Vector3 scale)> _neutral = new();

            public void Add(RectTransform rect, Vector2 position, Vector3 scale) => _neutral[rect] = (position, scale);

            public bool TryGetNeutral(RectTransform rect, out Vector2 anchoredPosition, out Vector3 localScale)
            {
                if (rect != null && _neutral.TryGetValue(rect, out var value))
                {
                    anchoredPosition = value.position;
                    localScale = value.scale;
                    return true;
                }

                anchoredPosition = default;
                localScale = Vector3.one;
                return false;
            }
        }

        [SetUp]
        public void SetUp()
        {
            _shotRoot = NewRoot("__StageViewShotRoot");
            _pan = NewStretchChild("__Pan", _shotRoot);
            _zoom = NewStretchChild("__Zoom", _pan);
            _framingTransform = NewStretchChild("__FramingTransform", _zoom);
            _framingScale = NewStretchChild("__FramingScale", _framingTransform);

            _refs = BuildRigUnder(_framingScale);

            // 중립 = 손대지 않은 무대(지금 값). 이후 테스트가 카메라를 움직여도 중립은 그대로다.
            _view = new FakeStageView();
            _view.Add(_pan, Vector2.zero, Vector3.one);
            _view.Add(_zoom, Vector2.zero, Vector3.one);
            _view.Add(_framingTransform, Vector2.zero, Vector3.one);
            _view.Add(_framingScale, Vector2.zero, Vector3.one);
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

        // ── 무대 꾸미기 ─────────────────────────────────────────────

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

        // 무대 컨테이너는 스트레치 풀 — 중립에서 항등 트랜스폼이다(코어의 가정과 같다).
        private static RectTransform NewStretchChild(string name, RectTransform parent)
        {
            GameObject go = new(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        private static CharacterRigRefs BuildRigUnder(RectTransform parent)
        {
            CharacterRigBuilder builder = new();
            RectTransform rigRoot = builder.BuildCharacterRigRoot();
            rigRoot.SetParent(parent, false);
            builder.BindRefsFromRoot(rigRoot, "", out CharacterRigRefs refs);
            return refs;
        }

        // 샷이 의도 쪽으로 움직인 카메라 — 팬 · 줌 · 깊이 층 시차를 모두 건다.
        private void MoveCamera()
        {
            _pan.anchoredPosition = new Vector2(300f, -40f);
            _zoom.localScale = new Vector3(1.6f, 1.6f, 1f);
            _framingTransform.anchoredPosition = new Vector2(40f, 0f);
            _framingScale.localScale = new Vector3(1.1f, 1.1f, 1f);
        }

        // ── 프로덕션 경로와 같은 계산 ────────────────────────────────

        private static RectTransform MeasureRectOf(CharacterRigRefs refs)
            => refs.GetRect(CharacterRigNode.CharacterPortrait_VisualOffset);

        private static RectTransform MoveRectOf(CharacterRigRefs refs)
            => refs.GetRect(CharacterRigNode.CharSlot_Track_Focus);

        // CharacterFocusPlacementSolver와 같은 순서: 사슬 → 이동 축 색인 → SolveFocusPlacement.
        private static Vector2 SolvePlace(
            CharacterPlacementTargetLedger ledger,
            CharacterRigRefs refs,
            RectTransform root,
            IStageViewNeutral view)
        {
            List<RectTransform> chainRects = new();
            RectNodeState[] chain = ledger.CaptureSettledChain(MeasureRectOf(refs), root, chainRects, view);

            int moveIndex = chainRects.IndexOf(MoveRectOf(refs));
            Assert.That(moveIndex, Is.GreaterThanOrEqualTo(0), "이동 축이 사슬에 없다");

            return SettledFocusMath.SolveFocusPlacement(
                    chain,
                    CharacterPlacementTargetLedger.SpaceOf(root),
                    moveIndex,
                    new Vector2(BustOffset.x, BustOffset.y).ToCore(),
                    DesiredLeft.ToCore(),
                    MoveRectOf(refs).anchoredPosition.ToCore())
                .ToUnity();
        }

        // CharacterFocusPointResolver(shot_focus)와 같은 측정: 정착 월드 점 → 기준 공간 로컬.
        private static Vector2 MeasureFocus(
            CharacterPlacementTargetLedger ledger,
            CharacterRigRefs refs,
            RectTransform root,
            IStageViewNeutral view)
        {
            Vector3 world = ledger.MeasureSettledWorldPoint(MeasureRectOf(refs), BustOffset, root, view);
            Vector3 local = root.InverseTransformPoint(world);
            return new Vector2(local.x, local.y);
        }

        private static void AssertClose(Vector2 expected, Vector2 actual, string what)
        {
            Assert.That(Vector2.Distance(expected, actual), Is.LessThan(Eps),
                $"{what}: expected {expected}, actual {actual}");
        }

        // ── 배치 ────────────────────────────────────────────────────

        [Test]
        public void 무대_중립을_쓰면_place의_해는_카메라_상태와_무관하다()
        {
            CharacterPlacementTargetLedger ledger = new();

            Vector2 atNeutralCamera = SolvePlace(ledger, _refs, _shotRoot, _view);

            MoveCamera();
            Vector2 atMovedCamera = SolvePlace(ledger, _refs, _shotRoot, _view);

            AssertClose(atNeutralCamera, atMovedCamera, "일반 재생(카메라 출발 전)과 롤백(카메라 도착 후)의 해");
        }

        [Test]
        public void 무대_중립의_해는_카메라_노드가_없는_무대의_해와_같다()
        {
            // 코어 폴드의 모델 — 리그가 기준 공간 바로 아래에 선다(카메라 = 항등).
            RectTransform bareRoot = NewRoot("__StageViewBareRoot");
            CharacterRigRefs bareRefs = BuildRigUnder(bareRoot);

            Vector2 bare = SolvePlace(new CharacterPlacementTargetLedger(), bareRefs, bareRoot, null);

            MoveCamera();
            Vector2 withCamera = SolvePlace(new CharacterPlacementTargetLedger(), _refs, _shotRoot, _view);

            AssertClose(bare, withCamera, "툴 프리뷰(카메라 없음)와 런타임의 해");
        }

        [Test]
        public void 무대_중립이_없으면_카메라가_해를_바꾼다()
        {
            // 고치기 전의 동작 — 버그의 재현. 이 차이가 일반 재생과 롤백을 갈랐다.
            CharacterPlacementTargetLedger ledger = new();

            Vector2 atNeutralCamera = SolvePlace(ledger, _refs, _shotRoot, null);

            MoveCamera();
            Vector2 atMovedCamera = SolvePlace(ledger, _refs, _shotRoot, null);

            Assert.That(Vector2.Distance(atNeutralCamera, atMovedCamera), Is.GreaterThan(1f),
                "라이브 카메라를 읽으면 해가 카메라를 따라 움직여야 한다(재현 실패 — 픽스처를 확인)");
        }

        [Test]
        public void 리그_예약_끝값은_무대_중립과_함께_적용된다()
        {
            // 같은 줄의 size가 깊이 크기를 예약한 상태 — 예약은 끝값으로, 카메라는 중립으로.
            CharacterPlacementTargetLedger ledger = new();
            RectTransform depthScale = _refs.GetRect(CharacterRigNode.CharSlot_DepthScale);
            ledger.Publish(depthScale, NodeTarget.LocalScaleXY(depthScale.name, new Vector2(1.58f, 1.58f).ToCore()));

            Vector2 atNeutralCamera = SolvePlace(ledger, _refs, _shotRoot, _view);

            MoveCamera();
            Vector2 atMovedCamera = SolvePlace(ledger, _refs, _shotRoot, _view);

            AssertClose(atNeutralCamera, atMovedCamera, "예약이 있을 때의 해");

            Vector2 withoutReservation = SolvePlace(new CharacterPlacementTargetLedger(), _refs, _shotRoot, _view);
            Assert.That(Vector2.Distance(atMovedCamera, withoutReservation), Is.GreaterThan(1f),
                "예약 끝값이 무시됐다");
        }

        // ── 같은 줄의 다른 트윈이 체인 위에 있을 때 ───────────────────
        // 2026-08에 손으로 게시를 넣을 때 빠졌던 둘(슬라이드 · 회전). 기반이 게시하므로 이제 빠질 수 없지만,
        // "끝값을 게시하면 place가 트윈 중간값을 읽지 않는다"는 성질은 여기서 고정한다.

        [Test]
        public void 슬라이드_끝값을_게시하면_place는_화면_밖_출발점을_읽지_않는다()
        {
            // slide_in: 끝값 = 현재 위치. CreateTween이 rect를 출발점(화면 밖)으로 밀어 둔다.
            RectTransform track = _refs.GetRect(CharacterRigNode.CharSlot_Track);
            Vector2 settled = track.anchoredPosition;

            Vector2 expected = SolvePlace(new CharacterPlacementTargetLedger(), _refs, _shotRoot, _view);

            CharacterPlacementTargetLedger ledger = new();
            ledger.Publish(track, NodeTarget.AnchoredPosition(track.name, settled.ToCore()));
            track.anchoredPosition = settled + new Vector2(-960f, 0f);

            Vector2 actual = SolvePlace(ledger, _refs, _shotRoot, _view);

            AssertClose(expected, actual, "슬라이드 출발점에 있을 때 place의 해");

            Vector2 unreserved = SolvePlace(new CharacterPlacementTargetLedger(), _refs, _shotRoot, _view);
            Assert.That(Vector2.Distance(expected, unreserved), Is.GreaterThan(1f),
                "게시가 없으면 출발점을 읽어 해가 달라져야 한다(재현 실패 — 픽스처를 확인)");

            track.anchoredPosition = settled;
        }

        [Test]
        public void 회전_끝값을_게시하면_place는_회전_중간값을_읽지_않는다()
        {
            // rotate_by: SwayPivot은 발 피벗이라 각도에 따라 얼굴이 원호를 그린다.
            RectTransform sway = _refs.GetRect(CharacterRigNode.CharSlot_SwayPivot);
            Vector3 endEuler = new(0f, 0f, 24f);

            sway.localEulerAngles = endEuler;
            Vector2 expected = SolvePlace(new CharacterPlacementTargetLedger(), _refs, _shotRoot, _view);

            CharacterPlacementTargetLedger ledger = new();
            ledger.Publish(sway, NodeTarget.LocalEuler(sway.name, endEuler.ToCore()));
            sway.localEulerAngles = new Vector3(0f, 0f, 8f);

            Vector2 actual = SolvePlace(ledger, _refs, _shotRoot, _view);

            AssertClose(expected, actual, "회전 중간값에 있을 때 place의 해");

            sway.localEulerAngles = Vector3.zero;
        }

        // ── 초점 측정(shot_focus) ───────────────────────────────────

        [Test]
        public void 무대_중립을_쓰면_초점_측정은_카메라와_무관하다_예약이_없어도()
        {
            // 장부가 비어 있으면 측정이 라이브 지름길을 탔다 — 무대 중립이 있으면 타지 않아야 한다.
            CharacterPlacementTargetLedger ledger = new();

            Vector2 atNeutralCamera = MeasureFocus(ledger, _refs, _shotRoot, _view);

            MoveCamera();
            Vector2 atMovedCamera = MeasureFocus(ledger, _refs, _shotRoot, _view);

            AssertClose(atNeutralCamera, atMovedCamera, "초점의 무대 좌표");
        }
    }
}
