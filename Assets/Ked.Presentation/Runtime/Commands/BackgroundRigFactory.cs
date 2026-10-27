public sealed class BackgroundRigCommandFactory : INodeCommandFactory
{
    private readonly PresentationStage _stage;
    private readonly BackgroundRigBuilder _rigBuilder;
    private readonly BackgroundRigSlotResolver _slotResolver;

    public BackgroundRigCommandFactory(
        PresentationStage stage,
        BackgroundRigBuilder backgroundRigBuilder,
        BackgroundRigSlotResolver backgroundRigSlotResolver)
    {
        _stage = stage;
        _rigBuilder = backgroundRigBuilder;
        _slotResolver = backgroundRigSlotResolver;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            // Setup
            SetupBackgroundRigCommandSpec s => new SetupBackgroundRigCommand(_stage, s, _rigBuilder, _slotResolver),

            // Basic State
            SetAnchorCommandSpecBgR s => new SetAnchorCommandBgR(_stage, s),
            SetBackgroundSpriteCommandSpecBgR s => new SetBackgroundSpriteCommandBgR(_stage, s),
            SetOriginSizeCommandSpecBgR s => new SetOriginSizeCommandBgR(_stage, s),

            // Visibility
            FadeInCommandSpecBgR s => new FadeInCommandBgR(_stage, s),
            FadeOutCommandSpecBgR s => new FadeOutCommandBgR(_stage, s),

            // Transform
            MoveByCommandSpecBgR s => new MoveByCommandBgR(_stage, s),
            ScaleToCommandSpecBgR s => new ScaleToCommandBgR(_stage, s),

            _ => null
        };

        return command != null;
    }
}