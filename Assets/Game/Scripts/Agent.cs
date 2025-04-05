using UnityEngine;
using AustinHarris.JsonRpc;
using UnityEngine.Events;


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
        public bool truncate;
        public MyVector3 obs;

        public RlResult(float reward, bool finished, bool truncate, MyVector3 obs)
        {
            this.reward = reward;
            this.finished = finished;
            this.truncate = truncate;
            this.obs = obs; 
        }

    }
    private UIHandler UIHandler;
    

    RPC rpc;
    public GameObject target;
    Simulation simulation;
    float reward = 0;
    bool finished = false;
    bool truncated = false;
    int step = 0;


    [SerializeField]int minX = 0;
    [SerializeField]int maxX = 0;
    [SerializeField]int minY = 0;
    [SerializeField]int maxY = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UIHandler = FindAnyObjectByType<UIHandler>();

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

        Vector3 newPosition = transform.position + direction;
        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        newPosition.y = 1;
        newPosition.z = Mathf.Clamp(newPosition.z, minY, maxY);
        transform.position = newPosition;

        simulation.Simulate();
        step += 1;
        // update UI
        UIHandler.UpdateSteps(step);
        UIHandler.UpdateEpisodes(1);

        if(step >= 1000)
        {
            Debug.Log("Ending Episode: Timeout");
            finished = false;
            truncated = true;
            UIHandler.UpdateFailure(1, true);
        }

        return new RlResult(reward, finished, truncated, GetObservation());
    }


    public MyVector3 Reset()
    {
        transform.position = Vector3.zero;

        transform.position = GetMinMax();
        target.transform.position = GetMinMax();

        while (transform.position == target.transform.position){
            target.transform.position = GetMinMax();
        }
      
        finished = false;
        truncated = false;
        step = 0;

        return GetObservation();
    }

    public Vector3 GetMinMax() {
        return new Vector3(Random.Range(minX, maxX + 1), 1, Random.Range(minY, maxY + 1));
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
            UIHandler.UpdateSuccess(1);
        }
        if (other.gameObject.CompareTag("Respawn"))
        {
            Debug.Log("Lost");
            reward -= 1;
            truncated = true;
            UIHandler.UpdateFailure(1, false);
        }
    }
}
