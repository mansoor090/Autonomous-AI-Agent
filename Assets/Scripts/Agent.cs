using UnityEngine;
using AustinHarris.JsonRpc;
using Unity.Hierarchy;


public class MyVector3
{
    public float x;
    public float y;
    public float z;


    public MyVector3(Vector3 v)
    {
        this.x = v.x;
        this.y = v.y;
        this.z = v.z;
    }

    public Vector3 AsVector3()
    {
        return new Vector3(x,y,z);
    }

}

public class Agent : MonoBehaviour
{
    
    public class RPC: JsonRpcService
    {
        Agent agent;
        public RPC(Agent agent)
        {
             this.agent = agent;
        }

        [JsonRpcMethod]
        void Say(string message)
        {
            Debug.Log($"New Message: {message} ");
        }

        [JsonRpcMethod]
        float GetHeight()
        {
            return agent.transform.position.y;
        }

        [JsonRpcMethod]
        MyVector3 GetPos()
        {
            return new MyVector3(agent.transform.position);
        }

        [JsonRpcMethod]
        void Translate(MyVector3 translate)
        {
            agent.transform.position += translate.AsVector3();
        }

        [JsonRpcMethod]
        RlResult Step(string action)
        {
            return agent.Step(action);
        }

        [JsonRpcMethod]
        MyVector3 Reset()
        {
            return agent.Reset();
        }
    }

    public class RlResult
    {
        public float reward;
        public bool finished;
        public MyVector3 obs;

        public RlResult(float reward, bool finished, MyVector3 obs)
        {
            this.reward = reward;
            this.finished = finished;
            this.obs = obs; 
        }


    }


    RPC rpc;
    public GameObject target;
    Simulation simulation;
    float reward = 0;
    bool finished = false;
    int step = 0;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        simulation = GetComponent<Simulation>();
        rpc = new RPC(this);

     }

    // Update is called once per frame
    RlResult Step(string action)
    {
        reward = 0;

        Vector3 direction = Vector3.zero;

        switch (action)
        {
            case "north":
                direction = Vector3.forward;
                break;
            case "south":
                direction = -Vector3.forward;
                break;
            case "west":
                direction = Vector3.left;
                break;
            case "east":
                direction = Vector3.right;
                break;
        }
        
        Vector3 newPosition = transform.position + ((direction * 10) * simulation.SimulationStepSize);
        newPosition.x = Mathf.Clamp(newPosition.x, -5f, 5f);
        newPosition.y = 0;
        newPosition.z = Mathf.Clamp(newPosition.z, -5f, 5f);
        transform.position = newPosition;

        simulation.Simulate();
        step += 1;

        if(step >= 1000)
        {
            Debug.Log("Ending Episode: Timeout");
            finished = true;
        }

        return new RlResult(reward, finished, GetObservation());
    }


    public MyVector3 Reset()
    {
        transform.position = Vector3.zero;

        target.transform.position = new Vector3(Random.Range(-5f,5f), 0, Random.Range(-5f, 5f));

        finished = false;
        step = 0;

        return GetObservation();
    }

    public MyVector3 GetObservation()
    {
        return new MyVector3(target.transform.position - transform.position);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Finish"))
        {
            Debug.Log("Win");
            reward += 1;
            finished = true;
        }
    }
}
