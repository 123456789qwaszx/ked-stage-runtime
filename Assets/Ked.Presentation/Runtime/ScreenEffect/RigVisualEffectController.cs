using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Runtime material owner for a rig's UI Image (character portrait / background sprite).
// POCO (not MonoBehaviour): created by the rig's Setup command, held in {Character,Background}RigRefs.VisualEffect,
// disposed by the matching registry's DestroyRig.
// 주의: runtime material은 rig마다 Instantiate되므로 Dispose가 누락되면 teardown(롤백/seek 리빌드)마다 누수됩니다.
//
// 이 리그의 비주얼은 출처 셋을 컨트롤러가 각각 의도로 들고 따라간다(IntentTransition) — 커맨드는 목표만 넘긴다.
//   모양(Style)  char_visual_* — 어둡게·림·자체 블러
//   초점 블러    focus_on / focus_clear — 깊이 차이만큼(CoC)
//   레이어 블러  screen_blur — 이 리그가 선 레이어의 흐림
// 셰이더의 _BlurAmount는 세 블러 중 가장 큰 값이다(소유자 결정 2026-11-03). 서로 덮어쓰지 않는다.
public sealed class RigVisualEffectController : IDisposable
{
    public readonly struct Style
    {
        public readonly float Dim;
        public readonly Color DimTintColor;
        public readonly float OuterRim;
        public readonly float InnerRim;
        public readonly Color OuterRimColor;
        public readonly Color InnerRimColor;
        public readonly float Blur;

        public Style(
            float dim,
            Color dimTintColor,
            float outerRim,
            float innerRim,
            Color outerRimColor,
            Color innerRimColor,
            float blur)
        {
            Dim = Mathf.Clamp01(dim);
            DimTintColor = dimTintColor;
            OuterRim = Mathf.Clamp01(outerRim);
            InnerRim = Mathf.Clamp01(innerRim);
            OuterRimColor = outerRimColor;
            InnerRimColor = innerRimColor;
            Blur = Mathf.Clamp01(blur);
        }

        public static Style Lerp(Style from, Style to, float t)
        {
            t = Mathf.Clamp01(t);

            return new Style(
                Mathf.Lerp(from.Dim, to.Dim, t),
                Color.Lerp(from.DimTintColor, to.DimTintColor, t),
                Mathf.Lerp(from.OuterRim, to.OuterRim, t),
                Mathf.Lerp(from.InnerRim, to.InnerRim, t),
                Color.Lerp(from.OuterRimColor, to.OuterRimColor, t),
                Color.Lerp(from.InnerRimColor, to.InnerRimColor, t),
                Mathf.Lerp(from.Blur, to.Blur, t));
        }
    }

    private const float DimBrightness = 0.62f;
    private const float DimSaturation = 0.70f;
    private static readonly Color DefaultDimTintColor = new(0.50f, 0.50f, 0.78f, 1f);

    private static readonly Color DefaultOuterRimColor = Color.white;
    private const float OuterRimWidth = 0.0025f;
    private const float OuterRimSoftness = 0.8f;

    private static readonly Color DefaultInnerRimColor = Color.white;
    private const float InnerRimWidth = 0.003f;
    private const float InnerRimSoftness = 0.8f;

    private static readonly int DimAmountId = Shader.PropertyToID("_DimAmount");
    private static readonly int DimBrightnessId = Shader.PropertyToID("_DimBrightness");
    private static readonly int DimSaturationId = Shader.PropertyToID("_DimSaturation");
    private static readonly int DimTintColorId = Shader.PropertyToID("_DimTintColor");

    private static readonly int RimAmountId = Shader.PropertyToID("_RimAmount");
    private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
    private static readonly int RimWidthId = Shader.PropertyToID("_RimWidth");
    private static readonly int RimSoftnessId = Shader.PropertyToID("_RimSoftness");

    private static readonly int InnerRimAmountId = Shader.PropertyToID("_InnerRimAmount");
    private static readonly int InnerRimColorId = Shader.PropertyToID("_InnerRimColor");
    private static readonly int InnerRimWidthId = Shader.PropertyToID("_InnerRimWidth");
    private static readonly int InnerRimSoftnessId = Shader.PropertyToID("_InnerRimSoftness");


    private static readonly int BlurAmountId = Shader.PropertyToID("_BlurAmount");

    // 셰이더 그래프의 Boolean Keyword(Multi Compile). 꺼져 있으면 밉+5탭 경로가 변형에서 통째로 빠진다.
    private const string BlurKeyword = "_BLUR";

    // 0에 가까운 값까지 켜 두면 눈에 안 보이는 블러 때문에 비싼 변형을 쓰게 된다.
    private const float BlurKeywordThreshold = 0.001f;

