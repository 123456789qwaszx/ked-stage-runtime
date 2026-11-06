using System;

public sealed class ScreenEffectCommandFactory : INodeCommandFactory
{
    private readonly PresentationStage _stage;
    private readonly ScreenEffectRig _screenEffects;
    private readonly ScreenFlashPresetDBSO _flashPresetDb;
    private readonly ScreenNoisePresetDBSO _noisePresetDb;
    private readonly ScreenVignettePresetDBSO _vignettePresetDb;
    
    private readonly StageMaskMotionPresetDBSO _stageMaskMotionPresetDb;
    private readonly StageRigRegistry _stages;
    private readonly RigBlurIntentSystem _rigBlur;

    public ScreenEffectCommandFactory(
        PresentationStage stage,
        ScreenEffectRig screenEffects,
        ScreenFlashPresetDBSO flashPresetDb,
        ScreenNoisePresetDBSO noisePresetDb,
        ScreenVignettePresetDBSO vignettePresetDb,
        StageMaskMotionPresetDBSO stageMaskMotionPresetDb,
        StageRigRegistry stages,
        RigBlurIntentSystem rigBlur)
    {
        _stage = stage ?? throw new ArgumentNullException(nameof(stage));
        _screenEffects = screenEffects ? screenEffects : throw new ArgumentNullException(nameof(screenEffects));
        _flashPresetDb = flashPresetDb ? flashPresetDb : throw new ArgumentNullException(nameof(flashPresetDb));
        _noisePresetDb = noisePresetDb ? noisePresetDb : throw new ArgumentNullException(nameof(noisePresetDb));
        _vignettePresetDb = vignettePresetDb ? vignettePresetDb : throw new ArgumentNullException(nameof(vignettePresetDb));
        _stageMaskMotionPresetDb = stageMaskMotionPresetDb ? stageMaskMotionPresetDb : throw new ArgumentNullException(nameof(stageMaskMotionPresetDb));
        _stages = stages ?? throw new ArgumentNullException(nameof(stages));
        _rigBlur = rigBlur ?? throw new ArgumentNullException(nameof(rigBlur));
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            ScreenFlashCommandSpec s    => new ScreenFlashCommand(s, _screenEffects, _flashPresetDb),
            ScreenNoiseCommandSpec s    => new ScreenNoiseCommand(s, _screenEffects, _noisePresetDb),
            ScreenVignetteCommandSpec s => new ScreenVignetteCommand(s, _screenEffects, _vignettePresetDb),
            
            StageLayerBlurCommandSpec s => new StageLayerBlurCommand(_rigBlur, s),
            
            // Stage-local Mask
            StageMaskMotionCommandSpec s => new StageMaskMotionCommand(s, _stageMaskMotionPresetDb, _stages),
            StageMaskClearCommandSpec s => new StageMaskClearCommand(s, _stages),
            
            _ => null
        };

        return command != null;
    }
}