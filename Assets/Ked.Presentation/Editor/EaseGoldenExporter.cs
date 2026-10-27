using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using UnityEditor;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// W66b — 이징 골든 덤프.
//
// VnTool 프리뷰가 그릴 곡선 모양의 정본. DOTween EaseManager.Evaluate를
// 표준 Ease 전 항목 × t ∈ [0,1] 257등분으로 샘플링해 JSON으로 낸다.
// (표준 = Unset·INTERNAL_* 제외. 이 덤프의 목록이 곧 정본이다.)
//
// 원칙 (U12 PresentationTuningExporter와 같다):
// - 값은 지금 값 그대로 — Back·Elastic·Flash가 쓰는 overshoot/period 기본값은
//   DOTween의 라이브 기본값(DOTween.defaultEaseOvershootOrAmplitude/-Period)을
//   읽어 파일에 명시한다.
// - 재덤프 시 바이트 동일: 타임스탬프 없음, 필드 순서 고정, invariant "R" 포맷,
//   UTF-8(BOM 없음) + LF. 같은 DLL이면 같은 바이트가 나온다.
//
// 실행: 메뉴 Ked/W66b/Export Ease Golden Dump
//       또는 batchmode -executeMethod EaseGoldenExporter.ExportAll
// 출력: <프로젝트 루트>/ExportedTuning/ease-golden.json
//       (VnTool 저장소로 보내 그쪽 테스트 픽스처가 된다)
// ─────────────────────────────────────────────────────────────────────────────
public static class EaseGoldenExporter
{
    private const int SampleCount = 257; // t = i / 256, i = 0..256

    [MenuItem("Ked/W66b/Export Ease Golden Dump")]
    public static void ExportAll()
    {
        string outDir = Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "ExportedTuning");
        Directory.CreateDirectory(outDir);
        string outPath = Path.Combine(outDir, "ease-golden.json");

        float overshootOrAmplitude = DOTween.defaultEaseOvershootOrAmplitude;
        float period = DOTween.defaultEasePeriod;

        List<Ease> eases = StandardEases();

        StringBuilder sb = new();
        sb.Append("{\n");
        sb.Append("  \"schema\": \"ease-golden/1\",\n");
        sb.Append("  \"source\": \"DOTween EaseManager.Evaluate(ease, null, t, 1, overshootOrAmplitude, period)\",\n");
        sb.Append($"  \"dotweenVersion\": \"{DOTween.Version}\",\n");
        sb.Append("  \"duration\": 1,\n");
        sb.Append($"  \"overshootOrAmplitude\": {F(overshootOrAmplitude)},\n");
        sb.Append($"  \"period\": {F(period)},\n");
        sb.Append($"  \"sampleCount\": {SampleCount},\n");
        sb.Append("  \"tRule\": \"t[i] = i / 256, i = 0..256\",\n");
        sb.Append("  \"notes\": [\n");
        sb.Append("    \"이 파일의 eases 목록이 표준 Ease의 정본이다 (Unset·INTERNAL_* 제외).\",\n");
        sb.Append("    \"Back·Elastic·Flash는 위 overshootOrAmplitude·period 기본값으로 샘플링했다 — SetEase(Ease)만 쓴 트윈과 같은 조건이다.\",\n");
        sb.Append("    \"Elastic은 period 0을 내부에서 duration*0.3(InOut은 *0.45)으로 해석한다.\",\n");
        sb.Append("    \"Flash의 overshootOrAmplitude는 깜빡임 스텝 수로 쓰인다.\",\n");
        sb.Append("    \"Back·Elastic 값은 [0,1] 범위를 벗어날 수 있다.\"\n");
        sb.Append("  ],\n");
        sb.Append("  \"eases\": [\n");

        for (int e = 0; e < eases.Count; e++)
        {
            Ease ease = eases[e];
            sb.Append("    { \"name\": \"").Append(ease.ToString()).Append("\", \"samples\": [");

            for (int i = 0; i < SampleCount; i++)
            {
                float t = i / 256f;
                float v = EaseManager.Evaluate(ease, null, t, 1f, overshootOrAmplitude, period);

                if (i > 0) sb.Append(", ");
                sb.Append(F(v));
            }

            sb.Append("] }").Append(e < eases.Count - 1 ? "," : "").Append('\n');
        }

        sb.Append("  ]\n");
        sb.Append("}\n");

        // 바이트 동일성의 마지막 조각: BOM 없는 UTF-8, LF 고정(StringBuilder에 \n만 썼다).
        File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(false));

        Debug.Log($"[EaseGoldenExporter] 완료. {eases.Count}종 × {SampleCount}샘플 → {outPath}");
    }

    /// <summary>표준 Ease 목록 — enum 선언 순서 그대로(결정적).</summary>
    private static List<Ease> StandardEases()
    {
        List<Ease> eases = new();

        foreach (Ease ease in Enum.GetValues(typeof(Ease)))
        {
            if (ease == Ease.Unset) continue;
            if (ease.ToString().StartsWith("INTERNAL_", StringComparison.Ordinal)) continue;
            eases.Add(ease);
        }

        return eases;
    }

    /// <summary>float → 9유효숫자(라운드트립 충분), invariant. 재덤프 결정성의 핵심.
    /// "R"이 아니라 "G9"인 이유: "R"은 Mono와 .NET Core가 표기를 다르게 낸다 —
    /// 명시 정밀도 지정자는 양쪽에서 같다.</summary>
    private static string F(float value)
        => value.ToString("G9", CultureInfo.InvariantCulture);
}
