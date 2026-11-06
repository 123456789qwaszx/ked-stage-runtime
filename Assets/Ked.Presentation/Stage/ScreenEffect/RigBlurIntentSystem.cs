using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 블러의 장면 전역 의도 — 초점(focus_on)과 레이어(screen_blur).
///
/// 커맨드는 의도만 넘기고, 이 시스템이 살아 있는 리그마다 목표 블러를 정해 리그의 RigVisualEffectController에 넘긴다.
/// 줄 머리에서 커맨드가 다 돈 뒤(Reconcile — 줄 흐름 LinePresentationFlow가 부른다) 다시 맞춘다 — 뒤에 등장한 리그, size로 바뀐 깊이,
/// cast가 따라간다. 처음 보는 리그는 Snap(등장 중이라 티가 나지 않는다),
/// 목표가 바뀐 리그는 Follow, 압축 중이면 모두 Snap. pause 뒤(같은 줄 후반)의 변화는 다음 줄 머리에서 따라간다.
///
/// 초점은 배역이 있는 리그만 흐린다(배경은 캐스팅하지 않은 슬롯이라 빠진다). 레이어는 그 레이어 아래 리그 전부다.
/// 정지에서 비운다(Clear) — 리플레이가 커맨드를 다시 지나가며 세운다.
/// </summary>
public sealed class RigBlurIntentSystem
{
    private readonly PresentationStage _stage;
    private readonly StageRigRegistry _stages;

    // 초점 의도. 대상 키는 줄마다 다시 푼다 — 배역 키면 슬롯을 옮겨도 따라간다.
    private bool _hasFocus;
    private string _focusRoleKey;
    private float _focusFalloff;
    private float _focusMaxBlur;
    private float _focusDuration = 0.4f;
    private Ease _focusEase = Ease.OutCubic;

    // 레이어 의도. 양이 0인 레이어는 두지 않는다.
    private readonly Dictionary<(string Stage, float Depth), float> _layerBlur = new();
    private float _layerDuration = 0.8f;
    private Ease _layerEase = Ease.OutCubic;

    // 지난번에 맞춘 살아 있는 리그 — 여기 없는 리그는 처음 보는 것이라 Snap한다. 맞출 때마다 새로 짠다(부서진 리그는 빠진다).
    private HashSet<RigVisualEffectController> _seen = new();
    private HashSet<RigVisualEffectController> _alive = new();

    private readonly List<KeyValuePair<string, CharacterRigRefs>> _rigs = new();

    public RigBlurIntentSystem(PresentationStage stage, StageRigRegistry stages)
    {
        _stage = stage;
        _stages = stages;
    }

    public bool IsFocusTransitioning => AnyTransitioning(focus: true);
    public bool IsLayerTransitioning => AnyTransitioning(focus: false);

    public void SetFocus(string roleKey, float falloff, float maxBlur, float duration, Ease ease, bool snap)
    {
        _hasFocus = true;
        _focusRoleKey = roleKey;
        _focusFalloff = falloff;
        _focusMaxBlur = maxBlur;
        _focusDuration = duration;
        _focusEase = ease;

        if (!TryResolveFocusScale(out _))
            Debug.LogWarning(
                $"[RigBlurIntentSystem] Focus character not found. role='{roleKey}'. " +
                "초점면을 정할 수 없어 리그가 나타날 때까지 초점 블러를 바꾸지 않는다.");

        Apply(snap);
    }

    public void ClearFocus(float duration, Ease ease, bool snap)
    {
        _hasFocus = false;
        _focusRoleKey = null;
        _focusDuration = duration;
        _focusEase = ease;

        Apply(snap);
    }

    public void SetLayerBlur(
        string stage,
        float layerDepth,
        float amount,
        float duration,
        Ease ease,
        bool snap)
    {
        amount = Mathf.Clamp01(amount);

        if (amount > 0f)
            _layerBlur[(stage, layerDepth)] = amount;
        else
            _layerBlur.Remove((stage, layerDepth));

        _layerDuration = duration;
        _layerEase = ease;

        Apply(snap);
    }

    /// <summary>무대 · 층 전부의 레이어 블러를 걷는다(screen_blur_reset).</summary>
    public void ClearLayerBlur(float duration, Ease ease, bool snap)
    {
        _layerBlur.Clear();

        _layerDuration = duration;
        _layerEase = ease;

        Apply(snap);
    }

