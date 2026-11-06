using System.Collections.Generic;
using UnityEngine;

//[CreateAssetMenu(menuName = "CPS/Portraits/Build Settings", fileName = "PortraitBuildSettings")]
public sealed class PortraitBuildSettings : ScriptableObject
{
    [Header("Scan Folders")]
    // 설정 에셋이 하나도 없을 때 빌더(PortraitDbBuilder)가 새로 만드는 에셋의 기본값이다.
    // 아래 출력 경로처럼 특정 프로젝트 · 예제의 자리를 가리키지 않는다(예제는 자기 설정 에셋에 Assets/Samples/Portraits를 적는다).
    [Tooltip("Portrait 스프라이트를 스캔할 폴더들")]
    public List<string> scanFolders = new List<string>
    {
        "Assets/Portraits",
    };

    [Header("Output")]
    [Tooltip("생성된 DB를 저장할 경로")]
    public string generatedDbPath = "Assets/Data/Generated/PortraitGeneratedDB.asset";

    [Header("Validation")]
    [Tooltip("구조적 오류(중복/파싱 불가 등) 발생 시 빌드 실패")]
    public bool strictMode = false;
}