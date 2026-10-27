using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ked.Presentation.Core.Tests.UnityParity
{
    /// <summary>
    /// 정착 측정의 신구 등가성.
    ///
    /// 종전 알고리즘("적용→측정→복원")을 이 파일 안에 오라클로 재현해, 실제 캐릭터 리그
    /// 위에서 새 어댑터와 값을 대조한다. 종전 구현을 지워 버리면 "같다"를 주장할 근거가
    /// 사라지므로, 지우기 전에 여기 박아 둔다.
    ///
    /// 그리고 이 단계의 진짜 수확 — 측정이 아무것도 쓰지 않는다는 것 — 을
    /// 전 계층 스냅샷으로 고정한다.
    /// </summary>
    public sealed class SettledLedgerUnityParityTests
    {
        private const float Eps = 0.01f;

        private readonly List<GameObject> _spawned = new();

        private RectTransform _stage;      // rigSpaceRoot 상당 (stopRoot)
        private RectTransform _rigRoot;
        private CharacterRigRefs _refs;

        [SetUp]
        public void SetUp()
        {
            GameObject stageGo = new("__LedgerParityStage", typeof(RectTransform));
            _spawned.Add(stageGo);

            _stage = (RectTransform)stageGo.transform;
            _stage.anchorMin = _stage.anchorMax = new Vector2(0.5f, 0.5f);
            _stage.pivot = new Vector2(0.5f, 0.5f);
            _stage.sizeDelta = new Vector2(1920f, 1080f);
            _stage.anchoredPosition = Vector2.zero;

            CharacterRigBuilder builder = new();
            _rigRoot = builder.BuildCharacterRigRoot();
            _rigRoot.SetParent(_stage, false);
            builder.BindRefsFromRoot(_rigRoot, "", out _refs);
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

        // ── 종전 알고리즘 오라클 ─────────────────────────────────────
        //
        // 원본 CharacterPlacementTargetLedger를 그대로 옮긴 것. 고치지 말 것 —
        // 이것이 "종전과 같다"의 기준이다.

        private sealed class LegacyLedger
        {
            private enum TargetKind { AnchoredPosition, LocalScale, LocalEuler }

            private readonly struct Entry
            {
                public readonly TargetKind kind;
                public readonly Vector3 value;

                public Entry(TargetKind kind, Vector3 value)
                {
                    this.kind = kind;
                    this.value = value;
                }
            }

            private readonly Dictionary<RectTransform, Entry> _targets = new();
            private readonly List<RectTransform> _scratchNodes = new(16);
            private readonly List<Entry> _scratchSaved = new(16);

            public void PublishAnchoredPosition(RectTransform node, Vector2 target)
                => _targets[node] = new Entry(TargetKind.AnchoredPosition, new Vector3(target.x, target.y, 0f));

            public void PublishLocalScale(RectTransform node, Vector2 target)
                => _targets[node] = new Entry(TargetKind.LocalScale, new Vector3(target.x, target.y, 0f));

            public void PublishLocalEuler(RectTransform node, Vector3 target)
                => _targets[node] = new Entry(TargetKind.LocalEuler, target);

            public Vector3 MeasureSettledWorldPoint(
                RectTransform measureRect, Vector3 localOffset, RectTransform stopRoot)
            {
                if (_targets.Count == 0)
                    return measureRect.TransformPoint(localOffset);

                _scratchNodes.Clear();
                _scratchSaved.Clear();

                Transform current = measureRect;

                while (current != null && current != stopRoot)
                {
                    if (current is RectTransform rect && _targets.TryGetValue(rect, out Entry entry))
                    {
                        _scratchNodes.Add(rect);
                        _scratchSaved.Add(CaptureLive(rect, entry.kind));
                        ApplyEntry(rect, entry);
                    }

                    current = current.parent;
                }

                Vector3 settledWorld = measureRect.TransformPoint(localOffset);

                for (int i = _scratchNodes.Count - 1; i >= 0; i--)
                    ApplyEntry(_scratchNodes[i], _scratchSaved[i]);

                _scratchNodes.Clear();
                _scratchSaved.Clear();

                return settledWorld;
            }

            public Vector2 WorldPointToSettledParentLocalPoint(
                RectTransform parentRect, Vector3 worldPoint, RectTransform stopRoot)
            {
                if (parentRect == null)
                    return Vector2.zero;

                if (_targets.Count == 0)
                {
                    Vector3 liveLocal = parentRect.InverseTransformPoint(worldPoint);
                    return new Vector2(liveLocal.x, liveLocal.y);
                }

                _scratchNodes.Clear();
                _scratchSaved.Clear();

                Transform current = parentRect;

                while (current != null && current != stopRoot)
                {
                    if (current is RectTransform rect && _targets.TryGetValue(rect, out Entry entry))
                    {
                        _scratchNodes.Add(rect);
                        _scratchSaved.Add(CaptureLive(rect, entry.kind));
                        ApplyEntry(rect, entry);
                    }

                    current = current.parent;
                }

                Vector3 settledLocal = parentRect.InverseTransformPoint(worldPoint);

                for (int i = _scratchNodes.Count - 1; i >= 0; i--)
                    ApplyEntry(_scratchNodes[i], _scratchSaved[i]);

                _scratchNodes.Clear();
                _scratchSaved.Clear();

                return new Vector2(settledLocal.x, settledLocal.y);
            }

            private static Entry CaptureLive(RectTransform rect, TargetKind kind)
            {
                switch (kind)
                {
                    case TargetKind.AnchoredPosition:
                        Vector2 ap = rect.anchoredPosition;
                        return new Entry(kind, new Vector3(ap.x, ap.y, 0f));

                    case TargetKind.LocalScale:
                        return new Entry(kind, rect.localScale);

                    case TargetKind.LocalEuler:
                        return new Entry(kind, rect.localEulerAngles);

                    default:
                        return new Entry(kind, Vector3.zero);
                }
            }

            private static void ApplyEntry(RectTransform rect, Entry entry)
            {
                switch (entry.kind)
                {
                    case TargetKind.AnchoredPosition:
                        rect.anchoredPosition = new Vector2(entry.value.x, entry.value.y);
                        break;

                    case TargetKind.LocalScale:
                        Vector3 s = rect.localScale;
                        rect.localScale = new Vector3(entry.value.x, entry.value.y, s.z);
                        break;

                    case TargetKind.LocalEuler:
                        rect.localEulerAngles = entry.value;
                        break;
                }
            }
        }

        // ── 등가성 ───────────────────────────────────────────────────

        [Test]
        public void 빈_장부는_라이브_측정과_같다()
        {
            CharacterPlacementTargetLedger ledger = new();

            Vector3 localOffset = new(0f, 950f, 0f);

            Vector3 actual = ledger.MeasureSettledWorldPoint(MeasureRect, localOffset, _stage);
            Vector3 expected = MeasureRect.TransformPoint(localOffset);

            AssertVector3(actual, expected, "빈 장부의 빠른 경로");
        }

        [Test]
        public void SetDepth_상당_예약에서_신구가_같다()
        {
            // 실사용 패턴: depthY에 위치, depthScale에 배율.
            RectTransform depthY = Rect(CharacterRigTarget.CharSlot_DepthY);
            RectTransform depthScale = Rect(CharacterRigTarget.CharSlot_DepthScale);

            Vector2 depthYTarget = new(0f, -320f);
            Vector2 depthScaleTarget = new(1.38f, 1.38f);

            CharacterPlacementTargetLedger fresh = new();
            fresh.PublishAnchoredPosition(depthY, depthYTarget);
            fresh.PublishLocalScale(depthScale, depthScaleTarget);

            LegacyLedger legacy = new();
            legacy.PublishAnchoredPosition(depthY, depthYTarget);
            legacy.PublishLocalScale(depthScale, depthScaleTarget);

            Vector3 localOffset = new(0f, 820f, 0f);

            Vector3 actual = fresh.MeasureSettledWorldPoint(MeasureRect, localOffset, _stage);
            Vector3 expected = legacy.MeasureSettledWorldPoint(MeasureRect, localOffset, _stage);

            AssertVector3(actual, expected, "SetDepth 상당");

            // 예약이 실제로 값을 바꿨는지 — 라이브와 같으면 이 테스트는 아무것도 검증하지 않는다.
            Vector3 live = MeasureRect.TransformPoint(localOffset);
            Assert.That((expected - live).magnitude, Is.GreaterThan(1f), "예약이 좌표를 바꿔야 유의미한 대조다");
        }

        [Test]
        public void MoveBy와_SetDepth가_겹친_실사용_패턴에서_신구가_같다()
        {
            RectTransform track = Rect(CharacterRigTarget.CharSlot_Track);
            RectTransform depthY = Rect(CharacterRigTarget.CharSlot_DepthY);
            RectTransform depthScale = Rect(CharacterRigTarget.CharSlot_DepthScale);

            // 라이브를 먼저 어질러 둔다 — 예약과 라이브가 같으면 대조가 무의미하다.
            track.anchoredPosition = new Vector2(-120f, 30f);
            depthScale.localScale = new Vector3(0.9f, 0.9f, 1f);

            CharacterPlacementTargetLedger fresh = new();
            LegacyLedger legacy = new();

            fresh.PublishAnchoredPosition(track, new Vector2(260f, -15f));
            fresh.PublishAnchoredPosition(depthY, new Vector2(0f, 240f));
            fresh.PublishLocalScale(depthScale, new Vector2(1.14f, 1.14f));

            legacy.PublishAnchoredPosition(track, new Vector2(260f, -15f));
            legacy.PublishAnchoredPosition(depthY, new Vector2(0f, 240f));
            legacy.PublishLocalScale(depthScale, new Vector2(1.14f, 1.14f));

            Vector3 localOffset = new(-40f, 680f, 0f);

            Vector3 actual = fresh.MeasureSettledWorldPoint(MeasureRect, localOffset, _stage);
            Vector3 expected = legacy.MeasureSettledWorldPoint(MeasureRect, localOffset, _stage);

            AssertVector3(actual, expected, "MoveBy + SetDepth");
        }

        [Test]
        public void 회전_예약에서_신구가_같다()
        {
            // 실호출부는 아직 오일러를 게시하지 않는다. 그래도 규약이 살아 있음을 고정한다.
            RectTransform sway = Rect(CharacterRigTarget.CharSlot_SwayPivot);

            CharacterPlacementTargetLedger fresh = new();
            fresh.PublishLocalEuler(sway, new Vector3(0f, 0f, 24f));

            LegacyLedger legacy = new();
            legacy.PublishLocalEuler(sway, new Vector3(0f, 0f, 24f));

            Vector3 localOffset = new(60f, 480f, 0f);

            AssertVector3(
                fresh.MeasureSettledWorldPoint(MeasureRect, localOffset, _stage),
                legacy.MeasureSettledWorldPoint(MeasureRect, localOffset, _stage),
                "회전 예약");
        }

        [Test]
        public void 역방향_변환에서_신구가_같다()
        {
            RectTransform depthY = Rect(CharacterRigTarget.CharSlot_DepthY);
            RectTransform depthScale = Rect(CharacterRigTarget.CharSlot_DepthScale);

            CharacterPlacementTargetLedger fresh = new();
            fresh.PublishAnchoredPosition(depthY, new Vector2(0f, 440f));
            fresh.PublishLocalScale(depthScale, new Vector2(1.58f, 1.58f));

            LegacyLedger legacy = new();
            legacy.PublishAnchoredPosition(depthY, new Vector2(0f, 440f));
            legacy.PublishLocalScale(depthScale, new Vector2(1.58f, 1.58f));

            RectTransform parent = Rect(CharacterRigTarget.CharSlot_Track);
            Vector3 worldPoint = _stage.TransformPoint(new Vector3(180f, -220f, 0f));

            Vector2 actual = fresh.WorldPointToSettledParentLocalPoint(parent, worldPoint, _stage);
            Vector2 expected = legacy.WorldPointToSettledParentLocalPoint(parent, worldPoint, _stage);

            Assert.That(actual.x, Is.EqualTo(expected.x).Within(Eps), $"역방향 x — 신={actual} 구={expected}");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(Eps), $"역방향 y — 신={actual} 구={expected}");
        }

        // ── 이 단계의 수확 ───────────────────────────────────────────

        [Test]
        public void 측정이_리그를_바꾸지_않는다()
        {
            RectTransform depthY = Rect(CharacterRigTarget.CharSlot_DepthY);
            RectTransform depthScale = Rect(CharacterRigTarget.CharSlot_DepthScale);
            RectTransform track = Rect(CharacterRigTarget.CharSlot_Track);

            CharacterPlacementTargetLedger ledger = new();
            ledger.PublishAnchoredPosition(depthY, new Vector2(0f, -320f));
            ledger.PublishLocalScale(depthScale, new Vector2(1.38f, 1.38f));
            ledger.PublishAnchoredPosition(track, new Vector2(90f, 12f));

            Dictionary<RectTransform, string> before = Snapshot();

            ledger.MeasureSettledWorldPoint(MeasureRect, new Vector3(0f, 950f, 0f), _stage);
            ledger.WorldPointToSettledParentLocalPoint(track, _stage.TransformPoint(Vector3.zero), _stage);

            AssertSnapshotUnchanged(before, "정착 측정 후");
        }

        [Test]
        public void 종전은_측정_중_트랜스폼에_썼고_새_어댑터는_안_쓴다()
        {
            // 값 스냅샷만으로는 종전 구현을 탓할 수 없다 — 복원하므로 사후 값은 같다.
            // 쓰기 자체는 Transform.hasChanged로 관찰된다. 그 사이에 예외가 나면
            // 리그가 더럽게 남던 것이 종전 문제였고, 이 단계가 없앤 것이 그것이다.
            RectTransform depthY = Rect(CharacterRigTarget.CharSlot_DepthY);
            Vector2 target = new(0f, -320f);

            LegacyLedger legacy = new();
            legacy.PublishAnchoredPosition(depthY, target);

            ClearHasChanged();
            legacy.MeasureSettledWorldPoint(MeasureRect, Vector3.zero, _stage);

            Assert.That(depthY.hasChanged, Is.True,
                "오라클(종전 구현)은 측정 도중 실제로 트랜스폼에 쓴다");

            CharacterPlacementTargetLedger fresh = new();
            fresh.PublishAnchoredPosition(depthY, target);

            ClearHasChanged();
            fresh.MeasureSettledWorldPoint(MeasureRect, Vector3.zero, _stage);

            foreach (RectTransform rect in _stage.GetComponentsInChildren<RectTransform>(true))
            {
                Assert.That(rect.hasChanged, Is.False,
                    $"새 어댑터가 '{rect.name}'에 썼다 — 측정은 아무것도 쓰지 않아야 한다");
            }
        }

        private void ClearHasChanged()
        {
            foreach (RectTransform rect in _stage.GetComponentsInChildren<RectTransform>(true))
                rect.hasChanged = false;
        }

        [Test]
        public void stopRoot가_조상이_아니면_예외이고_리그는_불변이다()
        {
            GameObject strangerGo = new("__Stranger", typeof(RectTransform));
            _spawned.Add(strangerGo);
            RectTransform stranger = (RectTransform)strangerGo.transform;

            CharacterPlacementTargetLedger ledger = new();
            ledger.PublishAnchoredPosition(Rect(CharacterRigTarget.CharSlot_DepthY), new Vector2(0f, -320f));

            Dictionary<RectTransform, string> before = Snapshot();

            // 종전 구현은 null까지 타고 올라가 조용히 다른 값을 냈다. 이제는 소리를 낸다.
            Assert.Throws<System.InvalidOperationException>(
                () => ledger.MeasureSettledWorldPoint(MeasureRect, Vector3.zero, stranger));

            AssertSnapshotUnchanged(before, "예외 후");
        }

        [Test]
        public void 한_노드에_위치와_스케일이_겹치면_새_구현만_둘_다_반영한다()
        {
            // 이 단계의 유일한 의도된 동작 차이다.
            // 종전은 노드당 종류 하나만 담아 나중 게시가 앞의 것을 지웠다.
            // 실사용에서는 겹치지 않으므로(위치=track/depthY, 스케일=scale/depthScale)
            // 재생 결과는 같고, 겹치는 경우 새 쪽이 맞다.
            RectTransform node = Rect(CharacterRigTarget.CharSlot_Track);

            CharacterPlacementTargetLedger fresh = new();
            fresh.PublishAnchoredPosition(node, new Vector2(300f, 0f));
            fresh.PublishLocalScale(node, new Vector2(2f, 2f));

            LegacyLedger legacy = new();
            legacy.PublishAnchoredPosition(node, new Vector2(300f, 0f));
            legacy.PublishLocalScale(node, new Vector2(2f, 2f));

            Vector3 localOffset = new(0f, 950f, 0f);

            Vector3 actual = fresh.MeasureSettledWorldPoint(MeasureRect, localOffset, _stage);
            Vector3 legacyValue = legacy.MeasureSettledWorldPoint(MeasureRect, localOffset, _stage);

            // 종전은 위치 예약을 잃어 x 이동이 빠진다.
            Assert.That((actual - legacyValue).magnitude, Is.GreaterThan(1f),
                "겹칠 때는 값이 달라야 한다 — 같다면 이 함정이 아직 남아 있다는 뜻");

            // 새 쪽이 맞다: 위치·스케일 예약을 직접 적용한 참값과 일치해야 한다.
            Vector2 savedPos = node.anchoredPosition;
            Vector3 savedScale = node.localScale;

            node.anchoredPosition = new Vector2(300f, 0f);
            node.localScale = new Vector3(2f, 2f, savedScale.z);

            Vector3 truth = MeasureRect.TransformPoint(localOffset);

            node.anchoredPosition = savedPos;
            node.localScale = savedScale;

            AssertVector3(actual, truth, "겹친 예약의 참값");
        }

        // ── helper ───────────────────────────────────────────────────

        private Dictionary<RectTransform, string> Snapshot()
        {
            Dictionary<RectTransform, string> snapshot = new();

            foreach (RectTransform rect in _stage.GetComponentsInChildren<RectTransform>(true))
            {
                snapshot[rect] =
                    $"{rect.anchoredPosition:F6}|{rect.anchorMin:F6}|{rect.anchorMax:F6}|" +
                    $"{rect.pivot:F6}|{rect.sizeDelta:F6}|{rect.localScale:F6}|{rect.localEulerAngles:F6}";
            }

            return snapshot;
        }

        private void AssertSnapshotUnchanged(Dictionary<RectTransform, string> before, string what)
        {
            Dictionary<RectTransform, string> after = Snapshot();

            Assert.That(after.Count, Is.EqualTo(before.Count), $"{what}: 노드 수가 달라졌다");

            foreach (KeyValuePair<RectTransform, string> pair in before)
            {
                Assert.That(after.TryGetValue(pair.Key, out string now), Is.True,
                    $"{what}: '{pair.Key.name}'가 사라졌다");

                Assert.That(now, Is.EqualTo(pair.Value),
                    $"{what}: '{pair.Key.name}'의 상태가 바뀌었다 — 측정이 무대를 건드렸다");
            }
        }

        private static void AssertVector3(Vector3 actual, Vector3 expected, string what)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(Eps), $"{what} x — 신={actual} 구={expected}");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(Eps), $"{what} y — 신={actual} 구={expected}");
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(Eps), $"{what} z — 신={actual} 구={expected}");
        }
    }
}
