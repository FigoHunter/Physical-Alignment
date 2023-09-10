using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static Codice.Client.BaseCommands.Import.Commit;
using Codice.Client.Common.GameUI;
using System.ComponentModel;

public enum DiceFace
{ 
    ONE,
    TWO,
    THREE,
    FOUR,
    FIVE,
    SIX
}

public class SetupObject:AssetPostprocessor
{
    void OnPreprocessModel()
    {
        ModelImporter modelImporter = assetImporter as ModelImporter;
        modelImporter.useFileScale = false;
        modelImporter.globalScale = 0.01f;
    }

    [MenuItem("GameObject/Rosita/Physical Alignment/Move To Plane")]
    public static void MoveToPlane()
    {
        MoveToPlane(GetSelected(), Quaternion.identity);
    }

    private static DiceFace diceFace = DiceFace.ONE;
    [MenuItem("GameObject/Rosita/Physical Alignment/Roll Dice")]
    public static void RollDice()
    {
        var selected = GetSelected();
        diceFace = (DiceFace)(((int)diceFace+1)%6);
        var rot = RollDice(diceFace);
        selected.transform.rotation = rot;
        MoveToPlane(selected, rot);
    }


    public static void MoveToPlane(GameObject target,Quaternion rot)
    {
        Mesh mesh = target.GetComponentInChildren<MeshFilter>().sharedMesh;
        mesh.RecalculateBounds();
        Bounds bbox = mesh.bounds;
        Vector3 pos = target.transform.position;
        var max = rot * bbox.max;
        var min = rot * bbox.min;

        target.transform.position = new Vector3(pos.x, -Mathf.Min(max.y, min.y), pos.z);
    }

    public static Quaternion RollDice(DiceFace diceface)
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
                return Quaternion.Euler(-90f,0f,0f);
            case DiceFace.FIVE:
                return Quaternion.Euler(0f, 0f, -90f);
            case DiceFace.SIX:
                return Quaternion.Euler(180f, 0f, 0f);
            default:
                throw new System.Exception("Value Not Valid");
        }
    }

    private static GameObject GetSelected()
    {
        GameObject selected = Selection.activeObject as GameObject;
        if (selected == null || selected.GetType() != typeof(GameObject))
        {
            throw new System.Exception("No GameObject Selected");
        }
        return selected;
    }

}
