using Figo;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public enum AlignmentState:int
{
    Not_Stable,
    Concatenate,
    Not_Concatenate
}

[System.Serializable]
public class AlignmentData
{
    [System.Serializable]
    public struct ConcatenateInfo
    {
        public float[] pos;
        public float[] rot;
    }
    public List<ConcatenateInfo> data = new List<ConcatenateInfo>();

    public void Append(Vector3 pos, Quaternion rot)
    {
        var piece = new ConcatenateInfo()
        {
            pos = new float[] { pos.x, pos.y, pos.z },
            rot = new float[] { rot.w, rot.x, rot.y, rot.z }
        };
        data.Add(piece);
    }
}

public class PhysicalAlignment : MonoBehaviour
{
    public const float DeltaThreshold = 10e-5f;
    public const float DeltaRotThreshold = 10e-5f;
    public const float FinalRotThreshold = 25f;
    public const float CamDistance = 3f;

    [SerializeField]
    private Bounds m_Bounds;

    public Bounds Bounds
    {
        get
        {
            if (m_Bounds.extents.sqrMagnitude < 10e-5)
            {
                Mesh mesh = GetComponentInChildren<MeshFilter>().sharedMesh;
                mesh.RecalculateBounds();
                m_Bounds = mesh.bounds;
            }
            return m_Bounds;
        }
    }

    [SerializeField]
    private string m_Path;
    public string Path { get => m_Path; set => m_Path = value; }

    [SerializeField]
    private GameObject[] m_FaceObjects = new GameObject[6];

    [SerializeField]
    private float m_MaxDimension;

    public float MaxDimension
    {
        get
        {
            if (m_MaxDimension < 10e-5)
            {
                m_MaxDimension = Mathf.Max(Bounds.max.x, Bounds.max.y, Bounds.max.z);
            }
            return m_MaxDimension;
        }
    }

    private Matrix4x4[] m_Delta = new Matrix4x4[6];

    [SerializeField]
    private float[] m_DeltaPos = new float[6];
    [SerializeField]
    private float[] m_DeltaRot = new float[6];

    [SerializeField]
    private Matrix4x4[] m_DeltaOverall = new Matrix4x4[6];

    [SerializeField]
    private float[] m_FinalTranslation = new float[6];
    [SerializeField]
    private float[] m_FinalRotation = new float[6];

    [SerializeField]
    private AlignmentState[] m_AlignmentStates = new AlignmentState[6];

    [SerializeField]
    private Matrix4x4[] mats = new Matrix4x4[6];


    private static RenderTexture s_RenderTexture;
    public static RenderTexture RenderTexture
    {
        get
        {
            if (s_RenderTexture == null)
            {
                s_RenderTexture = new RenderTexture(800, 600, 1, RenderTextureFormat.ARGB32, 0);
            }
            return s_RenderTexture;
        }
    }

    private static Texture2D s_Texture2d;
    public static Texture2D Texture2D
    {
        get
        {
            if (s_Texture2d == null)
            {
                s_Texture2d = new Texture2D(800, 600, TextureFormat.RGB24, false);
            }
            return s_Texture2d;
        }
    }

    public GameObject GetGameObject(DiceFace face)
    {
        return m_FaceObjects[(int)face];
    }
    public (float deltaPos, float deltaRot) GetDelta(DiceFace face)
    {
        (var pos, var rot, _) = m_Delta[(int)face].GetTrs();
        rot.ToAngleAxis(out var angle, out _);
        return (pos.magnitude, angle);
    }

    public (float deltaPos, float deltaRot) GetDeltaOverall(DiceFace face)
    {
        (var pos, var rot, _) = m_DeltaOverall[(int)face].GetTrs();

        rot.ToAngleAxis(out var angle, out _);

        return (pos.magnitude, angle);
    }

    public (Vector3 deltaPos, Quaternion deltaRot) GetDeltaTrOverall(DiceFace face)
    {
        (var pos, var rot, _) = m_DeltaOverall[(int)face].GetTrs();
        return (pos, rot);
    }

    public AlignmentState GetAlignmentState(DiceFace face)
    {
        return m_AlignmentStates[(int)face];
    }


    public void MoveToPlane(Transform t, DiceFace diceFace)
    {
        var rot = diceFace.RollDice();
        Vector3 pos = t.position;
        var max = rot * Bounds.max;
        var min = rot * Bounds.min;

        t.position = new Vector3(pos.x, -Mathf.Min(max.y, min.y), pos.z);
        t.rotation = rot;
    }

    public void MoveToPlane(Transform t, Quaternion rot)
    {
        Vector3 pos = t.position;
        var max = rot * Bounds.max;
        var min = rot * Bounds.min;

        t.position = new Vector3(pos.x, -Mathf.Min(max.y, min.y), pos.z);
        t.rotation = rot;
    }

