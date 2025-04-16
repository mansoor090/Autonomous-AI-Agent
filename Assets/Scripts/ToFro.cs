using UnityEngine;
using UnityEngine.InputSystem;

public class ToFro : MonoBehaviour
{
    public enum Axis { X, Y, Z }
    public Axis moveAxis = Axis.X;
    public float speed = 2f;
    public float distance = 3f;
    public float currentDistance = 0;
    private Vector3 startPos;

    public float increment = 1;

    int stepCounter = 0;
    public int MaxSteps = 10;

    void Start()
    {
        startPos = transform.position;   
        Reset();
    }
    
    void Update()
    {


        
        // decide direction 

        Vector3 direction = new Vector3();
        switch (moveAxis)
        {
            case Axis.X:
                direction = Vector3.right;
                break;
            case Axis.Y:
                direction = Vector3.up;
                break;
            case Axis.Z:
                direction = Vector3.forward;
                break;
        }
     
        transform.position = startPos + (direction * currentDistance);
    

        if(stepCounter % MaxSteps == 0){
            currentDistance += increment;
        }

        if(currentDistance == distance){
            increment = -1;
        }

        if(currentDistance == 0){
            increment = 1;
        }
        
        stepCounter++;
    
    }


    public void Reset()
    {
   
        transform.position = startPos;
    }
}
