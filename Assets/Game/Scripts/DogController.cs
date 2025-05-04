using System;
using UnityEngine;

public class DogController : MonoBehaviour
{
    
    public Animator animator;
    public bool hasReached = false;

    private float moveSpeed = 10;

    private void Update()
    {

        if (hasReached)
        {
            animator.SetFloat("Movement_f", 0f);
        }
        else
        {
            animator.SetFloat("Movement_f", 0.5f);
        }
    }
    

    
    
}
