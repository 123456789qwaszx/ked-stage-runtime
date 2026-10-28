using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Ked.Presentation.Core;
using UnityEditor;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// 모션 내보내기 — AnimationClip → motions.json.
//
// 모션 툴은 Unity Animation 창이다(도프시트·커브 에디터가 이미 있다). 리그 프리팹의 노드는 트랜스폼 계층이라
// 그대로 키잉되고, 키는 Hermite라 CurveKey와 1:1이다(CurveFunctions ↔ AnimationCurve.Evaluate 등가는
// CurveFunctionsAnimationCurveParityTests가 심판). 그래서 툴은 **내보내기 하나**다 — 런타임은 AnimationClip을 모른다.
//
// 읽는 것: RectTransform의 m_AnchoredPosition.x/y · m_LocalScale.x/y · localEulerAnglesRaw.x/y/z.
// 거부하는 것(이유를 전부 적어 돌려준다 — 조용히 빠뜨리지 않는다):
//   · 다른 속성(m_LocalPosition은 anchoredPosition으로, m_LocalRotation은 회전 보간을 Euler Angles로)
//   · 스키마에 없는 노드 · Track_Focus · 가중 탄젠트 · 무한 탄젠트(계단 키 — JSON에 Infinity가 없다)
//   · MotionRules가 거르는 전부(키 시각·노드·성분 모양). 값의 뜻(relative/absolute)은 파일에 없다 — 대본의 커맨드가 정한다(motions/2)
//
// 리그는 런타임 코드가 아니라 **rig-schemas.json**으로 안다 — 이 도구의 의존은 코어(규약)와 데이터 파일뿐이다.
// 그래서 저작 도구(VnTool)와 같은 조건이고, 언젠가 따로 떼어도 코드가 안 바뀐다.
// 노드는 끝 이름만이 아니라 **부모 사슬 전체**를 대조한다 — 끝 이름이 같아도 계층이 다른 리그에서 만든 클립은 거부된다.
// 제작 리그는 Ked/Motion/Create Authoring Rig가 스키마로 세운다(MotionAuthoringRig).
//
// 쓴 파일은 바로 MotionLibrary로 되읽어 개수를 대조한다 — 로더가 못 읽는 것을 냈으면 여기서 실패한다.
// 재내보내기 시 바이트 동일: 타임스탬프 없음, 필드 순서 고정, invariant "R", UTF-8(BOM 없음) + LF.
//
// 실행: 메뉴 Ked/Motion/Export motions.json (프로젝트의 MotionExportSettings 전부)
// ─────────────────────────────────────────────────────────────────────────────
public static class MotionClipExporter
{
    [MenuItem("Ked/Motion/Export motions.json")]
    public static void ExportAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:MotionExportSettings");

        if (guids.Length == 0)
        {
            Debug.LogWarning("[MotionClipExporter] MotionExportSettings 에셋이 없다 — Create › Ked › Motion Export Settings.");
            return;
        }

