using System.Collections;
using UnityEngine;

// Cleanup at the STEP boundary (when the next step/line starts) - the only lifetime.
// CommandBase implements this: every background routine is cleaned at the next step.
// State that must outlive a step (BGM, scene-global intents) is owned by its system, not by a command.
public interface IStepScopedCommand
{
    void RegisterStepLifetime(CommandRunScope scope, MonoBehaviour host, IEnumerator routine);
}

public abstract class CommandBase : ISequenceCommand, IStepScopedCommand
{
    protected virtual SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;
    public virtual bool WaitForCompletion => false;
    
    public IEnumerator Execute(CommandRunScope scope)
    {
        if (scope.IsCancelled)
            yield break;
        
        if (scope.ShouldCompressCommandExecution)
        {
            switch (SkipPolicy)
            {
                case SkipPolicy.Ignore:
                    yield break;

                case SkipPolicy.CompleteImmediately:
                    OnSkip(scope);
                    yield break;

                case SkipPolicy.ExecuteEvenIfSkipping:
                    break;
            }
        }
        
        IEnumerator inner = ExecuteInner(scope);

        if (inner != null)
            yield return inner;
    }

    protected abstract IEnumerator ExecuteInner(CommandRunScope scope);

    protected virtual void OnSkip(CommandRunScope scope)
    {
    }
    
    // Default step-lifetime binding.
    // Background routines are stopped at the next step boundary.
    // OnStepLifetimeFinished is called only when the boundary cleanup uses CleanupPolicy.Finish.
    // It is not a natural completion callback.
    public virtual void RegisterStepLifetime(CommandRunScope scope, MonoBehaviour host, IEnumerator routine)
    {
        scope.TrackStep(
            cancel: () =>
            {
                if (routine != null)
                    host.StopCoroutine(routine);
            },
            finish: () =>
            {
                if (routine != null)
                    host.StopCoroutine(routine);
                
                OnStepLifetimeFinished(scope);
            });
    }
    
    // Completion hook tied to step cleanup (CleanupPolicy.Finish).
    // Called when the step finishes (normal end / finish-all), not on Cancel-only cleanup.
    protected virtual void OnStepLifetimeFinished(CommandRunScope scope) { }
}