    private readonly Image _targetImage;

    // 같은 runtime material을 함께 쓰는 두 번째 그래픽(캐릭터 초상의 위 겹).
    // 없을 수 있다 — 그래픽이 하나뿐인 리그라면 null이다(배경도 지금은 캐릭터 리그라 둘 다 있다).
    private readonly Image _companionImage;

    private Material _runtimeMaterial;

    private float _dimAmount;
    private float _outerRimAmount;
    private float _innerRimAmount;

    private Color _dimTintColor;
    private Color _outerRimColor;
    private Color _innerRimColor;

    // 블러 출처 셋과, 셰이더에 쓰는 합성값(가장 큰 값).
    private float _styleBlur;
    private float _focusBlur;
    private float _layerBlur;
    private float _blurAmount;

    private readonly IntentTransition<Style> _styleTransition;
    private readonly IntentTransition<float> _focusBlurTransition;
    private readonly IntentTransition<float> _layerBlurTransition;

    public float DimAmount => _dimAmount;
    public float OuterRimAmount => _outerRimAmount;
    public float InnerRimAmount => _innerRimAmount;
    public float BlurAmount => _blurAmount;

    public Color DimTintColor => _dimTintColor;
    public Color OuterRimColor => _outerRimColor;
    public Color InnerRimColor => _innerRimColor;

    public Style PresentedStyle => new(
        _dimAmount,
        _dimTintColor,
        _outerRimAmount,
        _innerRimAmount,
        _outerRimColor,
        _innerRimColor,
        _styleBlur);

    public float FocusBlur => _focusBlur;
    public float LayerBlur => _layerBlur;

    // 전환이 향하는 값 — RigBlurIntentSystem이 목표가 바뀌었는지 볼 때 쓴다.
    public float FocusBlurIntent => _focusBlurTransition.Intent;
    public float LayerBlurIntent => _layerBlurTransition.Intent;

    public bool IsStyleTransitioning => _styleTransition.IsTransitioning;
    public bool IsFocusBlurTransitioning => _focusBlurTransition.IsTransitioning;
    public bool IsLayerBlurTransitioning => _layerBlurTransition.IsTransitioning;

    public RigVisualEffectController(
        Image targetImage,
        Material sourceMaterial,
        Image companionImage = null)
    {
        _targetImage = targetImage;
        _companionImage = companionImage;

        _dimTintColor = DefaultDimTintColor;
        _outerRimColor = DefaultOuterRimColor;
        _innerRimColor = DefaultInnerRimColor;

        _styleTransition = new IntentTransition<Style>(PresentedStyle, () => PresentedStyle, ApplyStyle, Style.Lerp, this);
        _focusBlurTransition = new IntentTransition<float>(0f, () => _focusBlur, ApplyFocusBlur, Mathf.Lerp, this);
        _layerBlurTransition = new IntentTransition<float>(0f, () => _layerBlur, ApplyLayerBlur, Mathf.Lerp, this);

        if (targetImage == null)
        {
            Debug.LogWarning("[RigVisualEffectController] targetImage is null.");
            return;
        }

        if (sourceMaterial == null)
        {
            Object context = targetImage;

            Debug.LogWarning(
                "[RigVisualEffectController] sourceMaterial is null. " +
                "Check the Resources path in the rig's Setup command.",
                context);
            return;
        }

        Image runtimeNameSource = targetImage;

        _runtimeMaterial = Object.Instantiate(sourceMaterial);
        _runtimeMaterial.name = $"{sourceMaterial.name}_Runtime_{runtimeNameSource.name}";

        if (targetImage != null)
            targetImage.material = _runtimeMaterial;

        if (_companionImage != null)
            _companionImage.material = _runtimeMaterial;

        ApplyStaticStyle();
        ApplyDynamicValues();

        MarkMaterialDirty();
    }

    public void FollowStyle(in Style style, float duration, Ease ease) => _styleTransition.Follow(style, duration, ease);
    public void SnapStyle(in Style style) => _styleTransition.Snap(style);
    public void AccelerateStyle(float speedUpMultiplier) => _styleTransition.Accelerate(speedUpMultiplier);

    public void FollowFocusBlur(float blur, float duration, Ease ease) => _focusBlurTransition.Follow(Mathf.Clamp01(blur), duration, ease);
    public void SnapFocusBlur(float blur) => _focusBlurTransition.Snap(Mathf.Clamp01(blur));
    public void AccelerateFocusBlur(float speedUpMultiplier) => _focusBlurTransition.Accelerate(speedUpMultiplier);

