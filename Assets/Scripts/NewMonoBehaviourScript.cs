using UnityEngine;
using AustinHarris.JsonRpc;

public class NewMonoBehaviourScript : MonoBehaviour
{

    public class RPC: JsonRpcService
    {
        NewMonoBehaviourScript agent;

        public RPC(NewMonoBehaviourScript agent)
        {
           this.agent = agent;
        }

        [JsonRpcMethod]
        public void Say(string message)
        {
            agent.print(message);
        }


    }


    RPC rPC;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rPC = new RPC(this);
    }



    // Update is called once per frame
    void Update()
    {
        
    }

    public void print(string msg){
            Debug.Log($"mansoor {msg}");

    }



}

