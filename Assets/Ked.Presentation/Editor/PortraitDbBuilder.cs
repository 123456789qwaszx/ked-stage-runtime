#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PortraitDbBuilder
{
    private const string DefaultSettingsPath = "Assets/Settings/PortraitBuildSettings.asset";

    [MenuItem("Tools/Build Generated Db (Strict)")]
    public static void BuildStrict() => BuildInternal(true);

    private static void BuildInternal(bool forceStrict)
    {
        var settings = LoadOrCreateSettings();
        var strictMode = forceStrict || settings.strictMode;

        EnsureFolder(Path.GetDirectoryName(settings.generatedDbPath));

        var db = AssetDatabase.LoadAssetAtPath<PortraitGeneratedDBSO>(settings.generatedDbPath);
        if (!db)
        {
            db = ScriptableObject.CreateInstance<PortraitGeneratedDBSO>();
            AssetDatabase.CreateAsset(db, settings.generatedDbPath);
        }

        var report = new List<string>();
        var entries = ScanPortraits(settings, report);

        Undo.RecordObject(db, "Build Portrait Generated Db");
        db.entries = entries;
        db.generatedTicksUtc = DateTime.UtcNow.Ticks;
        db.generatedTimeReadable = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        // Report
        var reportText = BuildReportText(db, report, strictMode);
        if (strictMode && HasErrors(report))
        {
            Debug.LogError(reportText, db);
            throw new Exception("Portrait Db build failed (strict mode). See console for details.");
        }
        else
        {
            Debug.Log(reportText, db);
        }

        Selection.activeObject = db;
        EditorGUIUtility.PingObject(db);
    }

    private static List<PortraitGeneratedDBSO.Entry> ScanPortraits(PortraitBuildSettings settings, List<string> report)
    {
        var list = new List<PortraitGeneratedDBSO.Entry>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        var validRoots = new List<string>();
        foreach (var folder in settings.scanFolders)
        {
            var root = (folder ?? "").Replace('\\', '/').Trim();
            if (string.IsNullOrEmpty(root)) continue;

            if (!AssetDatabase.IsValidFolder(root))
            {
                report.Add($"[ERROR] Scan folder not found: {root}");
                continue;
            }

            validRoots.Add(root);
        }

        if (validRoots.Count == 0)
        {
            report.Add("[ERROR] No valid scan folders");
            return list;
        }

        string[] guids = AssetDatabase.FindAssets("t:Sprite", validRoots.ToArray());

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.textureType != TextureImporterType.Sprite)
                continue;

            if (!TryParse(path, validRoots, out var cid, out var variant, out var fileEmotionToken))
            {
                // 폴더 규약(<root>/<캐릭터>/<변형>/<표정>.png) 밖의 파일.
                // 스캔 뿌리 안에 있는데 모양이 다르면 실수일 확률이 높으니 소리를 낸다.
                report.Add($"[WARN] Not in <character>/<variant>/<emotion> layout: {path}");
                continue;
            }

            var sprites = LoadSprites(path);

            if (sprites.Length == 0)
            {
                report.Add($"[WARN] No sprite found: {path}");
                continue;
            }

            if (sprites.Length == 1)
            {
                // 낱장: 파일 이름이 곧 표정 코드다 (01.png / 1.png).
                var emotionKey = PortraitResolver.NormalizeEmotionCode(fileEmotionToken);

                if (string.IsNullOrEmpty(emotionKey))
                {
                    report.Add($"[WARN] Filename is not a numeric emotion code: '{fileEmotionToken}' ({path})");
                    continue;
                }

                AddEntry(cid, variant, emotionKey, sprites[0], path);
            }
            else
            {
                // 시트: 서브 스프라이트 이름이 표정 코드다. 변형은 폴더가 정한다.
                foreach (var sp in sprites)
                {
                    if (!sp) continue;

                    string raw = (sp.name ?? "").Trim();
                    if (string.IsNullOrEmpty(raw))
                    {
                        report.Add($"[WARN] Sub-sprite has empty name: {path}");
                        continue;
                    }

                    string emotion = PortraitResolver.NormalizeEmotionCode(raw);

                    if (string.IsNullOrEmpty(emotion))
                    {
                        report.Add($"[WARN] Sub-sprite emotion is not numeric code: '{raw}' ({path})");
                        continue;
                    }

                    AddEntry(cid, variant, emotion, sp, path);
                }
            }
        }

        list.Sort((a, b) =>
        {
            int c = string.Compare(a.characterId, b.characterId, StringComparison.Ordinal);
            if (c != 0) return c;
            c = string.Compare(a.variantKey, b.variantKey, StringComparison.Ordinal);
            if (c != 0) return c;
            return string.Compare(a.emotionKey, b.emotionKey, StringComparison.Ordinal);
        });

        return list;

        void AddEntry(string cid, string variant, string emotion, Sprite sprite, string assetPath)
        {
            var key = cid + "|" + variant + "|" + emotion;
            if (!seen.Add(key))
            {
                report.Add($"[ERROR] Duplicate: {key} ({assetPath})");
                return;
            }

            list.Add(new PortraitGeneratedDBSO.Entry
            {
                characterId = cid,
                variantKey  = variant,
                emotionKey  = emotion,
                sprite      = sprite,
                assetPath   = assetPath
            });
        }
    }

    private static Sprite[] LoadSprites(string path)
    {
        // 1) Sub-sprites 시도
        var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
            .OfType<Sprite>()
            .ToArray();

        if (sprites.Length > 0)
            return sprites;

        // 2) Single sprite 시도
        var single = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (single)
            return new[] { single };

        return new Sprite[0];
    }

    // 초상 에셋 규약: <스캔 뿌리>/<캐릭터>/<변형>/<표정>.png
    //
    //   Assets/Samples/Portraits/harin/b/03.png
    //   뿌리 = Assets/Samples/Portraits, 캐릭터 = harin, 변형 = b, 표정 = 03
    //
    // 변형이 폴더로 서면서 파일 이름에서 캐릭터·변형이 빠졌다. 그래서 variantKey는
    // 종전의 'harin_b'가 아니라 폴더 이름 그대로인 'b'다 —
    // 조회 쪽(PortraitResolver·코어 PortraitKeyNormalizer)도 변형 키를 문자열 전체로 보므로
    // 여기서 내는 값이 곧 대본이 쓰는 변형 이름이다. 'school'·'casual'처럼 여러 글자도 된다.
    private static bool TryParse(
        string assetPath,
        List<string> validRoots,
        out string characterId,
        out string variantKey,
        out string emotionToken)
    {
        characterId = "";
        variantKey = "";
        emotionToken = "";

        assetPath = assetPath.Replace('\\', '/');

        string matchedPrefix = null;
        foreach (var root in validRoots)
        {
            var prefix = root.TrimEnd('/') + "/";
            if (assetPath.StartsWith(prefix, StringComparison.Ordinal))
            {
                matchedPrefix = prefix;
                break;
            }
        }

        if (matchedPrefix == null) return false;

        string rest = assetPath.Substring(matchedPrefix.Length); // {cid}/{variant}/{file}
        var parts = rest.Split('/');

        // 규칙: root/cid/variant/file (3단계 고정)
        if (parts.Length != 3) return false;

        characterId = parts[0].Trim();
        variantKey = parts[1].Trim();
        emotionToken = (Path.GetFileNameWithoutExtension(parts[2]) ?? "").Trim();

        if (string.IsNullOrEmpty(characterId)) return false;
        if (string.IsNullOrEmpty(variantKey)) return false;
        if (string.IsNullOrEmpty(emotionToken)) return false;

        return true;
    }

    private static string BuildReportText(PortraitGeneratedDBSO db, List<string> report, bool strictMode)
    {
        var head =
            $"[PortraitDb] Build complete ({(strictMode ? "STRICT" : "NORMAL")} mode)\n" +
            $"  Entries: {db.TotalEntries}\n" +
            $"  Characters: {db.TotalCharacters}\n" +
            $"  Variants: {db.TotalVariants}\n" +
            $"  Unique Emotions: {db.TotalUniqueEmotions}\n" +
            $"  Generated: {db.generatedTimeReadable}\n";

        if (report.Count == 0) return head + "No warnings/errors.";

        return head + "\n" + string.Join("\n", report);
    }

    private static bool HasErrors(List<string> report)
        => report.Any(r => r.Contains("[ERROR]"));

    // 설정 에셋은 타입으로 찾는다 — 어느 폴더에 두든 된다(예제는 Assets/Samples/Data/).
    // 하나도 없을 때만 기본 자리(DefaultSettingsPath)에 새로 만든다.
    private static PortraitBuildSettings LoadOrCreateSettings()
    {
        PortraitBuildSettings settings = null;

        string[] guids = AssetDatabase.FindAssets($"t:{nameof(PortraitBuildSettings)}");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            if (guids.Length > 1)
                Debug.LogWarning($"PortraitBuildSettings가 {guids.Length}개 있다. 첫 번째를 쓴다: {path}");

            settings = AssetDatabase.LoadAssetAtPath<PortraitBuildSettings>(path);
        }

        if (!settings)
        {
            EnsureFolder(Path.GetDirectoryName(DefaultSettingsPath));
            settings = ScriptableObject.CreateInstance<PortraitBuildSettings>();
            AssetDatabase.CreateAsset(settings, DefaultSettingsPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created default settings at {DefaultSettingsPath}", settings);
        }

        return settings;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath)) return;
        folderPath = folderPath.Replace('\\', '/');
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        var parts = folderPath.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
#endif
