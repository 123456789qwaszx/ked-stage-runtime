using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// ─────────────────────────────────────────────────────────────────────────────
// U12 — 연출 기준값 덤프.
//
// 코어(Ked.Presentation.Core)가 유니티 없이 무대를 재구성하려면 재료가 필요하다:
// 리그가 어떤 노드 트리인지, 각 노드의 초기 트랜스폼이 무엇인지, 프리셋 값이 얼마인지.
// 이 JSON들의 스키마가 곧 코어 Tuning 타입의 모양이 된다
// (코어 계약 2: 게임별 값은 코드가 아니라 데이터다).
//
// 원칙 셋:
// - 값은 지금 값 그대로. 이번에 튜닝하지 않는다.
// - 리그 스키마는 빌더 로직을 베껴 적지 않는다 — 실제 빌더로 리그를 세우고
//   실물 RectTransform에서 읽는다(사본 금지). 프리팹도 부트스트랩이 쓰는
//   바로 그 프리팹을 씬에서 꺼내 쓴다.
// - 씬에서는 본체 계약으로만 찾는다(IStageMaskProvider · IRigPrefabSource).
//   이 도구는 본체 어셈블리(Ked.Presentation.Editor)라 예제 타입을 모른다.
// - 내보낼 수 없는 항목은 건너뛰되 경고 목록에 남긴다. 조용히 빠뜨리지 않는다.
//
// 실행: 메뉴 Ked/U12/Export Presentation Tuning Dump
//       또는 batchmode -executeMethod PresentationTuningExporter.ExportAll
// 출력: <프로젝트 루트>/ExportedTuning/
// ─────────────────────────────────────────────────────────────────────────────
public static class PresentationTuningExporter
{
    private const string ScenePath = "Assets/Samples/Scenes/PresentationSample.unity";

    /// <summary>리그를 세울 때 쓰는 임시 부모. 캡처가 끝나면 지운다.</summary>
    private const string ExportStageName = "__U12ExportStage";

    /// <summary>덤프에서 리그 루트 노드의 키. 코어 RigSchemaLoader와의 규약이다.</summary>
    private const string RootNodeId = "__root";

    private static readonly (string group, string path)[] PresetAssets =
    {
        ("visual-focus",    "Assets/Ked.Presentation/Data/ScreenEffect/CharacterVisualFocusPresetDB.asset"),
        ("mask-motion",     "Assets/Ked.Presentation/Data/Generated/StageMaskMotionPresetDB.asset"),
        ("screen-flash",    "Assets/Ked.Presentation/Data/ScreenEffect/ScreenFlashPresetDB.asset"),
        ("screen-noise",    "Assets/Ked.Presentation/Data/ScreenEffect/ScreenNoisePresetDB.asset"),
        ("screen-vignette", "Assets/Ked.Presentation/Data/ScreenEffect/ScreenVignettePresetDB.asset"),
        ("depth",           "Assets/Ked.Presentation/Data/Generated/CharacterDepthTuning.asset"),
        ("focus-tuning",    "Assets/Ked.Presentation/Data/Generated/CharacterFocusTuningDB.asset"),
        ("role-anchor",     "Assets/Ked.Presentation/Data/Generated/RoleAnchorTuningDB.asset"),
        ("surface-layout",  "Assets/Samples/Data/DialogueSurfaceLayoutPresetDB.asset"),
    };

    /// <summary>초상 치수의 원천. 스프라이트 참조라 EditorJsonUtility로는 해석할 수 없어 따로 낸다.</summary>
    private const string PortraitDbPath = "Assets/Samples/Data/PortraitGeneratedDB.asset";

    // 이번 범위에 없어서 내보내지 않는 것들. 존재는 기록한다 —
    // 조용히 빠뜨린 것과 알고서 뺀 것은 다르다.
    private static readonly string[] KnownButOutOfScope =
    {
        "Assets/Samples/Data/DialogueSpeakerPresentationPolicyDB.asset (speaker policy — 범위 밖)",
        "Assets/Data/Generated/CharacterEmojiVisualPreset.asset (emoji preset — 범위 밖)",
        "Assets/Data/Generated/CharacterEmojiLibrary.asset (emoji library — 범위 밖)",
    };

