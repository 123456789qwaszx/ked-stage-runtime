using Ked.Presentation.Core;
using UnityEngine;

/// <summary>
/// 모션 하나의 **제작 원본** — AnimationClip과 이 에셋이 원본이고, motions.json은 그 둘을 변환한 산출물이다(손으로 고치지 않는다).
///
/// 끝난 뒤 무엇이 남는지(restore|commit)는 작가가 여기 적는다. 클립 이름에 뜻을 숨기지 않는다 — 나중에 읽는 사람이 알 수 있게.
/// "지금 자리에서"(relative)인지 "이 값으로"(absolute)인지는 모션이 아니라 **대본의 커맨드**가 정한다
/// (<c>&lt;&lt;motion slot 이름 relative|absolute&gt;&gt;</c>, motions/2) — 그래서 여기에는 없다.
/// 길이는 클립에서 파생한다(두 곳에 두지 않는다).
/// </summary>
[CreateAssetMenu(menuName = "Ked/Motion Definition", fileName = "Motion_")]
public sealed class MotionDefinition : ScriptableObject
{
    [Tooltip("motions.json에 실릴 이름 [a-z0-9_]+ — 대본의 <<motion slot 이름>>. 비우면 에셋 이름.")]
    public string motionId;

    [Tooltip("rig-schemas.json의 rigKind. 채널 노드를 이 리그로 검증한다.")]
    public string rigSchemaId = "character";

    public AnimationClip clip;

    [Tooltip("끝난 뒤 무엇이 남는가. restore = 아무것도(끝나면 시작값으로 돌아간다) · commit = 마지막 키가 남는다.")]
    public MotionEnd end = MotionEnd.Restore;

    public string EffectiveName => string.IsNullOrEmpty(motionId) ? name : motionId;
}
