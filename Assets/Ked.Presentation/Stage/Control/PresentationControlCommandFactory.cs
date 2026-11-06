using System;

public sealed class PresentationControlCommandFactory : INodeCommandFactory
{
    private readonly PresentationStage _stage;

    public PresentationControlCommandFactory(PresentationStage stage)
    {
        _stage = stage ?? throw new ArgumentNullException(nameof(stage));
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            // ActorAlias
            SetPresentationActorAliasCommandSpec s => new SetPresentationActorAliasCommand(_stage, s),

            WaitCommandSpec s =>  new WaitCommand(s),

            _ => null
        };

        return command != null;
    }
}