    public void FollowLayerBlur(float blur, float duration, Ease ease) => _layerBlurTransition.Follow(Mathf.Clamp01(blur), duration, ease);
    public void SnapLayerBlur(float blur) => _layerBlurTransition.Snap(Mathf.Clamp01(blur));
    public void AccelerateLayerBlur(float speedUpMultiplier) => _layerBlurTransition.Accelerate(speedUpMultiplier);

    private void ApplyStyle(Style style)
    {
        _dimAmount = style.Dim;
        _dimTintColor = style.DimTintColor;
        _outerRimAmount = style.OuterRim;
        _innerRimAmount = style.InnerRim;
        _outerRimColor = style.OuterRimColor;
        _innerRimColor = style.InnerRimColor;
        _styleBlur = style.Blur;

        Refresh();
    }

    private void ApplyFocusBlur(float blur)
    {
        _focusBlur = Mathf.Clamp01(blur);
        Refresh();
    }

    private void ApplyLayerBlur(float blur)
    {
        _layerBlur = Mathf.Clamp01(blur);
        Refresh();
    }

    private void Refresh()
    {
        _blurAmount = Mathf.Max(_styleBlur, Mathf.Max(_focusBlur, _layerBlur));

        if (_runtimeMaterial == null)
            return;

        ApplyDynamicValues();
        MarkMaterialDirty();
    }

    // 스타일은 생성 시 한 번만 기록. tween마다 반복하지 않음.
    private void ApplyStaticStyle()
    {
        if (_runtimeMaterial == null)
            return;

        ApplyStaticStyleTo(_runtimeMaterial);
        PushMaterialToGraphics();
    }

    private void ApplyDynamicValues()
    {
        if (_runtimeMaterial == null)
            return;

        ApplyDynamicValuesTo(_runtimeMaterial);
        PushMaterialToGraphics();
    }

    public void Dispose()
    {
        // material 파괴 전에 이 컨트롤러가 소유한 전환을 먼저 정리.
        // (tween target이 POCO이므로 KillTweenOnHierarchy로는 잡히지 않는다.)
        _styleTransition.Stop();
        _focusBlurTransition.Stop();
        _layerBlurTransition.Stop();
        DOTween.Kill(this, false);

        if (_runtimeMaterial != null)
        {
            Object.Destroy(_runtimeMaterial);
            _runtimeMaterial = null;
        }
    }

    private void MarkMaterialDirty()
    {
        if (_targetImage != null)
            _targetImage.SetMaterialDirty();

        if (_companionImage != null)
            _companionImage.SetMaterialDirty();
    }

    private void ApplyDynamicValuesTo(Material material)
    {
        material.SetFloat(DimAmountId, _dimAmount);
        material.SetColor(DimTintColorId, _dimTintColor);

        material.SetFloat(RimAmountId, _outerRimAmount);
        material.SetColor(RimColorId, _outerRimColor);

        material.SetFloat(InnerRimAmountId, _innerRimAmount);
        material.SetColor(InnerRimColorId, _innerRimColor);

        material.SetFloat(BlurAmountId, _blurAmount);

        // 캐시한 on/off로 가드하지 않는다 — materialForRendering은 Mask 등에 의해 새로 만들어질 수 있고,
        // 그때 키워드가 빠지면 Keyword의 Off 분기로 떨어진다.
        if (_blurAmount > BlurKeywordThreshold)
            material.EnableKeyword(BlurKeyword);
        else
            material.DisableKeyword(BlurKeyword);
    }

    private void ApplyStaticStyleTo(Material material)
    {
        material.SetFloat(DimBrightnessId, DimBrightness);
        material.SetFloat(DimSaturationId, DimSaturation);

        material.SetFloat(RimWidthId, OuterRimWidth);
        material.SetFloat(RimSoftnessId, OuterRimSoftness);

        material.SetFloat(InnerRimWidthId, InnerRimWidth);
        material.SetFloat(InnerRimSoftnessId, InnerRimSoftness);
    }

    // Mask 등에 의해 Canvas가 서브 Material을 생성했을 때, 실제 사용하는 것과 Image의 Material을 같게 함.
    private void PushMaterialToGraphics()
    {
        PushMaterialToGraphic(_targetImage);
        PushMaterialToGraphic(_companionImage);
    }

    private void PushMaterialToGraphic(Image image)
    {
        if (image == null)
            return;

        image.SetMaterialDirty();

        Material renderingMaterial = image.materialForRendering;
        if (renderingMaterial == null)
            return;

        ApplyStaticStyleTo(renderingMaterial);
        ApplyDynamicValuesTo(renderingMaterial);

        image.canvasRenderer.SetMaterial(renderingMaterial, image.mainTexture);
    }
}
