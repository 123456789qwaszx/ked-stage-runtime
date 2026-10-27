using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

[Serializable]
public sealed class MotionCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Motion")]
    [Tooltip("motions.json에서 읽은 클립(검증된 것). 채널마다 노드·종류·mode·키가 들어 있다.")]
    public MotionClip clip;
}

// ─────────────────────────────────────────────────────────────────────────────
// motion — 모션 클립 재생. 채널마다 **마지막 키 = 클레임**이다.
//
// 세 갈래가 같은 값을 본다(MotionMath.ClaimFor):
//   ① 장부 게시  — ClaimTarget에서 채널마다 종점을 게시한다(place가 정착 체인을 볼 수 있게)
//   ② 트윈 종점  — 샘플러가 길이에서 쓰는 값이 그 종점이다(마지막 키 Time = 길이, 규칙)
//   ③ 상태 폴드  — 리듀서 ApplyMotion이 같은 함수로 접는다
//
// 트윈은 **노드(RectTransform)마다 하나**다. 한 노드의 채널(위치·배율·회전)은 그 트윈 하나가
// 초 단위로 샘플링한다. 노드마다 트윈을 두는 이유: 다른 커맨드가 그 노드를 잡을 때
// rect.DOKill(true)로 완주시키는 규약("한 노드 = 한 축")이 모션에도 그대로 들어맞아야 한다.
// 그래서 ClaimTweenCommandBase(트윈 하나)를 쓰지 않고 같은 수명을 직접 돈다:
//   Resolve → Claim(DOKill·종점·게시) → 트윈들 → Commit(종점 쓰기·장부 Clear) / Skip / 스텝 경계.
//
// 스텝 경계 가속은 없다 — 초 단위 진행 트윈이라 "남은 거리"가 없다. gesture와 같은 판단으로 즉시 확정.
// ─────────────────────────────────────────────────────────────────────────────
public sealed class MotionCommandCharR : CommandBase
{
    private struct Bound
    {
        public MotionChannel Channel;
        public RectTransform Rect;
        public Vector3 Start;
        public Vector3 End;
    }

    private readonly PresentationStage _stage;
    private readonly MotionCommandSpecCharR _spec;

    private readonly List<Bound> _bound = new();
    private readonly List<RectTransform> _rects = new();
    private readonly List<Tween> _tweens = new();

    private CharacterRigRefs _rigRefs;
    private bool _resolved;
    private bool _claimed;

    public override bool WaitForCompletion => _spec.wait;

    public MotionCommandCharR(PresentationStage stage, MotionCommandSpecCharR spec)
    {
        _stage = stage;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Resolve();
        Claim();

        if (_bound.Count == 0 || _spec.clip.Duration <= 0f)
        {
            Commit();
            yield break;
        }

        for (int i = 0; i < _rects.Count; i++)
        {
            RectTransform rect = _rects[i];

            _tweens.Add(DOTween
                .To(() => 0f, seconds => ApplyRect(rect, seconds), _spec.clip.Duration, _spec.clip.Duration)
                .SetEase(Ease.Linear)
                .SetTarget(rect)
                .SetUpdate(true)
                .OnComplete(OnTweenComplete));
        }

        if (WaitForCompletion)
        {
            while (_claimed)
                yield return null;
        }
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        Resolve();

        if (!_claimed)
            Claim();

        Commit();
    }

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!_claimed)
            return;

        KillTweens();
        Commit();
    }

    private void Resolve()
    {
        if (_resolved)
            return;

        _resolved = true;
        _rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(_stage, _spec.slotKey);

        if (_rigRefs == null || _spec.clip?.Channels == null)
            return;

        foreach (MotionChannel channel in _spec.clip.Channels)
        {
            // 채널의 노드 id는 스키마 id이고, CharacterRigTarget의 이름과 같다.
            RectTransform rect = Enum.TryParse(channel.Node, out CharacterRigTarget target)
                ? _rigRefs.GetRect(target)
                : null;

            if (rect == null)
            {
                Debug.LogWarning(
                    $"[MotionCommandCharR] 모션 '{_spec.clip.Name}': 리그에 노드 '{channel.Node}'가 없다. 그 채널은 건너뛴다.");
                continue;
            }

            _bound.Add(new Bound { Channel = channel, Rect = rect });

            if (!_rects.Contains(rect))
                _rects.Add(rect);
        }
    }

    // DOKill → 시작값 → 종점(코어) → 게시. move_by·rotate_to와 같은 순서다.
    private void Claim()
    {
        _claimed = true;

        for (int i = 0; i < _rects.Count; i++)
            _rects[i].DOKill(true);

        for (int i = 0; i < _bound.Count; i++)
        {
            Bound b = _bound[i];

            b.Start = ReadStart(b.Rect, b.Channel.Kind);
            b.End = MotionMath.EndValue(b.Channel, b.Start.ToCore()).ToUnity();
            _bound[i] = b;

            Publish(b);
        }
    }

    private void Commit()
    {
        for (int i = 0; i < _bound.Count; i++)
        {
            Write(_bound[i].Rect, _bound[i].Channel.Kind, _bound[i].End);
            _rigRefs?.PlacementTargets.Clear(_bound[i].Rect);
        }

        _tweens.Clear();
        _claimed = false;
    }

    private void OnTweenComplete()
    {
        for (int i = 0; i < _tweens.Count; i++)
        {
            if (_tweens[i].IsActive() && !_tweens[i].IsComplete())
                return;
        }

        if (_claimed)
            Commit();
    }

    private void KillTweens()
    {
        for (int i = 0; i < _tweens.Count; i++)
            _tweens[i]?.Kill(false);

        _tweens.Clear();
    }

    // 한 노드의 모든 채널을 같은 시각에 쓴다 — 정본은 MotionMath.Compose.
    private void ApplyRect(RectTransform rect, float seconds)
    {
        for (int i = 0; i < _bound.Count; i++)
        {
            if (_bound[i].Rect != rect)
                continue;

            Write(rect, _bound[i].Channel.Kind, MotionMath.Compose(_bound[i].Channel, _bound[i].Start.ToCore(), seconds).ToUnity());
        }
    }

    private void Publish(in Bound b)
    {
        switch (b.Channel.Kind)
        {
            case MotionChannelKind.Position: _rigRefs.PlacementTargets.PublishAnchoredPosition(b.Rect, b.End); break;
            case MotionChannelKind.Scale: _rigRefs.PlacementTargets.PublishLocalScale(b.Rect, b.End); break;
            case MotionChannelKind.Euler: _rigRefs.PlacementTargets.PublishLocalEuler(b.Rect, b.End); break;
        }
    }

    private static Vector3 ReadStart(RectTransform rect, MotionChannelKind kind)
        => kind switch
        {
            MotionChannelKind.Position => rect.anchoredPosition,
            MotionChannelKind.Scale => rect.localScale,
            MotionChannelKind.Euler => rect.localEulerAngles,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "모르는 채널 종류"),
        };

    private static void Write(RectTransform rect, MotionChannelKind kind, Vector3 value)
    {
        switch (kind)
        {
            case MotionChannelKind.Position: rect.anchoredPosition = value; break;
            case MotionChannelKind.Scale: rect.localScale = value; break;
            case MotionChannelKind.Euler: rect.localEulerAngles = value; break;
        }
    }
}
