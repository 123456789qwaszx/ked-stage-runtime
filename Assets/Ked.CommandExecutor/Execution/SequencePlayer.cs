using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Every command is guaranteed one execution entry (first MoveNext), run synchronously in PlayCommands.
// A command that throws on entry is logged and skipped — the rest of the step still runs.
public sealed class SequencePlayer
{
    private readonly MonoBehaviour _host;

    public SequencePlayer(MonoBehaviour host)
    {
        _host = host;
    }

    public IEnumerator PlayCommands(
        IReadOnlyList<ISequenceCommand> commands, CommandRunScope scope, Func<bool> isValid)
    {
        int total = commands.Count;

        for (int i = 0; i < total; i++)
        {
            ISequenceCommand command = commands[i];
            IEnumerator routine = command.Execute(scope);

            // 첫 진입(압축이면 OnSkip까지)이 던져도 그 커맨드만 빠지고 나머지는 돈다 —
            // 라이브(ExecuteInner 본문은 유니티가 따로 잡는다)와 압축 재생의 결과가 같아야 리플레이가 갈리지 않는다.
            bool hasMore;

            try
            {
                hasMore = routine.MoveNext();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SequencePlayer] '{command.GetType().Name}' threw on entry — skipped.");
                Debug.LogException(e);
                continue;
            }

            object firstYield = hasMore 
                ? routine.Current 
                : null;
            
            if (!hasMore)
                continue;

            if (command.WaitForCompletion && !scope.ShouldCompressCommandExecution)
            {
                yield return RunAfterFirstYield(routine, firstYield, scope, isValid);
            }
            else
            {
                IEnumerator wrappedRoutine = RunAfterFirstYield(routine, firstYield, scope, isValid);
                BindBackgroundLifetime(command, scope, wrappedRoutine);
                _host.StartCoroutine(wrappedRoutine);
            }
        }
    }

    private static IEnumerator RunAfterFirstYield(
        IEnumerator routine, 
        object firstYield,
        CommandRunScope scope,
        Func<bool> isValid)
    {
        if (isValid() && !scope.Token.IsCancellationRequested)
            yield return firstYield;
        
        while (isValid() && !scope.Token.IsCancellationRequested)
        {
            bool movedNext;

            try
            {
                movedNext = routine.MoveNext();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                yield break;
            }

            if (!movedNext)
                yield break;

            yield return routine.Current;
        }
    }
    
    // Binds a background routine to the step lifetime (cleaned at the next step boundary).
    private void BindBackgroundLifetime(ISequenceCommand command, CommandRunScope scope, IEnumerator routine)
    {
        switch (command)
        {
            case IStepScopedCommand stepScoped:
                stepScoped.RegisterStepLifetime(scope, _host, routine);
                break;

            default:
                Debug.LogWarning(
                    $"[SequencePlayer] Background command '{command.GetType().Name}' implements no " +
                    $"lifetime scope. Derive from CommandBase, or implement IStepScopedCommand. " +
                    $"Binding to StepLifetime as a fallback.");

                scope.TrackStep(
                    cancel: () => _host.StopCoroutine(routine),
                    finish: () => _host.StopCoroutine(routine));
                break;
        }
    }
}