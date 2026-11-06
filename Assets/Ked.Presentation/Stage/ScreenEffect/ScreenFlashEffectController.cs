using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// 플래시는 이벤트다 — 한 번 터지고 0으로 돌아온다. 재생은 이 컨트롤러가 소유하고,
// 새 플래시는 앞 것을 끊고 0에서 시작한다. 정지·장면 경계에서는 ClearImmediate로 끊는다.
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class ScreenFlashEffectController :
    MonoBehaviour,
    IScreenEffectController
{
    public readonly struct Pulse
    {
        public readonly Color Color;
        public readonly float Amount;
        public readonly float AttackDuration;
        public readonly float HoldDuration;
        public readonly float ReleaseDuration;
        public readonly Ease AttackEase;
        public readonly Ease ReleaseEase;

        public Pulse(
            Color color,
            float amount,
            float attackDuration,
            float holdDuration,
            float releaseDuration,
            Ease attackEase,
            Ease releaseEase)
        {
            Color = color;
            Amount = Mathf.Clamp01(amount);
            AttackDuration = Mathf.Max(0f, attackDuration);
            HoldDuration = Mathf.Max(0f, holdDuration);
            ReleaseDuration = Mathf.Max(0f, releaseDuration);
            AttackEase = attackEase;
            ReleaseEase = releaseEase;
        }
    }

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

    private Sequence _pulse;

    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

    public float FlashAmount => _flashAmount;
    public Color FlashColor => _flashColor;

    public bool IsPlaying => _pulse != null;

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

    public Material SourceMaterial => sourceMaterial;

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

    public void Play(in Pulse pulse)
    {
        StopPulse();

        Color color = pulse.Color;
        ApplyImmediate(0f, color);

        if (pulse.AttackDuration <= 0f &&
            pulse.HoldDuration <= 0f &&
            pulse.ReleaseDuration <= 0f)
            return;

        Sequence sequence = DOTween.Sequence()
            .SetTarget(this)
            .SetUpdate(true);

        if (pulse.AttackDuration > 0f)
        {
            sequence.Append(DOTween.To(
                    () => _flashAmount,
                    value => ApplyImmediate(value, color),
                    pulse.Amount,
                    pulse.AttackDuration)
                .SetEase(pulse.AttackEase));
        }
        else
        {
            float amount = pulse.Amount;
            sequence.AppendCallback(() => ApplyImmediate(amount, color));
        }

        if (pulse.HoldDuration > 0f)
            sequence.AppendInterval(pulse.HoldDuration);

        if (pulse.ReleaseDuration > 0f)
        {
            sequence.Append(DOTween.To(
                    () => _flashAmount,
                    value => ApplyImmediate(value, color),
                    0f,
                    pulse.ReleaseDuration)
                .SetEase(pulse.ReleaseEase));
        }

        sequence.OnComplete(() =>
        {
            _pulse = null;
            ApplyImmediate(0f, color);
        });

        _pulse = sequence;
    }

    // 기준 상태 — 재생 중인 플래시를 끊고 0으로.
    public void ClearImmediate()
    {
        StopPulse();
        ApplyImmediate(0f, _flashColor);
    }

    // 플래시의 끝은 언제나 0이라, 완주(complete)는 끊고 0으로 두는 것과 같다.
    public void KillTween(bool complete)
    {
        if (complete)
            ClearImmediate();
        else
            StopPulse();
    }

    private void StopPulse()
    {
        _pulse?.Kill(false);
        _pulse = null;
    }

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