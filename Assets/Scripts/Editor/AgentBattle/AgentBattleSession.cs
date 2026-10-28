using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

// Editor-backed first migration stage. No damage/status/relic formulas belong in this adapter.
public sealed class AgentBattleSession : IDisposable
{
    private readonly string id = Guid.NewGuid().ToString("N");
    private readonly List<AgentBattleEvent> events = new List<AgentBattleEvent>();
    private readonly List<AgentBattleRequest> commands = new List<AgentBattleRequest>();
    private readonly Dictionary<string, UnityEngine.Object> actors = new Dictionary<string, UnityEngine.Object>();
    private readonly Dictionary<string, string> requestBodies = new Dictionary<string, string>();
    private readonly Dictionary<string, string> requestResults = new Dictionary<string, string>();
    private readonly Queue<string> cachedRequestIds = new Queue<string>();
    private readonly AgentBattleRequest configuration;
    private readonly string contentHash;
    private Scene scene;
    private GameObject root;
    private BattleManager manager;
    private BattleButton flow;
    private BattleSkillInfoOverlay skillFormatter;
    private BattleEncounterData encounter;
    private Random.State randomState;
    private int revision;
    private string failure;
    private bool truncated;
    private bool disposed;

    public string Id => id;

    public AgentBattleSession(AgentBattleRequest request) : this(request, null, null) { }

    public AgentBattleSession(AgentBattleRequest request, BattleEncounterData adventureEncounter, LevelTable_SO levels)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before opening an agent session.");
        if (BattleEncounterRuntime.ActiveEncounter != null || BattleEncounterRuntime.CurrentEncounter != null
            || TutorialBattleRuntime.IsActive
            || UnityEngine.Object.FindObjectsOfType<BattleManager>(true).Length != 0
            || UnityEngine.Object.FindObjectsOfType<BattleAllyController>(true).Length != 0
            || UnityEngine.Object.FindObjectsOfType<BattleEnemyController>(true).Length != 0)
            throw new InvalidOperationException("An existing battle owns the global runtime. Close it first.");
        if (adventureEncounter == null && (request.party == null || request.party.Length < 1 || request.party.Length > 4))
            throw new ArgumentException("party must contain 1 to 4 characters.");
        if (adventureEncounter == null && (request.enemies == null || request.enemies.Length < 1 || request.enemies.Length > 8))
            throw new ArgumentException("enemies must contain 1 to 8 enemy asset IDs.");
        if (request.maxRounds < 1 || request.maxRounds > 1000)
            throw new ArgumentException("maxRounds must be between 1 and 1000.");
        if (request.relics != null && request.relics.Length > 32)
            throw new ArgumentException("At most 32 relics are supported per session.");
        // Validate every asset before changing the scene or any global runtime state.
        CharacterData_SO[] characters = adventureEncounter != null ? adventureEncounter.partyMembers.Select(m => m.characterData).ToArray() : request.party.Select(p => Load<CharacterData_SO>(p.character)).ToArray();
        Weapon_SO[] weapons = adventureEncounter != null ? adventureEncounter.partyMembers.Select(m => m.weaponData).ToArray() : request.party.Select(p => Load<Weapon_SO>(p.weapon)).ToArray();
        EnemyData_SO[] enemyData = adventureEncounter != null ? adventureEncounter.Enemies.ToArray() : request.enemies.Select(Load<EnemyData_SO>).ToArray();
        Relic_SO[] relics = (request.relics ?? new string[0]).Select(Load<Relic_SO>).ToArray();
        if (adventureEncounter == null && relics.Any(HasAcquisitionEffect))
            throw new ArgumentException("This battle-only API does not simulate relic acquisition. Relics with OnRelicAcquired effects cannot be equipped yet.");
        configuration = Clone(request);
        contentHash = ContentHash();
        Scene previousScene = SceneManager.GetActiveScene();
        Random.State outsideRandom = Random.state;
        try
        {
            Random.InitState(request.seed);
            scene = EditorSceneManager.OpenScene("Assets/Scripts/Editor/AgentBattle/AgentBattleSandbox.unity", OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            root = new GameObject("AgentBattleSession");
            manager = Child("BattleManager", root.transform).AddComponent<BattleManager>();
            manager.alliesParent = Child("Allies", root.transform);
            manager.enemiesParent = Child("Enemies", root.transform);
            manager.Initialize();
            manager.StartBattle();
            manager.LevelTable = levels;
            if (adventureEncounter == null)
            {
                runtimeGroup = ScriptableObject.CreateInstance<EnemyGroup_SO>();
                runtimeGroup.name = "Agent battle group";
                runtimeGroup.enemies = new List<EnemyData_SO>(enemyData);
                encounter = new BattleEncounterData { seed = request.seed, enemyGroup = runtimeGroup, encounterLabel = "Agent battle", tileType = MapTileType.Battle, configuredExperienceReward = 0 };
                encounter.relicInventory = relics.Select(r => new RunRelicState { relicData = r, amount = 1 }).ToList();
            }
            else
            {
                encounter = adventureEncounter;
                encounter.partyMembers = adventureEncounter.partyMembers.Select(m => m.Clone()).ToList();
            }
            BattleEncounterRuntime.SetEncounter(encounter);
            BattleEncounterRuntime.ConsumeEncounter();
            for (int i = 0; i < characters.Length; i++)
            {
                var member = adventureEncounter != null ? encounter.partyMembers[i] : new RunPartyMemberState { memberId = "ally_" + i, characterData = characters[i], weaponData = weapons[i], level = 1, maxHP = characters[i].baseHP, currentHP = characters[i].baseHP };
                if (adventureEncounter == null) encounter.partyMembers.Add(member);
                BattleAllyController ally = BattleHeadlessCombatantFactory.CreateAlly(manager.alliesParent.transform, member);
                actors.Add("ally_" + i, ally);
                TrackAlly("ally_" + i, ally);
            }
            for (int i = 0; i < enemyData.Length; i++)
            {
                BattleEnemyController enemy = BattleHeadlessCombatantFactory.CreateEnemy(manager.enemiesParent.transform, enemyData[i], i);
                string actorId = "enemy_" + i;
                actors.Add(actorId, enemy);
                TrackEnemy(actorId, enemy);
            }
            manager.ManagedData.EnemyCount = enemyData.Length;
            Child("EncounterSpawner", root.transform).AddComponent<BattleSceneEncounterBootstrap>().ConfigureHeadlessExecution(manager);
            flow = Child("BattleFlow", root.transform).AddComponent<BattleButton>();
            flow.HeadlessLog += (round, message) => Event("log", null, 0, message, round);
            flow.ConfigureHeadlessExecution(manager);
            skillFormatter = Child("SkillRulesFormatter", root.transform).AddComponent<BattleSkillInfoOverlay>();
            flow.PrepareHeadlessRound();
            ConfigureButtons();
            randomState = Random.state;
        }
        catch { Dispose(); throw; }
        finally
        {
            Random.state = outsideRandom;
            if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
        }
    }

