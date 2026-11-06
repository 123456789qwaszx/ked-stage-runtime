using System;

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
    private readonly RigBlurIntentSystem _rigBlur;

    public CharacterRigCommandFactory(
        PresentationStage stage,
        CharRigSlotResolver charRigSlotResolver,
        CharacterRigBuilder charRigBuilder,
        PortraitResolver portraitResolver,
        RoleAnchorTuningDBSO roleTuningDb,
        CharacterFocusTuningDBSO characterFocusTuningDb,
        CharacterVisualFocusPresetDBSO characterVisualFocusPresetDb,
        CharacterDepthTuningSO characterDepthTuning,
        RigBlurIntentSystem rigBlur)
    {
        _stage = stage ?? throw new ArgumentNullException(nameof(stage));
        _rigSlotResolver = charRigSlotResolver ?? throw new ArgumentNullException(nameof(charRigSlotResolver));
        _rigBuilder = charRigBuilder ?? throw new ArgumentNullException(nameof(charRigBuilder));
        _portraitResolver = portraitResolver ?? throw new ArgumentNullException(nameof(portraitResolver));
        _roleTuningDb = roleTuningDb ? roleTuningDb : throw new ArgumentNullException(nameof(roleTuningDb));
        _characterFocusTuningDb = characterFocusTuningDb ? characterFocusTuningDb : throw new ArgumentNullException(nameof(characterFocusTuningDb));
        _characterVisualFocusPresetDb = characterVisualFocusPresetDb ? characterVisualFocusPresetDb : throw new ArgumentNullException(nameof(characterVisualFocusPresetDb));
        _characterDepthTuning = characterDepthTuning ? characterDepthTuning : throw new ArgumentNullException(nameof(characterDepthTuning));
        _rigBlur = rigBlur ?? throw new ArgumentNullException(nameof(rigBlur));
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
            
            SetDepthCommandSpecCharR s => new SetDepthCommandCharR(_stage, s, _characterDepthTuning, _characterFocusTuningDb),

            // Visibility / Root Layers
            FadeCommandSpecCharR s => new FadeCommandCharR(_stage, s),

            // Basic Transform
            MoveByCommandSpecCharR s => new MoveByCommandCharR(_stage, s),
            ScaleToCommandSpecCharR s => new ScaleToCommandCharR(_stage, s),
            RotateToCommandSpecCharR s => new RotateToCommandCharR(_stage, s),
            MotionCommandSpecCharR s => new MotionCommandCharR(_stage, s),
            SlideCommandSpecCharR s => new SlideCommandCharR(_stage, s),
            SetPortraitSpriteCommandSpecCharR s => new SetPortraitSpriteCommandCharR(_stage, s, _portraitResolver),
            SetEmotionPortraitWipeCommandSpecCharR s => new SetEmotionPortraitWipeCommandCharR(_stage, s, _portraitResolver),
            
            // Composition / Focus-aware Placement
            PlaceCharacterFocusCommandSpecCharR s => new PlaceCharacterFocusCommandCharR(_stage, s, _characterFocusTuningDb),

            // Visual State
            SetSpriteCommandSpecCharR s => new SetSpriteCommandCharR(_stage, s),
            
            CharVisualFocusCommandSpecCharR s => new CharVisualFocusCommandCharR(_stage, s, _characterVisualFocusPresetDb),
            DepthFocusCommandSpecCharR s => new DepthFocusCommandCharR(_rigBlur, s),

            _ => null
        };

        return command != null;
    }
}