using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class PathFinder : MonoBehaviour
{


    public LevelGenerator levelData;
    public LineRenderer lineRenderer;
    public Agent agent;
    
    public int pathLength = 0;

    [ContextMenu("UpdateRender")]
    public void UpdateRender()
    {
        var path = FindShortestPath(agent.transform.position, agent.target.transform.position);
        lineRenderer.positionCount = path.Count;
        lineRenderer.SetPositions(path.ToArray());
        pathLength = path.Count;
    }

    List<Vector3> GetNeighbors(Vector3 node, List<Vector3> allNodes)
    {
        List<Vector3> neighbors = new List<Vector3>();
        Vector3[] directions = new Vector3[]
        {
        Vector3.right,
        Vector3.left,
        Vector3.forward,
        Vector3.back
        };

        foreach (var dir in directions)
        {
            Vector3 neighborPos = node + dir;
            if (allNodes.Contains(neighborPos))
            {
                neighbors.Add(neighborPos);
            }
        }

        return neighbors;
    }

    public List<Vector3> FindShortestPath(Vector3 startPos, Vector3 endPos)
    {
        // 1. Collect only Simple prefabs
        List<Vector3> simpleNodes = levelData.GetAllSimpleAndWaterNodes();
        
        endPos.y = 0;
        startPos.y = 0;

        // 2. Perform BFS or Dijkstra
        Dictionary<Vector3, Vector3> cameFrom = new Dictionary<Vector3, Vector3>();
        Queue<Vector3> frontier = new Queue<Vector3>();
        frontier.Enqueue(startPos);
        cameFrom[startPos] = startPos;

        while (frontier.Count > 0)
        {   
            
            Vector3 current = frontier.Dequeue();

            if (current == endPos)
                break;

            foreach (Vector3 neighbor in GetNeighbors(current, simpleNodes))
            {
                if (!cameFrom.ContainsKey(neighbor))
                {
                    frontier.Enqueue(neighbor);
                    cameFrom[neighbor] = current;
                }
            }
        }
        // 3. Reconstruct Path
        List<Vector3> path = new List<Vector3>();
        if (!cameFrom.ContainsKey(endPos))
        {
            return path; // no path found
        }

        Vector3 curr = endPos;
        while (curr != startPos)
        {
            path.Add(curr);
            curr = cameFrom[curr];
        }
        path.Add(startPos);
        path.Reverse();
        // Debug.Log("Path Generated");

        return path;
    }


}
