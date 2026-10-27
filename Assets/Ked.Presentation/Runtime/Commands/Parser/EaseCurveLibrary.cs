using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Ked.Presentation.Core;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// curves.json — 커스텀 이징 곡선 저장소 (ease-curve-orders.md §3).
//
// 커브는 작가 자산이라 대본 번들 옆에 산다(이 레포의 예제는 Samples/Dialogue/). VnTool 내보내기가 이 파일을
// 함께 내고, 여기서는 읽기만 한다. 파일이 없으면 커브 0개로 조용히 동작한다 —
// 커브를 안 쓰는 프로젝트가 정상 경로다.
//
// 스키마 (ease-curves/1) — curves는 배열이다(원안의 딕셔너리를 수정 회신:
// 런타임 파서 JsonUtility가 딕셔너리를 못 읽는다):
// {
//   "schema": "ease-curves/1",
//   "curves": [
//     { "name": "hop_snappy",
//       "keys": [ { "t": 0, "v": 0, "inTangent": 0, "outTangent": 2.6 }, … ] }
//   ]
// }
//
// 검증(어긋난 커브는 로그 + 무시 — 조용히 빠뜨리지 않는다):
// 이름 [a-z0-9_]+ · 키 2개 이상 · t 오름차순 · 첫 키 (t,v)=(0,0) · 마지막 키 t=1.
//
// 마지막 키의 v가 곡선의 **종류**를 가른다(CurveKindRules):
//   v=1 → 이동 곡선(Motion)   — move_by·place·size·shot·scale·rotate. 종점이 명목 목표값이다.
//   v=0 → 진동 곡선(Oscillation) — gesture. 순변위 0이 정체다.
// 그 외 값은 어느 쪽도 아니라 거부한다. 중간이 1을 넘는 오버슛은 두 종류 모두 정상이다.
// 종류는 선언이 아니라 키에서 파생되므로 curves.json 스키마는 그대로다.
// ─────────────────────────────────────────────────────────────────────────────
public sealed class EaseCurveLibrary
{
    public const string BundleFileName = "curves.json";
    private const string ExpectedSchema = "ease-curves/1";

    private static readonly Regex NameRule = new("^[a-z0-9_]+$", RegexOptions.Compiled);

    private readonly Dictionary<string, Entry> _curves;

    private readonly struct Entry
    {
        public readonly CurveKind Kind;
        public readonly CurveKey[] Keys;

        public Entry(CurveKind kind, CurveKey[] keys)
        {
            Kind = kind;
            Keys = keys;
        }
    }

    public static EaseCurveLibrary Empty { get; } = new(new Dictionary<string, Entry>());

    private EaseCurveLibrary(Dictionary<string, Entry> curves)
    {
        _curves = curves;
    }

    public int Count => _curves.Count;

    /// <summary>
    /// 이름 + **종류**로 찾는다. 종류가 다르면 못 찾은 것으로 친다 —
    /// 이동 자리에 진동 곡선을 끼우면 끝에서 튀고, 그 반대는 제자리로 안 돌아온다.
    /// 호출부가 그 사정을 아는 메시지로 경고하도록 종류 불일치는 out으로 알린다.
    /// </summary>
    public bool TryGet(string name, CurveKind kind, out CurveKey[] keys, out bool wrongKind)
    {
        keys = null;
        wrongKind = false;

        if (!_curves.TryGetValue(name, out Entry entry))
            return false;

        if (entry.Kind != kind)
        {
            wrongKind = true;
            return false;
        }

        keys = entry.Keys;
        return true;
    }

    /// <summary>파일이 없으면 무음으로 Empty — 커브를 안 쓰는 프로젝트가 정상 경로다.</summary>
    public static EaseCurveLibrary LoadFrom(string jsonPath)
    {
        if (!File.Exists(jsonPath))
            return Empty;

        return Parse(File.ReadAllText(jsonPath), jsonPath);
    }

