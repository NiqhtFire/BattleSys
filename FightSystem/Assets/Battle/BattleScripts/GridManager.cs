using System.Collections;
using System.Collections.Generic; 
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridManager : Manager<GridManager>
{  
    public GameObject[] terrainGrids;

    public Dictionary<string, int> terrainNameAndIndexes = new Dictionary<string, int>();

    private Graph[] graphs;

    public Graph[] Graphs => graphs;

    private int prevGraph;

    [SerializeField]
    private int currentGraph;
    public int CurrentGraph{
        get{
            return currentGraph;
        }
        set{
            prevGraph = currentGraph;
            currentGraph = value;
            if(graphs[currentGraph] == null){
                InitializeGraph(value);
            }
        }
    }

    protected Dictionary<Team, int> startPositionPerTeam;
    
    
    protected void Awake()
    {
        base.Awake();

        for(int i = 0; i < terrainGrids.Length; i++){
            terrainNameAndIndexes.Add(terrainGrids[i].name, i);
        }

        graphs = new Graph[terrainGrids.Length];

        CurrentGraph = 0;

        startPositionPerTeam = new Dictionary<Team, int>();
        startPositionPerTeam.Add(Team.Team1, 0);
        startPositionPerTeam.Add(Team.Team2, graphs[currentGraph].Nodes.Count -1);
    }

    public Node GetFreeNode(Team forTeam)
    {
        int startIndex = startPositionPerTeam[forTeam];
        int currentIndex = startIndex;

        while(graphs[currentGraph].Nodes[currentIndex].IsOccupied)
        {
            if(startIndex == 0)
            {
                currentIndex++;
                if (currentIndex == graphs[currentGraph].Nodes.Count)
                    return null;
            }
            else
            {
                currentIndex--;
                if (currentIndex == -1)
                    return null;
            }
            
        }
        return graphs[currentGraph].Nodes[currentIndex];
    }

    public List<Node> GetPath(Node from, Node to)
    {
        return graphs[currentGraph].GetShortestPath(from, to);
    }

    public List<Node> GetNodesCloseTo(Node to)
    {
        return graphs[currentGraph].Neighbors(to);
    }

    public Node GetNodeForTile(Tile t)
    {
        var allNodes = graphs[currentGraph].Nodes;

        for (int i = 0; i < allNodes.Count; i++)
        {
            if (t.transform.GetSiblingIndex() == allNodes[i].index)
            {
                return allNodes[i];
            }
        }

        return null;
    }

    private void InitializeGraph(int index)
    {
        var allTiles = terrainGrids[index].GetComponentsInChildren<Tile>().ToList();
        graphs[index] = new Graph();

        for (int i = 0; i < allTiles.Count; i++)
        {
            Vector3 place = allTiles[i].transform.position;
            graphs[index].AddNode(place);
        }

        var allNodes = graphs[index].Nodes;
        foreach (Node from in allNodes)
        {
            foreach (Node to in allNodes)
            {
                if (Vector3.Distance(from.worldPosition, to.worldPosition) < 1f && from != to)
                {
                    graphs[index].AddEdge(from, to);
                }
            }
        }
    }

    public int fromIndex = 0;
    public int toIndex = 0;

    private void OnDrawGizmos()
    {
        
        if (graphs[currentGraph] == null)
            return;

        var allEdges = graphs[currentGraph].Edges;
        if (allEdges == null)
            return;

        foreach(Edge e in allEdges)
        {
            Debug.DrawLine(e.from.worldPosition, e.to.worldPosition, Color.black, 100);
        }

        var allNodes = graphs[currentGraph].Nodes;
        if (allNodes == null)
            return;

        foreach (Node n in allNodes)
        {
            Gizmos.color = n.IsOccupied ? Color.red : Color.green;
            Gizmos.DrawSphere(n.worldPosition, 0.1f);
            
        }

        if (fromIndex >= allNodes.Count || toIndex >= allNodes.Count)
            return;

        List<Node> path = graphs[currentGraph].GetShortestPath(allNodes[fromIndex], allNodes[toIndex]);
        if (path.Count > 1)
        {
            for (int i = 1; i < path.Count; i++)
            {
                Debug.DrawLine(path[i - 1].worldPosition, path[i].worldPosition, Color.red, 10);
            }
        }
    }
}