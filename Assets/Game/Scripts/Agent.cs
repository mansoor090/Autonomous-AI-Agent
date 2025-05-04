using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AustinHarris.JsonRpc;
using System.Threading;


public class Agent : MonoBehaviour
{

    public class Observations
    {
        //public bool dead = false;
        public int[] myPosition;
        public int[] targetPos;
        public int[][] walkablePos;
        public int[][] hurdlesPositions;
    }

    public class RPC : JsonRpcService
    {
        Agent agent;
        public RPC(Agent agent)
        {
            this.agent = agent;
        }

        [JsonRpcMethod]
        int GetWalkableCount()
        {
            return agent.levelGenerator.GetAllSimpleNodes().Count;
        }
        
        [JsonRpcMethod]
        int GetHurdleCount()
        {
            return agent.levelGenerator.GetAllHurdlesNodes().Count;
        }
        
        [JsonRpcMethod]
        RlResult Step(string action)
        {
            return agent.Step(action);
        }

        [JsonRpcMethod]
        Observations Reset()
        {
            return agent.Reset();
        }
    }

    public class RlResult
    {
        public float reward;
        public bool finished;
        public bool truncate;
        public Observations obs;

        public RlResult(float reward, bool finished, bool truncate, Observations obs)
        {
            this.reward = reward;
            this.finished = finished;
            this.truncate = truncate;
            this.obs = obs;
        }

    }
    
    public NetManager netManager;
    public LevelGenerator levelGenerator;
    public UIHandler UIHandler;
    public DogController dog;
    RPC rpc;
    public GameObject target;
    Simulation simulation;
    float reward = 0;
    bool finished = false;
    bool truncated = false;
    int stepCount = 0;

    [SerializeField] int minX = 0;
    [SerializeField] int maxX = 0;
    [SerializeField] int minY = 0;
    [SerializeField] int maxY = 0;

    [SerializeField] private Vector3[] hurdles;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UIHandler.UpdatePort(netManager.ListenPort);
        simulation = GetComponent<Simulation>();
        rpc = new RPC(this);
        // assign hurdles
        hurdles = levelGenerator.GetAllHurdlesNodes().ToArray();

        var walkablePositions = levelGenerator.GetAllSimpleNodes().ToArray();
        obs.walkablePos = new int[walkablePositions.Length][];
        for (int i = 0; i < walkablePositions.Length; i++)
        {
            Vector3 pos = walkablePositions[i];
            obs.walkablePos[i] = new int[] { (int)pos.x, 0, (int)pos.z };
        }

        maxX = levelGenerator.levelDimension.dimension.x;
        maxY = levelGenerator.levelDimension.dimension.y;
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

        float prevDist = Vector3.Distance(transform.position, target.transform.position);
        
        Vector3 targetPos = transform.position + direction;
        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        targetPos.y = 1;
        targetPos.z = Mathf.Clamp(targetPos.z, minY, maxY);
        transform.position = targetPos;

        Vector3 posToCompare = new Vector3(transform.position.x, 0, transform.position.z);
        
        float newDist = Vector3.Distance(transform.position, target.transform.position);

        // Reward shaping
        reward = (prevDist - newDist) * 0.1f; // Positive if getting closer, negative if moving away

        
        if (Vector3.SqrMagnitude(transform.position - target.transform.position) < 0.1f)
        {
            reward = 1f;
            finished = true;
            UIHandler.UpdateSuccess(1);
        }
        if (levelGenerator.GetAllHurdlesNodes().Contains(posToCompare))
        {
            reward = -1f;
            truncated = true;
            UIHandler.UpdateFailure(1, false);
        }
        
        
      
        
        simulation.Simulate();
        stepCount += 1;

        UIHandler.UpdateSteps(1);
        if (stepCount >= 1000)
        {
            Debug.Log("Ending Episode: Timeout");
            finished = false;
            truncated = true;
            UIHandler.UpdateFailure(1, true);
        }

        return new RlResult(reward, finished, truncated, GetObservation(truncated));
    }


    IEnumerator MoveToPosition(Vector3 target, float duration, System.Action onComplete)
    {
        dog.hasReached = false;
        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(start, target, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        dog.hasReached = true;
        transform.position = target;
        onComplete?.Invoke();

    }

    public Observations Reset()
    {
        transform.position = Vector3.zero;

        transform.position = GetNewPosition();
        target.transform.position = GetNewPosition();

        finished = false;
        truncated = false;
        stepCount = 0;

        UIHandler.UpdateEpisodes(1);
        return GetObservation(truncated);
    }

    public Vector3 GetMinMax()
    {
        return new Vector3(Random.Range(minX, maxX + 1), 1, Random.Range(minY, maxY + 1));
    }
    
    Observations obs = new Observations();
    
    public Observations GetObservation(bool dead)
    {
        // obs.dead = dead;
        //
        obs.myPosition = new int[] {
            (int)transform.position.x, (int)0, (int)transform.position.z
        };

        obs.targetPos = new int[] {
            (int)target.transform.position.x,
            (int)0,
            (int)target.transform.position.z
        };

        obs.hurdlesPositions = new int[hurdles.Length][];
        for (int i = 0; i < hurdles.Length; i++)
        {
            Vector3 hurdleRelative = hurdles[i];
            obs.hurdlesPositions[i] = new int[] { (int)hurdleRelative.x, 0, (int)hurdleRelative.z };
        }
        
        return obs;
    }
    

    private bool IsPositionOccupied(Vector3 position)
    {

        bool isOccupied = false;

        foreach (Vector3 hurdle in hurdles)
        {
            if (hurdle.x == position.x && hurdle.z == position.z)
            {
                isOccupied = true;
            }
        }
        return isOccupied; // Position is free
    }

    private Vector3 GetNewPosition()
    {

        Vector3 newPosition = GetMinMax();

        // check if new position is occupied... else return newposition
        while (IsPositionOccupied(newPosition))
        {
            newPosition = GetMinMax();
        }
        return newPosition;

    }


}