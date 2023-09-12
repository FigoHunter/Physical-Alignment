using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DiceFace
{
    ONE,
    TWO,
    THREE,
    FOUR,
    FIVE,
    SIX
}

public static class DiceFaceExtension
{
    public static Quaternion RollDice(this DiceFace diceface)
    {
        switch (diceface)
        {
            case DiceFace.ONE:
                return Quaternion.identity;
            case DiceFace.TWO:
                return Quaternion.Euler(90f, 0f, 0f);
            case DiceFace.THREE:
                return Quaternion.Euler(0f, 0f, 90f);
            case DiceFace.FOUR:
                return Quaternion.Euler(-90f, 0f, 0f);
            case DiceFace.FIVE:
                return Quaternion.Euler(0f, 0f, -90f);
            case DiceFace.SIX:
                return Quaternion.Euler(180f, 0f, 0f);
            default:
                throw new System.Exception("Value Not Valid");
        }
    }
}