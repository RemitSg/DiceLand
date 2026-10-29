using System;
using System.Collections;
using UnityEngine;

// A second driver for the existing battle flow, not a second implementation of its rules.
public partial class BattleButton
{
    private bool headlessExecution;
    private bool headlessCompleted;
    private string headlessOutcome;
    private BattleSettlementRelicChoiceData headlessRelicChoice;
    public BattleSettlementRelicChoiceData HeadlessRelicChoice => headlessRelicChoice;
    public event Action<int, string> HeadlessLog;
    public bool HeadlessCompleted => headlessCompleted;
    public string HeadlessOutcome => headlessOutcome;
    public int HeadlessRound => currentRound;
    public int HeadlessEnergy => currentSharedEnergy;
    public int HeadlessTemporaryEnergy => temporarySharedEnergy;
    public int HeadlessReservedEnergy => reservedSharedEnergy;

    public void ConfigureHeadlessExecution(BattleManager manager)
    {
        if (manager == null) throw new ArgumentNullException(nameof(manager));
        headlessExecution = true;
        battleManager = manager;
        InitializeBattleRuntime();
    }

    public void PrepareHeadlessRound()
    {
        RequireHeadless();
        if (headlessCompleted) return;
        if (currentPhase != BattlePhase.Ready || isResolvingCombat)
            throw new InvalidOperationException("The battle is not ready to prepare a round.");
        SetupTargetingPhase(HeadlessAllies(), HeadlessEnemies());
        if (!headlessCompleted) currentPhase = BattlePhase.Targeting;
    }

    public IEnumerator ExecuteHeadlessRound()
    {
        RequireHeadless();
        if (headlessCompleted || currentPhase != BattlePhase.Targeting || isResolvingCombat)
            throw new InvalidOperationException("The battle is not awaiting a battle plan.");
        return ExecuteCombatPhaseRoutine(HeadlessAllies(), HeadlessEnemies());
    }

    public void ReleaseHeadlessExecution()
    {
        if (!headlessExecution) return;
        ClearActiveRoundCombatants();
        // Detach the same listeners that the UI driver installs during targeting.
        BattleSharedEnergyListenerBindingService.Unbind(HeadlessAllies(), HeadlessEnemies(),
            HandleAllySelectedSkillChanged, HandleAllyStatusChanged, HandleAllyConnectionChanged, HandleEnemyStatusChanged);
        SpeedButton.CombatLocked = false;
        SpeedButton.InputSuppressed = false;
    }

    private BattleAllyController[] HeadlessAllies() => battleManager.alliesParent.GetComponentsInChildren<BattleAllyController>(false);
    private BattleEnemyController[] HeadlessEnemies() => battleManager.enemiesParent.GetComponentsInChildren<BattleEnemyController>(false);
    private void RequireHeadless()
    {
        if (!headlessExecution) throw new InvalidOperationException("Headless execution was not configured.");
    }
}
