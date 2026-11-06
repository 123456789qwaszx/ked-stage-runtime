using System;
using System.Collections.Generic;
using Ked.Presentation.Core;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// motions.json — 모션 저장소. 이징 곡선(curves.json)과 같은 결이다: 작가 자산이라 대본 번들 곁에
// 살고, 여기서는 읽기만 한다. 만드는 쪽은 에디터의 내보내기(AnimationClip → motions.json)다.
// 파일이 없으면 모션 0개로 조용히 동작한다 — 모션을 안 쓰는 프로젝트가 정상 경로다.
//
// 스키마 (motions/2) — 배열이다(JsonUtility가 딕셔너리를 못 읽는다):
// {
//   "schema": "motions/2",
//   "motions": [
//     { "name": "nod", "rig": "character", "duration": 0.6, "end": "restore",
//       "channels": [
//         { "node": "CharacterPortrait_Track_Move_Y", "kind": "position",
//           "y": [ { "t": 0, "v": 0, "inTangent": 0, "outTangent": -120 }, … ] }
//       ] }
//   ]
// }
//
// end: restore | commit (필수 — 기본값 없음) · kind: position | scale | euler. 채널에 mode는 **없다** — relative/absolute는
// 재생하는 커맨드(motion slot name mode)가 정한다. motions/1의 "mode"는 읽을 때 무시된다(스키마 경고만).
// 성분 x·y·z는 생략 가능(= 건드리지 않는다). 키의 t는 **초**. 뜻은 docs/motion-contract.md가 규범이다.
//
// 검증은 코어 MotionRules 한 곳이다(어긋난 모션은 로그 + 무시). 내보내기도 같은 규칙을 쓰므로
// "툴에서는 되는데 재생에서 사라지는" 모션이 안 생긴다.
// ─────────────────────────────────────────────────────────────────────────────
public sealed class MotionLibrary
{
    public const string BundleFileName = "motions.json";
    private const string ExpectedSchema = "motions/2";

    private readonly Dictionary<string, MotionClip> _clips;

    public static MotionLibrary Empty { get; } = new(new Dictionary<string, MotionClip>());

    private MotionLibrary(Dictionary<string, MotionClip> clips)
    {
        _clips = clips;
    }

    public int Count => _clips.Count;

    /// <summary>리듀서 튜닝(StageReducerTuning.Motions)에 그대로 넘긴다 — 재생과 폴드가 같은 클립을 본다.</summary>
    public IReadOnlyDictionary<string, MotionClip> Clips => _clips;

    public bool TryGet(string name, out MotionClip clip)
        => _clips.TryGetValue(name ?? "", out clip);

    /// <summary>
    /// 본문을 읽는다. 파일을 어디서 가져오는지(Resources · TextAsset · 경로)는 호스트가 정한다 —
    /// 무대는 Application.dataPath를 열지 않는다.
    /// rigNodeIds를 주면 채널 노드를 그 스키마로 검증한다(지금 모션은 캐릭터 리그용이다 — CharacterRigSchema.NodeIds()).
    /// </summary>
    public static MotionLibrary Parse(string json, string sourceLabel, ICollection<string> rigNodeIds = null)
    {
        FileDto file;

        try
        {
            file = JsonUtility.FromJson<FileDto>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[MotionLibrary] {sourceLabel}: JSON을 읽지 못했다 — {e.Message}. 모션 0개로 동작한다.");
            return Empty;
        }

        if (file == null)
        {
            Debug.LogError($"[MotionLibrary] {sourceLabel}: 빈 문서다. 모션 0개로 동작한다.");
            return Empty;
        }

        if (file.schema != ExpectedSchema)
        {
            Debug.LogWarning(
                $"[MotionLibrary] {sourceLabel}: schema '{file.schema}' (기대 '{ExpectedSchema}'). " +
                "읽기는 시도한다 — 어긋나면 모션 단위 검증이 거른다.");
        }

        Dictionary<string, MotionClip> clips = new();

        if (file.motions != null)
        {
            foreach (MotionDto dto in file.motions)
            {
                if (!TryConvert(dto, sourceLabel, out MotionClip clip))
                    continue;

                if (!MotionRules.TryValidate(clip, rigNodeIds, out string why))
                {
                    Warn(sourceLabel, clip.Name, why);
                    continue;
                }

                if (clips.ContainsKey(clip.Name))
                {
                    Warn(sourceLabel, clip.Name, "같은 이름이 둘이다 — 뒤의 것을 무시한다");
                    continue;
                }

                clips[clip.Name] = clip;
            }
        }

        return new MotionLibrary(clips);
    }

