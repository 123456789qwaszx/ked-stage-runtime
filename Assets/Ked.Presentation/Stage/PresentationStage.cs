// 공유 비주얼 무대 상태:
// - 화면에 살아있는 rig / cast 바인딩.
// - session 수명 동안 유지.
public sealed class PresentationStage
{
    public readonly CharacterRigRegistry characterRigs = new();
    public readonly CastRegistry castRegistry = new();

    public readonly CharacterRoleAliasRegistry roleAliases = new();

    // 카메라 중립에서 본 무대. 배치 계산(place · size의 초점 고정 · shot_focus의 캐릭터 측정)이
    // 샷이 움직이는 무대 노드를 지날 때 이 값을 쓴다 — 배치는 무대 좌표이고 카메라를 모른다.
    // 조립이 샷 시스템을 붙인다. 없으면(하네스 등) 라이브 값으로 잰다.
    public IStageViewNeutral StageView { get; private set; }

    public void AttachStageView(IStageViewNeutral stageView) => StageView = stageView;
    
    // 무대 위 모든 리그를 파괴하고 cast를 비운다.
    // session 경계(롤백 재시작, 에피소드 종료)에서만 호출.
    public void Clear()
    {
        roleAliases.Clear();
        castRegistry.Clear();
        characterRigs.Clear();
    }
}