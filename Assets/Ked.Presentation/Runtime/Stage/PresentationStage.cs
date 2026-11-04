// 공유 비주얼 무대 상태:
// - 화면에 살아있는 rig / cast 바인딩.
// - session 수명 동안 유지.
public sealed class PresentationStage
{
    public readonly CharacterRigRegistry characterRigs = new();
    public readonly CastRegistry castRegistry = new();

    public readonly CharacterRigTargetAliasRegistry characterTargetAliases = new();
    
    // 무대 위 모든 리그를 파괴하고 cast를 비운다.
    // session 경계(롤백 재시작, 에피소드 종료)에서만 호출.
    public void Clear()
    {
        characterTargetAliases.Clear();
        castRegistry.Clear();
        characterRigs.Clear();
    }
}