using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class ScreenFlashEffectController :
    MonoBehaviour,
    IScreenEffectController
{
    [Header("Target")]
    [SerializeField] private Image targetImage;

    [Header("Material")]
    [SerializeField] private Material sourceMaterial;

    [Header("Default")]
    [SerializeField] private Color defaultFlashColor = Color.white;

    private UIEffectMaterialBinding _material;

    private bool _stateInitialized;

    private float _flashAmount;
    private Color _flashColor;

    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

    public float FlashAmount => _flashAmount;
    public Color FlashColor => _flashColor;

    private void Reset()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
        
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

        if (targetImage == null)
            targetImage = GetComponent<Image>();
        
        InitializeStateIfNeeded();
        RebuildMaterial();

        if (targetImage != null)
            targetImage.raycastTarget = false;

        ApplyMaterialValues();
    }

    public void ApplyImmediate(float amount, Color color)
    {
        InitializeStateIfNeeded();
        EnsureMaterial();

        if (_material == null || !_material.IsValid)
            return;

        _flashAmount = Mathf.Clamp01(amount);
        _flashColor = color;

        ApplyMaterialValues();
    }

    public void ClearImmediate() => ApplyImmediate(0f, _flashColor);

    public void KillTween(bool complete) => transform.DOKill(complete);

    private void InitializeStateIfNeeded()
    {
        if (_stateInitialized)
            return;

        _flashAmount = 0f;
        _flashColor = defaultFlashColor;

        _stateInitialized = true;
    }

    private void RebuildMaterial()
    {
        DisposeMaterial();
        EnsureMaterial();
    }

    private void EnsureMaterial()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        if (_material != null)
            return;

        if (targetImage == null)
        {
            Debug.LogWarning("[ScreenFlashEffectController] targetImage is missing.", this);
            return;
        }

        _material = new UIEffectMaterialBinding(targetImage, sourceMaterial, gameObject.name);
    }

    private void ApplyMaterialValues()
    {
        if (_material == null)
            return;

        _material.SetFloat(FlashAmountId, _flashAmount);
        _material.SetColor(FlashColorId, _flashColor);
    }

    private void DisposeMaterial()
    {
        _material?.Dispose();
        _material = null;
    }
}