    [MenuItem("Ked/U12/Export Presentation Tuning Dump")]
    public static void ExportAll()
    {
        List<string> warnings = new();

        string outDir = Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "ExportedTuning");
        Directory.CreateDirectory(outDir);
        Directory.CreateDirectory(Path.Combine(outDir, "presets"));

        // 씬을 연다 — 프리팹 배선과 CanvasScaler의 원천이 씬이다.
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        BaseResolutionDto baseResolution = ExportBaseResolution(outDir, warnings);
        ExportRigSchemas(outDir, baseResolution, warnings);
        ExportPresets(outDir, warnings);
        ExportPortraitDimensions(outDir, warnings);
        ExportBackgroundDimensions(outDir, warnings);
        WriteReport(outDir, warnings);

        Debug.Log($"[PresentationTuningExporter] 완료. out={outDir}, 경고 {warnings.Count}건");
    }

    // ── 기준 해상도 ──────────────────────────────────────────────────
    //
    // 무대 루트 공간의 크기이자 1u 환산의 입력이다. CanvasScaler에서 실측한다.
    // 무대는 무대 마스크를 내주는 쪽(IStageMaskProvider)이다 — 예제에서는 PresentationUIRoot.

    private static BaseResolutionDto ExportBaseResolution(string outDir, List<string> warnings)
    {
        BaseResolutionDto dto = new();

        IStageMaskProvider stage = FindInScene<IStageMaskProvider>();
        CanvasScaler scaler = stage is Component stageComponent
            ? stageComponent.GetComponentInParent<CanvasScaler>(true)
            : null;

        if (scaler == null)
        {
            warnings.Add(
                "무대(IStageMaskProvider)의 CanvasScaler를 찾지 못했다. 폴백 1920x1080을 썼다 — 확인 필요.");

            dto.canvasName = "(not found — fallback)";
            dto.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize.ToString();
            dto.referenceResolution = new Vector2(1920f, 1080f);
            dto.matchWidthOrHeight = 0.5f;

            return WriteBaseResolution(outDir, dto);
        }

        dto.canvasName = scaler.gameObject.name;
        dto.uiScaleMode = scaler.uiScaleMode.ToString();
        dto.referenceResolution = scaler.referenceResolution;
        dto.matchWidthOrHeight = scaler.matchWidthOrHeight;

        // 좌표 해석의 전제가 흔들리는 조건은 소리를 낸다.
        if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            warnings.Add(
                $"프레젠테이션 CanvasScaler가 ScaleWithScreenSize가 아니다({scaler.uiScaleMode}). " +
                "기준 해상도 좌표 전제가 흔들린다 — 확인 필요.");
        }

        if (dto.referenceResolution.x <= 0f || dto.referenceResolution.y <= 0f)
        {
            warnings.Add(
                $"referenceResolution이 유효하지 않다({dto.referenceResolution}). " +
                "코어의 1u 환산·루트 공간 크기가 여기서 온다 — 확인 필요.");
        }

        return WriteBaseResolution(outDir, dto);
    }

    private static BaseResolutionDto WriteBaseResolution(string outDir, BaseResolutionDto dto)
    {
        File.WriteAllText(
            Path.Combine(outDir, "base-resolution.json"),
            JsonUtility.ToJson(dto, true));

        return dto;
    }

    // ── 리그 스키마 4종 ──────────────────────────────────────────────
    //
    // 이 단계의 핵심: 빌더 로직을 전사하지 않고 실제로 세워서 읽는다.
    // BindRefsFromRoot까지 태우는 것도 의도다 — 런타임과 같은 경로(그래프 검증·복구)를
    // 지난 뒤의 값이어야 재생과 같은 값이다.

    private static void ExportRigSchemas(
        string outDir, BaseResolutionDto baseResolution, List<string> warnings)
    {
        RectTransform characterPrefab = ResolveCharacterRigPrefab(warnings);

        // 리그가 딛고 설 임시 부모: 기준 해상도 크기, 가운데 pivot.
        // 코어의 RectSpace(BaseResolution, Vec2.Half)와 같은 공간이다 —
        // 이 사실이 capturedUnderParentSize와 함께 schema.md에 남아야 한다.
        GameObject stageGo = new(ExportStageName, typeof(RectTransform));
        RectTransform stage = (RectTransform)stageGo.transform;
        stage.anchorMin = stage.anchorMax = new Vector2(0.5f, 0.5f);
        stage.pivot = new Vector2(0.5f, 0.5f);
        stage.sizeDelta = baseResolution.referenceResolution;

        RigSchemasDto dump = new()
        {
            capturedUnderParentSize = baseResolution.referenceResolution,
        };

        try
        {
            // Character — 배경도 이 리그다(캐스팅하지 않은 슬롯). 리그 스키마는 둘뿐이다.
            {
                CharacterRigBuilder builder = new();
                RectTransform root = builder.BuildCharacterRigRoot(characterPrefab);
                root.SetParent(stage, false);
                builder.BindRefsFromRoot(root, "", out _);

                dump.rigs.Add(CaptureRig(
                    "character", AssetPathOf(characterPrefab), root,
                    CharacterSchemaNodes(), warnings));

                Object.DestroyImmediate(root.gameObject);
            }

            // ScreenEffect — 부트스트랩에 프리팹 배선이 없다.
            // 스키마 베이크가 곧 런타임 경로라 프리팹 없이 세우는 것이 맞다.
            {
                ScreenEffectRigBuilder builder = new();
                RectTransform root = builder.BuildRigRoot();
                root.SetParent(stage, false);
                builder.BindRefsFromRoot(root, out _);

                dump.rigs.Add(CaptureRig(
                    "screenEffect", null, root,
                    ScreenEffectSchemaNodes(), warnings));

                Object.DestroyImmediate(root.gameObject);
            }
        }
        finally
        {
            // 중간에 터져도 무대를 더럽게 남기지 않는다.
            Object.DestroyImmediate(stageGo);
        }

        File.WriteAllText(
            Path.Combine(outDir, "rig-schemas.json"),
            JsonUtility.ToJson(dump, true));
    }

    /// <summary>
    /// 부트스트랩이 실제로 쓰는 프리팹을 그대로 꺼낸다 — 씬에서 IRigPrefabSource를 내주는 쪽을 찾는다
    /// (예제에서는 SampleBootstrap). 못 찾으면 프리팹 없이 세우되 — 런타임과 다를 수 있으므로 경고한다.
    /// </summary>
    private static RectTransform ResolveCharacterRigPrefab(List<string> warnings)
    {
        IRigPrefabSource source = FindInScene<IRigPrefabSource>();

        if (source == null)
        {
            warnings.Add(
                "리그 프리팹을 내주는 컴포넌트(IRigPrefabSource)를 씬에서 찾지 못했다. " +
                "리그를 프리팹 없이(스키마 베이크) 세웠다 — 런타임과 다를 수 있다.");

            return null;
        }

        return source.CharacterRigPrefab;
    }

    /// <summary>열린 씬에서 본체 계약을 구현한 첫 컴포넌트를 찾는다. 비활성 오브젝트도 본다.</summary>
    private static T FindInScene<T>() where T : class
    {
        MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is T found)
                return found;
        }

        return null;
    }

    private static RigDto CaptureRig(
        string rigKind,
        string sourcePrefabPath,
        RectTransform root,
        List<(string id, string parent)> schemaNodes,
        List<string> warnings)
    {
        RigDto rig = new()
        {
            rigKind = rigKind,
            sourcePrefab = sourcePrefabPath,
        };

        // 루트 자체도 담는다 — 스키마의 Parent=null 노드는 루트의 자식이다.
        rig.nodes.Add(CaptureNode(RootNodeId, null, root));

        foreach ((string id, string parent) in schemaNodes)
        {
            RectTransform rect = FindByName(root, id);

            if (rect == null)
            {
                warnings.Add($"{rigKind}: 노드 '{id}'를 세운 리그에서 찾지 못했다. 건너뛰었다.");
                continue;
            }

            rig.nodes.Add(CaptureNode(id, parent ?? RootNodeId, rect));
        }

        return rig;
    }

    /// <summary>
    /// 노드 필드는 코어 RectNodeState와 1:1이다. measuredRectSize만 예외 —
    /// 캡처 시점의 파생값이라 재현 입력이 아니라 검산용이다.
    /// </summary>
    private static RigNodeDto CaptureNode(string id, string parent, RectTransform rect)
    {
        // 가시성 축: 스폰 직후의 alpha. 초상·이모지 루트는 0으로 태어난다.
        // ⚠ canvasGroupAlpha만 두면 안 된다 — JsonUtility는 "없는 필드"를 0으로 채우므로
        // 구덤프를 읽을 때 "CanvasGroup이 없다"와 "alpha가 0이다"가 구분되지 않는다.
        // bool 게이트를 함께 두면 구덤프는 false로 안전하게 떨어진다.
        bool hasCanvasGroup = rect.TryGetComponent(out CanvasGroup canvasGroup);

        return new RigNodeDto
        {
            id = id,
            parent = parent,
            anchoredPosition = rect.anchoredPosition,
            anchorMin = rect.anchorMin,
            anchorMax = rect.anchorMax,
            pivot = rect.pivot,
            sizeDelta = rect.sizeDelta,
            localScale = rect.localScale,
            localEulerAngles = rect.localEulerAngles,
            measuredRectSize = rect.rect.size,
            hasCanvasGroup = hasCanvasGroup,
            canvasGroupAlpha = hasCanvasGroup ? canvasGroup.alpha : 1f,
        };
    }

    // 스키마 → (id, parent) 목록. 리그마다 enum 타입이 달라 공통 인터페이스가 없으므로
    // 여기서 한 번씩 문자열로 평탄화한다.

    private static List<(string, string)> CharacterSchemaNodes()
    {
        List<(string, string)> nodes = new(CharacterRigSchema.Nodes.Length);

        foreach (CharacterRigSchema.NodeDef n in CharacterRigSchema.Nodes)
            nodes.Add((n.Id.ToString(), n.Parent?.ToString()));

        return nodes;
    }

    private static List<(string, string)> ScreenEffectSchemaNodes()
    {
        List<(string, string)> nodes = new(ScreenEffectRigSchema.Nodes.Length);

        foreach (ScreenEffectRigSchema.NodeDef n in ScreenEffectRigSchema.Nodes)
            nodes.Add((n.Id.ToString(), n.Parent?.ToString()));

        return nodes;
    }

    // ── DBSO 프리셋 ──────────────────────────────────────────────────
    //
    // 유니티 직렬화 그대로 낸다 — 필드를 골라 재조립하면 전사 실수가 들어올 자리가 생긴다.
    // 코어 DTO가 이 래퍼 모양({"MonoBehaviour": {...}})을 그대로 받는다.

    private static void ExportPresets(string outDir, List<string> warnings)
    {
        foreach ((string group, string path) in PresetAssets)
        {
            ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

            if (asset == null)
            {
                warnings.Add($"프리셋 '{group}': {path} 를 찾지 못했다. 건너뛰었다.");
                continue;
            }

            string json = EditorJsonUtility.ToJson(asset, true);

            // 에셋 참조는 instanceID로 직렬화된다 — 이 덤프만으로는 해석할 수 없는 값이다.
            if (json.Contains("\"instanceID\""))
            {
                warnings.Add(
                    $"프리셋 '{group}': 에셋 참조 필드(instanceID)가 들어 있다. " +
                    "그 필드는 이 덤프만으로 해석할 수 없다 — schema.md의 해당 절 참조.");
            }

            File.WriteAllText(Path.Combine(outDir, "presets", $"{group}.json"), json);
        }
    }

    // ── 초상 치수 ────────────────────────────────────────────────────
    //
    // 다른 프리셋과 달리 직렬화 그대로 낼 수 없다 — PortraitGeneratedDB의 값은
    // Sprite 참조이고, 그건 instanceID로만 직렬화되어 덤프만으로는 해석이 안 된다.
    // 그래서 여기서 스프라이트를 실제로 열어 rect 크기를 읽어 낸다.
    //
    // 키는 정규화하지 않고 원본 그대로 낸다(원칙: 값은 지금 값 그대로).
    // 정규화 규칙(문자 소문자화·변형 접미사·표정 2자리)은 코어가 PortraitResolver와
    // 같은 식으로 조회 시점에 적용한다 — 여기서 미리 접으면 규칙이 두 곳에 살게 된다.

    private static void ExportPortraitDimensions(string outDir, List<string> warnings)
    {
        PortraitDimensionsDto dto = new() { sourceAsset = PortraitDbPath };

        PortraitGeneratedDBSO db = AssetDatabase.LoadAssetAtPath<PortraitGeneratedDBSO>(PortraitDbPath);

        if (db == null)
        {
            warnings.Add($"초상 치수: {PortraitDbPath} 를 찾지 못했다. 빈 덤프를 냈다 — 초상 사이징 폴드가 전부 Unhandled가 된다.");
            WritePortraitDimensions(outDir, dto);
            return;
        }

        int missingSprites = 0;

        foreach (PortraitGeneratedDBSO.Entry entry in db.entries)
        {
            if (entry.sprite == null)
            {
                missingSprites++;
                continue;
            }

            Rect rect = entry.sprite.rect;

            if (rect.width <= 0f || rect.height <= 0f)
            {
                warnings.Add(
                    $"초상 치수: 스프라이트 rect가 유효하지 않다 " +
                    $"({entry.characterId}|{entry.variantKey}|{entry.emotionKey} = {rect.size}). 건너뛰었다.");

                continue;
            }

            dto.entries.Add(new PortraitDimensionDto
            {
                character = entry.characterId,
                variant = entry.variantKey,
                emotion = entry.emotionKey,
                width = rect.width,
                height = rect.height,
            });
        }

        if (missingSprites > 0)
        {
            warnings.Add(
                $"초상 치수: 스프라이트가 비어 있는 엔트리 {missingSprites}건을 건너뛰었다. " +
                "그 (캐릭터·변형·표정)은 재생에서도 초상이 나오지 않는다 — PortraitDb 재생성 필요.");
        }

        WritePortraitDimensions(outDir, dto);
    }

    private static void WritePortraitDimensions(string outDir, PortraitDimensionsDto dto)
    {
        File.WriteAllText(
            Path.Combine(outDir, "portrait-dimensions.json"),
            JsonUtility.ToJson(dto, true));
    }

    // ── 배경 치수 ────────────────────────────────────────────────────
    //
    // 배경은 캐스팅하지 않은 캐릭터 슬롯이다(StageReducer.Background.cs). 사이징은 초상과 같지만
    // 종횡비의 출처가 다르다 — `bg_sprite`의 키 하나로 Resources/Backgrounds/{키}를 연다.
    // 키는 런타임 해석기(ResourcesSpriteResolver)가 Resources.Load에 넘기는 그 문자열이다:
    // Backgrounds/ 아래 **상대 경로, 확장자 없이**. 하위 폴더(ep1/hall)도 그대로 키가 된다 —
    // 스프라이트 이름만 쓰면 하위 폴더의 같은 이름이 서로를 덮는다.

    private static void ExportBackgroundDimensions(string outDir, List<string> warnings)
    {
        BackgroundDimensionsDto dto = new() { sourceFolder = "Resources/" + ResourcesSpriteResolver.BackgroundsRoot };

        Sprite[] sprites = Resources.LoadAll<Sprite>(ResourcesSpriteResolver.BackgroundsRoot);

        if (sprites == null || sprites.Length == 0)
        {
            warnings.Add(
                $"배경 치수: Resources/{ResourcesSpriteResolver.BackgroundsRoot} 에 스프라이트가 없다. 빈 덤프를 냈다 — " +
                "배경 사이징 폴드가 전부 Unhandled가 된다.");
        }

        HashSet<string> seen = new(StringComparer.Ordinal);
        string marker = "/Resources/" + ResourcesSpriteResolver.BackgroundsRoot + "/";

        foreach (Sprite sprite in sprites ?? Array.Empty<Sprite>())
        {
            string assetPath = AssetDatabase.GetAssetPath(sprite).Replace('\\', '/');
            int at = assetPath.LastIndexOf(marker, StringComparison.Ordinal);

            if (at < 0)
            {
                warnings.Add($"배경 치수: Resources/{ResourcesSpriteResolver.BackgroundsRoot} 밖의 에셋이 섞였다 ({assetPath}). 건너뛰었다.");
                continue;
            }

            string key = Path.ChangeExtension(assetPath.Substring(at + marker.Length), null);

            // 한 텍스처에 스프라이트가 여럿이면 Resources.Load<Sprite>(키)는 첫 것을 준다 — 덤프도 첫 것만.
            if (!seen.Add(key))
            {
                warnings.Add($"배경 치수: '{key}'에 스프라이트가 여럿이다(Multiple 모드?). 첫 것만 냈다.");
                continue;
            }

            Rect rect = sprite.rect;

            if (rect.width <= 0f || rect.height <= 0f)
            {
                warnings.Add($"배경 치수: 스프라이트 rect가 유효하지 않다 ({key} = {rect.size}). 건너뛰었다.");
                continue;
            }

            dto.entries.Add(new BackgroundDimensionDto
            {
                key = key,
                width = rect.width,
                height = rect.height,
            });
        }

        dto.entries.Sort((a, b) => string.CompareOrdinal(a.key, b.key));

        File.WriteAllText(
            Path.Combine(outDir, "background-dimensions.json"),
            JsonUtility.ToJson(dto, true));
    }

    // ── 보고서 ───────────────────────────────────────────────────────

    private static void WriteReport(string outDir, List<string> warnings)
    {
        ReportDto report = new()
        {
            exportedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            unityVersion = Application.unityVersion,
            scenePath = ScenePath,
            warnings = warnings,
            knownButNotExported = new List<string>(KnownButOutOfScope),
        };

        File.WriteAllText(
            Path.Combine(outDir, "export-report.json"),
            JsonUtility.ToJson(report, true));

        foreach (string warning in warnings)
            Debug.LogWarning($"[PresentationTuningExporter] {warning}");
    }

    // ── helper ───────────────────────────────────────────────────────

    private static string AssetPathOf(RectTransform prefab)
        => prefab == null ? null : AssetDatabase.GetAssetPath(prefab);

    /// <summary>
    /// rolePrefix 없이 세웠으므로 오브젝트 이름이 곧 스키마 id다.
    /// (role prefix가 붙는 경로는 이 덤프의 관심사가 아니다 — 코어는 논리 키로 잇는다)
    /// </summary>
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

    // ── DTO (JsonUtility 직렬화 대상) ─────────────────────────────────
    //
    // 이 모양이 코어 Tuning 타입의 모양이 된다. 필드 이름을 바꾸면 코어가 덤프를 못 읽는다.

    [Serializable]
    private sealed class BaseResolutionDto
    {
        public string canvasName;
        public string uiScaleMode;
        public Vector2 referenceResolution;
        public float matchWidthOrHeight;
    }

    [Serializable]
    private sealed class RigSchemasDto
    {
        public Vector2 capturedUnderParentSize;
        public List<RigDto> rigs = new();
    }

    [Serializable]
    private sealed class RigDto
    {
        public string rigKind;
        public string sourcePrefab;
        public List<RigNodeDto> nodes = new();
    }

    [Serializable]
    private sealed class RigNodeDto
    {
        public string id;
        public string parent;
        public Vector2 anchoredPosition;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 sizeDelta;
        public Vector3 localScale;
        public Vector3 localEulerAngles;
        public Vector2 measuredRectSize;

        // 가시성 축. hasCanvasGroup이 bool 게이트다 — 위 CaptureNode 주석 참조.
        public bool hasCanvasGroup;
        public float canvasGroupAlpha;
    }

    [Serializable]
    private sealed class BackgroundDimensionsDto
    {
        public string sourceFolder;
        public List<BackgroundDimensionDto> entries = new();
    }

    [Serializable]
    private sealed class BackgroundDimensionDto
    {
        // Backgrounds/ 아래 상대 경로(확장자 없음) = bg_sprite가 쓰는 키.
        public string key;
        public float width;
        public float height;
    }

    [Serializable]
    private sealed class PortraitDimensionsDto
    {
        public string sourceAsset;
        public List<PortraitDimensionDto> entries = new();
    }

    [Serializable]
    private sealed class PortraitDimensionDto
    {
        public string character;
        public string variant;
        public string emotion;

        // 스프라이트 rect의 픽셀 크기. 사이징이 쓰는 건 종횡비 하나지만,
        // 둘 다 남겨야 나중에 값이 어긋났을 때 어느 쪽이 변했는지 알 수 있다.
        public float width;
        public float height;
    }

    [Serializable]
    private sealed class ReportDto
    {
        public string exportedAtUtc;
        public string unityVersion;
        public string scenePath;
        public List<string> warnings = new();
        public List<string> knownButNotExported = new();
    }
}