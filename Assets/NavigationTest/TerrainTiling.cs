#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UIElements;

public class TerrainTiling : MonoBehaviour
{
    #region Variable Fields
    private Terrain terrain;
    [Required, ValidateInput("ValidateNodeIsPresent", "This must have a Node component attached!")]
    public GameObject NodePrefab;

    private bool ValidateNodeIsPresent() => NodePrefab != null && NodePrefab.GetComponent<Node>() != null;


    #region Customization
    [FoldoutGroup("Customize"), Tooltip("How far in should the tile nodes start and end inside the terrain boundaries.")]
    public float XZOffset = 0.5f;
    [FoldoutGroup("Customize"), Tooltip("How high above the terrain should the tile nodes be placed.")]
    public float YOffset = 0.1f;
    #endregion

    #region Debug
    [FoldoutGroup("Debug"), Tooltip("Toggle for all debug data for gizmos and debug logs.")]
    public bool DebugData = false;
    [FoldoutGroup("Debug"), ShowIf("@DebugData"), Tooltip("Toggle for debugging any data to the console related to terrain.")]
    public bool DebugLog = false;
    [FoldoutGroup("Debug"), ShowIf("@DebugData && DebugLog"), Tooltip("Toggle for debugging coordinates that were deemed as passable tile nodes.")]
    public bool DebugLogPassableNodes = false;
    [FoldoutGroup("Debug"), ShowIf("@DebugData && DebugLog"), Tooltip("Toggle for debugging coordinates that were deemed as impassable tile nodes.")]
    public bool DebugLogImpassableNodes = false;
    [FoldoutGroup("Debug"), ShowIf("@DebugData && DebugLog"), Tooltip("Toggle for debug logging how many nodes were created.")]
    public bool DebugLogNodeCount = false;
    [FoldoutGroup("Debug"), ShowIf("@DebugData && DebugLog && DebugLogNodeCount"), Tooltip("Toggle for debug logging how many nodes that were deemed as passable tile nodes.")]
    public bool DebugLogTerrainGenerationPassableCount = false;
    [FoldoutGroup("Debug"), ShowIf("@DebugData && DebugLog && DebugLogNodeCount"), Tooltip("Toggle for debug logging how many nodes that were deemed as impassable tile nodes.")]
    public bool DebugLogTerrainGenerationImpassableCount = false;
    [FoldoutGroup("Debug"), ShowIf("@DebugData"), Tooltip("Toggle for drawing any kind of gizmos related to terrain.")]
    public bool DebugGizmos = false;
    [FoldoutGroup("Debug"), ShowIf("@DebugData && DebugGizmos"), Tooltip("Shows gizmos for passable terrain tile nodes.")]
    public bool DebugPassableGizmos = false;
    [FoldoutGroup("Debug"), ShowIf("@DebugData && DebugGizmos"), Tooltip("Shows gizmos for impassable terrain tile nodes.")]
    public bool DebugImpassableGizmos = false;
    #endregion

    public TerrainCollider terrainCollider { get; private set; }
    [ReadOnly, ShowInInspector]
    Dictionary<float, List<Node>> Nodes = new();

    int _TotalNodesCreated = 0;
    int _PassableNodesCreated = 0;
    int _ImpassableNodesCreated = 0;
    #endregion

#if UNITY_EDITOR
    [MenuItem("Tile Nodes/Generate Nodes for All Terrain")]
    public static void GenerateNodes()
    {
        //Get all the terrain objects in the scene
        TerrainTiling[] terrainCoordinates = FindObjectsOfType<TerrainTiling>();
        //Loop through them all to see if they need nodes generated.
        foreach (TerrainTiling terrainCoord in terrainCoordinates)
        {
            //Skip any terrain that already has it's nodes generated.
            if (terrainCoord._TotalNodesCreated > 0) continue;

            terrainCoord.GenerateTerrainNodes();
            terrainCoord.DebugTerrainNodes();
        }
    }

    [MenuItem("Tile Nodes/Clear All Nodes")]
    public static void ClearTerrainNodes()
    {
        //Get all the terrain objects in the scene
        TerrainTiling[] terrainCoordinates = FindObjectsOfType<TerrainTiling>();
        //Loop through them all
        foreach (TerrainTiling terrainCoord in terrainCoordinates)
        {
            //And clear their data.
            terrainCoord.ClearData();
        }
    }

