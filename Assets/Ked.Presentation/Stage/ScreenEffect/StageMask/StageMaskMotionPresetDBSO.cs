using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(
    menuName = "CPS/Presentation/Stage Mask Motion Preset DB",
    fileName = "StageMaskMotionPresetDB")]
public sealed class StageMaskMotionPresetDBSO : ScriptableObject
{
    public const string DefaultPresetKey = "hstrip_open";

    [Serializable]
    public struct Entry
    {
        [Tooltip("Yarn/Command에서 사용할 preset key. ex) hstrip_open, hstrip_in, iris_in")]
        public string key;

        [Header("Shape")]
        public StageMaskKind kind;

        [Header("Motion")]
        public Vector2 fromOffset;
        public Vector2 toOffset;

        [Header("Slanted")]
        public float slantPixels;
        public bool slantToRight;
        public bool flipVertical;

        [Header("Horizontal Strip")]
        public float stripHeightPixels;
        public float horizontalBleedPixels;
        public bool animateStripHeight;
        public float fromStripHeightPixels;

        [Header("Vertical Strip")]
        public float verticalStripWidthPixels;
        public float verticalBleedPixels;
        public bool animateStripWidth;
        public float fromVerticalStripWidthPixels;

        [Header("Diagonal Band")]
        public float diagonalBandWidthPixels;
        public float diagonalBandSlantPixels;
        public float diagonalBandBleedPixels;
        public bool diagonalBandToRight;

        [Header("Circle Iris")]
        public float fromIrisRadiusPixels;
        public float toIrisRadiusPixels;
        public float irisAspect;
        [Range(12, 128)] public int irisSegments;

        [Header("Edge")]
        public bool showEdge;
        public StageMaskEdgeMode edgeMode;
        public Color edgeColor;
        public float edgeThickness;
        public bool hideEdgeOnComplete;

        [Header("Tween")]
        public float duration;
        public Ease ease;

        [Header("Rubber")]
        public StageMaskRubberMode rubberMode;
        public float overshootPixels;
        [Range(0.01f, 0.99f)] public float overshootStart;
        public float pullPixels;
        [Range(0.01f, 0.99f)] public float pullEnd;

        [Header("Completion")]
        public bool fullVisibleOnComplete;
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<string, Entry> _map;

    public bool TryGet(string key, out Entry entry)
    {
        if (_map == null)
            Build();

        return _map.TryGetValue(NormalizeKey(key), out entry);
    }

    public static string NormalizeKey(string key)
    {
        key = (key ?? "").Trim();

        if (string.IsNullOrEmpty(key))
            return DefaultPresetKey;

        key = key.ToLowerInvariant();
        key = key.Replace(" ", "_");
        key = key.Replace("-", "_");

        // Yarn 커맨드 네임스페이스 접두사 흡수: "tx_hstrip_open" -> "hstrip_open"
        if (key.StartsWith("tx_"))
            key = key.Substring(3);

        return key;
    }

    private void OnEnable() => _map = null;

    // 플레이 모드 인스펙터 수정 -> 캐시 무효화 -> 다음 발화에 반영.
    private void OnValidate() => _map = null;

    private void Build()
    {
        _map = new Dictionary<string, Entry>(StringComparer.Ordinal);

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            string key = NormalizeKey(entry.key);

            if (string.IsNullOrEmpty(key))
                continue;

            entry.key = key;
            _map[key] = entry;
        }
    }

