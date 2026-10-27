public sealed class CharacterRigCommandFactory : INodeCommandFactory
{
    private readonly PresentationStage _stage;
    private readonly CharRigSlotResolver _rigSlotResolver;
    private readonly CharacterRigBuilder _rigBuilder;
    private readonly PortraitResolver _portraitResolver;

    private readonly RoleAnchorTuningDBSO _roleTuningDb;
    
    private readonly CharacterFocusTuningDBSO _characterFocusTuningDb;
    private readonly CharacterVisualFocusPresetDBSO _characterVisualFocusPresetDb;
    
    private readonly CharacterDepthTuningSO _characterDepthTuning;
    private readonly IShotResponseStageProvider _stageProvider;

    public CharacterRigCommandFactory(
        PresentationStage stage,
        CharRigSlotResolver charRigSlotResolver,
        CharacterRigBuilder charRigBuilder,
        PortraitResolver portraitResolver,
        RoleAnchorTuningDBSO roleTuningDb,
        CharacterFocusTuningDBSO characterFocusTuningDb,
        CharacterVisualFocusPresetDBSO characterVisualFocusPresetDb,
        CharacterDepthTuningSO characterDepthTuning,
        IShotResponseStageProvider stageProvider)
    {
        _stage = stage;
        _rigSlotResolver = charRigSlotResolver;
        _rigBuilder = charRigBuilder;
        _portraitResolver = portraitResolver;
        _roleTuningDb = roleTuningDb;
        _characterFocusTuningDb = characterFocusTuningDb;
        _characterVisualFocusPresetDb = characterVisualFocusPresetDb;
        _characterDepthTuning = characterDepthTuning;
        _stageProvider = stageProvider;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            // Setup / Casting
            SetupCharRigCommandSpec s => new SetupCharRigCommand(_stage, _rigSlotResolver, _rigBuilder, s),
            CastCharacterCommandSpec s => new CastCharacterCommand(_stage, s),
            SetPortraitPoseCommandSpecCharR s => new SetPortraitPoseCommandCharR(_stage, s),

            // Layout / Base State
            SetAnchorCommandSpecCharR s => new SetAnchorCommandCharR(_stage, s, _roleTuningDb),
            ApplyTrackOffsetCommandSpecCharR s => new ApplyTrackOffsetCommandCharR(_stage, s),
            MirrorCharacterCommandSpecCharR s => new MirrorCharacterCommandCharR(_stage, s),

            SetCharacterSiblingOrderCommandSpecCharR s => new SetCharacterSiblingOrderCommandCharR(_stage, s),
            MoveCharacterRigToStageLayerCommandSpecCharR s => new MoveCharacterRigToStageLayerCommandCharR(_stage, s, _rigSlotResolver),
            
            SetDepthCommandSpecCharR s => new SetDepthCommandCharR(_stage, s, _characterDepthTuning, _characterFocusTuningDb, _stageProvider),

            // Visibility / Root Layers
            FadeInCommandSpecCharR s => new FadeInCommandCharR(_stage, s),
            FadeOutCommandSpecCharR s => new FadeOutCommandCharR(_stage, s),

            // Basic Transform
            MoveByCommandSpecCharR s => new MoveByCommandCharR(_stage, s),
            ScaleToCommandSpecCharR s => new ScaleToCommandCharR(_stage, s),
            RotateToCommandSpecCharR s => new RotateToCommandCharR(_stage, s),
            RotateByCommandSpecCharR s => new RotateByCommandCharR(_stage, s),
            GestureCommandSpecCharR s => new GestureCommandCharR(_stage, s),
            MotionCommandSpecCharR s => new MotionCommandCharR(_stage, s),
            SlideInCommandSpecCharR s => new SlideInCommandCharR(_stage, s),
            SlideOutCommandSpecCharR s => new SlideOutCommandCharR(_stage, s),
            SetPortraitSpriteCommandSpecCharR s => new SetPortraitSpriteCommandCharR(_stage, s, _portraitResolver),
            SetEmotionPortraitWipeCommandSpecCharR s => new SetEmotionPortraitWipeCommandCharR(_stage, s, _portraitResolver),
            
            // Composition / Focus-aware Placement
            PlaceCharacterFocusCommandSpecCharR s => new PlaceCharacterFocusCommandCharR(_stage, s, _characterFocusTuningDb, _stageProvider),

            // Visual State
            SetSpriteCommandSpecCharR s => new SetSpriteCommandCharR(_stage, s),
            
            CharVisualFocusCommandSpecCharR s => new CharVisualFocusCommandCharR(_stage, s, _characterVisualFocusPresetDb),
            DepthFocusCommandSpecCharR s => new DepthFocusCommandCharR(_stage, s),

            _ => null
        };

        return command != null;
    }
}