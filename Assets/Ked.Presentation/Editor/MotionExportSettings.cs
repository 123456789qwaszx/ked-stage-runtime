using System;
using System.Collections.Generic;
using Ked.Presentation.Core;
using UnityEngine;

/// <summary>
/// 모션 내보내기 설정 — 어느 AnimationClip을 어떤 이름으로, 채널마다 어느 mode로 motions.json에 낼지.
///
/// AnimationClip의 키는 전부 절대값이라 "지금 자리에서"(offset)인지 "이 값으로"(absolute)인지는 작가가 적어야 한다.
/// 클립 이름에 뜻을 숨기지 않고 여기 적는다 — 나중에 읽는 사람이 알 수 있게. 채널마다 적고 기본값은 없다:
/// 클립에 있는데 여기 없는 (노드, 종류)는 내보내기가 거부한다.
/// </summary>
[CreateAssetMenu(menuName = "Ked/Motion Export Settings", fileName = "MotionExportSettings")]
public sealed class MotionExportSettings : ScriptableObject
{
    [Serializable]
    public sealed class ChannelMode
    {
        [Tooltip("리그 스키마의 노드 id (예: CharacterPortrait_Track_Move_Y)")]
        public string node;

        public MotionChannelKind kind;
        public MotionChannelMode mode;
    }

    [Serializable]
    public sealed class Entry
    {
        public AnimationClip clip;

        [Tooltip("motions.json에 실릴 이름 [a-z0-9_]+. 비우면 클립 이름을 쓴다.")]
        public string name;

        [Tooltip("클립이 키잉한 (노드, 종류)마다 하나. 빠진 채널은 내보내기가 거부한다.")]
        public List<ChannelMode> channelModes = new();
    }

    [Header("Rig")]
    [Tooltip("rig-schemas.json의 rigKind. 지금은 character만 있다.")]
    public string rig = "character";

    [Header("Output")]
    [Tooltip("프로젝트 루트 기준 경로. 모션은 게임 콘텐츠라 대본 번들 곁(curves.json 옆)에 두는 것이 보통이다.")]
    public string outputPath = "Assets/Motions/motions.json";

    [Header("Motions")]
    public List<Entry> entries = new();
}