    private bool adventureResultApplied;
    public bool ResultReady => flow.HeadlessCompleted && !AwaitingRelicChoice && failure == null && !truncated;
    public BattleEncounterData Encounter => encounter;
    private bool AwaitingRelicChoice => flow.HeadlessCompleted && encounter.wasVictory && flow.HeadlessRelicChoice != null && flow.HeadlessRelicChoice.HasChoices && !flow.HeadlessRelicChoice.rewardApplied;

    public void ApplyAdventureResult(MapRunSnapshot snapshot)
    {
        if (adventureResultApplied) return;
        if (!ResultReady) throw new InvalidOperationException("Finish the battle and required rewards before returning to the adventure.");
        if (snapshot?.runState == null || snapshot.sceneName != encounter.sourceSceneName)
            throw new ArgumentException("Adventure snapshot does not match the encounter.");
        Random.State outside = Random.state;
        try
        {
            Random.state = randomState;
            MapRunRuntime.ApplyEncounterResult(snapshot, encounter);
            adventureResultApplied = true;
        }
        finally { randomState = Random.state; Random.state = outside; }
    }

    private EnemyGroup_SO runtimeGroup;
    public AgentBattleObservation Observe()
    {
        if (disposed) throw new ObjectDisposedException(nameof(AgentBattleSession));
        RegisterNewActors();
        var result = new AgentBattleObservation
        {
            sessionId = id, revision = revision, seed = configuration.seed, round = flow.HeadlessRound,
            phase = failure != null ? "faulted" : (AwaitingRelicChoice ? "reward" : (truncated || flow.HeadlessCompleted ? "terminal" : "targeting")),
            outcome = failure ?? (truncated ? "round_limit" : flow.HeadlessOutcome),
            energy = flow.HeadlessEnergy, temporaryEnergy = flow.HeadlessTemporaryEnergy, reservedEnergy = flow.HeadlessReservedEnergy,
            actors = actors.Select(pair => DescribeActor(pair.Key, pair.Value)).ToArray(), events = events.ToArray(),
            relics = encounter.relicInventory.Where(r => r != null && r.relicData != null).Select(r => DescribeRelic(r.relicData)).ToArray(),
            rewards = DescribeRewards()
        };
        result.legalActions = (result.phase == "targeting" || result.phase == "reward") ? LegalActions().ToArray() : new AgentBattleLegalAction[0];
        return result;
    }

