using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// 비네트는 장면 전역 의도다 — 커맨드가 의도를 쓰고, 화면은 이 컨트롤러가 따라간다(IntentTransition).
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class ScreenVignetteEffectController :
    MonoBehaviour,
    IScreenEffectController
{
    public readonly struct State
    {
        public readonly float Amount;
        public readonly Color Color;
        public readonly float Radius;
        public readonly float Softness;
        public readonly float Aspect;

        public State(
            float amount,
            Color color,
            float radius,
            float softness,
            float aspect)
        {
            Amount = Mathf.Clamp01(amount);
            Color = color;
            Radius = Mathf.Clamp01(radius);
            Softness = Mathf.Max(0.001f, softness);
            Aspect = Mathf.Max(0f, aspect);
        }

        public static State Lerp(State from, State to, float t)
        {
            t = Mathf.Clamp01(t);

            return new State(
                Mathf.Lerp(from.Amount, to.Amount, t),
                Color.Lerp(from.Color, to.Color, t),
                Mathf.Lerp(from.Radius, to.Radius, t),
                Mathf.Lerp(from.Softness, to.Softness, t),
                Mathf.Lerp(from.Aspect, to.Aspect, t));
        }
    }

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

    private IntentTransition<State> _transition;

    public float Amount => _amount;
    public Color Color => _color;
    public float Radius => _radius;
    public float Softness => _softness;
    public float Aspect => _aspect;

    public State Presented => new(_amount, _color, _radius, _softness, _aspect);

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

    public void Follow(in State intent, float duration, Ease ease) => Transition.Follow(intent, duration, ease);

    public void Snap(in State intent) => Transition.Snap(intent);

    // 기준 상태 — 전환을 멈추고 세기만 0으로(색·반경 등은 남긴다).
    public void ClearImmediate()
    {
        Snap(new State(0f, _color, _radius, _softness, _aspect));
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
        ApplyImmediate(state.Amount, state.Color, state.Radius, state.Softness, state.Aspect);
    }

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