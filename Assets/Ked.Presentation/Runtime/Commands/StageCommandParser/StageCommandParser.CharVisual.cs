using Ked.Presentation.Core;
public sealed partial class StageCommandParser
{
    private void EnqueueCharVisualPresetSpec(string roleKey, string presetKey, float intensity, string durationToken)
        => Emit(new CharVisualFocusCommandSpecCharR 
        { 
            slotKey = roleKey,
            presetKey = CharacterVisualFocusPresetKeyParser.Parse(presetKey),
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken)
        });
}