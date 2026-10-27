using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ked.Presentation.Core.Tests.UnityParity
{
    /// <summary>
    /// 실제 덤프로 세운 코어 트리 vs 실제 빌더·프리팹으로 세운 유니티 리그.
    ///
    /// 익스포터(S1) · 좌표 수학(S1b) · 트리와 로더(S2)가 실제로 맞물리는지
    /// 판정하는 유일한 장치다. 셋 중 하나라도 어긋나면 여기서 소리가 난다:
    ///   - 덤프가 낡았다 (빌더·프리팹을 고치고 재내보내기를 잊음)
    ///   - 로더가 덤프 형식을 잘못 읽는다 (parent 규약·캡처 공간)
    ///   - 좌표 수학이 유니티와 다르다
    ///
    /// 프리팹은 덤프가 스스로 기록한 sourcePrefab에서 가져온다 —
    /// 익스포터가 무엇으로 세웠는지를 덤프 밖에서 다시 추측하지 않는다.
    /// </summary>
    public sealed class RigSchemaTreeUnityParityTests
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

        // ── 덤프 ─────────────────────────────────────────────────────

        private static string DumpPath
            => Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "ExportedTuning", "rig-schemas.json");

        private static RigSchemasFileDto LoadDump()
        {
            if (!File.Exists(DumpPath))
            {
                Assert.Inconclusive(
                    $"리그 스키마 덤프가 없다: {DumpPath}\n" +
                    "메뉴 Ked/U12/Export Presentation Tuning Dump 를 먼저 실행할 것.");
            }

            RigSchemasFileDto file = JsonUtility.FromJson<RigSchemasFileDto>(File.ReadAllText(DumpPath));

            Assert.That(file, Is.Not.Null, "덤프 역직렬화 실패");
            Assert.That(file.rigs, Is.Not.Null.And.Not.Empty, "덤프에 rigs가 없다");

            return file;
        }

        private static RigSchemaRigDto FindRig(RigSchemasFileDto file, string rigKind)
        {
            foreach (RigSchemaRigDto rig in file.rigs)
            {
                if (rig.rigKind == rigKind)
                    return rig;
            }

            Assert.Fail($"덤프에 rigKind '{rigKind}'가 없다.");
            return null;
        }

        // ── 조립 ─────────────────────────────────────────────────────

        /// <summary>익스포터가 캡처할 때 쓴 것과 같은 공간: 덤프 크기, 가운데 pivot.</summary>
        private RectTransform CreateStage(Vec2 size)
        {
            GameObject go = new("__ParityStage", typeof(RectTransform));
            _spawned.Add(go);

            RectTransform stage = (RectTransform)go.transform;
            stage.anchorMin = stage.anchorMax = new Vector2(0.5f, 0.5f);
            stage.pivot = new Vector2(0.5f, 0.5f);
            stage.sizeDelta = new Vector2(size.X, size.Y);
            stage.anchoredPosition = Vector2.zero;

            return stage;
        }

        /// <summary>
        /// 익스포터와 같은 경로로 리그를 세운다: 실제 빌더 + 덤프가 기록한 프리팹 +
        /// BindRefsFromRoot(그래프 검증·복구)까지.
        /// </summary>
        private static RectTransform BuildLiveRig(RigSchemaRigDto rig, RectTransform stage)
        {
            RectTransform prefab = LoadPrefab(rig.sourcePrefab);

            switch (rig.rigKind)
            {
                case "character":
                {
                    CharacterRigBuilder builder = new();
                    RectTransform root = builder.BuildCharacterRigRoot(prefab);
                    root.SetParent(stage, false);
                    builder.BindRefsFromRoot(root, "", out _);
                    return root;
                }

                case "background":
                {
                    BackgroundRigBuilder builder = new();
                    RectTransform root = builder.BuildBackgroundRigRoot(prefab);
                    root.SetParent(stage, false);
                    builder.BindRefsFromRoot(root, "", out _);
                    return root;
                }

                case "screenEffect":
                {
                    ScreenEffectRigBuilder builder = new();
                    RectTransform root = builder.BuildRigRoot(prefab);
                    root.SetParent(stage, false);
                    builder.BindRefsFromRoot(root, out _);
                    return root;
                }

                default:
                    Assert.Fail($"모르는 rigKind: {rig.rigKind}");
                    return null;
            }
        }

        private static RectTransform LoadPrefab(string assetPath)
        {
            // 빈 경로는 "프리팹 없이 스키마 베이크로 세웠다"는 덤프의 기록이다(screenEffect).
            if (string.IsNullOrEmpty(assetPath))
                return null;

            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            Assert.That(go, Is.Not.Null,
                $"덤프가 기록한 프리팹을 찾지 못했다: {assetPath} — 덤프가 낡았거나 에셋이 옮겨졌다.");

            return go.transform as RectTransform;
        }

        private static RectTransform FindByName(Transform root, string name)
        {
            if (root.name == name)
                return root as RectTransform;

            for (int i = 0; i < root.childCount; i++)
            {
                RectTransform found = FindByName(root.GetChild(i), name);

                if (found != null)
                    return found;
            }

            return null;
        }

        // ── 대조 ─────────────────────────────────────────────────────

        [TestCase("character")]
        [TestCase("background")]
        [TestCase("screenEffect")]
        public void 덤프로_세운_트리가_실제_리그와_같다(string rigKind)
        {
            RigSchemasFileDto file = LoadDump();
            RigSchemaRigDto rig = FindRig(file, rigKind);

            RectNodeTree tree = RigSchemaLoader.BuildTree(file, rigKind);

            RectTransform stage = CreateStage(file.capturedUnderParentSize.ToVec2());
            RectTransform liveRoot = BuildLiveRig(rig, stage);

            // 샘플 점: 원점 + 비대칭 오프셋 둘. 스케일·회전·pivot이 섞인 노드에서
            // 원점만 보면 평행이동 오류밖에 못 잡는다.
            Vector3[] samples =
            {
                Vector3.zero,
                new(120f, -80f, 0f),
                new(-45f, 260f, 15f),
            };

            // 1패스: 덤프의 논리 키 → 실제 rect.
            // __root는 익스포터가 붙인 논리 키이지 오브젝트 이름이 아니다
            // (실제 이름은 CharacterRig / BackgroundRig / …). 리그 루트 자신을 가리킨다.
            Dictionary<string, RectTransform> liveByKey = new();

            foreach (RigSchemaNodeDto node in rig.nodes)
            {
                RectTransform live = node.id == RigSchemaLoader.RootKey
                    ? liveRoot
                    : FindByName(liveRoot, node.id);

                Assert.That(live, Is.Not.Null,
                    $"[{rigKind}] 덤프의 노드 '{node.id}'가 실제 리그에 없다 — 덤프가 낡았다.");

                liveByKey[node.id] = live;
            }

            int compared = 0;

            foreach (RigSchemaNodeDto node in rig.nodes)
            {
                RectTransform live = liveByKey[node.id];

                // 1) 부모 관계 — 이름이 아니라 참조로 본다.
                //    논리 키(__root)와 오브젝트 이름(CharacterRig)이 다르므로 이름 비교는 어긋난다.
                Transform expectedParent;

                if (string.IsNullOrEmpty(node.parent))
                {
                    expectedParent = stage;
                }
                else
                {
                    Assert.That(liveByKey.ContainsKey(node.parent), Is.True,
                        $"[{rigKind}] '{node.id}'의 부모 '{node.parent}'가 덤프의 노드 목록에 없다.");

                    expectedParent = liveByKey[node.parent];
                }

                Assert.That(live.parent, Is.SameAs(expectedParent),
                    $"[{rigKind}] '{node.id}'의 부모가 다르다 — 덤프의 트리 구조가 실제와 어긋난다. " +
                    $"덤프='{node.parent}' 실제='{live.parent.name}'");

                // 2) rect 크기 (스트레치 앵커면 사슬을 타야 나오는 값)
                Vec2 treeSize = tree.GetRectSize(node.id);

                Assert.That(treeSize.X, Is.EqualTo(live.rect.size.x).Within(Eps),
                    $"[{rigKind}] '{node.id}' rect 폭");
                Assert.That(treeSize.Y, Is.EqualTo(live.rect.size.y).Within(Eps),
                    $"[{rigKind}] '{node.id}' rect 높이");

                // 3) 양방향 좌표
                foreach (Vector3 sample in samples)
                {
                    Vector3 expected = stage.InverseTransformPoint(live.TransformPoint(sample));
                    Vec3 actual = tree.TransformPoint(node.id, new Vec3(sample.x, sample.y, sample.z));

                    AssertVec3(actual, expected, $"[{rigKind}] '{node.id}' TransformPoint({sample})");

                    Vector3 backExpected = live.InverseTransformPoint(stage.TransformPoint(expected));
                    Vec3 backActual = tree.InverseTransformPoint(
                        node.id, new Vec3(expected.x, expected.y, expected.z));

                    AssertVec3(backActual, backExpected,
                        $"[{rigKind}] '{node.id}' InverseTransformPoint");
                }

                compared++;
            }

            Assert.That(compared, Is.EqualTo(rig.nodes.Count));
            Assert.That(tree.Count, Is.EqualTo(rig.nodes.Count), "트리 노드 수가 덤프와 다르다");
        }

        [Test]
        public void 실제_리그에_있는_노드가_덤프에서_빠지지_않았다()
        {
            // 위 테스트는 "덤프의 노드가 리그에 있는가"만 본다. 반대 방향 —
            // 리그에 노드가 늘었는데 재내보내기를 잊은 경우는 여기서 잡는다.
            RigSchemasFileDto file = LoadDump();

            foreach (RigSchemaRigDto rig in file.rigs)
            {
                RectTransform stage = CreateStage(file.capturedUnderParentSize.ToVec2());
                RectTransform liveRoot = BuildLiveRig(rig, stage);

                int liveCount = liveRoot.GetComponentsInChildren<RectTransform>(true).Length;

                Assert.That(liveCount, Is.EqualTo(rig.nodes.Count),
                    $"[{rig.rigKind}] 실제 리그 노드 {liveCount}개 vs 덤프 {rig.nodes.Count}개 — " +
                    "리그가 바뀌었는데 재내보내기를 잊었을 수 있다.");
            }
        }

        [Test]
        public void 덤프의_measuredRectSize가_트리_계산과_일치한다()
        {
            // 익스포터가 실물에서 읽은 파생값 vs 코어가 사슬로 계산한 값.
            // 유니티 리그를 세우지 않고도 도는 검산이라, 로더·수학·덤프의
            // 어긋남을 가장 싸게 잡는다.
            RigSchemasFileDto file = LoadDump();

            foreach (RigSchemaRigDto rig in file.rigs)
            {
                RectNodeTree tree = RigSchemaLoader.BuildTree(file, rig.rigKind);

                foreach (RigSchemaNodeDto node in rig.nodes)
                {
                    Assert.That(node.measuredRectSize, Is.Not.Null,
                        $"[{rig.rigKind}] '{node.id}'에 measuredRectSize가 없다");

                    Vec2 computed = tree.GetRectSize(node.id);

                    Assert.That(computed.X, Is.EqualTo(node.measuredRectSize.x).Within(Eps),
                        $"[{rig.rigKind}] '{node.id}' 폭: 계산={computed.X} 덤프={node.measuredRectSize.x}");
                    Assert.That(computed.Y, Is.EqualTo(node.measuredRectSize.y).Within(Eps),
                        $"[{rig.rigKind}] '{node.id}' 높이: 계산={computed.Y} 덤프={node.measuredRectSize.y}");
                }
            }
        }

        private static void AssertVec3(Vec3 actual, Vector3 expected, string what)
        {
            Assert.That(actual.X, Is.EqualTo(expected.x).Within(Eps), $"{what} X — 코어={actual} 유니티={expected}");
            Assert.That(actual.Y, Is.EqualTo(expected.y).Within(Eps), $"{what} Y — 코어={actual} 유니티={expected}");
            Assert.That(actual.Z, Is.EqualTo(expected.z).Within(Eps), $"{what} Z — 코어={actual} 유니티={expected}");
        }
    }
}
