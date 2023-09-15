using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Figo;
using System.IO;
using UnityEditor.SceneManagement;
using System.Linq;
using Codice.CM.Client.Differences.Merge;

public class SetupObject:AssetPostprocessor
{
    public const int SimulationFPS = 120;
    public const float SimTime = 2f;
    public static string OBJ_ASSET_PATH => Path.Combine("Assets", "Res","Decimated");
    public const string SCENE_PATH = "Assets/Scenes/SampleScene.unity";

    void OnPreprocessModel()
    {
        ModelImporter modelImporter = assetImporter as ModelImporter;
        modelImporter.useFileScale = false;
        modelImporter.globalScale = 0.01f;
    }

    [MenuItem("Rosita/Physical Alignment/Executing_Process")]
    public static void Execute_Main()
    {
        EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
#if UNITY_EDITOR_WIN
        var sep = ';';
#elif UNITY_EDITOR_LINUX
        var sep = ':';
#else
        throw new System.Exception("Platform Not Valid");
#endif
        //if(Application.platform)
        var args = System.Environment.GetEnvironmentVariable("ROSITA_OBJS");
        List<string> objs;
        if (!string.IsNullOrEmpty(args))
        {
            objs = args.Split(sep).ToList();
        }
        else
        {
            if (Application.isBatchMode)
            {
                return;
            }
            objs = new List<string>();
            var path = FigoEditorUtility.OpenFolderPanel("Load Obj", "", "");
            foreach (var o in FigoPath.WalkThrough(path, true))
            {
                var p = Path.Combine(path, o);
                if (p.EndsWith(".obj"))
                {
                    objs.Add(Path.GetFullPath(p).ForwardSlash());
                }
            }
        }    
        ClearResPath();
        var assets = ImportObj(objs.Where(p => p.EndsWith(".obj")).ToArray());

        var gos = InstantiateObjs(assets);
        Vector3 currentPos = Vector3.zero;

        foreach (var go in gos)
        {
            Debug.Log(go);
            var physicalAlignment = SetupAlignment(go);
            currentPos += new Vector3(0f, 0f, physicalAlignment.MaxDimension) * 3f;
            physicalAlignment.transform.position = currentPos;
            currentPos += new Vector3(0f, 0f, physicalAlignment.MaxDimension) * 3f;
        }

        var phyAligns = gos.Select(x => x.GetComponent<PhysicalAlignment>());

        EditorPlay(phyAligns.ToArray());
        DumpJson(phyAligns.ToArray());
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
            Object.DestroyImmediate(o.gameObject);
        }

        List<PhysicalAlignment> phyAligns = new List<PhysicalAlignment>();

        var assets = FigoAsset.FindAssets<GameObject>("t:GameObject", "Assets/Res/Decimated");
        assets = assets.Where(a=>a.GetComponent<PhysicalAlignment>()!=null).ToArray();
        var gos = InstantiateObjs(assets);
        Vector3 currentPos = Vector3.zero;
        foreach (var go in gos)
        {
            var physicalAlignment = SetupAlignment(go);
            currentPos += new Vector3(0f, 0f, physicalAlignment.MaxDimension)*3f;
            physicalAlignment.transform.position = currentPos;
            currentPos += new Vector3(0f, 0f, physicalAlignment.MaxDimension)*3f;
            phyAligns.Add(physicalAlignment);
        }
    }

    [MenuItem("Rosita/Physical Alignment/Reload Scene")]
    public static void Menu_ReloadScene()
    {
        EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
    }

    [MenuItem("GameObject/Rosita/Physical Alignment/Move To Plane")]
    public static void Menu_MoveToPlane()
    {
        GetSelected().GetOrAddComponent<PhysicalAlignment>().SetupAlignment();
    }

    private static DiceFace diceFace = DiceFace.ONE;
    [MenuItem("GameObject/Rosita/Physical Alignment/Roll Dice")]
    public static void Menu_RollDice()
    {
        var selected = GetSelected();
        diceFace = (DiceFace)(((int)diceFace+1)%6);
        var rot = diceFace.RollDice();
        selected.transform.rotation = rot;
        GetSelected().GetOrAddComponent<PhysicalAlignment>().SetupAlignment();
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
        var phyAligns = Object.FindObjectsOfType<PhysicalAlignment>();
        EditorPlay(phyAligns);
    }

    public static void EditorPlay(params PhysicalAlignment[] phyAligns)
    {
        Physics.autoSimulation = false;
        var step = 1f / SimulationFPS;
        foreach (var pa in phyAligns)
        {
            pa.Start();
        }
        for (int j = 0; j < SimTime * SimulationFPS; j++)
        {
            foreach (var pa in phyAligns)
            {
                pa.FixedUpdate();
            }
            Physics.Simulate(step);
            foreach (var pa in phyAligns)
            {
                for (int i = 0; i < 6; i++)
                {
                    (var pos, var rot) = pa.GetDelta((DiceFace)i);
                }
            }
        }
    }


    public static PhysicalAlignment SetupAlignment(GameObject go)
    {
        //var rigidbody = go.GetOrAddComponent<Rigidbody>();
        //var collider = go.GetOrAddComponent<MeshCollider>();
        //var mesh = go.GetComponentInChildren<MeshFilter>().sharedMesh;
        //collider.sharedMesh = mesh;
        //collider.convex = true;

        var physicalAlignment = go.GetOrAddComponent<PhysicalAlignment>();
        physicalAlignment.SetupAlignment();
        return physicalAlignment;
    }

    public static GameObject[] ImportObj(params string[] path)
    {
        Debug.Log(ApplicationPath.projectPath);

        string[] assetPath = new string[path.Length];

        for (int i = 0; i < path.Length; i++)
        {
            var p = path[i];
            var ps = p.Split('/');
            var baseName = ps[ps.Length-3] + ".obj";
            var target = Path.Combine(ApplicationPath.projectPath, OBJ_ASSET_PATH, baseName);
            File.Copy(p, target);
            assetPath[i] = Path.Combine(OBJ_ASSET_PATH, baseName);
        }
        List<GameObject> result = new List<GameObject>();

        for (int i = 0; i < assetPath.Length; i++)
        {
            var p = assetPath[i];
            AssetDatabase.ImportAsset(p);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            var newPath = p.Substring(0, p.Length - ".obj".Length)+".prefab";
            var newGo = Object.Instantiate(go);
            newGo.name = go.name;
            var comp = newGo.GetOrAddComponent<PhysicalAlignment>();
            comp.Path = path[i];
            go = PrefabUtility.SaveAsPrefabAsset(newGo, newPath);
            Object.DestroyImmediate(newGo);
            result.Add(go);
        }
        AssetDatabase.Refresh();
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

    public static PhysicalAlignment[] InstantiateObjs(params PhysicalAlignment[] assets)
    {
        PhysicalAlignment[] instantiated = new PhysicalAlignment[assets.Length];
        for (int i = 0; i < assets.Length; i++)
        {
            instantiated[i] = Object.Instantiate(assets[i].gameObject).GetComponent<PhysicalAlignment>();
            instantiated[i].gameObject.name = assets[i].gameObject.name;
        }
        return instantiated;
    }


    public static void DumpJson(params PhysicalAlignment[] phyAligns)
    {
        foreach (var pa in phyAligns)
        {
            var result = pa.GetFinalJson();
            var targetP = Path.ChangeExtension(pa.Path, "json");
            File.WriteAllText(targetP, result);
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