    private void Reset()
    {
        entries = new List<Entry>
        {
            new()
            {
                key = "slant_in",
                kind = StageMaskKind.Slanted,
                fromOffset = new Vector2(-2200f, 0f),
                toOffset = new Vector2(-770f, 0f),
                slantPixels = 220f, slantToRight = false, flipVertical = true,
                showEdge = true, edgeMode = StageMaskEdgeMode.Leading,
                edgeColor = new Color(1f, 1f, 1f, 0.92f), edgeThickness = 6f,
                hideEdgeOnComplete = false,
                duration = 0.65f, ease = Ease.OutCubic,
                rubberMode = StageMaskRubberMode.OvershootEnd,
                overshootPixels = 72f, overshootStart = 0.72f,
            },
            new()
            {
                key = "slant_out",
                kind = StageMaskKind.Slanted,
                fromOffset = new Vector2(-770f, 0f),
                toOffset = new Vector2(-2200f, 0f),
                slantPixels = 220f, slantToRight = false, flipVertical = true,
                showEdge = true, edgeMode = StageMaskEdgeMode.Leading,
                edgeColor = new Color(1f, 1f, 1f, 0.86f), edgeThickness = 6f,
                hideEdgeOnComplete = true,
                duration = 0.45f, ease = Ease.InCubic,
                rubberMode = StageMaskRubberMode.PullStart,
                pullPixels = 24f, pullEnd = 0.28f,
            },
            new()
            {
                key = "hstrip_open",
                kind = StageMaskKind.HorizontalStrip,
                fromOffset = Vector2.zero, toOffset = Vector2.zero,
                animateStripHeight = true,
                fromStripHeightPixels = 0f, stripHeightPixels = 360f,
                horizontalBleedPixels = 96f,
                showEdge = true, edgeMode = StageMaskEdgeMode.Both,
                edgeColor = new Color(1f, 1f, 1f, 0.82f), edgeThickness = 4f,
                hideEdgeOnComplete = false,
                duration = 0.45f, ease = Ease.OutCubic,
                rubberMode = StageMaskRubberMode.None,
            },
            new()
            {
                key = "hstrip_close",
                kind = StageMaskKind.HorizontalStrip,
                fromOffset = Vector2.zero, toOffset = Vector2.zero,
                animateStripHeight = true,
                fromStripHeightPixels = 360f, stripHeightPixels = 0f,
                horizontalBleedPixels = 96f,
                showEdge = true, edgeMode = StageMaskEdgeMode.Both,
                edgeColor = new Color(1f, 1f, 1f, 0.72f), edgeThickness = 4f,
                hideEdgeOnComplete = true,
                duration = 0.34f, ease = Ease.InCubic,
                rubberMode = StageMaskRubberMode.None,
            },
            new()
            {
                key = "hstrip_in",
                kind = StageMaskKind.HorizontalStrip,
                fromOffset = new Vector2(-2200f, 0f), toOffset = Vector2.zero,
                stripHeightPixels = 360f, horizontalBleedPixels = 96f,
                showEdge = true, edgeMode = StageMaskEdgeMode.Both,
                edgeColor = new Color(1f, 1f, 1f, 0.82f), edgeThickness = 4f,
                hideEdgeOnComplete = false,
                duration = 0.45f, ease = Ease.OutCubic,
                rubberMode = StageMaskRubberMode.OvershootEnd,
                overshootPixels = 48f, overshootStart = 0.74f,
            },
            new()
            {
                key = "hstrip_out",
                kind = StageMaskKind.HorizontalStrip,
                fromOffset = Vector2.zero, toOffset = new Vector2(2200f, 0f),
                stripHeightPixels = 360f, horizontalBleedPixels = 96f,
                showEdge = true, edgeMode = StageMaskEdgeMode.Both,
                edgeColor = new Color(1f, 1f, 1f, 0.72f), edgeThickness = 4f,
                hideEdgeOnComplete = true,
                duration = 0.34f, ease = Ease.InCubic,
                rubberMode = StageMaskRubberMode.PullStart,
                pullPixels = 18f, pullEnd = 0.25f,
            },
            new()
            {
                key = "vstrip_open",
                kind = StageMaskKind.VerticalStrip,
                fromOffset = Vector2.zero, toOffset = Vector2.zero,
                animateStripWidth = true,
                fromVerticalStripWidthPixels = 0f, verticalStripWidthPixels = 520f,
                verticalBleedPixels = 96f,
                showEdge = true, edgeMode = StageMaskEdgeMode.Both,
                edgeColor = new Color(1f, 1f, 1f, 0.78f), edgeThickness = 4f,
                hideEdgeOnComplete = false,
                duration = 0.42f, ease = Ease.OutCubic,
                rubberMode = StageMaskRubberMode.None,
            },
            new()
            {
                key = "vstrip_close",
                kind = StageMaskKind.VerticalStrip,
                fromOffset = Vector2.zero, toOffset = Vector2.zero,
                animateStripWidth = true,
                fromVerticalStripWidthPixels = 520f, verticalStripWidthPixels = 0f,
                verticalBleedPixels = 96f,
                showEdge = true, edgeMode = StageMaskEdgeMode.Both,
                edgeColor = new Color(1f, 1f, 1f, 0.72f), edgeThickness = 4f,
                hideEdgeOnComplete = true,
                duration = 0.32f, ease = Ease.InCubic,
                rubberMode = StageMaskRubberMode.None,
            },
            new()
            {
                key = "vstrip_in",
                kind = StageMaskKind.VerticalStrip,
                fromOffset = new Vector2(2200f, 0f), toOffset = Vector2.zero,
                verticalStripWidthPixels = 520f, verticalBleedPixels = 96f,
                showEdge = true, edgeMode = StageMaskEdgeMode.Both,
                edgeColor = new Color(1f, 1f, 1f, 0.78f), edgeThickness = 4f,
                hideEdgeOnComplete = false,
                duration = 0.42f, ease = Ease.OutCubic,
                rubberMode = StageMaskRubberMode.OvershootEnd,
                overshootPixels = 42f, overshootStart = 0.72f,
            },
            new()
            {
                key = "vstrip_out",
                kind = StageMaskKind.VerticalStrip,
                fromOffset = Vector2.zero, toOffset = new Vector2(2200f, 0f),
                verticalStripWidthPixels = 520f, verticalBleedPixels = 96f,
                showEdge = true, edgeMode = StageMaskEdgeMode.Both,
                edgeColor = new Color(1f, 1f, 1f, 0.72f), edgeThickness = 4f,
                hideEdgeOnComplete = true,
                duration = 0.32f, ease = Ease.InCubic,
                rubberMode = StageMaskRubberMode.PullStart,
                pullPixels = 18f, pullEnd = 0.25f,
            },
            new()
            {
                key = "band_in",
                kind = StageMaskKind.DiagonalBand,
                fromOffset = new Vector2(-2600f, 0f), toOffset = new Vector2(1100f, 0f),
                diagonalBandWidthPixels = 760f, diagonalBandSlantPixels = 520f,
                diagonalBandBleedPixels = 320f, diagonalBandToRight = true,
                showEdge = true, edgeMode = StageMaskEdgeMode.Both,
                edgeColor = new Color(1f, 1f, 1f, 0.88f), edgeThickness = 5f,
                hideEdgeOnComplete = false,
                duration = 0.38f, ease = Ease.OutQuart,
                rubberMode = StageMaskRubberMode.None,
            },
            new()
            {
                key = "band_out",
                kind = StageMaskKind.DiagonalBand,
                fromOffset = new Vector2(1100f, 0f), toOffset = new Vector2(2600f, 0f),
                diagonalBandWidthPixels = 760f, diagonalBandSlantPixels = 520f,
                diagonalBandBleedPixels = 320f, diagonalBandToRight = true,
                showEdge = true, edgeMode = StageMaskEdgeMode.Both,
                edgeColor = new Color(1f, 1f, 1f, 0.78f), edgeThickness = 5f,
                hideEdgeOnComplete = true,
                duration = 0.28f, ease = Ease.InQuart,
                rubberMode = StageMaskRubberMode.None,
            },
            new()
            {
                key = "iris_in",
                kind = StageMaskKind.CircleIris,
                fromOffset = Vector2.zero, toOffset = Vector2.zero,
                fromIrisRadiusPixels = 0f, toIrisRadiusPixels = 1280f,
                irisAspect = 1.777f, irisSegments = 72,
                showEdge = true, edgeMode = StageMaskEdgeMode.Outline,
                edgeColor = new Color(1f, 1f, 1f, 0.66f), edgeThickness = 3f,
                hideEdgeOnComplete = true,
                duration = 0.5f, ease = Ease.OutCubic,
                rubberMode = StageMaskRubberMode.None,
            },
            new()
            {
                key = "iris_out",
                kind = StageMaskKind.CircleIris,
                fromOffset = Vector2.zero, toOffset = Vector2.zero,
                fromIrisRadiusPixels = 1280f, toIrisRadiusPixels = 0f,
                irisAspect = 1.777f, irisSegments = 72,
                showEdge = true, edgeMode = StageMaskEdgeMode.Outline,
                edgeColor = new Color(1f, 1f, 1f, 0.66f), edgeThickness = 3f,
                hideEdgeOnComplete = true,
                duration = 0.42f, ease = Ease.InCubic,
                rubberMode = StageMaskRubberMode.None,
            },
            new()
            {
                key = "daze_close",
                kind = StageMaskKind.HorizontalStrip,
                fromOffset = Vector2.zero, toOffset = Vector2.zero,
                animateStripHeight = true,
                fromStripHeightPixels = 680f, stripHeightPixels = 0f,
                horizontalBleedPixels = 96f,
                showEdge = true, edgeMode = StageMaskEdgeMode.Both,
                edgeColor = new Color(1f, 1f, 1f, 0.32f), edgeThickness = 2f,
                hideEdgeOnComplete = true,
                duration = 0.85f, ease = Ease.InOutCubic,
                rubberMode = StageMaskRubberMode.None,
            },
            new()
            {
                key = "daze_open",
                kind = StageMaskKind.HorizontalStrip,
                fromOffset = Vector2.zero, toOffset = Vector2.zero,
                animateStripHeight = true,
                fromStripHeightPixels = 0f, stripHeightPixels = 680f,
                horizontalBleedPixels = 96f,
                showEdge = true, edgeMode = StageMaskEdgeMode.Both,
                edgeColor = new Color(1f, 1f, 1f, 0.32f), edgeThickness = 2f,
                hideEdgeOnComplete = true,
                duration = 0.65f, ease = Ease.InOutCubic,
                fullVisibleOnComplete = true,
                rubberMode = StageMaskRubberMode.None,
            },
            new()
            {
                key = "strip_cover",
                kind = StageMaskKind.FullRect,
                fromOffset = Vector2.zero,
                toOffset = new Vector2(2400f, 0f),
                showEdge = true, edgeMode = StageMaskEdgeMode.Leading,  // 쓸고 지나가는 단일 날
                edgeColor = new Color(1f, 1f, 1f, 0.4f), edgeThickness = 4f,
                hideEdgeOnComplete = true,
                duration = 0.4f, ease = Ease.Linear,
                rubberMode = StageMaskRubberMode.None,
            },
            new()
            {
                key = "strip_clear",
                kind = StageMaskKind.FullRect,
                fromOffset = new Vector2(2400f, 0f),
                toOffset = Vector2.zero,                               // 우측에서 슬라이드 인 -> 클리어
                showEdge = true, edgeMode = StageMaskEdgeMode.Leading,
                edgeColor = new Color(1f, 1f, 1f, 0.4f), edgeThickness = 4f,
                hideEdgeOnComplete = true,
                duration = 0.4f, ease = Ease.Linear,
                fullVisibleOnComplete = true,
                rubberMode = StageMaskRubberMode.None,
            },
        };
    }
}