    public AgentBattleResponse Act(AgentBattleRequest request)
    {
        if (request.sessionId != id) throw new ArgumentException("Unknown sessionId.");
        if (string.IsNullOrWhiteSpace(request.requestId)) throw new ArgumentException("requestId is required for mutations.");
        string body = JsonUtility.ToJson(request);
        if (requestBodies.TryGetValue(request.requestId, out string previous))
        {
            if (previous != body) throw new ArgumentException("requestId was already used with different parameters.");
            if (!requestResults.TryGetValue(request.requestId, out string cached))
                throw new ArgumentException("This old request was already executed. Its response expired; observe the current state.");
            return JsonUtility.FromJson<AgentBattleResponse>(cached);
        }
        if (request.revision != revision) throw new ArgumentException("Stale revision. Observe the session before retrying.");
        if (failure != null || truncated || (flow.HeadlessCompleted && !AwaitingRelicChoice)) throw new ArgumentException("The session is terminal.");
        if (commands.Count >= 10000) throw new ArgumentException("Session command limit reached. Export the replay and close the session.");
        // Reject invalid requests before consuming RNG or changing the battle.
        AgentBattleLegalAction legal = LegalActions().FirstOrDefault(a => Matches(a, request));
        if (legal == null) throw new ArgumentException("Illegal action. Choose an entry from legalActions.");
        Random.State outsideRandom = Random.state;
        try
        {
            Random.state = randomState;
            if (request.op == "choose_relic")
            {
                BattleSettlementRelicChoiceData choice = flow.HeadlessRelicChoice;
                choice.selectedChoice = choice.choices[request.choiceIndex];
                if (!BattleRelicChoiceSelectionService.TryApply(encounter, choice, out string error)) throw new InvalidOperationException(error);
            }
            else if (request.op == "refresh_rewards")
            {
                if (!BattleRelicChoiceSelectionService.TryRefresh(flow.HeadlessRelicChoice, new System.Random(Random.Range(int.MinValue, int.MaxValue))))
                    throw new InvalidOperationException("No replacement reward choices were available.");
            }
            else if (request.op == "set_skill")
            {
                BattleAllyController ally = (BattleAllyController)actors[request.actorId];
                if (!ally.SetSelectedSkillIndexForButton(ally.SpeedButtons[request.buttonIndex], request.skillIndex, flow.HeadlessRound))
                    throw new InvalidOperationException("The battle controller rejected the selected skill.");
            }
            else if (request.op == "target")
            {
                BattleAllyController ally = (BattleAllyController)actors[request.actorId];
                BattleEnemyController enemy = (BattleEnemyController)actors[request.targetId];
                if (!ally.SpeedButtons[request.buttonIndex].TryAssignTarget(enemy.SpeedButtons[request.targetButtonIndex]))
                    throw new InvalidOperationException("The battle controller rejected the selected target.");
            }
            else if (request.op == "disconnect")
            {
                BattleAllyController ally = (BattleAllyController)actors[request.actorId];
                ally.SpeedButtons[request.buttonIndex].DisconnectLine();
            }
            else if (request.op == "commit")
            {
                Drain(flow.ExecuteHeadlessRound());
                if (!flow.HeadlessCompleted)
                {
                    flow.PrepareHeadlessRound();
                    if (!flow.HeadlessCompleted && flow.HeadlessRound > configuration.maxRounds) truncated = true;
                }
                ConfigureButtons();
            }
            revision++;
            commands.Add(Clone(request));
        }
        catch (Exception exception)
        {
            failure = "execution_error";
            Event("error", null, 0, exception.ToString(), flow.HeadlessRound);
            throw;
        }
        finally { randomState = Random.state; Random.state = outsideRandom; }
        var response = new AgentBattleResponse { success = true, state = Observe() };
        requestBodies.Add(request.requestId, body);
        requestResults.Add(request.requestId, JsonUtility.ToJson(response));
        cachedRequestIds.Enqueue(request.requestId);
        while (cachedRequestIds.Count > 32) requestResults.Remove(cachedRequestIds.Dequeue());
        return response;
    }

