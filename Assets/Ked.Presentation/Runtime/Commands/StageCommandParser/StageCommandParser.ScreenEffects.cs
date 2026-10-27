using Ked.Presentation.Core;
public sealed partial class StageCommandParser
{
    private void EnqueueScreenFlashPresetSpec(string presetKey, float intensity)
        => Emit(new ScreenFlashCommandSpec
        {
            presetKey = ScreenFlashPresetDBSO.NormalizeKey(presetKey),
            intensity = intensity,
        });

    private void EnqueueScreenFlashClearSpec()
        => Emit(new ScreenFlashCommandSpec
        {
            presetKey = "clear",
            intensity = 1f,
        });

    private void EnqueueScreenVignettePresetSpec(string presetKey, float intensity, string durationToken)
        => Emit(new ScreenVignetteCommandSpec
        {
            presetKey = ScreenVignettePresetDBSO.NormalizeKey(presetKey),
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueueScreenVignetteClearSpec(string durationToken)
        => Emit(new ScreenVignetteCommandSpec
        {
            presetKey = "clear",
            intensity = 1f,
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueueScreenNoisePresetSpec(string presetKey, float intensity, string durationToken)
        => Emit(new ScreenNoiseCommandSpec
        {
            presetKey = ScreenNoisePresetDBSO.NormalizeKey(presetKey),
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueueScreenNoiseClearSpec(string durationToken)
        => Emit(new ScreenNoiseCommandSpec
        {
            presetKey = "clear",
            intensity = 1f,
            duration = YarnDurationParser.Parse(durationToken),
        });
}