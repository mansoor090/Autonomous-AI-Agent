using UnityEngine;

public class ToFroMotion : MonoBehaviour
{
    public enum Axis { X, Y, Z }
    public Axis moveAxis = Axis.X;
    public float speed = 2f;
    public float distance = 3f;
    private Vector3 startPos;
    private int direction = 1;
    
    void Start()
    {
        startPos = transform.position;
    }
    
    void Update()
    {
        float movement = Mathf.PingPong(Time.time * speed, distance * 2) - distance;
        Vector3 newPosition = startPos;
        
        switch (moveAxis)
        {
            case Axis.X:
                newPosition.x += movement;
                break;
            case Axis.Y:
                newPosition.y += movement;
                break;
            case Axis.Z:
                newPosition.z += movement;
                break;
        }
        
        transform.position = newPosition;
    }
}
