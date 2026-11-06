using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// 노이즈는 장면 전역 의도다 — 커맨드가 의도를 쓰고, 화면은 이 컨트롤러가 따라간다(IntentTransition).
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class ScreenNoiseEffectController :
    MonoBehaviour,
    IScreenEffectController
{
    public readonly struct State
    {
        public readonly float Amount;
        public readonly Color Color;
        public readonly float Scale;
        public readonly float SpeedX;
        public readonly float SpeedY;
        public readonly float Contrast;

        public State(
            float amount,
            Color color,
            float scale,
            float speedX,
            float speedY,
            float contrast)
        {
            Amount = Mathf.Clamp01(amount);
            Color = color;
            Scale = Mathf.Max(0f, scale);
            SpeedX = speedX;
            SpeedY = speedY;
            Contrast = Mathf.Max(0f, contrast);
        }

        public static State Lerp(State from, State to, float t)
        {
            t = Mathf.Clamp01(t);

            return new State(
                Mathf.Lerp(from.Amount, to.Amount, t),
                Color.Lerp(from.Color, to.Color, t),
                Mathf.Lerp(from.Scale, to.Scale, t),
                Mathf.Lerp(from.SpeedX, to.SpeedX, t),
                Mathf.Lerp(from.SpeedY, to.SpeedY, t),
                Mathf.Lerp(from.Contrast, to.Contrast, t));
        }
    }

    [Header("Target")]
    [SerializeField] private Image targetImage;

    [Header("Material")]
    [SerializeField] private Material sourceMaterial;

    [Header("Default")]
    [SerializeField, Range(0f, 1f)] private float defaultAmount = 0f;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField, Min(0f)] private float defaultScale = 0.8f;
    [SerializeField] private float defaultSpeedX = 0.015f;
    [SerializeField] private float defaultSpeedY = 0.012f;
    [SerializeField, Min(0f)] private float defaultContrast = 1f;

    private UIEffectMaterialBinding _material;

    private bool _stateInitialized;

    private float _amount;
    private Color _color;
    private float _scale;
    private float _speedX;
    private float _speedY;
    private float _contrast;

    private static readonly int NoiseAmountId = Shader.PropertyToID("_NoiseAmount");
    private static readonly int NoiseColorId = Shader.PropertyToID("_NoiseColor");
    private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
    private static readonly int NoiseSpeedXId = Shader.PropertyToID("_NoiseSpeedX");
    private static readonly int NoiseSpeedYId = Shader.PropertyToID("_NoiseSpeedY");
    private static readonly int NoiseContrastId = Shader.PropertyToID("_NoiseContrast");

    private IntentTransition<State> _transition;

    public float Amount => _amount;
    public Color Color => _color;
    public float Scale => _scale;
    public float SpeedX => _speedX;
    public float SpeedY => _speedY;
    public float Contrast => _contrast;

    public State Presented => new(_amount, _color, _scale, _speedX, _speedY, _contrast);

    public State Intent => Transition.Intent;

    public bool IsTransitioning => Transition.IsTransitioning;

    private IntentTransition<State> Transition
    {
        get
        {
            if (_transition != null)
                return _transition;

            InitializeStateIfNeeded();
            _transition = new IntentTransition<State>(Presented, () => Presented, ApplyState, State.Lerp, this);
            return _transition;
        }
    }

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

    public Material SourceMaterial => sourceMaterial;

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
        float scale,
        float speedX,
        float speedY,
        float contrast)
    {
        InitializeStateIfNeeded();
        EnsureMaterial();

        if (_material == null || !_material.IsValid)
            return;

        _amount = Mathf.Clamp01(amount);
        _color = color;
        _scale = Mathf.Max(0f, scale);
        _speedX = speedX;
        _speedY = speedY;
        _contrast = Mathf.Max(0f, contrast);

        ApplyMaterialValues();
    }

    public void Follow(in State intent, float duration, Ease ease) => Transition.Follow(intent, duration, ease);

    public void Snap(in State intent) => Transition.Snap(intent);

    // 기준 상태 — 전환을 멈추고 세기만 0으로(색·스케일 등은 남긴다).
    public void ClearImmediate()
    {
        Snap(new State(0f, _color, _scale, _speedX, _speedY, _contrast));
    }

    public void KillTween(bool complete)
    {
        if (complete)
            Snap(Intent);
        else
            Transition.Stop();
    }

    private void ApplyState(State state)
    {
        ApplyImmediate(state.Amount, state.Color, state.Scale, state.SpeedX, state.SpeedY, state.Contrast);
    }

    private void InitializeStateIfNeeded()
    {
        if (_stateInitialized)
            return;

        _amount = Mathf.Clamp01(defaultAmount);
        _color = defaultColor;
        _scale = Mathf.Max(0f, defaultScale);
        _speedX = defaultSpeedX;
        _speedY = defaultSpeedY;
        _contrast = Mathf.Max(0f, defaultContrast);

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
            Debug.LogWarning("[ScreenNoiseEffectController] targetImage is missing.", this);
            return;
        }

        _material = new UIEffectMaterialBinding(targetImage, sourceMaterial, gameObject.name);
    }

    private void ApplyMaterialValues()
    {
        if (_material == null)
            return;

        _material.SetFloat(NoiseAmountId, _amount);
        _material.SetColor(NoiseColorId, _color);
        _material.SetFloat(NoiseScaleId, _scale);
        _material.SetFloat(NoiseSpeedXId, _speedX);
        _material.SetFloat(NoiseSpeedYId, _speedY);
        _material.SetFloat(NoiseContrastId, _contrast);
    }

    private void DisposeMaterial()
    {
        _material?.Dispose();
        _material = null;
    }
}