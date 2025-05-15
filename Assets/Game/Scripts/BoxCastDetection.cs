using System;
using UnityEngine;
[ExecuteInEditMode]
public class BoxCastDetection : MonoBehaviour
{
    
    [Header("BoxCast Settings")]
    [Tooltip("Local-space center offset of the box")]
    public Vector3 boxCenterOffset = Vector3.zero;
    [Tooltip("Half-size of the box (X, Y, Z) in local space")]
    public Vector3 boxHalfExtents = new Vector3(0.5f, 0.5f, 1f);
    [Tooltip("Distance to cast the box forward")]
    public float castDistance = 1f;
    [Tooltip("Which layers to detect")]
    public LayerMask layerMask = ~0;
    public QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Runtime Info (read-only)")]
    [Tooltip("True if the sweep hit anything")]
    public bool[] isHurdle = new bool[4];
    public bool[] isWater = new bool[4];
    [Tooltip("Details of what was hit")]
    public RaycastHit hitInfo;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    public void ProcessBoxCast()
    {
        // 1) Compute world‐space parameters
        Vector3 origin      = transform.TransformPoint(boxCenterOffset);
        Quaternion orient   =  Quaternion.Euler(Vector3.forward);
        
        // 2) Perform the BoxCast
        Vector3 direction   = Vector3.forward;
        (isHurdle[0], isWater[0]) = PerformCast(origin, direction, orient);
        
        direction   = -Vector3.forward;
        (isHurdle[1], isWater[1]) = PerformCast(origin, direction, orient);

        direction   = Vector3.right;
        (isHurdle[2], isWater[2]) = PerformCast(origin, direction, orient);

        direction   = -Vector3.right;
        (isHurdle[3], isWater[3]) = PerformCast(origin, direction, orient);

        
    }

    (bool, bool) PerformCast(Vector3 origin, Vector3 direction, Quaternion orient)
    {
        bool isBlocked = Physics.BoxCast(
            origin,
            boxHalfExtents,
            direction,
            out hitInfo,
            orient,
            castDistance,
            layerMask,
            queryTriggerInteraction
        );

        if (isBlocked)
        {
            bool hurdle = false, water = false;
            if (hitInfo.collider.CompareTag("Water"))
            {
                water = true;
            }
        
            if (hitInfo.collider.CompareTag("Hurdle"))
            {
                hurdle = true;
            }

            return (hurdle, water);
        }

        
        return (false, false);
    }
    
    
    void OnDrawGizmos()
    {
        ProjectBoxCast(Vector3.forward, 0);
        ProjectBoxCast(-Vector3.forward, 1);
        ProjectBoxCast(Vector3.right,2);
        ProjectBoxCast(Vector3.left,3);
    }

    void ProjectBoxCast(Vector3 Direction, int index)
    {
        Vector3 origin      = transform.TransformPoint(boxCenterOffset);
        Quaternion orient   =  Quaternion.Euler(Vector3.forward);
        Vector3 direction   = Direction;

        // 3) Choose color: green if clear, red if blocked
        Gizmos.color = (isHurdle[index] || isWater[index]) ? Color.red : Color.green;

        // 4) Save current matrix
        Matrix4x4 oldMat = Gizmos.matrix;

        // 5) Draw starting box
        Gizmos.matrix = Matrix4x4.TRS(origin, orient, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boxHalfExtents * 2f);

        // 6) Draw sweep line
        Vector3 endCenter = origin + direction * castDistance;
        Gizmos.matrix = oldMat;
        Gizmos.DrawLine(origin, endCenter);

        // 7) Draw end box
        Gizmos.matrix = Matrix4x4.TRS(endCenter, orient, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boxHalfExtents * 2f);

        // 8) Restore matrix
        Gizmos.matrix = oldMat;    
    }
  
}