    [MenuItem("Tile Nodes/Update All Nodes")]
    public static void UpdateTerrainNodes()
    {
        //Get all the terrain objects in the scene
        TerrainTiling[] terrainCoordinates = FindObjectsOfType<TerrainTiling>();
        //Loop through them all to see if they need nodes updated
        foreach (TerrainTiling terrainCoord in terrainCoordinates)
        {
            //Skip any terrain that already has nodes generated.
            if (terrainCoord._TotalNodesCreated != 0) continue;

            //Finally, update the nodes for the terrain =)
            terrainCoord.UpdateNodesFromChildren();
        }
    }
#endif

    void Start()
    {
        GenerateTerrainNodes();
        DebugTerrainNodes();
    }

    [Button("Generate Nodes"), EnableIf("@_TotalNodesCreated == 0")]
    private void GenerateTerrainNodes()
    {
        if (terrain == null)
            terrain = GetComponent<Terrain>();

        if (terrainCollider == null)
            terrainCollider = terrain.GetComponent<TerrainCollider>();

        if (Nodes.Count == 0)
        {
            bool childrenFound = UpdateNodesFromChildren();

            if (childrenFound)
            {
                Debug.Log($"{_TotalNodesCreated} nodes retrieved from children!");
                return;
            }
        }

        // Get the bounds of the terrain collider
        Bounds terrainBounds = terrainCollider.bounds;

        if (DebugLog)
            Debug.Log($"Bounds: {terrainBounds}\nSize: {terrainBounds.size}\nMin: {terrainBounds.min}\nMax: {terrainBounds.max}");

        float totalNeededNodes = terrainBounds.size.x * terrainBounds.size.z;

        if (DebugLog && DebugLogNodeCount)
            Debug.Log($"Number of Nodes Required: {totalNeededNodes}");

        // Loop through each tile within the terrain bounds
        for (float x = terrainBounds.min.x - XZOffset; x < terrainBounds.max.x + XZOffset; x += 1)
        {
            for (float z = terrainBounds.min.z - XZOffset; z < terrainBounds.max.z + XZOffset; z += 1)
            {
                //Vector2 nodePosition = new Vector2(x, z)/* + new Vector2(terrain.transform.position.x, terrain.transform.position.z)*/;

                // Check if the node is within the terrain bounds
                if (terrainBounds.Contains(new Vector3(x, 0, z)))
                {
                    float y = terrain.SampleHeight(new Vector3(x, 0, z));
                    var newNode = Instantiate(NodePrefab, new Vector3(x, y + NodePrefab.transform.localScale.y / 2, z), NodePrefab.transform.rotation, transform).GetComponent<Node>();
                    newNode.CheckForColliders();
                    AddNewNode(z, newNode, ref Nodes);
                }
            }
        }

        _PassableNodesCreated = Nodes.SelectMany(keyValuePair => keyValuePair.Value).Count(node => node.IsPassable);
        _ImpassableNodesCreated = Nodes.SelectMany(keyValuePair => keyValuePair.Value).Count(node => !node.IsPassable);
        _TotalNodesCreated = _ImpassableNodesCreated + _PassableNodesCreated;

        if (DebugLog && DebugLogTerrainGenerationPassableCount && DebugLogNodeCount)
            Debug.Log($"Number of Passable Nodes Created: {_PassableNodesCreated}");

        if (DebugLog && DebugLogTerrainGenerationImpassableCount && DebugLogNodeCount)
            Debug.Log($"Number of Impassable Nodes Created: {_ImpassableNodesCreated}");

        if (DebugLog && DebugLogNodeCount)
            Debug.Log($"Total nodes created: {_TotalNodesCreated}");

        DebugTerrainNodes();
    }

    [Button("Clear Nodes")]
    private void ClearData()
    {
        if (DebugLog)
            Debug.Log($"Clearing nodes for {name}...");

        UpdateNodesFromChildren();

        foreach (var z in Nodes.Values)
            foreach (var node in z)
                DestroyImmediate(node.gameObject);

        Nodes.Clear();
        _TotalNodesCreated = 0;
        _PassableNodesCreated = 0;
        _ImpassableNodesCreated = 0;

        if (DebugLog)
            Debug.Log($"{name} has been cleared!");
    }


    [Button("Get Children Nodes")]
    private bool UpdateNodesFromChildren()
    {
        var children = GetComponentsInChildren<Node>();
        if (children.Length > 0)
        {
            foreach (var child in children)
            {
                if (!Nodes.ContainsKey(child.Z) || !Nodes[child.Z].Contains(child))
                    AddNewNode(child.Z, child, ref Nodes);
            }

            return true;
        }
        return false;
    }

