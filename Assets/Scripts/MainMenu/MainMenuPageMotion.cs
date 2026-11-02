using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Interruptible menu page entrance/return. Existing pages retain their own close semantics.</summary>
[DisallowMultipleComponent]
public sealed class MainMenuPageMotion : MonoBehaviour
{
    private CanvasGroup group;
    private RectTransform content;
    private Action returning, closed;
    private float alphaVelocity, positionVelocity;
    private float originalAlpha;
    private bool originalInteractable, originalBlocksRaycasts;
    private bool moving, closing, presented;

    public bool IsTransitioning => moving;
    public bool IsClosing => closing;

    public static void Show(Canvas canvas, Action onReturning)
    {
        if (canvas == null || !Application.isPlaying) return;
        MainMenuPageMotion motion = canvas.GetComponent<MainMenuPageMotion>();
        if (motion == null) motion = canvas.gameObject.AddComponent<MainMenuPageMotion>();
        motion.Prepare();
        motion.returning = onReturning;
        if (motion.presented && !motion.closing) return;
        motion.closed = null;
        if (!motion.presented)
        {
            motion.group.alpha = 0f;
            motion.content.anchoredPosition = new Vector2(48f, 0f);
            motion.alphaVelocity = motion.positionVelocity = 0f;
        }
        motion.presented = true;
        motion.closing = false;
        motion.moving = true;
        motion.group.interactable = false;
        motion.group.blocksRaycasts = true;
    }

    public static bool TryHide(Canvas canvas, Action onClosed)
    {
        if (canvas == null || !canvas.gameObject.activeSelf || !Application.isPlaying) return false;
        MainMenuPageMotion motion = canvas.GetComponent<MainMenuPageMotion>();
        if (motion == null || !motion.presented) return false;
        if (motion.closing) return true;
        motion.Prepare();
        motion.closing = motion.moving = true;
        motion.closed = onClosed;
        motion.group.interactable = false;
        motion.group.blocksRaycasts = true;
        motion.returning?.Invoke();
        return true;
    }

    private void Prepare()
    {
        if (group == null)
        {
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            originalAlpha = group.alpha;
            originalInteractable = group.interactable;
            originalBlocksRaycasts = group.blocksRaycasts;
        }
        if (content != null) return;
        var children = new List<Transform>();
        foreach (Transform child in transform) children.Add(child);
        content = new GameObject("PageMotionRoot", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(transform, false);
        content.anchorMin = Vector2.zero; content.anchorMax = Vector2.one;
        content.offsetMin = content.offsetMax = Vector2.zero;
        foreach (Transform child in children) child.SetParent(content, false);
    }

    private void Update()
    {
        Advance(Time.unscaledDeltaTime);
    }

    private void Advance(float deltaTime)
    {
        if (!moving || group == null || content == null) return;
        if (deltaTime <= 0f) return;
        float dt = Mathf.Min(deltaTime, 0.05f);
        float targetAlpha = closing ? 0f : originalAlpha;
        float targetX = closing ? 36f : 0f;
        float smoothTime = closing ? 0.055f : 0.07f;
        group.alpha = Mathf.SmoothDamp(group.alpha, targetAlpha, ref alphaVelocity, smoothTime, Mathf.Infinity, dt);
        float x = Mathf.SmoothDamp(content.anchoredPosition.x, targetX, ref positionVelocity, smoothTime, Mathf.Infinity, dt);
        content.anchoredPosition = new Vector2(x, 0f);
        if (Mathf.Abs(group.alpha-targetAlpha) > 0.004f || Mathf.Abs(x-targetX) > 0.35f) return;
        group.alpha = targetAlpha;
        content.anchoredPosition = new Vector2(targetX, 0f);
        moving = false;
        if (closing)
        {
            Action callback = closed;
            closed = null;
            callback?.Invoke();
        }
        else
        {
            group.interactable = originalInteractable;
            group.blocksRaycasts = originalBlocksRaycasts;
        }
    }

    private void OnDisable()
    {
        moving = closing = presented = false;
        closed = returning = null;
        alphaVelocity = positionVelocity = 0f;
        if (group != null)
        {
            group.alpha = originalAlpha;
            group.interactable = originalInteractable;
            group.blocksRaycasts = originalBlocksRaycasts;
        }
        if (content != null) content.anchoredPosition = Vector2.zero;
    }
}
