using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class ScreenVignetteEffectController :
    MonoBehaviour,
    IScreenEffectController
{
    [Header("Target")]
    [SerializeField] private Image targetImage;

    [Header("Material")]
    [SerializeField] private Material sourceMaterial;

    [Header("Default")]
    [SerializeField] private Color defaultColor = Color.black;
    [SerializeField, Range(0f, 1f)] private float defaultAmount = 0f;
    [SerializeField, Range(0f, 1f)] private float defaultRadius = 0.45f;
    [SerializeField, Range(0.001f, 1f)] private float defaultSoftness = 0.35f;
    [SerializeField, Min(0f)] private float defaultAspect = 1.777f;

    private UIEffectMaterialBinding _material;

    private bool _stateInitialized;

    private float _amount;
    private Color _color;
    private float _radius;
    private float _softness;
    private float _aspect;

    private static readonly int VignetteAmountId = Shader.PropertyToID("_VignetteAmount");
    private static readonly int VignetteColorId = Shader.PropertyToID("_VignetteColor");
    private static readonly int VignetteRadiusId = Shader.PropertyToID("_VignetteRadius");
    private static readonly int VignetteSoftnessId = Shader.PropertyToID("_VignetteSoftness");
    private static readonly int AspectId = Shader.PropertyToID("_Aspect");

    public float Amount => _amount;
    public Color Color => _color;
    public float Radius => _radius;
    public float Softness => _softness;
    public float Aspect => _aspect;

    private void Reset() => EnsureTarget();

    private void Awake()
    {
        EnsureTarget();
        InitializeStateIfNeeded();
        EnsureMaterial();

        ApplyMaterialValues();

        if (targetImage != null)
            targetImage.raycastTarget = false;
    }

    private void OnDestroy()
    {
        KillTween(false);
        DisposeMaterial();
    }

    public void Bind(Image image, Material material)
    {
        if (image != null)
            targetImage = image;

        if (material != null)
            sourceMaterial = material;

        EnsureTarget();
        InitializeStateIfNeeded();
        RebuildMaterial();

        if (targetImage != null)
            targetImage.raycastTarget = false;

        ApplyMaterialValues();
    }

    public void ApplyImmediate(
        float amount,
        Color color,
        float radius,
        float softness,
        float aspect)
    {
        InitializeStateIfNeeded();
        EnsureMaterial();

        if (_material == null || !_material.IsValid)
            return;

        _amount = Mathf.Clamp01(amount);
        _color = color;
        _radius = Mathf.Clamp01(radius);
        _softness = Mathf.Max(0.001f, softness);
        _aspect = Mathf.Max(0f, aspect);

        ApplyMaterialValues();
    }

    public void ClearImmediate()
    {
        ApplyImmediate(0f, _color, _radius, _softness, _aspect);
    }

    public void KillTween(bool complete) => transform.DOKill(complete);

    private void InitializeStateIfNeeded()
    {
        if (_stateInitialized)
            return;

        _amount = Mathf.Clamp01(defaultAmount);
        _color = defaultColor;
        _radius = Mathf.Clamp01(defaultRadius);
        _softness = Mathf.Max(0.001f, defaultSoftness);
        _aspect = Mathf.Max(0f, defaultAspect);

        _stateInitialized = true;
    }

    private void EnsureTarget()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    private void RebuildMaterial()
    {
        DisposeMaterial();
        EnsureMaterial();
    }

    private void EnsureMaterial()
    {
        EnsureTarget();

        if (_material != null)
            return;

        if (targetImage == null)
        {
            Debug.LogWarning("[ScreenVignetteEffectController] targetImage is missing.", this);
            return;
        }

        _material = new UIEffectMaterialBinding(targetImage, sourceMaterial, gameObject.name);
    }

    private void ApplyMaterialValues()
    {
        if (_material == null)
            return;

        _material.SetFloat(VignetteAmountId, _amount);
        _material.SetColor(VignetteColorId, _color);
        _material.SetFloat(VignetteRadiusId, _radius);
        _material.SetFloat(VignetteSoftnessId, _softness);
        _material.SetFloat(AspectId, _aspect);
    }

    private void DisposeMaterial()
    {
        _material?.Dispose();
        _material = null;
    }
}