using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class AgentAdventureSession
{
    private long operationUtcSeconds;
    private AgentAdventureReplay replayJournal;
    private readonly List<AgentAdventureReplayCommand> replayCommands = new List<AgentAdventureReplayCommand>();

    private void InitializeReplayJournal(AgentAdventureRequest request)
    {
        replayJournal = new AgentAdventureReplay
        {
            engineVersion = Application.unityVersion, contentHash = AgentAdventureReplayService.ContentHash(),
            create = new AgentAdventureRequest { op = "create_run", seed = request.seed, maxRounds = request.maxRounds },
            initialProfile = profile.Capture(), startedUtcSeconds = operationUtcSeconds,
            initialStateHash = AgentAdventureReplayService.StateHash(Observe())
        };
    }

    private void RecordReplayCommand(AgentAdventureRequest request, AgentAdventureObservation state)
    {
        if (replayJournal == null) return;
        var copy = AgentAdventureReplayService.Clone(request); copy.sessionId = null;
        replayCommands.Add(new AgentAdventureReplayCommand { request = copy, utcSeconds = operationUtcSeconds,
            stateHash = AgentAdventureReplayService.StateHash(state) });
    }

    public AgentAdventureReplay Replay()
    {
        if (disposed) throw new ObjectDisposedException(nameof(AgentAdventureSession));
        if (phase == "faulted") throw new InvalidOperationException("A faulted adventure cannot export a complete replay.");
        if (replayJournal == null) throw new InvalidOperationException("Replay verification does not record another journal.");
        if (replayJournal.contentHash != AgentAdventureReplayService.ContentHash())
            throw new InvalidOperationException("Saved gameplay content changed during this adventure; start a new run.");
        var result = AgentAdventureReplayService.Clone(replayJournal);
        result.commands = replayCommands.ToArray();
        result.finalStateHash = AgentAdventureReplayService.StateHash(Observe());
        // Neither the caller nor a response serializer owns the live command objects.
        return AgentAdventureReplayService.Clone(result);
    }
}
