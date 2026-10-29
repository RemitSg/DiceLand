using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class AgentAdventureSession
{
    private long operationUtcSeconds;
    private AgentAdventureReplay replayJournal;
    private AgentAdventureProfileStore profileStore;
    private string profileOwner;
    private readonly List<AgentAdventureReplayCommand> replayCommands = new List<AgentAdventureReplayCommand>();

    private void InitializeReplayJournal(AgentAdventureRequest request)
    {
        replayJournal = new AgentAdventureReplay
        {
            engineVersion = Application.unityVersion, contentHash = AgentAdventureReplayService.ContentHash(),
            create = new AgentAdventureRequest { op = "create_run", seed = request.seed, maxRounds = request.maxRounds,
                difficultyId = AssetId(map.Configuration.starting.difficulty) },
            initialProfile = profile.Capture(), startedUtcSeconds = operationUtcSeconds,
            initialStateHash = AgentAdventureReplayService.StateHash(Observe())
        };
        if (profileStore != null)
        {
            profileOwner = id;
            profileStore.Commit(profileOwner, CaptureReplay(), true);
        }
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
        return CaptureReplay();
    }

    private AgentAdventureReplay CaptureReplay()
    {
        var result = AgentAdventureReplayService.Clone(replayJournal);
        result.commands = replayCommands.ToArray();
        result.finalStateHash = AgentAdventureReplayService.StateHash(Observe());
        // Neither the caller nor a response serializer owns the live command objects.
        return AgentAdventureReplayService.Clone(result);
    }

    internal void AdoptReplay(AgentAdventureReplay replay)
    {
        replayJournal = AgentAdventureReplayService.Clone(replay);
        replayCommands.Clear(); replayCommands.AddRange(replayJournal.commands);
        replayJournal.commands = null;
    }

    private void CommitAcceptedCommand(string op)
    {
        if (profileStore == null) return;
        bool advances = op == "run_settle" || op == "run_abandon" || op == "run_unlock_ability" || op == "run_set_ability";
        profileStore.Commit(profileOwner, CaptureReplay(), advances);
    }
}
