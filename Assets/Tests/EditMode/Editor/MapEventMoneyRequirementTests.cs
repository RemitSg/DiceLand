using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class MapEventMoneyRequirementTests
{
    private GameObject managerObject;

    [TearDown]
    public void TearDown()
    {
        if (managerObject != null)
            Object.DestroyImmediate(managerObject);
    }

    [Test]
    public void BuildEventOptionPreview_AllowsZeroMoneyRequirementWhenMoneyIsNegative()
    {
        RunGameManager manager = CreateManagerWithRunState(new RunState { money = -17 });
        MapEventOptionData option = new MapEventOptionData
        {
            optionId = "free",
            optionLabel = "Free Choice",
            requiredMoney = 0
        };

        MapEventOverlay.EventOptionPreviewData preview = InvokeBuildEventOptionPreview(manager, option);

        Assert.IsTrue(preview.isSelectable);
        Assert.IsTrue(string.IsNullOrWhiteSpace(preview.unavailableReason));
    }

    [Test]
    public void BuildEventOptionPreview_BlocksPositiveMoneyRequirementWhenMoneyIsNegative()
    {
        RunGameManager manager = CreateManagerWithRunState(new RunState { money = -17 });
        MapEventOptionData option = new MapEventOptionData
        {
            optionId = "paid",
            optionLabel = "Paid Choice",
            requiredMoney = 1
        };

        MapEventOverlay.EventOptionPreviewData preview = InvokeBuildEventOptionPreview(manager, option);

        Assert.IsFalse(preview.isSelectable);
        Assert.IsFalse(string.IsNullOrWhiteSpace(preview.unavailableReason));
        Assert.That(preview.unavailableReason, Does.Contain("1"));
    }

    private RunGameManager CreateManagerWithRunState(RunState runState)
    {
        managerObject = new GameObject("RunGameManagerTestHost");
        managerObject.SetActive(false);
        RunGameManager manager = managerObject.AddComponent<RunGameManager>();
        SetPrivateField(manager, "runState", runState);
        return manager;
    }

    private static MapEventOverlay.EventOptionPreviewData InvokeBuildEventOptionPreview(
        RunGameManager manager,
        MapEventOptionData option)
    {
        MethodInfo method = typeof(RunGameManager).GetMethod(
            "BuildEventOptionPreview",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        return (MapEventOverlay.EventOptionPreviewData)method.Invoke(
            manager,
            new object[] { null, null, option });
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        field.SetValue(target, value);
    }
}
