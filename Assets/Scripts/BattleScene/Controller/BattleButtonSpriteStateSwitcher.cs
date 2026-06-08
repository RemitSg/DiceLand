using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BattleButtonSpriteStateSwitcher : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Sprite Targets")]
    public Image targetImage;
    public Sprite unpressedSprite;
    public Sprite pressedSprite;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        ApplyUnpressedSprite();
    }

    private void OnDisable()
    {
        ApplyUnpressedSprite();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && !button.interactable)
            return;

        ApplyPressedSprite();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ApplyUnpressedSprite();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ApplyUnpressedSprite();
    }

    private void ApplyUnpressedSprite()
    {
        ApplySprite(unpressedSprite);
    }

    private void ApplyPressedSprite()
    {
        ApplySprite(pressedSprite);
    }

    private void ApplySprite(Sprite sprite)
    {
        if (targetImage == null || sprite == null)
            return;

        targetImage.sprite = sprite;
    }
}