    public void SetupAlignment()
    {
        var meshObject = gameObject.GetComponentInChildren<MeshFilter>().gameObject;
        var mesh = gameObject.GetComponentInChildren<MeshFilter>().sharedMesh;

        var rigidbody = meshObject.GetOrAddComponent<Rigidbody>();
        var collider = meshObject.GetOrAddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
        collider.convex = true;
        var meshObjName = meshObject.name;
        meshObject.name = meshObjName + "_ONE";
        m_FaceObjects[(int)DiceFace.ONE] = meshObject;

        Vector3 currentPos = transform.position;
        meshObject.transform.position = currentPos;
        MoveToPlane(meshObject.transform, DiceFace.ONE);
        //mats[0] = meshObject.transform.localToWorldMatrix;
        m_DeltaOverall[0] = Matrix4x4.identity;

        for (int i = 0; i < 5; i++)
        {
            currentPos += Vector3.right * 6f * MaxDimension;
            var face = (DiceFace)i + 1;
            var obj = Object.Instantiate(meshObject, transform);
            obj.name = meshObjName + "_" + face;
            obj.transform.position = currentPos;
            MoveToPlane(obj.transform, face);
            m_FaceObjects[(int)face] = obj;
            //mats[i + 1] = obj.transform.localToWorldMatrix;
            m_DeltaOverall[i + 1] = Matrix4x4.identity;
        }
    }

    public void Start()
    {
        for (int i = 0; i < 6; i++)
        {
            var obj = GetGameObject((DiceFace)i);
            mats[i] = obj.transform.localToWorldMatrix;
            m_AlignmentStates[i] = AlignmentState.Not_Stable;
        }
    }

    public Vector3[] pre = new Vector3[6];
    public void FixedUpdate()
    {
        for (int i = 0; i < 6; i++)
        {
            var preM = mats[i] * m_DeltaOverall[i];
            (pre[i],_,_) = preM.GetTrs();
            var delta = Matrix4x4.Inverse(preM) * GetGameObject((DiceFace)i).transform.localToWorldMatrix;
            m_DeltaOverall[i] = Matrix4x4.Inverse(mats[i]) * GetGameObject((DiceFace)i).transform.localToWorldMatrix;
            m_Delta[i] = delta;
            (m_FinalTranslation[i], m_FinalRotation[i]) = GetDeltaOverall((DiceFace)i);
            (m_DeltaPos[i], m_DeltaRot[i]) = GetDelta((DiceFace)i);

            if (m_DeltaPos[i] < DeltaThreshold && m_DeltaRot[i] < DeltaRotThreshold)
            {
                if (m_FinalRotation[i] < FinalRotThreshold)
                {
                    m_AlignmentStates[i] = AlignmentState.Concatenate;
                }
                else
                {
                    m_AlignmentStates[i] = AlignmentState.Not_Concatenate;
                }
            }
            else
            {
                m_AlignmentStates[i] = AlignmentState.Not_Stable;
            }
        }
    }

    [RuntimeInitializeOnLoadMethod]
    public static void Init()
    {
        Physics.autoSimulation = true;
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < 6; i++)
        {
            switch (GetAlignmentState((DiceFace)i))
            {
                case AlignmentState.Not_Stable:
                    Gizmos.color = Color.yellow;
                    break;
                case AlignmentState.Not_Concatenate:
                    Gizmos.color = Color.red;
                    break;
                case AlignmentState.Concatenate:
                    Gizmos.color = Color.green;
                    break;
            }
            var go = GetGameObject((DiceFace)i);
            Mesh mesh = go.GetComponentInChildren<MeshFilter>().sharedMesh;
            Gizmos.matrix = go.transform.localToWorldMatrix;
            Gizmos.DrawWireMesh(mesh);
        }
    }

    public string GetFinalJson()
    {
        AlignmentData data = new AlignmentData();
        for (int i = 0; i < 6; i++)
        {
            var state = GetAlignmentState((DiceFace)i);
            if (state == AlignmentState.Concatenate)
            {
                (var pos, var rot) = GetDeltaTrOverall((DiceFace)i);
                pos = pos - Vector3.up*0.1f;
                data.Append(pos, rot);
            }
        }
        return JsonUtility.ToJson(data);
    }

    public string[] RenderCamera(Camera cam)
    {
        List<string> paths = new List<string>();
        for (int i = 0; i < 6; i++)
        {
            var state = GetAlignmentState((DiceFace)i);
            if (state == AlignmentState.Concatenate)
            {
                var go = GetGameObject((DiceFace)i);
                cam.transform.position = go.transform.position - cam.transform.forward * CamDistance * MaxDimension;
                cam.targetTexture = RenderTexture;
                cam.Render();
                RenderTexture.active = RenderTexture;
                Texture2D.ReadPixels(new Rect(0, 0, RenderTexture.width, RenderTexture.height), 0, 0);
                Texture2D.Apply();
                var bytes = Texture2D.EncodeToPNG();
                var path = System.IO.Path.ChangeExtension(Path,"") + $"_{(DiceFace)i}.png";
                File.WriteAllBytes(path, bytes);
                paths.Add(path);
            }
        }
        return paths.ToArray();
    }
}
