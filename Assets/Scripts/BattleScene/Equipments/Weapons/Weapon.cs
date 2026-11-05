using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public Weapon_SO weaponData;
    public Transform HoldPoint;

    internal Transform BattleVisualRoot { get; private set; }
    internal Transform CharacterHoldPoint { get; private set; }

    internal void AttachToBattleGrip(Transform visualRoot, Transform characterHoldPoint)
    {
        BattleVisualRoot = visualRoot;
        CharacterHoldPoint = characterHoldPoint;
        if (characterHoldPoint != null)
            AlignBattleGrip(characterHoldPoint.position, characterHoldPoint.rotation);
    }

    internal void AlignBattleGrip(Vector3 position, Quaternion rotation)
    {
        if (BattleVisualRoot == null || HoldPoint == null)
            return;

        Quaternion rotationOffset = rotation * Quaternion.Inverse(HoldPoint.rotation);
        BattleVisualRoot.rotation = rotationOffset * BattleVisualRoot.rotation;
        BattleVisualRoot.position += position - HoldPoint.position;
    }
}
