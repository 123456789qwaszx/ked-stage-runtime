using System;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

[Serializable]
public sealed class SlideCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Node (Track)")]
    public CharacterRigNode node = CharacterRigNode.CharSlot_Track;

    [Header("Slide")]
    [Tooltip("In = 지금 위치가 도착점(화면 밖에서 들어온다) · Out = 지금 위치가 출발점(화면 밖으로 나간다).")]
    public SlideMotion.Kind kind;
    public CharRigDirection direction;
    public float distance;

    [Header("Tween")]
    public float duration;

    [Tooltip("파서가 채운다. 대본에서 생략하면 종류별 코어 기본값(SlideMotion.EaseOf).")]
    public Ease ease;

    [Tooltip("경로를 벗어나 튀는 양(px). 0이면 직선. 파서가 채운다 — 생략하면 SlideMotion.PunchPixels.")]
    public float punch;
}

public sealed class SlideCommandCharR : ClaimTweenCommandBase
{
    private readonly PresentationStage _stage;
    private readonly SlideCommandSpecCharR _spec;

    private CharacterRigRefs _rig;
    private RectTransform _rect;

    private Vector2 _startPos;
    private Vector2 _endPos;
    private Vector2 _slideDir;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    private bool Entering => _spec.kind == SlideMotion.Kind.In;

    public SlideCommandCharR(PresentationStage stage, SlideCommandSpecCharR spec)
    {
        _stage = stage;
        _spec = spec;
    }

    protected override void ResolveRefs()
    {
        _rig = CharacterRigResolver.ResolveRig(_stage, _spec.roleKey);
        _rect = _rig.GetRect(_spec.node);
    }

    protected override void ClaimTarget()
    {
        _rect.DOKill(true);

        Vector2 current = _rect.anchoredPosition;
        Vector2 offset = DirectionToVector(_spec.direction) * _spec.distance;

        if (Entering)
        {
            _endPos = current;
            _startPos = current + offset;
        }
        else
        {
            _startPos = current;
            _endPos = current + offset;
        }

        Vector2 travel = _endPos - _startPos;

        // 거리가 0이면 진행 방향을 잃는다 — 등장은 들어오는 쪽, 퇴장은 나가는 쪽이 답이다.
        _slideDir = travel.sqrMagnitude > 0f
            ? travel.normalized
            : (Entering 
                ? -DirectionToVector(_spec.direction) 
                : DirectionToVector(_spec.direction));

        // 끝값은 목표 계산이 아니라 여기서 정한 값이다(등장은 현재 위치, 퇴장은 현재 + 거리).
        // 등장은 CreateTween이 rect를 출발점으로 밀어 두므로, 게시가 없으면 같은 줄의 place가 화면 밖 값을 읽는다.
        _rig.PlacementTargets.Reserve(_rect, NodeTarget.AnchoredPosition(_rect.name, _endPos.ToCore()), Releases);
    }

    protected override Tween CreateTween(float duration)
    {
        // 슬라이드는 출발점에서 시작한다. 퇴장은 출발점이 곧 현재 위치라 무해하다.
        _rect.anchoredPosition = _startPos;

        return DOTween
            .To(() => 0f, ApplyProgress, 1f, duration)
            .SetEase(Ease.Linear)
            .SetTarget(_rect);
    }

    protected override void OnCommitFinalState()
        => _rect.anchoredPosition = _endPos;

    private void ApplyProgress(float progress)
    {
        float eased = DOVirtual.EasedValue(0f, 1f, progress, _spec.ease);

        Vector2 basePos = Vector2.LerpUnclamped(_startPos, _endPos, eased);
        Vector2 offset = _slideDir * (_spec.punch * SlideMotion.Punch(_spec.kind, eased));

        _rect.anchoredPosition = basePos + offset;
    }

    // public인 이유: 순수 함수이고, 등가성 하네스가 <b>파서 → 열거형 → 벡터</b> 사슬을
    // 코어와 대조해야 한다(SlideMotionParityTests). 상태를 안 들므로 열어도 잃을 것이 없다.
    public static Vector2 DirectionToVector(CharRigDirection direction)
    {
        Vec2 axis = SlideMotion.DirectionVector(CanonicalWord(direction));

        return new Vector2(axis.X, axis.Y);
    }

    /// <summary>열거형 → 파서가 읽는 정본 낱말. 별칭이 아니라 대표 낱말이다.</summary>
    private static string CanonicalWord(CharRigDirection direction) => direction switch
    {
        CharRigDirection.Right => "right",
        CharRigDirection.Up => "up",
        CharRigDirection.Down => "down",
        _ => "left",
    };
}
