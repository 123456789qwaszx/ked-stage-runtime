using System;
using System.Threading;

public sealed class CommandRunScope
{
    private readonly ICommandPacePolicy _pace;

    public CancellationToken Token { get; set; }

    private LifetimeScope StepLifetime { get; } = new();

    private bool _isCancelled;

    public CommandRunScope(ICommandPacePolicy pace)
    {
        _pace = pace;
        Token = CancellationToken.None;
    }

    public bool IsCancelled => _isCancelled;
    public void MarkCancelled() => _isCancelled = true;

    public bool CommandIsPlaying { get; set; }

    public bool ShouldCompressCommandExecution => _pace.CompressExecution;

    public void SetCommandIsPlaying(bool busy) => CommandIsPlaying = busy;
    
    public void ClearRuntimeState(CleanupPolicy policy = CleanupPolicy.Cancel)
    {
        CleanupStep(policy);

        Token = CancellationToken.None;
        SetCommandIsPlaying(false);
    }

    public void TrackStep(Action cancel, Action finish = null) => StepLifetime.Track(cancel, finish);

    public void CleanupStep(CleanupPolicy policy) => StepLifetime.Cleanup(policy);
}