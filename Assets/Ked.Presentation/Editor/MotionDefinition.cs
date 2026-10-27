using System;
using System.Collections.Generic;
using Ked.Presentation.Core;
using UnityEngine;

/// <summary>
/// 모션 하나의 **제작 원본** — AnimationClip과 이 에셋이 원본이고, motions.json은 그 둘을 변환한 산출물이다(손으로 고치지 않는다).
///
/// AnimationClip의 키는 전부 절대값이라 "지금 자리에서"(relative)인지 "이 값으로"(absolute)인지, 끝난 뒤 무엇이
/// 남는지(restore|commit)는 작가가 여기 적는다. 클립 이름에 뜻을 숨기지 않는다 — 나중에 읽는 사람이 알 수 있게.
/// 길이는 클립에서 파생한다(두 곳에 두지 않는다).
/// </summary>
[CreateAssetMenu(menuName = "Ked/Motion Definition", fileName = "Motion_")]
public sealed class MotionDefinition : ScriptableObject
{
    [Serializable]
    public sealed class ChannelMode
    {
        [Tooltip("리그 스키마의 노드 id (예: CharacterPortrait_Track_Move_Y)")]
        public string node;

        public MotionChannelKind kind;
        public MotionChannelMode mode;
    }

    [Tooltip("motions.json에 실릴 이름 [a-z0-9_]+ — 대본의 <<motion slot 이름>>. 비우면 에셋 이름.")]
    public string motionId;

    [Tooltip("rig-schemas.json의 rigKind. 채널 노드를 이 리그로 검증한다.")]
    public string rigSchemaId = "character";

    public AnimationClip clip;

    [Tooltip("끝난 뒤 무엇이 남는가. restore = 아무것도(모든 채널 relative + 마지막 키 중립값, 내보내기가 검증) · commit = 마지막 키가 남는다.")]
    public MotionEnd end = MotionEnd.Restore;

    [Tooltip("채널 재정의가 없는 (노드, 종류)에 쓰는 mode. 파일에는 채널마다 명시돼 나간다.")]
    public MotionChannelMode defaultMode = MotionChannelMode.Relative;

    [Tooltip("특정 (노드, 종류)만 다른 mode로. 비워도 된다.")]
    public List<ChannelMode> channelModes = new();

    public string EffectiveName => string.IsNullOrEmpty(motionId) ? name : motionId;

    public MotionChannelMode ModeOf(string node, MotionChannelKind kind)
    {
        foreach (ChannelMode m in channelModes)
        {
            if (m != null && m.node == node && m.kind == kind)
                return m.mode;
        }

        return defaultMode;
    }
}
