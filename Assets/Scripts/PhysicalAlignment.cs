using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class PhysicalAlignment : MonoBehaviour
{
    [SerializeField]
    Bounds? m_Bounds;

    public Bounds Bounds
    {
        get
        {
            if (!m_Bounds.HasValue)
            {
                Mesh mesh = GetComponentInChildren<MeshFilter>().sharedMesh;
                mesh.RecalculateBounds();
                m_Bounds = mesh.bounds;
            }
            return m_Bounds.Value;
        }
    }


    public void MoveToPlane(DiceFace diceFace)
    {
        var rot = diceFace.RollDice();
        Vector3 pos = transform.position;
        var max = rot * Bounds.max;
        var min = rot * Bounds.min;

        transform.position = new Vector3(pos.x, -Mathf.Min(max.y, min.y), pos.z);
        transform.rotation = rot;
    }

    public void MoveToPlane(Quaternion rot)
    {
        Vector3 pos = transform.position;
        var max = rot * Bounds.max;
        var min = rot * Bounds.min;

        transform.position = new Vector3(pos.x, -Mathf.Min(max.y, min.y), pos.z);
        transform.rotation = rot;
    }
}
