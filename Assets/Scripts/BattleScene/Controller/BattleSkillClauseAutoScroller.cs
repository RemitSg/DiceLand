using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BattleSkillClauseAutoScroller : MonoBehaviour
{
    private enum ScrollState
    {
        WaitingAtTop,
        ScrollingDown,
        WaitingAtBottom
    }

    private RectTransform viewport;
    private RectTransform content;
    private TextMeshProUGUI text;
    private float scrollSpeed = 24f;
    private float topDelay = 3f;
    private float bottomDelay = 3f;
    private float waitTimer;
    private ScrollState state = ScrollState.WaitingAtTop;

    public void Configure(
        RectTransform viewportRect,
        RectTransform contentRect,
        TextMeshProUGUI contentText,
        float pixelsPerSecond,
        float startDelaySeconds,
        float bottomPauseSeconds)
    {
        viewport = viewportRect;
        content = contentRect;
        text = contentText;
        scrollSpeed = Mathf.Max(0f, pixelsPerSecond);
        topDelay = Mathf.Max(0f, startDelaySeconds);
        bottomDelay = Mathf.Max(0f, bottomPauseSeconds);
        ResetToTop();
    }

    public void StartLoopFromTop()
    {
        ResetToTop();
    }

    private void OnDisable()
    {
        ResetToTop();
    }

    private void Update()
    {
        if (!CanScroll())
            return;

        float maxOffset = GetMaxScrollOffset();
        if (maxOffset <= 0f)
        {
            ResetToTop();
            return;
        }

        Vector2 position = content.anchoredPosition;
        switch (state)
        {
            case ScrollState.WaitingAtTop:
                waitTimer += Time.unscaledDeltaTime;
                if (waitTimer >= topDelay)
                {
                    waitTimer = 0f;
                    state = ScrollState.ScrollingDown;
                }
                break;
            case ScrollState.ScrollingDown:
                position.y = Mathf.Min(maxOffset, position.y + scrollSpeed * Time.unscaledDeltaTime);
                content.anchoredPosition = position;
                if (position.y >= maxOffset)
                {
                    waitTimer = 0f;
                    state = ScrollState.WaitingAtBottom;
                }
                break;
            case ScrollState.WaitingAtBottom:
                waitTimer += Time.unscaledDeltaTime;
                if (waitTimer >= bottomDelay)
                    ResetToTop();
                break;
        }
    }

    public void ResetToTop()
    {
        waitTimer = 0f;
        state = ScrollState.WaitingAtTop;
        if (content == null)
            return;

        Vector2 position = content.anchoredPosition;
        position.y = 0f;
        content.anchoredPosition = position;
    }

    private bool CanScroll()
    {
        return viewport != null && content != null && text != null && scrollSpeed > 0f;
    }

    private float GetMaxScrollOffset()
    {
        if (!CanScroll())
            return 0f;

        Canvas.ForceUpdateCanvases();
        float contentHeight = Mathf.Max(content.rect.height, text.preferredHeight);
        float viewportHeight = viewport.rect.height;
        return Mathf.Max(0f, contentHeight - viewportHeight);
    }
}