        foreach (string guid in guids)
        {
            MotionExportSettings settings = AssetDatabase.LoadAssetAtPath<MotionExportSettings>(AssetDatabase.GUIDToAssetPath(guid));
            Export(settings);
        }
    }

    public static bool Export(MotionExportSettings settings)
    {
        List<MotionClip> clips = new();
        List<string> errors = new();

        if (!TryLoadRigSchemas(settings.rigSchemasPath, out Dictionary<string, RigNodeSet> rigs, out string loadError))
        {
            Debug.LogError($"[MotionClipExporter] {settings.name}: {loadError}", settings);
            return false;
        }

        foreach (MotionDefinition def in settings.motions)
        {
            if (def == null)
            {
                errors.Add("비어 있는 항목이 있다");
                continue;
            }

            if (def.clip == null)
            {
                errors.Add($"'{def.name}': 클립이 없다");
                continue;
            }

            if (!rigs.TryGetValue(def.rigSchemaId ?? "", out RigNodeSet rig))
            {
                errors.Add($"'{def.name}': rig-schemas.json에 리그 '{def.rigSchemaId}'가 없다");
                continue;
            }

            if (TryConvert(def.clip, def.EffectiveName, def.rigSchemaId, def.end, rig, out MotionClip clip, out List<string> why))
                clips.Add(clip);
            else
                errors.AddRange(why);
        }

        if (errors.Count > 0)
        {
            Debug.LogError($"[MotionClipExporter] {settings.name}: 내보내지 않았다 —\n  " + string.Join("\n  ", errors), settings);
            return false;
        }

        string json = ToJson(clips);
        string outPath = Path.Combine(Path.GetDirectoryName(Application.dataPath)!, settings.outputPath);

        // 되읽기 — 로더가 받아들이는 것만 낸다. 노드 검증은 변환에서 했으므로 여기선 형식만.
        MotionLibrary check = MotionLibrary.Parse(json, settings.outputPath);

        if (check.Count != clips.Count)
        {
            Debug.LogError($"[MotionClipExporter] {settings.name}: 되읽기에서 {clips.Count - check.Count}개가 걸렸다. 위 경고를 볼 것. 파일을 쓰지 않았다.", settings);
            return false;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        File.WriteAllText(outPath, json, new UTF8Encoding(false));

        if (settings.outputPath.StartsWith("Assets/", StringComparison.Ordinal))
            AssetDatabase.ImportAsset(settings.outputPath);

        Debug.Log($"[MotionClipExporter] {settings.name}: 모션 {clips.Count}개 → {settings.outputPath}", settings);
        return true;
    }

    /// <summary>리그 하나의 노드 id와, 루트 직속부터의 기대 경로(id를 '/'로 이은 것, __root 제외).</summary>
    public sealed class RigNodeSet
    {
        public readonly HashSet<string> Ids = new();
        public readonly Dictionary<string, string> PathOf = new();
    }

    /// <summary>rig-schemas.json → rigKind별 노드 집합. 코어 DTO로 읽는다 — 런타임 스키마 코드를 모른다.</summary>
    public static bool TryLoadRigSchemas(string rigSchemasPath, out Dictionary<string, RigNodeSet> rigs, out string error)
    {
        rigs = null;
        error = null;

        string path = Path.IsPathRooted(rigSchemasPath)
            ? rigSchemasPath
            : Path.Combine(Path.GetDirectoryName(Application.dataPath)!, rigSchemasPath);

        if (!File.Exists(path))
        {
            error = $"rig-schemas.json이 없다: {rigSchemasPath} (튜닝 덤프를 먼저 낼 것)";
            return false;
        }

        RigSchemasFileDto file = JsonUtility.FromJson<RigSchemasFileDto>(File.ReadAllText(path));

        if (file?.rigs == null)
        {
            error = $"rig-schemas.json을 읽지 못했다: {rigSchemasPath}";
            return false;
        }

        rigs = new Dictionary<string, RigNodeSet>();

        foreach (RigSchemaRigDto rig in file.rigs)
        {
            RigNodeSet set = new();

            // 부모가 항상 자식보다 먼저 온다(덤프 규약) — 순서대로 경로를 잇는다.
            foreach (RigSchemaNodeDto node in rig.nodes ?? new List<RigSchemaNodeDto>())
            {
                if (string.IsNullOrEmpty(node.id) || node.id == "__root")
                    continue;

                set.Ids.Add(node.id);
                set.PathOf[node.id] = !string.IsNullOrEmpty(node.parent) && set.PathOf.TryGetValue(node.parent, out string parentPath)
                    ? parentPath + "/" + node.id
                    : node.id;
            }

            rigs[rig.rigKind ?? ""] = set;
        }

        return true;
    }

    /// <summary>
    /// 클립 하나를 변환한다. 실패하면 이유를 전부 모아 돌려준다(첫 오류에서 멈추지 않는다 — 작가가 한 번에 고치게).
    /// </summary>
    public static bool TryConvert(
        AnimationClip clip,
        string name,
        string rigId,
        MotionEnd end,
        RigNodeSet rig,
        out MotionClip result,
        out List<string> errors)
    {
        result = null;
        errors = new List<string>();

        if (clip == null)
        {
            errors.Add("클립이 null이다");
            return false;
        }

        string label = $"'{name}'({clip.name})";
        Dictionary<(string node, MotionChannelKind kind), MotionChannel> channels = new();
        List<(string node, MotionChannelKind kind)> order = new();

        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (!TryMapProperty(binding.propertyName, out MotionChannelKind kind, out int component, out string hint))
            {
                errors.Add($"{label}: '{binding.path}'의 속성 '{binding.propertyName}'은 낼 수 없다 — {hint}");
                continue;
            }

            if (!TryMapNode(binding.path, rig, out string node, out string pathWhy))
            {
                errors.Add($"{label}: {pathWhy}");
                continue;
            }

            if (!TryConvertKeys(AnimationUtility.GetEditorCurve(clip, binding), $"{label} {node}.{binding.propertyName}", errors, out CurveKey[] keys))
                continue;

            (string, MotionChannelKind) id = (node, kind);

            if (!channels.TryGetValue(id, out MotionChannel channel))
            {
                channel = new MotionChannel { Node = node, Kind = kind };
                order.Add(id);
            }

            // Unity는 회전·위치·배율을 세 성분 묶음으로 기록해 한 축만 키잉해도 나머지가 상수로 따라온다.
            // 그대로 싣는다 — 파일은 mode를 모르므로(motions/2) 상수 0이 "무변"(relative)인지 "0으로"(absolute)인지
            // 여기서 정할 수 없다. 성분을 빼는 것은 작가가 Animation 창에서 한다.
            switch (component)
            {
                case 0: channel.X = keys; break;
                case 1: channel.Y = keys; break;
                default: channel.Z = keys; break;
            }

            channels[id] = channel;
        }

        if (errors.Count > 0)
            return false;

        if (order.Count == 0)
        {
            errors.Add($"{label}: 낼 수 있는 채널이 없다 — 리그 노드의 anchoredPosition·localScale·localEulerAngles 키가 없다");
            return false;
        }

        MotionClip converted = new()
        {
            Name = name,
            Rig = rigId,
            Duration = clip.length,
            End = end,
            Channels = new MotionChannel[order.Count],
        };

        for (int i = 0; i < order.Count; i++)
            converted.Channels[i] = channels[order[i]];

        // 규칙은 코어 한 곳 — 로더와 같은 함수.
        if (!MotionRules.TryValidate(converted, rig.Ids, out string reason))
        {
            errors.Add($"{label}: {reason}");
            return false;
        }

        result = converted;
        return true;
    }

    // 속성 이름 → (종류, 성분). RectTransform의 앵커 위치·배율·오일러 회전만.
    private static bool TryMapProperty(string property, out MotionChannelKind kind, out int component, out string hint)
    {
        kind = default;
        component = -1;
        hint = null;

        int dot = property.LastIndexOf('.');
        string head = dot >= 0 ? property.Substring(0, dot) : property;
        string axis = dot >= 0 ? property.Substring(dot + 1) : "";

        component = axis switch { "x" => 0, "y" => 1, "z" => 2, _ => -1 };

        switch (head)
        {
            case "m_AnchoredPosition":
                kind = MotionChannelKind.Position;
                break;

            case "m_LocalScale":
                kind = MotionChannelKind.Scale;
                break;

            case "localEulerAnglesRaw":
            case "m_LocalEulerAnglesRaw":
            case "m_LocalEulerAngles":
                kind = MotionChannelKind.Euler;
                break;

            case "m_LocalPosition":
                hint = "리그 노드는 anchoredPosition으로 움직인다 — Rect Transform › Anchored Position을 키잉할 것";
                return false;

            case "m_LocalRotation":
                hint = "쿼터니언 회전이다 — Animation 창에서 회전 보간을 Euler Angles로 바꿀 것";
                return false;

            default:
                hint = "모션 채널은 anchoredPosition · localScale · localEulerAngles 뿐이다";
                return false;
        }

        if (component < 0 || (kind != MotionChannelKind.Euler && component == 2))
        {
            hint = kind == MotionChannelKind.Euler ? "성분은 x·y·z여야 한다" : $"{kind}에는 z 성분이 없다";
            return false;
        }

        return true;
    }

    // 경로 → 노드 id. 끝 마디로 id를 찾고(리그 노드 이름은 {rolePrefix}{id}라 끝맞춤, 가장 긴 id), 그 다음 **부모 사슬 전체**를
    // 스키마의 기대 경로와 마디마다 대조한다. 끝 이름만 같고 계층이 다른 클립(스키마 밖 리그에서 만든 것)은 여기서 걸린다.
    private static bool TryMapNode(string path, RigNodeSet rig, out string node, out string why)
    {
        node = null;
        why = null;

        if (string.IsNullOrEmpty(path))
        {
            why = "리그 루트 자체에 키가 있다 — 루트는 노드가 아니다";
            return false;
        }

        string[] actual = path.Split('/');
        string leaf = actual[actual.Length - 1];

        foreach (string id in rig.Ids)
        {
            if (SegmentMatches(leaf, id) && (node == null || id.Length > node.Length))
                node = id;
        }

        if (node == null)
        {
            why = $"경로 '{path}'의 끝이 리그 스키마의 노드가 아니다";
            return false;
        }

        string[] expected = rig.PathOf[node].Split('/');

        if (actual.Length != expected.Length)
        {
            why = $"노드 '{node}'의 계층이 스키마와 다르다 — 기대 '{rig.PathOf[node]}', 실제 '{path}'. 제작 리그는 Ked/Motion/Create Authoring Rig로 세울 것";
            node = null;
            return false;
        }

        for (int i = 0; i < expected.Length; i++)
        {
            if (!SegmentMatches(actual[i], expected[i]))
            {
                why = $"노드 '{node}'의 계층이 스키마와 다르다 — 기대 '{rig.PathOf[node]}', 실제 '{path}'. 제작 리그는 Ked/Motion/Create Authoring Rig로 세울 것";
                node = null;
                return false;
            }
        }

        return true;
    }

    private static bool SegmentMatches(string segment, string id)
        => segment == id || segment.EndsWith(id, StringComparison.Ordinal);

    private static bool TryConvertKeys(AnimationCurve curve, string label, List<string> errors, out CurveKey[] keys)
    {
        keys = null;

        if (curve == null)
        {
            errors.Add($"{label}: 커브가 없다");
            return false;
        }

        Keyframe[] frames = curve.keys;
        CurveKey[] result = new CurveKey[frames.Length];

        for (int i = 0; i < frames.Length; i++)
        {
            Keyframe k = frames[i];

            if (k.weightedMode != WeightedMode.None)
            {
                errors.Add($"{label}: 키 {i}가 가중 탄젠트다 — CurveKey는 가중을 모른다. 키의 Weighted를 끌 것");
                return false;
            }

            if (float.IsInfinity(k.inTangent) || float.IsInfinity(k.outTangent))
            {
                errors.Add($"{label}: 키 {i}가 계단(Constant) 탄젠트다 — JSON에 Infinity가 없다. Auto나 Linear로 바꿀 것");
                return false;
            }

            result[i] = new CurveKey(k.time, k.value, k.inTangent, k.outTangent);
        }

        keys = result;
        return true;
    }

    // ── JSON 쓰기 — MotionLibrary의 DTO 필드명이 스키마다 ─────────────

    public static string ToJson(IReadOnlyList<MotionClip> clips)
    {
        StringBuilder sb = new();
        sb.Append("{\n  \"schema\": \"motions/2\",\n  \"motions\": [");

        for (int m = 0; m < clips.Count; m++)
        {
            MotionClip clip = clips[m];
            sb.Append(m > 0 ? ",\n" : "\n");
            sb.Append("    { \"name\": \"").Append(clip.Name).Append("\", \"rig\": \"").Append(clip.Rig)
              .Append("\", \"duration\": ").Append(F(clip.Duration))
              .Append(", \"end\": \"").Append(clip.End == MotionEnd.Restore ? "restore" : "commit").Append("\",\n      \"channels\": [");

            for (int c = 0; c < clip.Channels.Length; c++)
            {
                MotionChannel ch = clip.Channels[c];
                sb.Append(c > 0 ? ",\n" : "\n");
                sb.Append("        { \"node\": \"").Append(ch.Node).Append("\", \"kind\": \"").Append(KindToken(ch.Kind)).Append('"');
                AppendComponent(sb, "x", ch.X);
                AppendComponent(sb, "y", ch.Y);
                AppendComponent(sb, "z", ch.Z);
                sb.Append(" }");
            }

            sb.Append("\n      ] }");
        }

        sb.Append("\n  ]\n}\n");
        return sb.ToString();
    }

    private static void AppendComponent(StringBuilder sb, string axis, CurveKey[] keys)
    {
        if (keys == null || keys.Length == 0)
            return;

        sb.Append(",\n          \"").Append(axis).Append("\": [");

        for (int i = 0; i < keys.Length; i++)
        {
            sb.Append(i > 0 ? ", " : " ");
            sb.Append("{ \"t\": ").Append(F(keys[i].Time)).Append(", \"v\": ").Append(F(keys[i].Value))
              .Append(", \"inTangent\": ").Append(F(keys[i].InTangent)).Append(", \"outTangent\": ").Append(F(keys[i].OutTangent)).Append(" }");
        }

        sb.Append(" ]");
    }

    private static string KindToken(MotionChannelKind kind)
        => kind switch { MotionChannelKind.Position => "position", MotionChannelKind.Scale => "scale", _ => "euler" };

    private static string F(float v) => v.ToString("R", CultureInfo.InvariantCulture);
}
