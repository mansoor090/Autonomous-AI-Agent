using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public Vector3 offset;
    public float smoothSpeed = 0.125f;
    public float threshold = 0.5f;

    private void Start()
    {
        Camera getCamera = Camera.main;
        getCamera.depthTextureMode = DepthTextureMode.Depth;
    }

    void LateUpdate()
    {
        Vector3 desiredPosition = target.position + offset;
        float distance = Vector3.Distance(transform.position, desiredPosition);

        if (distance > threshold)
        {
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;
        }
    }

}
