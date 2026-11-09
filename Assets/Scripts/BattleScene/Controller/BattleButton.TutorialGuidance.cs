using UnityEngine;

public partial class BattleButton
{
    private bool tutorialInteractionArmed;
    private TutorialBattleInteraction pendingTutorialInteraction;
    private TutorialBattleInteraction tutorialPanelCloseInteraction;

    private void LateUpdate()
    {
        RefreshTutorialInteractionGuidance();
    }

    private void ArmTutorialInteractionGuidance()
    {
        pendingTutorialInteraction = TutorialBattleInteractionGuidance.ForStep(
            TutorialBattleRuntime.CurrentStep, currentPhase == BattlePhase.Ready);
        tutorialInteractionArmed = TutorialBattleInteractionGuidance.NeedsGuidance(pendingTutorialInteraction);
        RefreshTutorialInteractionGuidance();
    }

    private void RefreshTutorialInteractionGuidance()
    {
        if (!TutorialBattleRuntime.IsActive || tutorialBattleGuideOverlay == null ||
            tutorialBattleGuideOverlay.IsModalVisible || isResolvingCombat)
            return;

        bool infoVisible = battleSkillInfoOverlay != null && battleSkillInfoOverlay.IsVisible;
        if (infoVisible)
        {
            bool selecting = pendingTutorialInteraction == TutorialBattleInteraction.SwitchSkill ||
                pendingTutorialInteraction == TutorialBattleInteraction.EnergyConnect &&
                !battleSkillInfoOverlay.HasTutorialEnergySkillSelected;
            if (tutorialInteractionArmed && selecting && battleSkillInfoOverlay.IsAllySelectionVisible)
            {
                ShowFirstInteractionTarget(TutorialBattleHighlightTarget.SkillSelection);
            }
            else if (TutorialBattleInteractionGuidance.NeedsGuidance(tutorialPanelCloseInteraction))
            {
                ShowFirstInteractionTarget(TutorialBattleHighlightTarget.CloseInformation);
            }
            else
            {
                HideTutorialTargetHighlightIfVisible();
            }
            return;
        }

        if (!tutorialInteractionArmed || TutorialBattleRuntime.HasPendingStepPopup)
            return;
        if (currentPhase == BattlePhase.Ready && pendingTutorialInteraction != TutorialBattleInteraction.Prepare &&
            pendingTutorialInteraction != TutorialBattleInteraction.EnemyDetail)
            return;

        switch (pendingTutorialInteraction)
        {
            case TutorialBattleInteraction.EnemyDetail:
                ShowFirstInteractionTarget(TutorialBattleHighlightTarget.FirstEnemyBody);
                break;
            case TutorialBattleInteraction.EnemySkill:
                ShowFirstInteractionTarget(TutorialBattleHighlightTarget.FirstEnemySpeedButton);
                break;
            case TutorialBattleInteraction.AllySkill:
            case TutorialBattleInteraction.SwitchSkill:
                ShowFirstInteractionTarget(TutorialBattleHighlightTarget.FirstAllySpeedButton);
                break;
            case TutorialBattleInteraction.Connect:
            case TutorialBattleInteraction.Redirect:
            case TutorialBattleInteraction.EnergyConnect:
                ShowFirstInteractionTarget(TutorialBattleHighlightTarget.Connection);
                break;
            case TutorialBattleInteraction.Prepare:
            case TutorialBattleInteraction.Execute:
                ShowFirstInteractionTarget(TutorialBattleHighlightTarget.BattleButton);
                break;
        }
    }

    private void ShowFirstInteractionTarget(TutorialBattleHighlightTarget target)
    {
        if (activeTutorialHighlightTarget == target && tutorialBattleGuideOverlay.IsTargetHighlightVisible)
            return;
        if (!TryResolveTutorialHighlightRect(target, out Rect rect))
            return;

        activeTutorialHighlightTarget = target;
        tutorialBattleGuideOverlay.ShowTargetHighlight(rect);
        tutorialBattleGuideOverlay.TrackTarget(() =>
            TryResolveTutorialHighlightRect(target, out Rect current) ? current : (Rect?)null);
        if (target == TutorialBattleHighlightTarget.Connection)
            tutorialBattleGuideOverlay.TrackAdditionalTarget(() =>
                TryGetFirstEnemySpeedButtonScreenRect(out Rect enemy) ? enemy : (Rect?)null);
        else if (target == TutorialBattleHighlightTarget.CloseInformation)
            tutorialBattleGuideOverlay.TrackAdditionalTarget(() =>
                battleSkillInfoOverlay.TryGetTutorialReadingRect(out Rect reading) ? reading : (Rect?)null);
    }

    private void CompleteTutorialInteraction(TutorialBattleInteraction interaction)
    {
        if (!TutorialBattleRuntime.IsActive) return;
        TutorialBattleInteractionGuidance.Complete(interaction);
        if (pendingTutorialInteraction == interaction)
            tutorialInteractionArmed = false;
        HideTutorialTargetHighlightIfVisible();
    }

    private bool TryGetTutorialAllyButtonRect(out Rect rect)
    {
        rect = default;
        var allies = activeRoundAllies != null && activeRoundAllies.Length > 0
            ? activeRoundAllies : FindObjectsOfType<BattleAllyController>();
        string step = TutorialBattleRuntime.CurrentStep?.stepId;
        string requiredCharacter = step == "battle02_view_molly_skill" || step == "battle02_probe_with_molly"
            ? CharacterData_SO.MollyId
            : step == "battle02_view_chara_guard" || step == "battle02_redirect_with_guard" ? CharacterData_SO.CharaId : null;
        Rect? fallback = null;
        foreach (BattleAllyController ally in allies)
        {
            if (ally == null || ally.IsDead ||
                requiredCharacter != null && (ally.CharacterData == null || !ally.CharacterData.MatchesId(requiredCharacter)))
                continue;
            foreach (SpeedButton button in BattleSpeedButtonListService.GetAllyButtons(ally))
            {
                if (button == null || !button.gameObject.activeInHierarchy || !button.IsInteractable)
                    continue;
                if (!button.TryGetScreenHighlightRect(out Rect candidate))
                    continue;
                if (!fallback.HasValue)
                    fallback = candidate;
                if (pendingTutorialInteraction == TutorialBattleInteraction.EnergyConnect &&
                    (ally.GetSelectedSkillForButton(button) == null || ally.GetSelectedSkillForButton(button).energyCost <= 0))
                    continue;
                rect = candidate;
                return true;
            }
        }
        if (fallback.HasValue)
            rect = fallback.Value;
        return fallback.HasValue;
    }

    private bool TryGetTutorialEnemyBodyRect(out Rect rect)
    {
        rect = default;
        Camera camera = Camera.main;
        if (camera == null) return false;
        foreach (BattleEnemyController enemy in FindObjectsOfType<BattleEnemyController>())
        {
            if (enemy.IsDead) continue;
            foreach (BattleEnemyDetailClickHandler handler in enemy.GetComponentsInChildren<BattleEnemyDetailClickHandler>())
            {
                Collider2D collider = handler.GetComponent<Collider2D>();
                if (collider == null || !collider.enabled) continue;
                Bounds bounds = collider.bounds;
                Vector3 min = camera.WorldToScreenPoint(bounds.min);
                Vector3 max = camera.WorldToScreenPoint(bounds.max);
                rect = Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y),
                    Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
                return rect.width > 1 && rect.height > 1;
            }
        }
        return false;
    }
}
