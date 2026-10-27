using Ked.Presentation.Core;

public sealed partial class StageCommandParser
{
    // Animator 식 프레임 대기 별칭 — 24fps 기준 <<24fr>> = 1초, <<48fr>>까지.
    // 더 길거나 정밀한 대기는 <<pause 초>>.
    public const int FramePauseAliasMaxFrame = 48;

    private void EnqueueWaitSpec(float duration)
        => Emit(new WaitCommandSpec { duration = duration });

    private void EnqueueUIPatchSpec(string themeId)
        => Emit(new UIPatchCommandSpec { themeId = themeId });

    private void EnqueuePresentationActorAliasSpec(string aliasSymbol, string targetKey)
        => Emit(new SetPresentationActorAliasCommandSpec
        {
            aliasSymbol = aliasSymbol,
            targetKey = targetKey
        });
}
