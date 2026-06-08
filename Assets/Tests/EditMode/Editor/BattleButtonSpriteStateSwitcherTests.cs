using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleButtonSpriteStateSwitcherTests
{
    private GameObject buttonObject;
    private Image image;
    private BattleButtonSpriteStateSwitcher switcher;
    private Sprite unpressedSprite;
    private Sprite pressedSprite;

    [SetUp]
    public void SetUp()
    {
        buttonObject = new GameObject(
            "BattleButtonSpriteStateSwitcherTests",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(BattleButtonSpriteStateSwitcher));
        image = buttonObject.GetComponent<Image>();
        switcher = buttonObject.GetComponent<BattleButtonSpriteStateSwitcher>();
        unpressedSprite = CreateSprite(Color.white);
        pressedSprite = CreateSprite(Color.red);

        switcher.targetImage = image;
        switcher.unpressedSprite = unpressedSprite;
        switcher.pressedSprite = pressedSprite;
    }

    [TearDown]
    public void TearDown()
    {
        if (buttonObject != null)
            Object.DestroyImmediate(buttonObject);
        if (unpressedSprite != null)
            Object.DestroyImmediate(unpressedSprite.texture);
        if (pressedSprite != null)
            Object.DestroyImmediate(pressedSprite.texture);
    }

    [Test]
    public void OnEnable_AppliesUnpressedSprite()
    {
        image.sprite = pressedSprite;

        switcher.enabled = false;
        switcher.enabled = true;

        Assert.AreSame(unpressedSprite, image.sprite);
    }

    [Test]
    public void PointerDown_UsesPressedSprite_AndPointerUpRestoresUnpressedSprite()
    {
        switcher.enabled = false;
        switcher.enabled = true;

        switcher.OnPointerDown(new PointerEventData(EventSystem.current));
        Assert.AreSame(pressedSprite, image.sprite);

        switcher.OnPointerUp(new PointerEventData(EventSystem.current));
        Assert.AreSame(unpressedSprite, image.sprite);
    }

    [Test]
    public void PointerExit_RestoresUnpressedSprite()
    {
        switcher.enabled = false;
        switcher.enabled = true;

        switcher.OnPointerDown(new PointerEventData(EventSystem.current));
        switcher.OnPointerExit(new PointerEventData(EventSystem.current));

        Assert.AreSame(unpressedSprite, image.sprite);
    }

    private static Sprite CreateSprite(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
    }
}
