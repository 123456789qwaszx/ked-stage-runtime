public sealed class ScreenEffectCommandFactory : INodeCommandFactory
{
    private readonly PresentationStage _stage;
    private readonly ScreenEffectRig _screenEffects;
    private readonly ScreenFlashPresetDBSO _flashPresetDb;
    private readonly ScreenNoisePresetDBSO _noisePresetDb;
    private readonly ScreenVignettePresetDBSO _vignettePresetDb;
    
    private readonly StageMaskMotionPresetDBSO _stageMaskMotionPresetDb;
    private readonly IStageMaskProvider _stageMaskProvider;
    private readonly IStageDepthContentSlotProvider _stageDepthContentSlots;

    public ScreenEffectCommandFactory(
        PresentationStage stage,
        ScreenEffectRig screenEffects,
        ScreenFlashPresetDBSO flashPresetDb,
        ScreenNoisePresetDBSO noisePresetDb,
        ScreenVignettePresetDBSO vignettePresetDb,
        StageMaskMotionPresetDBSO stageMaskMotionPresetDb,
        IStageMaskProvider stageMaskProvider,
        IStageDepthContentSlotProvider stageDepthContentSlots)
    {
        _stage = stage;
        _screenEffects = screenEffects;
        _flashPresetDb = flashPresetDb;
        _noisePresetDb = noisePresetDb;
        _vignettePresetDb = vignettePresetDb;
        _stageMaskMotionPresetDb = stageMaskMotionPresetDb;
        _stageMaskProvider = stageMaskProvider;
        _stageDepthContentSlots = stageDepthContentSlots;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            ScreenFlashCommandSpec s    => new ScreenFlashCommand(s, _screenEffects, _flashPresetDb),
            ScreenNoiseCommandSpec s    => new ScreenNoiseCommand(s, _screenEffects, _noisePresetDb),
            ScreenVignetteCommandSpec s => new ScreenVignetteCommand(s, _screenEffects, _vignettePresetDb),
            
            StageLayerBlurCommandSpec s => new StageLayerBlurCommand(_stage, s, _stageDepthContentSlots),
            
            // Stage-local Mask
            StageMaskMotionCommandSpec s => new StageMaskMotionCommand(s, _stageMaskMotionPresetDb, _stageMaskProvider),
            StageMaskClearCommandSpec s => new StageMaskClearCommand(s, _stageMaskProvider),
            
            _ => null
        };

        return command != null;
    }
}