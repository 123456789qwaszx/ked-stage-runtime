using Ked.Presentation.Core;

public sealed partial class StageCommandParser
{
    // 대기는 <<pause 시간>> 하나다 — 시간은 다른 커맨드와 같은 토큰(12fr · 0.5s · 0.5).
    // 옛 프레임 별칭 <<1fr>>~<<48fr>>은 걷었다(2026-11-03): <<12fr>> → <<pause 12fr>>.
    private const float DefaultPauseSeconds = 0.18f;

    private void EnqueueWaitSpec(string durationToken)
        => Emit(new WaitCommandSpec { duration = YarnDurationParser.Parse(durationToken, DefaultPauseSeconds) });

    private void EnqueuePresentationActorAliasSpec(string aliasSymbol, string targetKey)
        => Emit(new SetPresentationActorAliasCommandSpec
        {
            aliasSymbol = aliasSymbol,
            targetKey = targetKey
        });
}
