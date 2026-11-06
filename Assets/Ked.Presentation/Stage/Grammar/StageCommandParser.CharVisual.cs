using Ked.Presentation.Core;
public sealed partial class StageCommandParser
{
    private void EnqueueCharVisualPresetSpec(string roleKey, string presetKey, float intensity, string durationToken)
        => Emit(new CharVisualFocusCommandSpecCharR 
        { 
            roleKey = roleKey,
            presetKey = CharacterVisualFocusPresetKeyParser.Parse(presetKey),
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueCharFocusSpec(string roleKey, float intensity, string durationToken)
        => Emit(new CharVisualFocusCommandSpecCharR
        {
            presetKey = "focus",
            
            roleKey = roleKey,
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueueCharDefocusSpec(string roleKey, float intensity, string durationToken)
        => Emit(new CharVisualFocusCommandSpecCharR
        {
            presetKey = "defocus",
            
            roleKey = roleKey,
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueueCharDimSpec(string roleKey, float intensity, string durationToken)
        => Emit(new CharVisualFocusCommandSpecCharR
        {
            presetKey = "dim",
            
            roleKey = roleKey,
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueueCharSilhouetteSpec(string roleKey, float intensity, string durationToken)
        => Emit(new CharVisualFocusCommandSpecCharR
        {
            presetKey = "silhouette",
            
            roleKey = roleKey,
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueCharInnerRimSpec(string roleKey, float intensity, string durationToken)
        => Emit(new CharVisualFocusCommandSpecCharR
        {
            presetKey = "inner_rim",
            
            roleKey = roleKey,
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueCharOuterRimSpec(string roleKey, float intensity, string durationToken)
        => Emit(new CharVisualFocusCommandSpecCharR
        {
            presetKey = "outer_rim",
            
            roleKey = roleKey,
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken)
        });
    
    private void EnqueueCharRigVisualClearSpec(string roleKey, string durationToken)
        => Emit(new CharVisualFocusCommandSpecCharR
        {
            presetKey = "clear",
            
            roleKey = roleKey,
            intensity = 1f,
            duration = YarnDurationParser.Parse(durationToken),
        });
}