    public static EaseCurveLibrary Parse(string json, string sourceLabel)
    {
        FileDto file;

        try
        {
            file = JsonUtility.FromJson<FileDto>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[EaseCurveLibrary] {sourceLabel}: JSON을 읽지 못했다 — {e.Message}. 커브 0개로 동작한다.");
            return Empty;
        }

        if (file == null)
        {
            Debug.LogError($"[EaseCurveLibrary] {sourceLabel}: 빈 문서다. 커브 0개로 동작한다.");
            return Empty;
        }

        if (file.schema != ExpectedSchema)
        {
            Debug.LogWarning(
                $"[EaseCurveLibrary] {sourceLabel}: schema '{file.schema}' (기대 '{ExpectedSchema}'). " +
                "읽기는 시도한다 — 어긋나면 커브 단위 검증이 거른다.");
        }

        Dictionary<string, Entry> curves = new();

        if (file.curves != null)
        {
            foreach (CurveDto curve in file.curves)
            {
                if (!TryValidate(curve, sourceLabel, out CurveKey[] keys, out CurveKind kind))
                    continue;

                curves[curve.name] = new Entry(kind, keys);
            }
        }

        return new EaseCurveLibrary(curves);
    }

    private static bool TryValidate(
        CurveDto curve, string sourceLabel, out CurveKey[] keys, out CurveKind kind)
    {
        keys = null;
        kind = CurveKind.Motion;

        string name = curve?.name ?? "(null)";

        if (curve == null || string.IsNullOrEmpty(curve.name) || !NameRule.IsMatch(curve.name))
        {
            Warn(sourceLabel, name, "이름은 [a-z0-9_]+ 여야 한다 (커맨드 토큰에 실린다)");
            return false;
        }

        if (curve.keys == null || curve.keys.Count < 2)
        {
            Warn(sourceLabel, name, "키가 2개 이상이어야 한다");
            return false;
        }

        if (curve.keys[0].t != 0f)
        {
            Warn(sourceLabel, name, $"첫 키 t가 0이 아니다 ({curve.keys[0].t})");
            return false;
        }

        if (curve.keys[curve.keys.Count - 1].t != 1f)
        {
            Warn(sourceLabel, name, $"마지막 키 t가 1이 아니다 ({curve.keys[curve.keys.Count - 1].t})");
            return false;
        }

        for (int i = 1; i < curve.keys.Count; i++)
        {
            if (curve.keys[i].t <= curve.keys[i - 1].t)
            {
                Warn(sourceLabel, name, $"키 t가 오름차순이 아니다 (index {i})");
                return false;
            }
        }

        CurveKey[] parsed = new CurveKey[curve.keys.Count];

        for (int i = 0; i < curve.keys.Count; i++)
        {
            KeyDto k = curve.keys[i];
            parsed[i] = new CurveKey(k.t, k.v, k.inTangent, k.outTangent);
        }

        // 끝값이 종류를 가른다 — 규칙은 코어 한 곳(CurveKindRules)이 진다.
        // 툴 저작 검증도 같은 규칙을 써야 "툴에선 되는데 재생에서 사라지는" 곡선이 안 생긴다.
        if (!CurveKindRules.TryClassify(parsed, out kind, out string why))
        {
            Warn(sourceLabel, name, why);
            return false;
        }

        keys = parsed;
        return true;
    }

    private static void Warn(string sourceLabel, string curveName, string why)
        => Debug.LogWarning($"[EaseCurveLibrary] {sourceLabel}: 커브 '{curveName}' 무시 — {why}.");

    // ── DTO (JsonUtility 직렬화 대상 — 필드명이 곧 스키마다) ─────────

    [Serializable]
    private sealed class FileDto
    {
        public string schema = "";
        public List<CurveDto> curves = new();
    }

    [Serializable]
    private sealed class CurveDto
    {
        public string name = "";
        public List<KeyDto> keys = new();
    }

    [Serializable]
    private sealed class KeyDto
    {
        public float t = 0f;
        public float v = 0f;
        public float inTangent = 0f;
        public float outTangent = 0f;
    }
}
