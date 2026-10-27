using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 내보내기 묶음 — 어느 MotionDefinition들을 어디로 낼지. 모션의 뜻은 MotionDefinition에 있고 여기는 목록과 자리뿐이다.
/// </summary>
[CreateAssetMenu(menuName = "Ked/Motion Export Settings", fileName = "MotionExportSettings")]
public sealed class MotionExportSettings : ScriptableObject
{
    [Header("Rig schema")]
    [Tooltip("프로젝트 루트 기준 rig-schemas.json 경로. 채널 노드를 이 파일의 리그로 검증한다 — 내보내기는 런타임 코드가 아니라 데이터로 리그를 안다.")]
    public string rigSchemasPath = "ExportedTuning/rig-schemas.json";

    [Header("Output")]
    [Tooltip("프로젝트 루트 기준 경로. 모션은 게임 콘텐츠라 대본 번들 곁(curves.json 옆)에 두는 것이 보통이다.")]
    public string outputPath = "Assets/Motions/motions.json";

    [Header("Motions")]
    public List<MotionDefinition> motions = new();
}
