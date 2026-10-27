#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CharacterFocusDebugView : MonoBehaviour
{
    [Serializable]
    public sealed class Point
    {
        public bool enabled = true;

        [Header("Identity")]
        public string label = "";

        [Header("Focus")]
        public CharacterFocusPreset focusPreset = CharacterFocusPreset.Face;

        [Header("Visual")]
        public Color color = Color.white;
        public float radius = 12f;

        [Header("Final Offset")]
        [Tooltip("프리셋/DB 보정 후 이 포인트에만 마지막으로 더하는 수동 보정값입니다.")]
        public Vector2 finalOffset = Vector2.zero;

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(label))
                    return label.Trim();

                return focusPreset.ToString();
            }
        }
    }

#if UNITY_EDITOR
    private readonly struct ScenePoint
    {
        public readonly Vector3 world;
        public readonly Point point;
        public readonly string label;

        public ScenePoint(
            Vector3 world,
            Point point,
            string label)
        {
            this.world = world;
            this.point = point;
            this.label = label;
        }
    }
#endif

    [Header("Marker Root")]
    [Tooltip("마커들이 생성될 DebugView Root입니다. 비워두면 이 오브젝트의 RectTransform을 사용합니다.")]
    [SerializeField] private RectTransform markerRoot;

    [Header("Observed Slots")]
    [Tooltip("디버그 표시를 시도할 캐릭터 슬롯들입니다. Cast되지 않은 슬롯은 조용히 무시합니다.")]
    [SerializeField] private List<string> observedSlotKeys = new()
    {
        "c1",
        "c2",
        "c3",
    };

    [Header("Tuning")]
    [SerializeField] private CharacterFocusTuningDBSO focusTuningDb;

    [Header("Points")]
    [SerializeField] private List<Point> points = new()
    {
        new Point
        {
            label = "Feet",
            focusPreset = CharacterFocusPreset.Feet,
            color = new Color(0.4f, 0.8f, 1f, 0.9f),
            radius = 13f,
        },
        new Point
        {
            label = "Body",
            focusPreset = CharacterFocusPreset.Body,
            color = new Color(0.4f, 1f, 0.4f, 0.9f),
            radius = 14f,
        },
        new Point
        {
            label = "Bust",
            focusPreset = CharacterFocusPreset.Bust,
            color = new Color(1f, 0.8f, 0.25f, 0.9f),
            radius = 15f,
        },
        new Point
        {
            label = "Face",
            focusPreset = CharacterFocusPreset.Face,
            color = new Color(1f, 0.35f, 0.35f, 0.95f),
            radius = 16f,
        },
    };

    [Header("Game View Draw")]
    [SerializeField] private bool drawInGameView = true;
    [SerializeField] private bool hideInPlayMode = false;

    [Header("Scene View Draw")]
    [SerializeField] private bool drawInSceneView = true;
    [SerializeField] private bool drawLabels = true;

    private PresentationStage _stage;
    private IShotResponseStageProvider _stageProvider;

    private readonly Dictionary<string, RectTransform> _markerMap =
        new(StringComparer.Ordinal);

    private readonly HashSet<string> _visibleMarkerKeysThisFrame =
        new(StringComparer.Ordinal);

#if UNITY_EDITOR
    private readonly List<ScenePoint> _scenePoints = new();
    private GUIStyle _cachedLabelStyle;
