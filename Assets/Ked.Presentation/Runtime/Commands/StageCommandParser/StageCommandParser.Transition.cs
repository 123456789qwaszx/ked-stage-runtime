using Ked.Presentation.Core;
public sealed partial class StageCommandParser
{
    // durationToken이 빈 문자열이면 override 없음 - 프리셋 자체 duration을 사용.
    private void EnqueueStageMaskMotionPresetSpec(
        string presetKey,
        string stage = "01",
        string durationToken = "")
        => Emit(new StageMaskMotionCommandSpec
        {
            stage = PresentationStageKeyParser.Parse(stage),
            presetKey = StageMaskMotionPresetDBSO.NormalizeKey(presetKey),
            durationOverride = string.IsNullOrEmpty(durationToken)
                ? -1f
                : YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueSlantedMaskCutInSpec(string stage, string durationToken)
        => EnqueueStageMaskMotionPresetSpec("slant_in", stage, durationToken);

    private void EnqueueSlantedMaskCutOutSpec(string stage, string durationToken)
        => EnqueueStageMaskMotionPresetSpec("slant_out", stage, durationToken);

    private void EnqueueHorizontalStripOpenInSpec(string stage, string durationToken)
        => EnqueueStageMaskMotionPresetSpec("hstrip_open", stage, durationToken);

    private void EnqueueHorizontalStripCloseOutSpec(string stage, string durationToken)
        => EnqueueStageMaskMotionPresetSpec("hstrip_close", stage, durationToken);

    private void EnqueueHorizontalStripCutInSpec(string stage, string durationToken)
        => EnqueueStageMaskMotionPresetSpec("hstrip_in", stage, durationToken);

    private void EnqueueHorizontalStripCutOutSpec(string stage, string durationToken)
        => EnqueueStageMaskMotionPresetSpec("hstrip_out", stage, durationToken);

    private void EnqueueVerticalStripOpenInSpec(string stage, string durationToken)
        => EnqueueStageMaskMotionPresetSpec("vstrip_open", stage, durationToken);

    private void EnqueueVerticalStripCloseOutSpec(string stage, string durationToken)
        => EnqueueStageMaskMotionPresetSpec("vstrip_close", stage, durationToken);

    private void EnqueueVerticalStripCutInSpec(string stage, string durationToken)
        => EnqueueStageMaskMotionPresetSpec("vstrip_in", stage, durationToken);

    private void EnqueueVerticalStripCutOutSpec(string stage, string durationToken)
        => EnqueueStageMaskMotionPresetSpec("vstrip_out", stage, durationToken);

    private void EnqueueDiagonalBandCutInSpec(string stage, string durationToken)
        => EnqueueStageMaskMotionPresetSpec("band_in", stage, durationToken);

    private void EnqueueDiagonalBandCutOutSpec(string stage, string durationToken)
        => EnqueueStageMaskMotionPresetSpec("band_out", stage, durationToken);

    private void EnqueueCircleIrisInSpec(string stage, string durationToken)
        => EnqueueStageMaskMotionPresetSpec("iris_in", stage, durationToken);

    private void EnqueueCircleIrisOutSpec(string stage, string durationToken)
        => EnqueueStageMaskMotionPresetSpec("iris_out", stage, durationToken);

    // preset
    private void EnqueueDazeFadeCloseSpec(string stage, string durationToken)
        => EnqueueStageMaskMotionPresetSpec("daze_close", stage, durationToken);

    private void EnqueueVerticalStripCoverSpec(string stage, string durationToken)
        => EnqueueStageMaskMotionPresetSpec("strip_cover", stage, durationToken);

    private void EnqueueTransitionOutDazeFadeSpec(string stage, string durationToken)
        => EnqueueStageMaskMotionPresetSpec("daze_open", stage, durationToken);

    private void EnqueueTransitionOutStripSpec(string stage, string durationToken)
        => EnqueueStageMaskMotionPresetSpec("strip_clear", stage, durationToken);
    
    private void EnqueueStageMaskClearSpec()
    {
        Emit(new StageMaskClearCommandSpec
        {
            stage = PresentationStageKey.Stage00,
            mode = StageMaskClearMode.UnmaskedFullVisible,
            hideEdge = true
        });
        Emit(new StageMaskClearCommandSpec
        {
            stage = PresentationStageKey.Stage01,
            mode = StageMaskClearMode.UnmaskedFullVisible,
            hideEdge = true
        });
        Emit(new StageMaskClearCommandSpec
        {
            stage = PresentationStageKey.Stage02,
            mode = StageMaskClearMode.UnmaskedFullVisible,
            hideEdge = true
        });
    }
}