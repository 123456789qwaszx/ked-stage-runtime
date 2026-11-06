using Ked.Presentation.Core;
public sealed partial class StageCommandParser
{
    private void EnqueuePlayBgmSpec(string clipKey, string fadeDurationToken)
        => Emit(new PlayBgmCommandSpec
        {
            clipKey = clipKey,
            fadeDuration = YarnDurationParser.Parse(fadeDurationToken)
        });

    private void EnqueueStopBgmSpec(string fadeDurationToken)
        => Emit(new StopBgmCommandSpec
        {
            fadeDuration = YarnDurationParser.Parse(fadeDurationToken)
        });

    private void EnqueuePlaySfxSpec(string clipKey)
        => Emit(new PlaySfxCommandSpec { clipKey = clipKey });

    private void EnqueueStopAllSfxSpec()
        => Emit(new StopAllSfxCommandSpec());

    private void EnqueuePlayVoiceSpec(string clipKey)
        => Emit(new PlayVoiceCommandSpec { clipKey = clipKey });

    private void EnqueueStopVoiceSpec()
        => Emit(new StopVoiceCommandSpec());
}