#endif

    private static Sprite _cachedMarkerSprite;

    private const string MarkerNameToken = "__FocusPoint_";

    public void Initialize(
        PresentationStage stage,
        IShotResponseStageProvider stageProvider,
        CharacterFocusTuningDBSO tuningDb)
    {
        _stage = stage;
        _stageProvider = stageProvider;

        if (tuningDb != null)
            focusTuningDb = tuningDb;

        ResolveMarkerRoot();
        HideAllMarkers();
    }

    private void Reset()
    {
        ResolveMarkerRoot();
    }

    private void OnEnable()
    {
        ResolveMarkerRoot();
    }

    private void OnValidate()
    {
        ResolveMarkerRoot();

#if UNITY_EDITOR
        _cachedLabelStyle = null;
#endif
    }

    private void LateUpdate()
    {
        if (Application.isPlaying && hideInPlayMode)
        {
            ClearScenePoints();
            HideAllMarkers();
            return;
        }

        if (!drawInGameView && !drawInSceneView)
        {
            ClearScenePoints();
            HideAllMarkers();
            return;
        }

        UpdateFocusMarkers();
    }

    [ContextMenu("Hide Focus Debug Markers")]
    public void HideAllMarkers()
    {
        foreach (KeyValuePair<string, RectTransform> pair in _markerMap)
            SetMarkerVisible(pair.Value, false);
    }

    private void UpdateFocusMarkers()
    {
        _visibleMarkerKeysThisFrame.Clear();
        ClearScenePoints();

        if (_stage == null || _stageProvider == null)
        {
            HideAllMarkers();
            return;
        }

        ResolveMarkerRoot();

        if (markerRoot == null)
        {
            HideAllMarkers();
            return;
        }

        RectTransform rigSpaceRoot = _stageProvider.RigSpaceRoot;

        if (rigSpaceRoot == null)
        {
            HideAllMarkers();
            return;
        }

        for (int i = 0; i < observedSlotKeys.Count; i++)
        {
            string slotKey = NormalizeSlotKey(observedSlotKeys[i]);

            if (string.IsNullOrEmpty(slotKey))
                continue;

            // DebugView는 매 프레임 돌기 때문에 경고 로그 없는 조회 API만 사용한다.
            if (!_stage.castRegistry.TryPeekCharacter(slotKey, out string characterKey))
                continue;

            if (string.IsNullOrWhiteSpace(characterKey))
                continue;

            if (!_stage.characterRigs.TryPeekRig(slotKey, out CharacterRigRefs rigRefs))
                continue;

            _stage.castRegistry.TryPeekFacing(slotKey, out CharacterFacing facing);

            UpdateCharacterFocusMarkers(
                slotKey,
                characterKey.Trim(),
                rigRefs,
                rigSpaceRoot,
                facing);
        }

        HideStaleMarkers();
    }

    private void UpdateCharacterFocusMarkers(
        string slotKey,
        string characterKey,
        CharacterRigRefs rigRefs,
        RectTransform rigSpaceRoot,
        CharacterFacing facing)
    {
        for (int i = 0; i < points.Count; i++)
        {
            Point point = points[i];

            if (point == null || !point.enabled)
                continue;

            if (!CharacterFocusPointResolver.TryResolveFromRigRefs(
                    rigRefs,
                    rigSpaceRoot,
                    characterKey,
                    point.focusPreset,
                    point.finalOffset,
                    focusTuningDb,
                    useSettledPlacementTargets: true,
                    facing: facing,
                    result: out CharacterFocusPointResult focus))
            {
                continue;
            }

            Vector3 world =
                focus.RigSpaceRoot.TransformPoint(
                    new Vector3(
                        focus.FocusPointInRigSpace.x,
                        focus.FocusPointInRigSpace.y,
                        0f));

            string markerKey = BuildMarkerKey(slotKey, point);
            string markerName = BuildMarkerName(slotKey, characterKey, point);

            RegisterScenePoint(world, point, markerName);

            if (!drawInGameView)
                continue;

            RectTransform marker = EnsureMarker(markerKey, markerName);

            if (marker == null)
                continue;

            Vector3 local = markerRoot.InverseTransformPoint(world);

            marker.anchoredPosition = new Vector2(local.x, local.y);

            float diameter = Mathf.Max(1f, point.radius) * 2f;

            marker.sizeDelta = new Vector2(diameter, diameter);
            marker.localScale = Vector3.one;
            marker.localRotation = Quaternion.identity;

            Image image = marker.GetComponent<Image>();
            if (image != null)
            {
                image.color = point.color;
                image.raycastTarget = false;
                image.enabled = true;
            }

            SetMarkerVisible(marker, true);
            _visibleMarkerKeysThisFrame.Add(markerKey);
        }
    }

    private RectTransform EnsureMarker(
        string markerKey,
        string markerName)
    {
        if (_markerMap.TryGetValue(markerKey, out RectTransform cached) &&
            cached != null)
        {
            return cached;
        }

        RectTransform existing = FindExistingMarker(markerName);

        if (existing != null)
        {
            _markerMap[markerKey] = existing;
            return existing;
        }

        GameObject go = new GameObject(
            markerName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        // Debug marker 전용. 씬/프리팹에 저장하지 않는다.
        go.hideFlags = HideFlags.DontSave;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            Undo.RegisterCreatedObjectUndo(go, "Create Character Focus Debug Marker");
#endif

        RectTransform marker = go.GetComponent<RectTransform>();
        marker.SetParent(markerRoot, false);

        marker.anchorMin = new Vector2(0.5f, 0.5f);
        marker.anchorMax = new Vector2(0.5f, 0.5f);
        marker.pivot = new Vector2(0.5f, 0.5f);
        marker.localScale = Vector3.one;
        marker.localRotation = Quaternion.identity;

        Image image = marker.GetComponent<Image>();
        image.raycastTarget = false;
        image.sprite = LoadMarkerSprite();

        _markerMap[markerKey] = marker;
        return marker;
    }

    private RectTransform FindExistingMarker(string markerName)
    {
        if (markerRoot == null)
            return null;

        for (int i = 0; i < markerRoot.childCount; i++)
        {
            RectTransform child = markerRoot.GetChild(i) as RectTransform;

            if (child == null)
                continue;

            if (string.Equals(child.name, markerName, StringComparison.Ordinal))
                return child;
        }

        return null;
    }

    private void HideStaleMarkers()
    {
        foreach (KeyValuePair<string, RectTransform> pair in _markerMap)
        {
            if (_visibleMarkerKeysThisFrame.Contains(pair.Key))
                continue;

            SetMarkerVisible(pair.Value, false);
        }
    }

    private static void SetMarkerVisible(
        RectTransform marker,
        bool visible)
    {
        if (marker == null)
            return;

        Graphic graphic = marker.GetComponent<Graphic>();

        if (graphic != null)
            graphic.enabled = visible;
    }

    private void ResolveMarkerRoot()
    {
        if (markerRoot != null)
            return;

        markerRoot = transform as RectTransform;
    }

    private static string NormalizeSlotKey(string raw)
    {
        return (raw ?? "").Trim();
    }

    private static string BuildMarkerKey(
        string slotKey,
        Point point)
    {
        return slotKey + "/" + point.DisplayName;
    }

    private static string BuildMarkerName(
        string slotKey,
        string characterKey,
        Point point)
    {
        return MarkerNameToken +
               slotKey +
               "_" +
               characterKey +
               "_" +
               point.DisplayName;
    }

    private static Sprite LoadMarkerSprite()
    {
        if (_cachedMarkerSprite != null)
            return _cachedMarkerSprite;

#if UNITY_EDITOR
        Sprite editorSprite =
            AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        if (editorSprite != null)
        {
            _cachedMarkerSprite = editorSprite;
            return _cachedMarkerSprite;
        }
#endif

        Texture2D tex = new Texture2D(
            1,
            1,
            TextureFormat.ARGB32,
            false);

        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        _cachedMarkerSprite = Sprite.Create(
            tex,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f));

        return _cachedMarkerSprite;
    }

    private void ClearScenePoints()
    {
#if UNITY_EDITOR
        _scenePoints.Clear();
#endif
    }

    private void RegisterScenePoint(
        Vector3 world,
        Point point,
        string label)
    {
#if UNITY_EDITOR
        _scenePoints.Add(new ScenePoint(world, point, label));
#endif
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawInSceneView)
            return;

        for (int i = 0; i < _scenePoints.Count; i++)
            DrawScenePoint(_scenePoints[i]);
    }

    private void DrawScenePoint(ScenePoint scenePoint)
    {
        Point point = scenePoint.point;

        if (point == null)
            return;

        Color prevColor = Handles.color;
        Handles.color = point.color;

        float radius = Mathf.Max(1f, point.radius);

        Handles.DrawSolidDisc(
            scenePoint.world,
            Vector3.forward,
            radius);

        Handles.DrawWireDisc(
            scenePoint.world,
            Vector3.forward,
            radius + 2f);

        if (drawLabels)
        {
            EnsureLabelStyle();
            _cachedLabelStyle.normal.textColor = point.color;

            Handles.Label(
                scenePoint.world + new Vector3(radius + 4f, radius + 4f, 0f),
                scenePoint.label,
                _cachedLabelStyle);
        }

        Handles.color = prevColor;
    }

    private void EnsureLabelStyle()
    {
        if (_cachedLabelStyle != null)
            return;

        _cachedLabelStyle = new GUIStyle(EditorStyles.boldLabel);
    }
#endif
}