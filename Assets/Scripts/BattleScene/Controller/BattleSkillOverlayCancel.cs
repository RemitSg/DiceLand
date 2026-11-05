using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Uses the configured EventSystem cancel binding, including keyboard and controller input.</summary>
public sealed class BattleSkillOverlayCancel : MonoBehaviour, ICancelHandler
{
    public Action close;
    public void OnCancel(BaseEventData data) { close?.Invoke(); data.Use(); }
}