    public AgentBattleReplay Replay()
    {
        if (runtimeGroup == null)
            throw new InvalidOperationException("Adventure battles require the owning adventure replay, including carried state and encounter configuration.");
        return new AgentBattleReplay { engineVersion = Application.unityVersion, contentHash = contentHash, create = Clone(configuration), commands = commands.Select(Clone).ToArray() };
    }

    private IEnumerable<AgentBattleLegalAction> LegalActions()
    {
        if (AwaitingRelicChoice)
        {
            BattleSettlementRelicChoiceData choice = flow.HeadlessRelicChoice;
            for (int i = 0; i < choice.choices.Count; i++)
                if (choice.choices[i]?.relicData != null) yield return new AgentBattleLegalAction { op = "choose_relic", choiceIndex = i };
            if (BattleRelicChoiceSelectionService.CanRefresh(choice)) yield return new AgentBattleLegalAction { op = "refresh_rewards" };
            yield break;
        }
        yield return new AgentBattleLegalAction { op = "commit" };
        foreach (var pair in actors)
        {
            BattleAllyController ally = pair.Value as BattleAllyController;
            if (ally == null || ally.IsDead) continue;
            EnemySkill_SO[] skills = ally.GetAvailableSkills();
            for (int b = 0; b < ally.SpeedButtons.Count; b++)
            {
                SpeedButton button = ally.SpeedButtons[b];
                if (button == null || !button.IsInteractable) continue;
                if (button.ConnectedTarget != null)
                    yield return new AgentBattleLegalAction { op = "disconnect", actorId = pair.Key, buttonIndex = b };
                for (int k = 0; k < skills.Length; k++)
                    if (flow.CanSelectSkillForAllyButton(ally, button, skills[k]))
                        yield return new AgentBattleLegalAction { op = "set_skill", actorId = pair.Key, buttonIndex = b, skillIndex = k };
                if (!flow.CanSelectSkillForAllyButton(ally, button, ally.GetSelectedSkillForButton(button))) continue;
                foreach (var target in actors)
                {
                    BattleEnemyController enemy = target.Value as BattleEnemyController;
                    if (enemy == null || enemy.IsDead) continue;
                    for (int e = 0; e < enemy.SpeedButtons.Count; e++)
                        yield return new AgentBattleLegalAction { op = "target", actorId = pair.Key, buttonIndex = b, targetId = target.Key, targetButtonIndex = e };
                }
            }
        }
    }

    private static bool Matches(AgentBattleLegalAction action, AgentBattleRequest request)
    {
        if (action.op != request.op) return false;
        if (action.op == "commit" || action.op == "refresh_rewards") return true;
        if (action.op == "choose_relic") return action.choiceIndex == request.choiceIndex;
        if (action.actorId != request.actorId || action.buttonIndex != request.buttonIndex) return false;
        if (action.op == "disconnect") return true;
        return action.op == "set_skill" ? action.skillIndex == request.skillIndex
            : action.targetId == request.targetId && action.targetButtonIndex == request.targetButtonIndex;
    }

