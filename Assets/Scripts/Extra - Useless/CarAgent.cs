using UnityEngine;
using AustinHarris.JsonRpc;


public class CarAgent : MonoBehaviour
{
    
    public class RPC: JsonRpcService
    {
        CarAgent agent;
        public RPC(CarAgent agent)
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

    private Vector3 startPositionCar;
    private Vector3 startPositionTarget;
    public CarControl carControl;


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
        simulation = GetComponent<Simulation>();
        rpc = new RPC(this);

        startPositionCar = this.transform.position;
        startPositionTarget = target.transform.position;

     }

    // Update is called once per frame
    RlResult Step(string action)
    {
        reward = 0;

        Vector2 direction = Vector2.zero;

        switch (action)
        {
            case "Drive":
                direction = new Vector2(1,0);
                break;
            case "Reverse":
                direction = new Vector2(-1, 0);
                break;
            case "DriveRight":
                direction = new Vector2(1, -1);
                break;
            case "DriveLeft":
                direction = new Vector2(1, 1);
                break;
            case "ReverseRight":
                direction = new Vector2(-1, -1);
                break;
            case "ReverseLeft":
                direction = new Vector2(-1, 1);
                break;
        }

        // Action Received Logic
        carControl.UpdateCar(direction.x, direction.y);

        //Vector3 newPosition = transform.position + direction;
        //newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        //newPosition.y = 1;
        //newPosition.z = Mathf.Clamp(newPosition.z, minY, maxY);
        //transform.position = newPosition;

    //    simulation.Simulate();
        step += 1;

        if(step >= 1000)
        {
            Debug.Log("Ending Episode: Timeout");
            finished = false;
            truncated = true;
        }

        return new RlResult(reward, finished, truncated, GetObservation());
    }


    public MyVector3 Reset()
    {
        transform.position = startPositionCar;
        transform.rotation = Quaternion.identity;
        target.transform.position = startPositionTarget;
        carControl.Reset();
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
        }
        if (other.gameObject.CompareTag("Respawn"))
        {
            Debug.Log("Lost");
            reward -= 1;
            truncated = true;
        }
    }
}
