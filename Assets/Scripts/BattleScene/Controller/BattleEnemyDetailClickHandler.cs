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

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        BattleSkillInfoOverlay overlay = FindObjectOfType<BattleSkillInfoOverlay>(true);
        if (overlay == null)
            return;

        overlay.ShowEnemyDetail(enemyController);
    }
}
