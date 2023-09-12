using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Figo;
using System.IO;
public class SetupObject:AssetPostprocessor
{
    public static string OBJ_ASSET_PATH => Path.Combine("Assets", "Res","Decimated");


    void OnPreprocessModel()
    {
        ModelImporter modelImporter = assetImporter as ModelImporter;
        modelImporter.useFileScale = false;
        modelImporter.globalScale = 0.01f;
    }

    [MenuItem("Rosita/Physical Alignment/Import Obj")]
    public static void Menu_ImportObj()
    {
        var path = FigoEditorUtility.OpenFolderPanel("Load Obj", "", "");
        ClearResPath();
        foreach (var o in FigoPath.WalkThrough(path, true))
        {
            var p = Path.Combine(path, o);
            if (p.EndsWith(".obj"))
            {
                ImportObj(p);
            }
        }
    }

    [MenuItem("Rosita/Physical Alignment/Instantiate Objs")]
    public static void Menu_InstantiateObj()
    {
        var os = Object.FindObjectsOfType<PhysicalAlignment>();
        foreach (var o in os)
        {
            Object.DestroyImmediate(o);
        }

        var assets = FigoAsset.FindAssets<GameObject>("t:GameObject", "Assets/Res");
        var gos = InstantiateObjs(assets);
        foreach (var go in gos)
        {
            SetupAlignment(go);
        }
    }

    [MenuItem("GameObject/Rosita/Physical Alignment/Move To Plane")]
    public static void Menu_MoveToPlane()
    {
        GetSelected().GetOrAddComponent<PhysicalAlignment>().MoveToPlane(Quaternion.identity);
    }

    private static DiceFace diceFace = DiceFace.ONE;
    [MenuItem("GameObject/Rosita/Physical Alignment/Roll Dice")]
    public static void Menu_RollDice()
    {
        var selected = GetSelected();
        diceFace = (DiceFace)(((int)diceFace+1)%6);
        var rot = diceFace.RollDice();
        selected.transform.rotation = rot;
        GetSelected().GetOrAddComponent<PhysicalAlignment>().MoveToPlane(rot);
    }

    [MenuItem("GameObject/Rosita/Physical Alignment/Setup Rigidbody")]
    public static void Menu_SetupRigidbody()
    {
        var selected = GetSelected();
        SetupAlignment(selected);
    }

    [MenuItem("Rosita/Physical Alignment/Play")]
    public static void Menu_Play()
    {
        EditorApplication.EnterPlaymode();
    }



    public static (PhysicalAlignment pa, Rigidbody rb, MeshCollider mc) SetupAlignment(GameObject go)
    {
        var rigidbody = go.GetOrAddComponent<Rigidbody>();
        var collider = go.GetOrAddComponent<MeshCollider>();
        var mesh = go.GetComponentInChildren<MeshFilter>().sharedMesh;
        collider.sharedMesh = mesh;
        collider.convex = true;
        var physicalAlignment = go.GetOrAddComponent<PhysicalAlignment>();
        return (physicalAlignment, rigidbody, collider);
    }

    public static GameObject[] ImportObj(params string[] path)
    {
        Debug.Log(ApplicationPath.projectPath);

        for (int i = 0; i < path.Length; i++)
        {
            var p = path[i];
            var ps = p.Split('/');
            var baseName = ps[ps.Length-3] + ".obj";
            var target = Path.Combine(ApplicationPath.projectPath, OBJ_ASSET_PATH, baseName);
            File.Copy(p, target);
            path[i] = Path.Combine(OBJ_ASSET_PATH, baseName);
        }
        List<GameObject> result = new List<GameObject>();
        foreach (var p in path)
        {
            AssetDatabase.ImportAsset(p);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            result.Add(go);
        }
        return result.ToArray();
    }

    public static void ClearResPath()
    {
        DirectoryInfo d = new DirectoryInfo(Path.Combine(ApplicationPath.projectPath, OBJ_ASSET_PATH));
        foreach (var f in d.EnumerateFiles())
        {
            File.Delete(f.FullName);
        }
        AssetDatabase.Refresh();
    }

    public static GameObject[] InstantiateObjs(params GameObject[] assets)
    {
        GameObject[] instantiated = new GameObject[assets.Length];
        for (int i = 0; i < assets.Length; i++)
        {
            instantiated[i] = Object.Instantiate(assets[i]);
            instantiated[i].name = assets[i].name;
        }
        return instantiated;
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
