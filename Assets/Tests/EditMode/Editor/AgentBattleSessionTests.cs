using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public sealed class AgentBattleSessionTests
{
    private AgentBattleSession session;
    [TearDown] public void TearDown() { session?.Dispose(); session = null; }

    private static AgentBattleRequest Configuration(int seed = 1234, int maxRounds = 20)
    {
        AgentBattleCatalogue catalogue = AgentBattleSession.Catalogue();
        return new AgentBattleRequest { op = "create", seed = seed, maxRounds = maxRounds,
            party = new[] { new AgentBattlePartyMember { character = catalogue.characters.First(c => c.name == "Chara").id, weapon = catalogue.weapons.First(w => w.name == "新手木剑").id } },
            enemies = new[] { catalogue.enemies.First(e => e.name == "绿色史莱姆").id } };
    }
    private AgentBattleRequest Command(string op)
    {
        AgentBattleObservation state = session.Observe();
        return new AgentBattleRequest { op = op, sessionId = state.sessionId, revision = state.revision, requestId = Guid.NewGuid().ToString("N") };
    }

    [Test] public void Create_PreparesRealContentWithoutCanvas_AndPreservesUserScene()
    {
        Scene original = SceneManager.GetActiveScene();
        int canvases = UnityEngine.Object.FindObjectsOfType<Canvas>(true).Length;
        session = new AgentBattleSession(Configuration());
        AgentBattleObservation state = session.Observe();
        Assert.AreEqual("targeting", state.phase);
        Assert.AreEqual(5, state.actors.Single(a => a.side == "enemy").hp);
        Assert.IsTrue(state.legalActions.Any(a => a.op == "target"));
        Assert.AreEqual(canvases, UnityEngine.Object.FindObjectsOfType<Canvas>(true).Length);
        Assert.AreEqual(original, SceneManager.GetActiveScene());
        session.Dispose(); session = null;
        Assert.AreEqual(original, SceneManager.GetActiveScene());
        Assert.IsNull(BattleEncounterRuntime.ActiveEncounter);
    }

    [Test] public void CommitWithoutAllyTarget_UsesRealEnemyAttackAndAdvancesRound()
    {
        session = new AgentBattleSession(Configuration());
        int hp = session.Observe().actors.Single(a => a.side == "ally").hp;
        AgentBattleObservation state = session.Act(Command("commit")).state;
        Assert.AreEqual(2, state.round);
        Assert.Less(state.actors.Single(a => a.side == "ally").hp, hp, "Skipping graphics must still invoke the damage callback.");
        Assert.IsTrue(state.events.Any(e => e.kind == "damage" && e.actorId == "ally_0"));
    }

    [Test] public void InvalidTarget_DoesNotChangeStateOrRevision()
    {
        session = new AgentBattleSession(Configuration());
        string before = JsonUtility.ToJson(session.Observe());
        AgentBattleRequest request = Command("target"); request.actorId = "ally_0"; request.targetId = "missing";
        Assert.Throws<ArgumentException>(() => session.Act(request));
        Assert.AreEqual(before, JsonUtility.ToJson(session.Observe()));
    }

    [Test] public void RetriedCommit_ReturnsOriginalResponseWithoutExecutingAgain()
    {
        session = new AgentBattleSession(Configuration());
        AgentBattleRequest command = Command("commit");
        string first = JsonUtility.ToJson(session.Act(command));
        Assert.AreEqual(first, JsonUtility.ToJson(session.Act(command)));
        Assert.AreEqual(1, session.Observe().revision);
        Assert.AreEqual(2, session.Observe().round);
        command.op = "target";
        Assert.Throws<ArgumentException>(() => session.Act(command));
    }

    [Test] public void StaleRevision_IsRejectedBeforeMutation()
    {
        session = new AgentBattleSession(Configuration());
        AgentBattleRequest stale = Command("commit");
        session.Act(Command("commit"));
        string before = JsonUtility.ToJson(session.Observe());
        Assert.Throws<ArgumentException>(() => session.Act(stale));
        Assert.AreEqual(before, JsonUtility.ToJson(session.Observe()));
    }

    [Test] public void RoundLimit_IsTerminalAndDoesNotPretendToBeVictory()
    {
        session = new AgentBattleSession(Configuration(maxRounds: 1));
        AgentBattleObservation state = session.Act(Command("commit")).state;
        Assert.AreEqual("terminal", state.phase);
        Assert.AreEqual("round_limit", state.outcome);
        Assert.IsEmpty(state.legalActions);
        Assert.Throws<ArgumentException>(() => session.Act(Command("commit")));
    }

    [Test] public void SeededSessions_ReproduceResultsAndDoNotConsumeOutsideRandom()
    {
        Random.State before = Random.state;
        string first = PlayTrace();
        Assert.AreEqual(JsonUtility.ToJson(before), JsonUtility.ToJson(Random.state));
        string second = PlayTrace();
        Assert.AreEqual(first, second);
        Assert.AreEqual(JsonUtility.ToJson(before), JsonUtility.ToJson(Random.state));
    }

    private string PlayTrace()
    {
        session = new AgentBattleSession(Configuration(seed: 98765));
        AgentBattleObservation state = session.Observe();
        for (int i = 0; i < 8 && state.phase == "targeting"; i++)
        {
            AgentBattleLegalAction target = state.legalActions.FirstOrDefault(a => a.op == "target");
            if (target != null)
            {
                AgentBattleRequest command = Command("target"); command.actorId = target.actorId; command.buttonIndex = target.buttonIndex;
                command.targetId = target.targetId; command.targetButtonIndex = target.targetButtonIndex;
                session.Act(command);
            }
            state = session.Act(Command("commit")).state;
        }
        Assert.IsTrue(state.phase == "terminal" || state.round > 1);
        state.sessionId = "";
        string result = JsonUtility.ToJson(state);
        session.Dispose(); session = null;
        return result;
    }

    [Test] public void InvalidAsset_FailsWithoutLeavingSessionObjectsOrEncounter()
    {
        AgentBattleRequest request = Configuration(); request.enemies[0] = "does-not-exist";
        int scenes = SceneManager.sceneCount;
        Assert.Throws<ArgumentException>(() => new AgentBattleSession(request));
        Assert.AreEqual(scenes, SceneManager.sceneCount);
        Assert.IsNull(BattleEncounterRuntime.ActiveEncounter);
    }

    [Test] public void BattleStartRelic_UsesRealBurnEffect_AndReportsLoadout()
    {
        AgentBattleRequest request = Configuration();
        request.relics = new[] { AgentBattleSession.Catalogue().relics.Single(r => r.name == "女孩的火柴").id };
        session = new AgentBattleSession(request);
        AgentBattleObservation state = session.Observe();
        Assert.AreEqual("女孩的火柴", state.relics.Single().name);
        Assert.AreEqual(1, state.actors.Single(a => a.side == "enemy").statuses.Single(s => s.id == "burn").stacks);
    }

    [Test] public void AcquisitionRelic_IsExplicitlyRejectedInsteadOfSilentlyIgnoringEffect()
    {
        AgentBattleRequest request = Configuration();
        AgentBattleAsset strawberry = AgentBattleSession.Catalogue().relics.Single(r => r.name == "草莓");
        Assert.AreEqual("acquisition_not_supported", strawberry.limitation);
        request.relics = new[] { strawberry.id };
        Assert.Throws<ArgumentException>(() => new AgentBattleSession(request));
        Assert.IsNull(BattleEncounterRuntime.ActiveEncounter);
    }

    [Test] public void SkillDescriptions_ExposeTheInGameStatusRules()
    {
        AgentBattleRequest request = Configuration();
        request.party[0].weapon = AgentBattleSession.Catalogue().weapons.Single(w => w.name == "熔火铁剑").id;
        session = new AgentBattleSession(request);
        Assert.IsTrue(session.Observe().actors.Single(a => a.side == "ally").skills.Any(s => s.description.Contains("燃烧")));
    }

    [Test] public void MultipleAlliesAndEnemies_ExposeAllSlotsAndLegalTargets()
    {
        AgentBattleRequest request = Configuration();
        request.party = new[] { request.party[0], request.party[0] };
        request.enemies = new[] { request.enemies[0], request.enemies[0] };
        session = new AgentBattleSession(request);
        AgentBattleObservation state = session.Observe();
        Assert.AreEqual(4, state.actors.Length);
        Assert.AreEqual(4, state.legalActions.Count(a => a.op == "target"));
        state = session.Act(Command("commit")).state;
        Assert.AreEqual(2, state.round);
        Assert.IsTrue(state.actors.Where(a => a.side == "ally").Any(a => a.hp < a.maxHp));
    }

    [Test] public void SummoningRelic_UsesTheExistingBootstrapAndAddsARealAlly()
    {
        AgentBattleRequest request = Configuration();
        request.relics = new[] { AgentBattleSession.Catalogue().relics.Single(r => r.name == "断线傀儡").id };
        session = new AgentBattleSession(request);
        AgentBattleObservation state = session.Observe();
        Assert.AreEqual(2, state.actors.Count(a => a.side == "ally"));
        Assert.IsTrue(state.actors.Where(a => a.side == "ally").Any(a => a.maxHp == 15));
        Assert.IsTrue(state.events.Any(e => e.message != null && e.message.Contains("遗物召唤")));
    }

    [Test] public void EnemySummoning_UsesTheExistingSpawnEntryAndAppearsInObservation()
    {
        session = new AgentBattleSession(Configuration());
        EnemyData_SO slime = AssetDatabase.LoadAssetAtPath<EnemyData_SO>(AssetDatabase.GUIDToAssetPath(Configuration().enemies[0]));
        BattleSceneEncounterBootstrap bootstrap = UnityEngine.Object.FindObjectOfType<BattleSceneEncounterBootstrap>();
        Assert.IsNotNull(bootstrap.SpawnEnemyDuringBattle(slime));
        AgentBattleObservation state = session.Observe();
        Assert.AreEqual(2, state.actors.Count(a => a.side == "enemy"));
        Assert.AreEqual(2, state.legalActions.Count(a => a.op == "target"));
    }
}
