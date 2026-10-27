using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ked.Presentation.Core.Tests.UnityParity
{
    /// <summary>
    /// 캡처와 리듀서가 **같은 키 규약**을 쓰는지 판정한다.
    ///
    /// 이게 어긋나면 하네스가 전 노드를 불일치로 보고한다 — 값이 맞아도 소용없다.
    /// 실제 오브젝트 이름에는 role prefix가 붙으므로 이름 파싱으로 키를 잡으면
    /// 여기서 깨진다(S2에서 __root로 겪은 것과 같은 종류의 함정).
    /// </summary>
    public sealed class StageStateCaptureUnityParityTests
    {
        private const string SlotKey = "c1";

        private readonly List<GameObject> _spawned = new();

        private RectTransform _stage;
        private CharacterRigRegistry _registry;
        private CharacterRigRefs _refs;

        [SetUp]
        public void SetUp()
        {
            GameObject stageGo = new("__CaptureParityStage", typeof(RectTransform));
            _spawned.Add(stageGo);

            _stage = (RectTransform)stageGo.transform;
            _stage.anchorMin = _stage.anchorMax = new Vector2(0.5f, 0.5f);
            _stage.pivot = new Vector2(0.5f, 0.5f);
            _stage.sizeDelta = new Vector2(1920f, 1080f);
            _stage.anchoredPosition = Vector2.zero;

            // 실제 재생과 같은 경로: role prefix를 붙여 세운다.
            CharacterRigBuilder builder = new();
            RectTransform rigRoot = builder.BuildCharacterRigRoot(null, SlotKey);
            rigRoot.SetParent(_stage, false);
            builder.BindRefsFromRoot(rigRoot, SlotKey, out _refs);

            _registry = new CharacterRigRegistry();
            _registry.Register(SlotKey, _refs);
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

        private StageState Capture()
            => StageStateCapture.Capture(_registry, null, new Vec2(1920f, 1080f));

        // ── 키 규약 ──────────────────────────────────────────────────

        [Test]
        public void 오브젝트_이름에_role_prefix가_붙어도_키는_스키마_id다()
        {
            // 전제 확인: 실제 이름에는 prefix가 붙어 있다.
            RectTransform track = _refs.GetRect(CharacterRigTarget.CharSlot_Track);
            Assert.That(track.name, Does.Contain(SlotKey), "빌더가 role prefix를 붙였다");
            Assert.That(track.name, Is.Not.EqualTo("CharSlot_Track"));

            // 그래도 캡처 키는 스키마 id로 잡힌다 — 이름 파싱이 아니라 타입 접근이라서.
            StageState state = Capture();

            Assert.That(state.Nodes.Contains("c1/CharSlot_Track"), Is.True);
            Assert.That(state.Nodes.Contains("c1/__root"), Is.True);
        }

        [Test]
        public void 리듀서가_세운_키_집합과_캡처_키_집합이_같다()
        {
            // 하네스가 성립하는 조건이다. 어긋나면 전 노드가 불일치로 보고된다.
            string dumpPath = Path.Combine(
                Path.GetDirectoryName(Application.dataPath)!, "ExportedTuning", "rig-schemas.json");

            if (!File.Exists(dumpPath))
                Assert.Inconclusive($"리그 스키마 덤프가 없다: {dumpPath}");

            RigSchemasFileDto file = JsonUtility.FromJson<RigSchemasFileDto>(File.ReadAllText(dumpPath));

            StageReducerTuning tuning = new()
            {
                RigSchemas = file,
                ReferenceStageWidth = 1920f,
                BaseResolution = new Vec2(1920f, 1080f),
                RoleAnchors = new RoleAnchorTuningBodyDto(),
            };

            StageState folded = StageReducer.Apply(
                StageReducer.CreateInitialState(tuning),
                new StageCommand("slot", new[] { SlotKey }, "test:1"),
                tuning);

            HashSet<string> foldedKeys = new(folded.Nodes.Keys);
            HashSet<string> capturedKeys = new(Capture().Nodes.Keys);

            Assert.That(capturedKeys.Except(foldedKeys), Is.Empty,
                "캡처에만 있는 키 — 폴드가 못 세운 노드다");

            Assert.That(foldedKeys.Except(capturedKeys), Is.Empty,
                "폴드에만 있는 키 — 캡처가 못 잡은 노드다");
        }

        [Test]
        public void 슬롯_키가_노드_키의_prefix가_된다()
        {
            _registry.Register("c2", BuildExtraRig("c2"));

            StageState state = Capture();

            Assert.That(state.Slots, Is.EquivalentTo(new[] { "c1", "c2" }));
            Assert.That(state.Nodes.Contains("c1/CharSlot_Track"), Is.True);
            Assert.That(state.Nodes.Contains("c2/CharSlot_Track"), Is.True);
        }

        // ── 값 ───────────────────────────────────────────────────────

        [Test]
        public void 라이브_트랜스폼_값을_그대로_읽는다()
        {
            RectTransform track = _refs.GetRect(CharacterRigTarget.CharSlot_Track);
            track.anchoredPosition = new Vector2(123f, -45f);

            RectTransform scale = _refs.GetRect(CharacterRigTarget.CharSlot_Scale);
            scale.localScale = new Vector3(1.5f, 1.5f, 1f);

            StageState state = Capture();

            Assert.That(state.Nodes.GetState("c1/CharSlot_Track").AnchoredPosition,
                Is.EqualTo(new Vec2(123f, -45f)));
            Assert.That(state.Nodes.GetState("c1/CharSlot_Scale").LocalScale.XY,
                Is.EqualTo(new Vec2(1.5f, 1.5f)));
        }

        [Test]
        public void 부모_관계가_스키마와_같다()
        {
            StageState state = Capture();

            Assert.That(state.Nodes.GetParentKey("c1/__root"), Is.Null);
            Assert.That(state.Nodes.GetParentKey("c1/CharSlot_Track_Focus"), Is.EqualTo("c1/__root"));
            Assert.That(state.Nodes.GetParentKey("c1/CharSlot_Track"), Is.EqualTo("c1/CharSlot_Track_Idle"));
        }

        [Test]
        public void CanvasGroup_alpha를_읽고_없으면_기본값_1이다()
        {
            RectTransform spriteRoot = _refs.GetRect(CharacterRigTarget.CharacterPortraitSprite_Root);

            Assert.That(spriteRoot.TryGetComponent(out CanvasGroup group), Is.True,
                "스키마의 NeedsCanvasGroup 노드다");

            group.alpha = 0.25f;

            StageState state = Capture();

            Assert.That(state.GetAlpha("c1/CharacterPortraitSprite_Root"), Is.EqualTo(0.25f).Within(1e-4f));

            // CanvasGroup이 없는 노드는 기록하지 않는다 — 상태의 기본값이 곧 답이다.
            Assert.That(state.GetAlpha("c1/CharSlot_Track"), Is.EqualTo(1f));
        }

        [Test]
        public void 초상_스프라이트_루트는_alpha_0으로_태어난다()
        {
            // 스키마의 InitialCanvasGroupAlpha = 0. 이 값이 실재생 첫 판정의
            // 최대 불일치 클래스가 될 후보다 — 폴드가 스폰 시 이걸 반영해야 한다.
            StageState state = Capture();

            Assert.That(state.GetAlpha("c1/CharacterPortraitSprite_Root"), Is.EqualTo(0f),
                "리그가 세워진 직후 초상 루트는 보이지 않는다");
        }

        // ── 읽기 전용 ────────────────────────────────────────────────

        [Test]
        public void 캡처는_무대를_건드리지_않는다()
        {
            foreach (RectTransform rect in _stage.GetComponentsInChildren<RectTransform>(true))
                rect.hasChanged = false;

            Capture();

            foreach (RectTransform rect in _stage.GetComponentsInChildren<RectTransform>(true))
                Assert.That(rect.hasChanged, Is.False, $"'{rect.name}'에 썼다 — 캡처는 읽기 전용이다");
        }

        // ── helper ───────────────────────────────────────────────────

        private CharacterRigRefs BuildExtraRig(string slotKey)
        {
            CharacterRigBuilder builder = new();
            RectTransform root = builder.BuildCharacterRigRoot(null, slotKey);
            root.SetParent(_stage, false);
            builder.BindRefsFromRoot(root, slotKey, out CharacterRigRefs refs);

            return refs;
        }
    }
}
