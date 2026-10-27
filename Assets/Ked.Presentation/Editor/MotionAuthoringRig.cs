using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ─────────────────────────────────────────────────────────────────────────────
// 모션 제작 리그 — 메뉴 Ked/Motion/Create Authoring Rig.
//
// 모션은 특정 리그에 묶인 데이터라, 키잉하는 리그는 **스키마가 세운 것**이어야 한다. 손으로 만든 프리팹은
// "지금 같다"일 뿐이고 스키마 생성은 "앞으로도 같다"다. 그래서 재생이 쓰는 CharacterRigBuilder가 여기서도 리그를 세운다
// (런타임과 같은 코드, 같은 계층). 내보내기는 다시 rig-schemas.json의 부모 사슬로 전체 경로를 대조한다(MotionClipExporter) —
// 두 겹이다: 세울 때 스키마, 낼 때 데이터.
//
// 환경: 기준 해상도 캔버스(ExportedTuning/base-resolution.json) 아래 무대 하나, 그 아래 리그. 깊이·카메라는 없다 —
// 모션은 리그 로컬 단위로 만들고 원근·시차·카메라는 재생 때 위에서 곱해진다. 초상은 보이도록 알파를 켠다.
// ─────────────────────────────────────────────────────────────────────────────
public static class MotionAuthoringRig
{
    public const string CanvasName = "[MotionAuthoring]";
    public const string StageName = "Stage";
    public const string RigName = "CharacterRig";

    [MenuItem("Ked/Motion/Create Authoring Rig")]
    public static void CreateFromMenu()
    {
        Sprite portrait = Selection.activeObject as Sprite;
        RectTransform rig = Create(portrait);

        Selection.activeGameObject = rig.gameObject;
        EditorGUIUtility.PingObject(rig.gameObject);

        Debug.Log(
            $"[MotionAuthoringRig] 스키마로 리그를 세웠다: {CanvasName}/{StageName}/{RigName}. " +
            (portrait != null ? $"초상 '{portrait.name}'을 넣었다. " : "초상은 CharacterPortraitSprite_Image에 스프라이트를 넣을 것. ") +
            "Animation 창을 열고 노드의 Anchored Position · Scale · Rotation(Euler)에 키를 찍는다. 완성한 클립은 MotionDefinition에 적어 Ked/Motion/Export motions.json.",
            rig.gameObject);
    }

    /// <summary>캔버스·무대·리그를 세우고 리그 루트를 돌려준다. 캔버스와 무대는 있으면 다시 쓴다.</summary>
    public static RectTransform Create(Sprite portrait = null)
    {
        RectTransform stage = EnsureStage();

        CharacterRigBuilder builder = new();
        RectTransform rig = builder.BuildCharacterRigRoot(null, "", RigName);
        Undo.RegisterCreatedObjectUndo(rig.gameObject, "Create Authoring Rig");
        rig.SetParent(stage, false);

        builder.BindRefsFromRoot(rig, "", out CharacterRigRefs refs);

        // 스폰 직후 알파 0(스키마)은 재생의 fade_in을 위한 것이다 — 제작에서는 보여야 한다.
        CanvasGroup group = refs.CharacterPortraitSprite_Root != null ? refs.CharacterPortraitSprite_Root.GetComponent<CanvasGroup>() : null;

        if (group != null)
            group.alpha = 1f;

        if (portrait != null && refs.CharacterPortraitSprite_Image != null)
        {
            refs.CharacterPortraitSprite_Image.sprite = portrait;
            refs.CharacterPortraitSprite_Image.SetNativeSize();
        }

        return rig;
    }

    private static RectTransform EnsureStage()
    {
        GameObject canvasGo = GameObject.Find(CanvasName);

        if (canvasGo == null)
        {
            canvasGo = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Authoring Canvas");

            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReadReferenceResolution();
            scaler.matchWidthOrHeight = 0.5f;
        }

        Transform stageTr = canvasGo.transform.Find(StageName);

        if (stageTr == null)
        {
            GameObject stageGo = new(StageName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(stageGo, "Create Authoring Stage");
            RectTransform stage = (RectTransform)stageGo.transform;
            stage.SetParent(canvasGo.transform, false);
            stage.anchorMin = Vector2.zero;
            stage.anchorMax = Vector2.one;
            stage.offsetMin = Vector2.zero;
            stage.offsetMax = Vector2.zero;
            stageTr = stage;
        }

        return (RectTransform)stageTr;
    }

    // ExportedTuning/base-resolution.json — 재생 캔버스와 같은 기준 해상도. 없으면 1920×1080.
    private static Vector2 ReadReferenceResolution()
    {
        string path = Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "ExportedTuning", "base-resolution.json");

        if (File.Exists(path))
        {
            BaseResolutionDto dto = JsonUtility.FromJson<BaseResolutionDto>(File.ReadAllText(path));

            if (dto?.referenceResolution != null && dto.referenceResolution.x > 0f && dto.referenceResolution.y > 0f)
                return new Vector2(dto.referenceResolution.x, dto.referenceResolution.y);
        }

        return new Vector2(1920f, 1080f);
    }

    [System.Serializable]
    private sealed class BaseResolutionDto
    {
        public Vector2Dto referenceResolution;
    }

    [System.Serializable]
    private sealed class Vector2Dto
    {
        public float x;
        public float y;
    }
}
