using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIMovement : MonoBehaviour
{

    public Agent agent;

    public Button forward;
    public Button backward;
    public Button right;
    public Button left;
    
    public Button J_forward;
    public Button J_backward;
    public Button J_right;
    public Button J_left;
    
    
    public Button Reset;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        forward.onClick.AddListener(() =>
        {
            string action = "north";
            agent.Step(action);
        });
        
        backward.onClick.AddListener(() =>
        {
            string action = "south";
            agent.Step(action);
        });
        
        left.onClick.AddListener(() =>
        {
            string action = "west";
            agent.Step(action);
        });
        
        right.onClick.AddListener(() =>
        {
            string action = "east";
            agent.Step(action);
        });
        
        J_forward.onClick.AddListener(() =>
        {
            string action = "jump_north";
            agent.Step(action);
        });
        
        J_backward.onClick.AddListener(() =>
        {
            string action = "jump_south";
            agent.Step(action);
        });
        
        J_left.onClick.AddListener(() =>
        {
            string action = "jump_west";
            agent.Step(action);
        });
        
        J_right.onClick.AddListener(() =>
        {
            string action = "jump_east";
            agent.Step(action);
        });
        
        Reset.onClick.AddListener(() =>
        {
            agent.Reset();
        });
        
    }


}
