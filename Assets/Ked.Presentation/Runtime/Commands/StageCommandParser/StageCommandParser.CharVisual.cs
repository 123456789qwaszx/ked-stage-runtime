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

    private void EnqueueCharFocusSpec(string roleKey, float intensity, string durationToken)
        => Emit(new CharVisualFocusCommandSpecCharR
        {
            presetKey = "focus",
            
            slotKey = roleKey,
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueueCharDefocusSpec(string roleKey, float intensity, string durationToken)
        => Emit(new CharVisualFocusCommandSpecCharR
        {
            presetKey = "defocus",
            
            slotKey = roleKey,
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueueCharDimSpec(string roleKey, float intensity, string durationToken)
        => Emit(new CharVisualFocusCommandSpecCharR
        {
            presetKey = "dim",
            
            slotKey = roleKey,
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueueCharSilhouetteSpec(string roleKey, float intensity, string durationToken)
        => Emit(new CharVisualFocusCommandSpecCharR
        {
            presetKey = "silhouette",
            
            slotKey = roleKey,
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueCharInnerRimSpec(string roleKey, float intensity, string durationToken)
        => Emit(new CharVisualFocusCommandSpecCharR
        {
            presetKey = "inner_rim",
            
            slotKey = roleKey,
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueCharOuterRimSpec(string roleKey, float intensity, string durationToken)
        => Emit(new CharVisualFocusCommandSpecCharR
        {
            presetKey = "outer_rim",
            
            slotKey = roleKey,
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken)
        });
    
    private void EnqueueCharRigVisualClearSpec(string roleKey, string durationToken)
        => Emit(new CharVisualFocusCommandSpecCharR
        {
            presetKey = "clear",
            
            slotKey = roleKey,
            intensity = 1f,
            duration = YarnDurationParser.Parse(durationToken),
        });
}