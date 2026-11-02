public sealed class PresentationControlCommandFactory : INodeCommandFactory
{
    private readonly PresentationStage _stage;
    
    private readonly ITimeSource _time;
    private readonly ISignalBus _signal;
    private readonly ISignalLatch _latch;
    
    public PresentationControlCommandFactory(
        PresentationStage stage,
        ITimeSource time, 
        ISignalBus signal, 
        ISignalLatch latch)
    {
        _stage = stage;
        _time = time;
        _signal = signal;
        _latch = latch;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            // ActorAlias
            SetPresentationActorAliasCommandSpec s => new SetPresentationActorAliasCommand(_stage, s),
            
            WaitCommandSpec s =>  new WaitCommand(s),
            HoldSignalCommandSpec s => Create(s),
            RaiseSignalCommandSpec s => Create(s),

            _ => null
        };

        return command != null;
    }
    
    private HoldSignalCommand Create(HoldSignalCommandSpec s)
        => new(
            _latch,
            _time,
            key: s.signalKey,
            consume: s.consume,
            timeoutSeconds: s.timeoutSeconds,
            respectTimeScale: s.respectTimeScale);

    private RaiseSignalCommand Create(RaiseSignalCommandSpec s)
        => new(
            _signal,
            key: s.signalKey,
            raiseOnSkip: s.raiseOnSkip);
}