    private static bool TryConvert(MotionDto dto, string sourceLabel, out MotionClip clip)
    {
        clip = null;

        if (dto == null)
        {
            Warn(sourceLabel, "(null)", "항목이 비어 있다");
            return false;
        }

        string name = string.IsNullOrEmpty(dto.name) ? "(이름 없음)" : dto.name;

        if (!TryParseEnd(dto.end, out MotionEnd end))
        {
            Warn(sourceLabel, name, $"end는 restore|commit 이어야 한다 ('{dto.end}') — 기본값은 없다");
            return false;
        }

        MotionChannel[] channels = new MotionChannel[dto.channels?.Count ?? 0];

        for (int i = 0; i < channels.Length; i++)
        {
            ChannelDto c = dto.channels[i];

            if (c == null)
            {
                Warn(sourceLabel, name, $"채널 {i}가 비어 있다");
                return false;
            }

            if (!TryParseKind(c.kind, out MotionChannelKind kind))
            {
                Warn(sourceLabel, name, $"채널 {i}: kind는 position|scale|euler 여야 한다 ('{c.kind}')");
                return false;
            }

            channels[i] = new MotionChannel
            {
                Node = c.node,
                Kind = kind,
                X = ToKeys(c.x),
                Y = ToKeys(c.y),
                Z = ToKeys(c.z),
            };
        }

        clip = new MotionClip { Name = dto.name, Rig = dto.rig, Duration = dto.duration, End = end, Channels = channels };
        return true;
    }

    // JsonUtility는 없는 배열을 빈 리스트로 채운다 — 빈 것은 "성분 없음"(null)으로 읽는다.
    private static CurveKey[] ToKeys(List<KeyDto> keys)
    {
        if (keys == null || keys.Count == 0)
            return null;

        CurveKey[] result = new CurveKey[keys.Count];

        for (int i = 0; i < keys.Count; i++)
            result[i] = new CurveKey(keys[i].t, keys[i].v, keys[i].inTangent, keys[i].outTangent);

        return result;
    }

    private static bool TryParseKind(string token, out MotionChannelKind kind)
    {
        switch ((token ?? "").Trim().ToLowerInvariant())
        {
            case "position": kind = MotionChannelKind.Position; return true;
            case "scale": kind = MotionChannelKind.Scale; return true;
            case "euler": kind = MotionChannelKind.Euler; return true;
            default: kind = default; return false;
        }
    }

    private static bool TryParseEnd(string token, out MotionEnd end)
    {
        switch ((token ?? "").Trim().ToLowerInvariant())
        {
            case "restore": end = MotionEnd.Restore; return true;
            case "commit": end = MotionEnd.Commit; return true;
            default: end = default; return false;
        }
    }

    private static void Warn(string sourceLabel, string motionName, string why)
        => Debug.LogWarning($"[MotionLibrary] {sourceLabel}: 모션 '{motionName}' 무시 — {why}.");

    // ── DTO (JsonUtility 직렬화 대상 — 필드명이 곧 스키마다) ─────────

    [Serializable]
    private sealed class FileDto
    {
        public string schema = "";
        public List<MotionDto> motions = new();
    }

    [Serializable]
    private sealed class MotionDto
    {
        public string name = "";
        public string rig = "";
        public float duration = 0f;
        public string end = "";
        public List<ChannelDto> channels = new();
    }

    [Serializable]
    private sealed class ChannelDto
    {
        public string node = "";
        public string kind = "";
        public List<KeyDto> x = new();
        public List<KeyDto> y = new();
        public List<KeyDto> z = new();
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
