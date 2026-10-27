using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Runtime material owner for a rig's UI Image (character portrait / background sprite).
// POCO (not MonoBehaviour): created by the rig's Setup command, held in {Character,Background}RigRefs.VisualEffect,
// disposed by the matching registry's DestroyRig.
// 주의: runtime material은 rig마다 Instantiate되므로 Dispose가 누락되면 teardown(롤백/seek 리빌드)마다 누수됩니다.
public sealed class RigVisualEffectController : IDisposable
{
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
    // 없을 수 있다 — 배경 리그처럼 그래픽이 하나뿐인 경우는 null이다.
    private readonly Image _companionImage;
    
    private Material _runtimeMaterial;
    
    private float _dimAmount;
    private float _outerRimAmount;
    private float _innerRimAmount;
    private float _blurAmount;

    private Color _dimTintColor;
    private Color _outerRimColor;
    private Color _innerRimColor;

    public float DimAmount => _dimAmount;
    public float OuterRimAmount => _outerRimAmount;
    public float InnerRimAmount => _innerRimAmount;
    public float BlurAmount => _blurAmount;

    public Color DimTintColor => _dimTintColor;
    public Color OuterRimColor => _outerRimColor;
    public Color InnerRimColor => _innerRimColor;

    public RigVisualEffectController(
        Image targetImage,
        Material sourceMaterial,
        Image companionImage = null)
    {
        _targetImage = targetImage;
        _companionImage = companionImage;

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

        _dimAmount = 0f;
        _outerRimAmount = 0f;
        _innerRimAmount = 0f;
        _blurAmount = 0f;

        _dimTintColor = DefaultDimTintColor;
        _outerRimColor = DefaultOuterRimColor;
        _innerRimColor = DefaultInnerRimColor;

        ApplyStaticStyle();
        ApplyDynamicValues();

        MarkMaterialDirty();
    }

    public void ApplyImmediate(
        float dim,
        Color dimTintColor,
        float outerRim,
        float innerRim,
        Color outerColor,
        Color innerColor,
        float blur)
    {
        if (_runtimeMaterial == null)
            return;

        _dimAmount = Mathf.Clamp01(dim);
        _dimTintColor = dimTintColor;
        _outerRimAmount = Mathf.Clamp01(outerRim);
        _innerRimAmount = Mathf.Clamp01(innerRim);
        _outerRimColor = outerColor;
        _innerRimColor = innerColor;
        _blurAmount = Mathf.Clamp01(blur);

        ApplyDynamicValues();
        MarkMaterialDirty();
    }

    public void SetBlurAmountImmediate(float value)
    {
        if (_runtimeMaterial == null)
            return;

        _blurAmount = Mathf.Clamp01(value);
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
        // material 파괴 전에 이 컨트롤러를 target으로 도는 focus tween을 먼저 정리.
        // (tween target이 POCO이므로 KillTweenOnHierarchy로는 잡히지 않는다.)
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