    private void AddNewNode(float key, Node newNode, ref Dictionary<float, List<Node>> dictionary)
    {
        if (newNode == null)
        {
            Debug.LogError("Node not found!");
            return;
        }

        if (!dictionary.ContainsKey(key))
            dictionary.Add(key, new List<Node>());

        dictionary[key].Add(newNode);
    }

    private void DebugTerrainNodes()
    {

        if (DebugData && DebugLog)
        {
            var passableTerrainBuilder = new StringBuilder("Passable terrain:\n");
            var impassableTerrainBuilder = new StringBuilder("Impassable terrain:\n");

            Nodes.SelectMany(keyValuePair => keyValuePair.Value)
                 .Where(node => (DebugLogPassableNodes && node.IsPassable) ||
                                (DebugLogImpassableNodes && !node.IsPassable))
                 .ToList()
                 .ForEach(node =>
                 {
                     string nodeInfo = $"X: {node.X}, Z: {node.Z}";
                     if (node.IsPassable)
                     {
                         passableTerrainBuilder.AppendLine(nodeInfo);
                     }
                     else
                     {
                         impassableTerrainBuilder.AppendLine(nodeInfo);
                     }
                 });

            string passableTerrain = passableTerrainBuilder.ToString();
            string impassableTerrain = impassableTerrainBuilder.ToString();

            if (DebugLogPassableNodes)
                Debug.Log(passableTerrain);
            if (DebugLogImpassableNodes)
            Debug.Log(impassableTerrain);
        }
    }

    internal protected List<Node> FindShortestPathDijkstra(Node startingNode, Node targetNode)
    {
        List<Node> shortestPathCollection = new List<Node>();

        // Initialize a dictionary to store the distance from the start node to each node
        Dictionary<Node, float> distance = new Dictionary<Node, float>();
        // Initialize a dictionary to store the previous node in the shortest path
        Dictionary<Node, Node> previous = new Dictionary<Node, Node>();

        // Ensure start and goal nodes are valid
        if (startingNode == null || targetNode == null)
        {
            Debug.LogError("Start or goal node is null.");
            return shortestPathCollection;
        }

        // Initialize all distances to infinity and previous nodes to null
        foreach (var node in Nodes.Values.SelectMany(nodes => nodes))
        {
            distance[node] = Mathf.Infinity;
            previous[node] = null;
        }

        // The distance from the start node to itself is 0
        distance[startingNode] = 0;

        // Create a list to store nodes
        List<Node> nodesList = new List<Node>();
        nodesList.Add(startingNode);

        while (nodesList.Count > 0)
        {
            // Sort the list based on distance
            nodesList.Sort((a, b) => distance[a].CompareTo(distance[b]));
            Node currentNode = nodesList[0];
            nodesList.RemoveAt(0);

            // If the current node is the goal node, reconstruct the shortest path
            if (currentNode == targetNode)
            {
                while (previous[currentNode] != null)
                {
                    shortestPathCollection.Insert(0, currentNode);
                    currentNode = previous[currentNode];
                }
                shortestPathCollection.Insert(0, startingNode);
                break;
            }

            // Get neighbors of the current node
            List<Node> neighbors = GetNeighbors(currentNode);

            foreach (var neighbor in neighbors)
            {
                // Calculate the tentative distance from the start node to the neighbor
                float tentativeDistance = distance[currentNode] + Vector3.Distance(currentNode.transform.position, neighbor.transform.position);

                // If the tentative distance is less than the current distance to the neighbor, update it
                if (tentativeDistance < distance[neighbor])
                {
                    distance[neighbor] = tentativeDistance;
                    previous[neighbor] = currentNode;
                    // Add the neighbor to the list if it's not already there
                    if (!nodesList.Contains(neighbor) && (neighbor.IsPassable || neighbor == startingNode || neighbor == targetNode))
                    {
                        nodesList.Add(neighbor);
                    }
                }
            }
        }

        return shortestPathCollection;
    }

