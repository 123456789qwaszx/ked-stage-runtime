// 공유 비주얼 무대 상태: 화면에 살아있는 rig / cast 바인딩.
// 배경도 캐릭터 리그다 — 캐스팅하지 않은 슬롯일 뿐이라 레지스트리가 하나다.
// 렌더링 오브젝트의 실제 소유권이지, per-run 실행 임시값이 아니다.
// 단일 커맨드 묶음의 취소/정리와 무관하게 session 수명 동안 유지된다.
public sealed class PresentationStage
{
    public readonly CharacterRigRegistry characterRigs = new();
    public readonly CastRegistry castRegistry = new();

    public readonly CharacterRigTargetAliasRegistry characterTargetAliases = new();


    // 무대 위 모든 리그를 파괴하고 cast를 비운다.
    // session 경계(롤백 재시작, 에피소드 종료)에서만 호출.
    // per-run 커맨드 정리에서는 호출하지 않음.
    public void Clear()
    {
        characterTargetAliases.Clear();
        castRegistry.Clear();
        characterRigs.Clear();
    }
}