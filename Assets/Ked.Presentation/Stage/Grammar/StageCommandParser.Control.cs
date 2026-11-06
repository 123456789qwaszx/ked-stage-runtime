using Ked.Presentation.Core;

public sealed partial class StageCommandParser
{
    // Animator 식 프레임 대기 별칭 — 24fps 기준 <<24fr>> = 1초, <<48fr>>까지.
    // 더 길거나 정밀한 대기는 <<pause 시간>>(다른 커맨드와 같은 시간 토큰: 12fr · 0.5s · 0.5).
    public const int FramePauseAliasMaxFrame = 48;

    private const float DefaultPauseSeconds = 0.18f;

    private void EnqueueWaitSpec(string durationToken)
        => EnqueueWaitSpec(YarnDurationParser.Parse(durationToken, DefaultPauseSeconds));

    private void EnqueueWaitSpec(float seconds)
        => Emit(new WaitCommandSpec { duration = seconds });

    private void EnqueuePresentationActorAliasSpec(string aliasSymbol, string roleKey)
        => Emit(new SetPresentationActorAliasCommandSpec
        {
            aliasSymbol = aliasSymbol,
            roleKey = roleKey
        });
}