    /// <summary>줄 머리에서 커맨드가 다 돈 뒤 — 살아 있는 리그를 의도에 다시 맞춘다.</summary>
    public void Reconcile(bool compress) => Apply(compress);

    public void AccelerateFocus(float speedUpMultiplier)
    {
        foreach (RigVisualEffectController fx in _seen)
            fx.AccelerateFocusBlur(speedUpMultiplier);
    }

    public void AccelerateLayer(float speedUpMultiplier)
    {
        foreach (RigVisualEffectController fx in _seen)
            fx.AccelerateLayerBlur(speedUpMultiplier);
    }

    public void Clear()
    {
        _hasFocus = false;
        _focusRoleKey = null;
        _layerBlur.Clear();
        _seen.Clear();
        _alive.Clear();
    }

    private void Apply(bool snapAll)
    {
        // 초점 대상이 아직 없으면(등장 전) 초점 블러는 그대로 둔다.
        float focusScale = 0f;
        bool focusKnown = !_hasFocus || TryResolveFocusScale(out focusScale);

        _rigs.Clear();
        _stage.characterRigs.CollectAliveRigEntries(_rigs);
        _alive.Clear();

        for (int i = 0; i < _rigs.Count; i++)
        {
            CharacterRigRefs refs = _rigs[i].Value;
            RigVisualEffectController fx = refs?.VisualEffect;
            if (fx == null || !_alive.Add(fx))
                continue;

            bool snap = snapAll || !_seen.Contains(fx);

            if (focusKnown && _stage.castRegistry.TryPeekCharacter(_rigs[i].Key, out _))
            {
                float focus = _hasFocus ? CircleOfConfusion(refs.SettledDepthScale, focusScale) : 0f;

                if (snap)
                    fx.SnapFocusBlur(focus);
                else if (!Mathf.Approximately(fx.FocusBlurIntent, focus))
                    fx.FollowFocusBlur(focus, _focusDuration, _focusEase);
            }

            float layer = LayerBlurOf(refs.RigRoot);

            if (snap)
                fx.SnapLayerBlur(layer);
            else if (!Mathf.Approximately(fx.LayerBlurIntent, layer))
                fx.FollowLayerBlur(layer, _layerDuration, _layerEase);
        }

        (_seen, _alive) = (_alive, _seen);
    }

    // 조용히 푼다 — 줄마다 부르므로 경고를 남기지 않는다.
    private bool TryResolveFocusScale(out float focusScale)
    {
        focusScale = 0f;

        if (string.IsNullOrEmpty(_focusRoleKey))
            return false;

        string rigKey = CharacterRigResolver.ResolveSlotKey(_stage, _focusRoleKey);

        if (!_stage.characterRigs.TryPeekRig(rigKey, out CharacterRigRefs focusRig) || focusRig?.RigRoot == null)
            return false;

        focusScale = focusRig.SettledDepthScale;
        return true;
    }

    // 깊이 차이에 비례 — depthScale은 Far 0.68 ~ Close 1.38이라 최대 간격이 0.70이다.
    private float CircleOfConfusion(float depthScale, float focusScale)
        => Mathf.Clamp01(Mathf.Abs(depthScale - focusScale) * _focusFalloff)
           * Mathf.Clamp01(_focusMaxBlur);

    private float LayerBlurOf(Transform rigRoot)
    {
        float blur = 0f;

        foreach (KeyValuePair<(string Stage, float Depth), float> pair in _layerBlur)
        {
            // 블러 의도는 아직 서지 않은 층에도 걸 수 있다 — 그 층에 리그가 없으니 흐릴 것도 없다.
            if (!_stages.Get(pair.Key.Stage).TryGetLayer(pair.Key.Depth, out StageLayerRefs layer))
                continue;

            if (IsDescendantOf(rigRoot, layer.Content))
                blur = Mathf.Max(blur, pair.Value);
        }

        return blur;
    }

    private bool AnyTransitioning(bool focus)
    {
        foreach (RigVisualEffectController fx in _seen)
        {
            if (focus ? fx.IsFocusBlurTransitioning : fx.IsLayerBlurTransitioning)
                return true;
        }

        return false;
    }

    private static bool IsDescendantOf(Transform child, Transform parent)
    {
        if (child == null || parent == null)
            return false;

        for (Transform t = child; t != null; t = t.parent)
        {
            if (t == parent)
                return true;
        }

        return false;
    }
}
