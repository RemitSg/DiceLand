using System.Collections.Generic;
using UnityEngine;

public partial class SpeedButton
{
    private readonly Dictionary<SpriteRenderer, Color> enabledBodyColors = new Dictionary<SpriteRenderer, Color>();
    private bool isShownForTargeting;

    public bool IsActionDisabled { get; private set; }

    // A disabled slot remains present and targetable, but owns no action or skill.
    public void SetActionDisabled(bool disabled)
    {
        if (IsActionDisabled == disabled)
            return;

        IsActionDisabled = disabled;
        MarkTargetingChanged();
        if (disabled)
        {
            EnsureRuntimeReferences();
            StopCurve();
            ResetPointerPressState();
            if (contestingAllyButton != null)
                contestingAllyButton.DowngradeFromContestedConnection(this);
            contestingAllyButton = null;
            DisconnectFromTarget();
        }

        ApplyDisabledBodyStyle(unpressedButton);
        ApplyDisabledBodyStyle(pressedButton);
        UpdateSpeedText();
        if (speedText != null)
            speedText.SetActive(isShownForTargeting && !disabled);
        UpdateHoverOutlineVisibility();
        BattleSpeedButtonLook look = GetComponent<BattleSpeedButtonLook>();
        if (look != null)
            look.RefreshLook();
    }

    private void ApplyDisabledBodyStyle(GameObject body)
    {
        if (body == null || headlessExecution)
            return;

        foreach (SpriteRenderer renderer in body.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (IsActionDisabled)
            {
                if (!enabledBodyColors.ContainsKey(renderer))
                    enabledBodyColors.Add(renderer, renderer.color);
                renderer.color = new Color(0.32f, 0.32f, 0.38f, enabledBodyColors[renderer].a);
            }
            else if (enabledBodyColors.TryGetValue(renderer, out Color original))
            {
                renderer.color = original;
                enabledBodyColors.Remove(renderer);
            }
        }
    }

    internal void RestoreClonedBodyStyle(SpeedButton template)
    {
        if (template == null || !template.IsActionDisabled)
            return;
        SpriteRenderer[] sources = template.GetComponentsInChildren<SpriteRenderer>(true);
        SpriteRenderer[] clones = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < sources.Length && i < clones.Length; i++)
        {
            if (template.enabledBodyColors.TryGetValue(sources[i], out Color original))
                clones[i].color = original;
        }
    }
}
