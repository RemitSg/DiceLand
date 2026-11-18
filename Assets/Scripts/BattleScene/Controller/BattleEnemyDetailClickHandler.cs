using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class BattleEnemyDetailClickHandler : MonoBehaviour
{
    [SerializeField] private BattleEnemyController enemyController;

    public void Bind(BattleEnemyController controller)
    {
        enemyController = controller;
    }

    private void OnMouseUpAsButton()
    {
        if (enemyController == null || enemyController.IsDead)
            return;

        if (SpeedButton.CombatLocked || SpeedButton.InputSuppressed || BattlePresentationContinueInputService.IsEnemyDetailInteractionBlocked)
            return;

        Vector2 point = Input.mousePosition;
        if (!SpeedButton.IsScreenPointAllowedByTutorialHighlight(point) || BattleRoundPreviewOverlay.ContainsPointer(point))
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        BattleSkillInfoOverlay overlay = FindObjectOfType<BattleSkillInfoOverlay>(true);
        if (overlay == null || overlay.IsVisible)
            return;

        overlay.ShowEnemyDetail(enemyController);
    }
}