    internal protected List<Node> FindShortestPathAStar(Node startingNode, Node targetNode)
    {
        List<Node> shortestPathCollection = new List<Node>();

        // Initialize a dictionary to store the total estimated cost from the start node to each node
        Dictionary<Node, float> totalCost = new();
        // Initialize a dictionary to store the actual cost from the start node to each node
        Dictionary<Node, float> distance = new();
        // Initialize a dictionary to store the previous node in the shortest path
        Dictionary<Node, Node> previous = new();

        // Ensure start and goal nodes are valid
        if (startingNode == null || targetNode == null)
        {
            Debug.LogError("Start or goal node is null.");
            return shortestPathCollection;
        }

        // Initialize all distances to infinity and previous nodes to null
        foreach (var node in Nodes.Values.SelectMany(nodes => nodes))
        {
            totalCost[node] = Mathf.Infinity;
            distance[node] = Mathf.Infinity;
            previous[node] = null;
        }

        // The distance from the start node to itself is 0
        distance[startingNode] = 0;
        // Calculate the heuristic estimate of the cost from the start node to the target node
        float heuristicDistance = Vector3.Distance(startingNode.transform.position, targetNode.transform.position);

        // The total cost of the start node is the sum of actual cost and heuristic distance
        totalCost[startingNode] = heuristicDistance;

        // Create a list to store nodes
        List<Node> nodesList = new List<Node>();
        nodesList.Add(startingNode);

        while (nodesList.Count > 0)
        {
            // Sort the list based on total cost (actual cost + heuristic)
            nodesList.Sort((a, b) => totalCost[a].CompareTo(totalCost[b]));
            Node currentNode = nodesList[0];
            nodesList.RemoveAt(0);

            // If the current node is the goal node, reconstruct the shortest path
            if (currentNode == targetNode)
            {
                while (previous[currentNode] != null)
                {
                    shortestPathCollection.Insert(0, currentNode);
                    currentNode = previous[currentNode];
                }
                shortestPathCollection.Insert(0, startingNode);
                break;
            }

            // Get neighbors of the current node
            List<Node> neighbors = GetNeighbors(currentNode);

            foreach (var neighbor in neighbors)
            {
                // Check if neighbor is passable
                if (!neighbor.IsPassable)
                    continue;

                // Calculate the tentative actual cost from the start node to the neighbor
                float tentativeDistance = distance[currentNode] + Vector3.Distance(currentNode.transform.position, neighbor.transform.position);

                // If the tentative actual cost is less than the current actual cost to the neighbor, update it
                if (tentativeDistance < distance[neighbor])
                {
                    distance[neighbor] = tentativeDistance;
                    // Calculate the heuristic estimate of the cost from the neighbor to the target node
                    heuristicDistance = Vector3.Distance(neighbor.transform.position, targetNode.transform.position);
                    // Update the total cost of the neighbor (actual cost + heuristic)
                    totalCost[neighbor] = distance[neighbor] + heuristicDistance;
                    previous[neighbor] = currentNode;
                    // Add the neighbor to the list if it's not already there
                    if (!nodesList.Contains(neighbor))
                    {
                        nodesList.Add(neighbor);
                    }
                }
            }
        }

        return shortestPathCollection;
    }


    public Node? GetCurrentNode(Vector3 position) => 
        GetNodeList(Mathf.Round(position.z))?.First(node => node.X == Mathf.Round(position.x));

    public List<Node> GetNodeList(float Key)
    {
        if (Nodes.Count == 0) UpdateNodesFromChildren();


        if (Nodes.ContainsKey(Key))
            return Nodes[Key];
        else return null;
    }
    public bool GetNodeList(float Key, out List<Node> ZNodes)
    {
        if (Nodes.Count == 0) UpdateNodesFromChildren();


        ZNodes = GetNodeList(Key);

        return ZNodes != null;
    }

    [Button("Test for getting neighbors")]
    public List<Node> GetNeighbors(Node node)
    {
        List<Node> neighborNodes = new List<Node>();
        
        if (node == null) return neighborNodes;

        Node possibleNode;
        List<Node> ZNodes;

        for (var x = node.X -1; x < node.X +2; x++)
        {
            for (var z = node.Z -1; z < node.Z +2; z++)
            {
                if (x == node.X && z == node.Z)
                    continue;


                if (GetNodeList(z, out ZNodes))
                {
                    possibleNode = ZNodes.LastOrDefault(node => node.X == x);

                    if (possibleNode != null)
                        neighborNodes.Add(possibleNode);
                }
            }
        }

        return neighborNodes;
    }

    public NodeCluster CreateNodeCluster(Node trueTile)
    {
        List<Node> clusterNodes = new List<Node>();

        // Add the true tile to the cluster nodes list
        clusterNodes.Add(trueTile);

        // Get the neighbors of the true tile
        List<Node> trueTileNeighbors = GetNeighbors(trueTile);

        // Loop through the true tile neighbors
        foreach (Node neighbor in trueTileNeighbors)
        {
            // Check if the neighbor is contested by the same entity as the true tile
            if (neighbor.ContestedBy == trueTile.ContestedBy)
            {
                // Add the neighbor to the cluster nodes list
                clusterNodes.Add(neighbor);
            }
        }

        // Create a new node cluster with the true tile and its contested neighbors
        NodeCluster cluster = new NodeCluster(trueTile, clusterNodes);

        return cluster;
    }
}