    private AgentBattleRewards DescribeRewards()
    {
        BattleSettlementRelicChoiceData choice = flow.HeadlessRelicChoice;
        bool victory = flow.HeadlessCompleted && encounter.wasVictory;
        return new AgentBattleRewards
        {
            experience = encounter.experienceGained, money = encounter.moneyGained,
            refreshesRemaining = choice != null ? choice.refreshesRemaining : 0,
            party = (encounter.resolvedPartyMembers ?? new List<RunPartyMemberState>()).Where(m => m != null).Select(m => new AgentBattleSettledMember
                { memberId = m.memberId, hp = m.currentHP, maxHp = m.maxHP, level = m.level, experience = m.experience }).ToArray(),
            loot = (encounter.resolvedLoot ?? new List<RunLootState>()).Concat(victory ? encounter.pendingVictoryLootRewards : new List<RunLootState>()).Where(x => x?.lootItem != null).Select(x => new AgentBattleRewardItem { id = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(x.lootItem)), name = x.lootItem.lootName, description = x.lootItem.description, amount = x.amount }).ToArray(),
            relics = (encounter.resolvedRelics ?? new List<RunRelicState>()).Concat(victory ? encounter.pendingVictoryRelicRewards : new List<RunRelicState>()).Where(x => x?.relicData != null).Select(x => new AgentBattleRewardItem { id = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(x.relicData)), name = x.relicData.DisplayName, description = x.relicData.description, amount = x.amount }).ToArray(),
            blueprints = (victory ? encounter.pendingVictoryBlueprintRewards : new List<RunBlueprintState>()).Where(x => x?.blueprintData != null).Select(x => new AgentBattleRewardItem { id = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(x.blueprintData)), name = x.blueprintData.DisplayName, description = x.blueprintData.description, amount = x.amount }).ToArray(),
            weapons = (victory ? encounter.pendingVictoryWeaponRewards : new List<RunWeaponState>()).Where(x => x?.weaponData != null).Select(x => new AgentBattleRewardItem { id = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(x.weaponData)), name = x.weaponData.weaponName, description = x.weaponData.description, amount = x.amount }).ToArray(),
            choices = choice?.choices != null ? choice.choices.Select((x,i) => new AgentBattleRewardItem { index = i, id = x?.relicData != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(x.relicData)) : "", name = x?.displayName, description = x?.description, amount = x != null ? x.amount : 0 }).ToArray() : new AgentBattleRewardItem[0]
        };
    }

    private AgentBattleActor DescribeActor(string actorId, UnityEngine.Object value)
    {
        BattleAllyController ally = value as BattleAllyController;
        BattleEnemyController enemy = value as BattleEnemyController;
        bool isAlly = ally != null;
        IReadOnlyList<SpeedButton> buttons = isAlly ? ally.SpeedButtons : enemy.SpeedButtons;
        IReadOnlyList<BattleStatusRuntimeEntry> statuses = isAlly ? ally.StatusEntries : enemy.StatusEntries;
        EnemySkill_SO[] skills = isAlly ? ally.GetAvailableSkills() : new EnemySkill_SO[0];
        return new AgentBattleActor
        {
            id = actorId, side = isAlly ? "ally" : "enemy", name = isAlly ? ally.GetDebugDisplayName() : enemy.GetDebugDisplayName(),
            hp = isAlly ? ally.CurrentHP : enemy.CurrentHP, maxHp = isAlly ? ally.MaxHP : enemy.MaxHP,
            dead = isAlly ? ally.IsDead : enemy.IsDead, stage = isAlly ? 0 : enemy.CurrentStageIndex,
            pendingDelayedDeath = !isAlly && enemy.HasPendingCalamityDelayedDeath,
            stageName = !isAlly && enemy.enemyData != null && enemy.enemyData.stageSprites != null
                && enemy.CurrentStageIndex < enemy.enemyData.stageSprites.Count
                ? enemy.enemyData.stageSprites[enemy.CurrentStageIndex].stageName : "",
            passives = !isAlly && enemy.enemyData != null && enemy.enemyData.passiveDescriptions != null
                ? enemy.enemyData.passiveDescriptions.Select(p => p.passiveName + "：" + p.description).ToArray() : Array.Empty<string>(),
            statuses = statuses.Select(s => new AgentBattleStatus { id = s.Key, name = s.DisplayName, stacks = s.stacks }).ToArray(),
            skills = skills.Select((s, i) => DescribeSkill(s, i, isAlly, ally, enemy)).ToArray(),
            slots = buttons.Select((b, i) => new AgentBattleSlot { index = i, speed = b.Speed,
                skillIndex = isAlly ? ally.GetSelectedSkillIndexForButton(b) : -1,
                selectedSkill = DescribeSkill(isAlly ? ally.GetSelectedSkillForButton(b) : enemy.GetSelectedSkillForButton(b), -1, isAlly, ally, enemy),
                targetId = FindActor(b.ConnectedTarget), targetButtonIndex = FindSlot(b.ConnectedTarget), contested = b.IsContestedConnection }).ToArray()
        };
    }

    private AgentBattleSkill DescribeSkill(EnemySkill_SO skill, int index, bool isAlly, BattleAllyController ally, BattleEnemyController enemy)
    {
        AgentBattleSkill described = AgentBattleAssetReader.Skill(skill, index, isAlly, skillFormatter);
        if (described == null) return null;
        described.displayedDice = Enumerable.Range(0, Mathf.Max(0, skill.diceCount)).Select(i =>
        {
            var range = isAlly ? ally.GetDisplayedDicePowerRange(skill, i) : enemy.GetEffectiveDisplayedDicePowerRange(skill, i);
            return new AgentBattleDie { min = range.minPower, max = range.maxPower, category = skill.GetDiceSkillCategory(i).ToString() };
        }).ToArray();
        return described;
    }

    private string FindActor(SpeedButton button)
    {
        if (button == null) return null;
        Component owner = (Component)button.GetComponentInParent<BattleAllyController>() ?? button.GetComponentInParent<BattleEnemyController>();
        return actors.FirstOrDefault(p => p.Value == owner).Key;
    }
    private int FindSlot(SpeedButton button)
    {
        string owner = FindActor(button);
        if (owner == null) return -1;
        BattleAllyController ally = actors[owner] as BattleAllyController;
        IReadOnlyList<SpeedButton> buttons = ally != null ? ally.SpeedButtons : ((BattleEnemyController)actors[owner]).SpeedButtons;
        for (int i = 0; i < buttons.Count; i++) if (buttons[i] == button) return i;
        return -1;
    }

    private void RegisterNewActors()
    {
        foreach (BattleAllyController ally in manager.alliesParent.GetComponentsInChildren<BattleAllyController>(false))
            if (!actors.ContainsValue(ally)) { string key = "ally_" + actors.Count; actors.Add(key, ally); TrackAlly(key, ally); }
        foreach (BattleEnemyController enemy in manager.enemiesParent.GetComponentsInChildren<BattleEnemyController>(false))
            if (!actors.ContainsValue(enemy)) { string key = "enemy_" + actors.Count; actors.Add(key, enemy); TrackEnemy(key, enemy); }
    }
    private void TrackAlly(string key, BattleAllyController ally)
    {
        ally.Damaged += (unit, amount) => Event("damage", key, amount, null, flow != null ? flow.HeadlessRound : 1);
        ally.StatusChanged += unit => Event("status_changed", key, 0, null, flow != null ? flow.HeadlessRound : 1);
        ally.Died += unit => Event("death", key, 0, null, flow != null ? flow.HeadlessRound : 1);
    }
    private void TrackEnemy(string key, BattleEnemyController enemy)
    {
        enemy.Damaged += (unit, amount) => Event("damage", key, amount, null, flow != null ? flow.HeadlessRound : 1);
        enemy.StatusChanged += unit => Event("status_changed", key, 0, null, flow != null ? flow.HeadlessRound : 1);
        enemy.Died += unit => Event("death", key, 0, null, flow != null ? flow.HeadlessRound : 1);
    }
    private void Event(string kind, string actorId, int amount, string message, int round)
    {
        if (events.Count >= 20000) throw new InvalidOperationException("Session event limit exceeded.");
        events.Add(new AgentBattleEvent { index = events.Count, round = round, kind = kind, actorId = actorId, amount = amount, message = message });
    }
    private void ConfigureButtons()
    {
        foreach (SpeedButton button in root.GetComponentsInChildren<SpeedButton>(true)) button.ConfigureHeadlessExecution();
    }
    private static GameObject Child(string name, Transform parent)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); return go;
    }
    private static T Load<T>(string id) where T : UnityEngine.Object
    {
        string path = id != null && id.StartsWith("Assets/", StringComparison.Ordinal) ? id : AssetDatabase.GUIDToAssetPath(id ?? "");
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new ArgumentException("Invalid " + typeof(T).Name + " asset ID: " + id);
        return asset;
    }
    private static AgentBattleRequest Clone(AgentBattleRequest request) => JsonUtility.FromJson<AgentBattleRequest>(JsonUtility.ToJson(request));
    private static void Drain(IEnumerator routine)
    {
        var stack = new Stack<IEnumerator>(); stack.Push(routine);
        int steps = 0;
        try
        {
            while (stack.Count != 0)
            {
                if (++steps > 200000) throw new InvalidOperationException("Coroutine step limit exceeded.");
                IEnumerator top = stack.Peek();
                if (!top.MoveNext()) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                if (top.Current is IEnumerator nested) stack.Push(nested);
                else if (top.Current != null) throw new InvalidOperationException("Unexpected asynchronous yield in headless combat: " + top.Current.GetType().Name);
            }
        }
        finally { while (stack.Count != 0) (stack.Pop() as IDisposable)?.Dispose(); }
    }

    public static AgentBattleCatalogue Catalogue()
    {
        var catalogue = new AgentBattleCatalogue
        {
            characters = Assets<CharacterData_SO>(x => x.characterName, x => x.basicDescription),
            weapons = Assets<Weapon_SO>(x => x.weaponName, x => x.description),
            enemies = Assets<EnemyData_SO>(x => x.enemyName, x => "HP " + x.baseHP),
            relics = Assets<Relic_SO>(x => x.DisplayName, x => x.description).Select(x => { x.limitation = HasAcquisitionEffect(Load<Relic_SO>(x.id)) ? "acquisition_not_supported" : ""; return x; }).ToArray()
        };
        using (var reader = new AgentBattleAssetReader())
            catalogue.weaponDetails = catalogue.weapons.Select(w => reader.Weapon(Load<Weapon_SO>(w.id))).ToArray();
        catalogue.characterDetails = catalogue.characters.Select(c =>
        {
            var data = Load<CharacterData_SO>(c.id);
            return new AgentBattleCharacterDetails { id = c.id, name = c.name, weaponType = data.compatibleWeaponType.ToString(),
                initialWeapon = data.initialWeaponData != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(data.initialWeaponData)) : "",
                baseHp = data.baseHP, minSpeed = data.minSpeed, maxSpeed = data.maxSpeed };
        }).ToArray();
        catalogue.blueprintDetails = Assets<CraftingBlueprint_SO>(b => b.DisplayName, b => b.description)
            .Select(b => AgentBattleAssetReader.Blueprint(Load<CraftingBlueprint_SO>(b.id))).ToArray();
        catalogue.difficulties = AgentAdventureConfiguration.DescribeDifficulties();
        return catalogue;
    }
    private static bool HasAcquisitionEffect(Relic_SO relic) => relic.builtInEffects != null && relic.builtInEffects.Any(e => e != null && e.trigger == Relic_SO.RelicTrigger.OnRelicAcquired);
    private static AgentBattleAsset DescribeRelic(Relic_SO relic) => new AgentBattleAsset { id = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(relic)), name = relic.DisplayName, description = relic.description };
    private static AgentBattleAsset[] Assets<T>(Func<T, string> name, Func<T, string> description) where T : UnityEngine.Object
    {
        return AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets/Data" }).OrderBy(g => AssetDatabase.GUIDToAssetPath(g), StringComparer.Ordinal)
            .Select(g => { T asset = Load<T>(g); return new AgentBattleAsset { id = g, name = name(asset), description = description(asset) }; }).ToArray();
    }
    private static string ContentHash()
    {
        string[] paths = new[] { "Assets/Data", "Assets/Scripts" };
        string input = string.Join("\n", paths.SelectMany(p => AssetDatabase.FindAssets("", new[] { p })).Distinct().OrderBy(g => g, StringComparer.Ordinal)
            .Select(g => g + ":" + AssetDatabase.GetAssetDependencyHash(AssetDatabase.GUIDToAssetPath(g))));
        return Hash128.Compute(input).ToString();
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (flow != null) flow.ReleaseHeadlessExecution();
        if (root != null)
        {
            BattleSceneEncounterBootstrap bootstrap = root.GetComponentInChildren<BattleSceneEncounterBootstrap>();
            if (bootstrap != null) bootstrap.ReleaseHeadlessDefinitions();
        }
        if (root != null) UnityEngine.Object.DestroyImmediate(root);
        if (runtimeGroup != null) UnityEngine.Object.DestroyImmediate(runtimeGroup);
        if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
        if (encounter != null && BattleEncounterRuntime.ActiveEncounter == encounter) BattleEncounterRuntime.Clear();
        SpeedButton.CombatLocked = false; SpeedButton.InputSuppressed = false;
    }
}
