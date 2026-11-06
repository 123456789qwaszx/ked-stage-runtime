using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class CommandExecutor : MonoBehaviour
{
    private SequencePlayer _sequencePlayer;
    private CompositeCommandFactory _factory;

    private CancellationTokenSource _cts;
    private Coroutine _mainRoutine;
    private CommandRunScope _activeScope;

    private int _runId;

    // 스텝(PlaySpecs 한 번)의 세대 — _runId는 런(Stop)의 세대다.
    private int _stepId;

    public void Initialize(CompositeCommandFactory factory)
    {
        _sequencePlayer = new SequencePlayer(this);
        _factory = factory;
    }
    
    public void PlaySpecs(IReadOnlyList<CommandSpecBase> specs, CommandRunScope scope)
    {
        // 세션(scope)이 없을 때 스텝이 오는 것은 호출 순서의 잘못이다 — 막는 척하지 않고 드러낸다.
        if (scope == null)
        {
            Debug.LogWarning("[CommandExecutor] PlaySpecs without a scope — the session has not started or already ended. Skipped.");
            return;
        }

        int runId = _runId;
        int stepId = ++_stepId;
        _activeScope = scope;

        _activeScope.CleanupStep(CleanupPolicy.Finish);

        List<ISequenceCommand> commands = BuildCommandsFromSpecs(specs);

        ResetToken();
        _activeScope.Token = _cts.Token;

        _mainRoutine = StartCoroutine(RunNode(commands, _activeScope, runId, stepId));
    }

    private List<ISequenceCommand> BuildCommandsFromSpecs(IReadOnlyList<CommandSpecBase> specs)
    {
        var list = new List<ISequenceCommand>();

        for (int i = 0; i < specs.Count; i++)
        {
            if (!_factory.TryCreate(specs[i], out ISequenceCommand command))
                continue;

            list.Add(command);
        }

        return list;
    }

    private IEnumerator RunNode(
        List<ISequenceCommand> commands,
        CommandRunScope scope,
        int runId,
        int stepId)
    {
        scope.SetCommandIsPlaying(true);

        try
        {
            yield return _sequencePlayer.PlayCommands(
                commands,
                scope,
                isValid: () => runId == _runId);
        }
        finally
        {
            // 마지막 스텝의 실행일 때만 정리한다 — 늦게 끝난 앞 스텝이 지금 스텝의 토큰·진행 표시를 덮지 않게.
            // 런이 바뀌었다면(Stop) 정리는 Stop이 이미 했다.
            if (runId == _runId && stepId == _stepId)
            {
                scope.Token = CancellationToken.None;
                _mainRoutine = null;
                scope.SetCommandIsPlaying(false);
            }
        }
    }

    public void Stop(CleanupPolicy policy)
    {
        _runId++;

        if(policy == CleanupPolicy.Cancel)
            _activeScope?.MarkCancelled();
        
        CancelAndDisposeToken();
        
        if (_mainRoutine != null)
            StopCoroutine(_mainRoutine);
        
        _activeScope?.ClearRuntimeState(policy);
        
        _mainRoutine = null;
    }

    private void ResetToken()
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
    }

    private void CancelAndDisposeToken()
    {
        if (_cts == null)
            return;

        if (!_cts.IsCancellationRequested)
            _cts.Cancel();

        _cts.Dispose();
        _cts = null;
    }
}