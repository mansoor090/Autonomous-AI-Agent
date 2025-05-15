using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AustinHarris.JsonRpc;
using System.Threading;
using Random = UnityEngine.Random;


public class Agent : MonoBehaviour
{

    public class Observations
    {
        //public bool dead = false;
        public int[] myPosition;
        public int[] targetPos;
        public bool[] hurdleBools;
        public bool[] waterBools;
        // public int[][] walkablePos;
        // public int[][] hurdlesPositions;

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
    [SerializeField] LayerMask mask;
    public NetManager netManager;
    public LevelGenerator levelGenerator;
    public UIHandler UIHandler;
    public DogController dog;
    private BoxCastDetection boxCastDetector;
  
    
    RPC rpc;
    public GameObject target;
    Simulation simulation;
    float reward = 0;
    bool finished = false;
    bool truncated = false;
    int stepCount = 0;


    [SerializeField] private PathFinder pathFinder;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        boxCastDetector = GetComponent<BoxCastDetection>();
        UIHandler.UpdatePort(netManager.ListenPort);
        simulation = GetComponent<Simulation>();
        rpc = new RPC(this);
     
       
    }

    // Update is called once per frame
    public RlResult Step(string action)
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
            case "jump_north":
                direction = Vector3.forward * 2;
                break;
            case "jump_south":
                direction = -Vector3.forward * 2;
                break;
            case "jump_west":
                direction = Vector3.left * 2;
                break;
            case "jump_east":
                direction = Vector3.right * 2;
                break;
        }
        
        // check if i am going to make a valid move or not. 
        bool isJump = action.StartsWith("jump");
        Vector3 currentPos = transform.position;
        Vector3 checkPos = currentPos + (direction/2f);
        bool isLake = levelGenerator.GetAllWaterNodes()
            .Any(p => p == new Vector3(checkPos.x, 0, checkPos.z));

        if (isJump && !isLake)
        {
            // invalid movement
            reward = -1; // or no movement
            finished = false;
            truncated = true;
            UIHandler.UpdateFailure("Invalid Jump");
            return new RlResult(reward, finished, truncated, GetObservation());
        }
        
        pathFinder.UpdateRender();
        int prevPathLen = pathFinder.pathLength;
        
        
        
        transform.LookAt(transform.position + direction);
        Vector3 nextPos = transform.position + direction;
        nextPos.x = Mathf.Clamp(nextPos.x, 0, levelGenerator.levelDimension.dimension.x);
        nextPos.y = 0;
        nextPos.z = Mathf.Clamp(nextPos.z, 0, levelGenerator.levelDimension.dimension.y);
        transform.position = nextPos;
        
        
        
        pathFinder.UpdateRender();
        int newPathLen = pathFinder.pathLength;

        // Reward shaping
        if (newPathLen < prevPathLen)
        {
            reward = 0.1f;
        }
        else if (newPathLen >= prevPathLen)
        {
            reward = -0.1f;
        }

        simulation.Simulate();
        stepCount += 1;

        // CHECK IF THE DOG HAS REACHED THE TARGET
        if (Vector3.SqrMagnitude(transform.position - target.transform.position) < 0.1f)
        {
            reward = 1f;
            finished = true;
            UIHandler.UpdateSuccess(1);
        }

        // CHECK IF THE DOG HAS HIT A HURDLE
        Vector3 posToCompare = new Vector3(transform.position.x, 0, transform.position.z);
        if (levelGenerator.GetAllHurdleAndWaterNodes().Contains(posToCompare))
        {
            reward = -1f;
            truncated = true;
            UIHandler.UpdateFailure("Touched with Hurdle or Water");
            UIHandler.UpdateFailure(1, false);
        }
        
        UIHandler.UpdateSteps(1);
        if (stepCount >= 1000)
        {
            Debug.Log("Ending Episode: Timeout");
            finished = false;
            truncated = true;
            UIHandler.UpdateFailure(1, true);
            UIHandler.UpdateFailure("Too many steps taken");
        }

        return new RlResult(reward, finished, truncated, GetObservation());
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

        (transform.position, target.transform.position) = GetNewPosition(); 
        finished = false;
        truncated = false;
        stepCount = 0;

        UIHandler.UpdateEpisodes(1);
        return GetObservation();
    }
    
    public Observations GetObservation()
    {
        Observations obs = new Observations();
        obs.myPosition = new int[] {
            (int)transform.position.x, (int)0, (int)transform.position.z
        };

        obs.targetPos = new int[] {
            (int)target.transform.position.x,
            (int)0,
            (int)target.transform.position.z
        };
        
        boxCastDetector.ProcessBoxCast();
        
        obs.hurdleBools = boxCastDetector.isHurdle;
        obs.waterBools = boxCastDetector.isWater;
        
        return obs;
    }

    (Vector3, Vector3) GetNewPosition()
    {
        Vector3 firstPos;
        Vector3 secondPos;
        
        List<Vector3> availablePos = levelGenerator.GetAllSimpleNodes();
        int random = Random.Range(0, availablePos.Count);
        firstPos = availablePos[random];
        availablePos.RemoveAt(random);

        random = Random.Range(0, availablePos.Count);
        secondPos = availablePos[random];
        availablePos.RemoveAt(random);
        
        return (firstPos, secondPos);
    }



}