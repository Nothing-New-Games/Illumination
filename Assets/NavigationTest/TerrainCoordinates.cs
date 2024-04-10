#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq; // Required for Contains method

public class TerrainCoordinates : MonoBehaviour
{
    private Terrain terrain;

    [FoldoutGroup("Customize")]
    public float nodeSize = 1f; // Size of each terrain node (default value is 1)
    [FoldoutGroup("Customize")]
    public float yOffset = 0.1f; // Offset to move the tile gizmos up from the terrain surface

    [FoldoutGroup("Debug")]
    public bool DebugData = false;
    [FoldoutGroup("Debug"), ShowIf("@DebugData")]
    public bool DebugLog = false;
    [FoldoutGroup("Debug"), ShowIf("@DebugData && DebugLog")]
    public bool DebugLogPassableNodes = false;
    [FoldoutGroup("Debug"), ShowIf("@DebugData && DebugLog")]
    public bool DebugLogImpassableNodes = false;
    [FoldoutGroup("Debug"), ShowIf("@DebugData")]
    public bool DebugGizmos = false;
    [FoldoutGroup("Debug"), ShowIf("@DebugData && DebugGizmos")]
    public bool DebugPassableGizmos = false;
    [FoldoutGroup("Debug"), ShowIf("@DebugData && DebugGizmos")]
    public bool DebugImpassableGizmos = false;

    private TerrainCollider collider;

    Dictionary<float, List<float>> PassableTerrain = new Dictionary<float, List<float>>();
    Dictionary<float, List<float>> ImpassableTerrain = new Dictionary<float, List<float>>();

#if UNITY_EDITOR
    [MenuItem("Terrain Navigation/Check Terrain Colliders")]
    public static void CheckColliders()
    {
        Terrain terrain = FindObjectOfType<Terrain>();
        if (terrain == null)
        {
            Debug.LogWarning("No terrain found in the scene.");
            return;
        }

        TerrainCoordinates terrainCoordinates = terrain.GetComponent<TerrainCoordinates>();
        if (terrainCoordinates == null)
        {
            Debug.LogWarning("TerrainCoordinates script not found on terrain object.");
            return;
        }

        terrainCoordinates.GenerateTerrainNodes();
        terrainCoordinates.DebugTerrainNodes();
    }

    [MenuItem("Terrain Navigation/Clear Terrain Nodes")]
    public static void ClearTerrainNodes()
    {
        TerrainCoordinates[] terrainCoordinates = FindObjectsOfType<TerrainCoordinates>();
        foreach (TerrainCoordinates terrainCoord in terrainCoordinates)
        {
            terrainCoord.PassableTerrain.Clear();
            terrainCoord.ImpassableTerrain.Clear();
        }
    }
#endif

    void Start()
    {
        GenerateTerrainNodes();
        DebugTerrainNodes();
    }

    private void GenerateTerrainNodes()
    {
        if (terrain == null)
            terrain = GetComponent<Terrain>();

        if (collider == null)
            collider = terrain.GetComponent<TerrainCollider>();

        // Get the bounds of the terrain collider
        Bounds terrainBounds = collider.bounds;
        float offset = (2 + nodeSize / 2);
        float nodeScale = 1 / nodeSize;

        terrainBounds.size *= nodeScale;

        if (DebugLog)
            Debug.Log($"Bounds: {terrainBounds}\nSize: {terrainBounds.size}\nMin: {terrainBounds.min}\nMax: {terrainBounds.max}");

        float totalNodes = terrainBounds.size.x * terrainBounds.size.z / nodeSize;
        int nodesCreated = 0;

        if (DebugLog)
            Debug.Log($"Number of Nodes: {totalNodes}");

        //I guess fuck all logic, who tf needs it

        // Loop through each node within the terrain bounds
        for (float x = (terrainBounds.min.x / nodeSize) - offset; x < (terrainBounds.max.x / nodeSize) + offset; x += 1)
        {
            for (float z = (terrainBounds.min.z / nodeSize) - offset; z < (terrainBounds.max.z / nodeSize) + offset; z += 1)
            {
                Vector2 nodePosition = new Vector2(x, z);

                // Check if the node is within the terrain bounds
                if (terrainBounds.Contains(new Vector3(nodePosition.x, 0, nodePosition.y)))
                {
                    // Check for colliders intersecting with the node
                    Collider[] colliders = Physics.OverlapBox(new Vector3(x, 0, z), new Vector3(1, 0.1f, 1))
                                       .Where(col => col != collider)
                                       .ToArray();
                    
                    if (colliders.Length == 0)
                    {
                        AddPassableTerrainCoordinate(nodePosition, ref nodesCreated);
                        continue;
                    }
                    
                    bool hasPassableCollider = false;

                    foreach (Collider col in colliders)
                    {
                        ObjectTags objTags = col.GetComponent<ObjectTags>();
                        if (objTags != null && objTags.ContainsTag("Passable"))
                        {
                            hasPassableCollider = true;
                            break;
                        }
                    }

                    // Categorize the node as passable or impassable
                    if (hasPassableCollider)
                    {
                        AddPassableTerrainCoordinate(nodePosition, ref nodesCreated);
                        continue;
                    }
                    else
                    {
                        AddImpassableTerrainCoordinate(nodePosition, ref nodesCreated);
                        continue;
                    }
                }
            }
        }
    }

    private void AddPassableTerrainCoordinate(Vector2 terrainPos, ref int nodesCreated)
    {
        if (!PassableTerrain.ContainsKey(terrainPos.y))
            PassableTerrain.Add(terrainPos.y, new List<float>());
        PassableTerrain[terrainPos.y].Add(terrainPos.x);

        //if (DebugLog)
        //    Debug.Log($"Number of Nodes Created: {nodesCreated}");

        nodesCreated++;
    }

    private void AddImpassableTerrainCoordinate(Vector2 terrainPos, ref int nodesCreated)
    {
        if (!ImpassableTerrain.ContainsKey(terrainPos.y))
            ImpassableTerrain.Add(terrainPos.y, new List<float>());
        ImpassableTerrain[terrainPos.y].Add(terrainPos.x);

        //if (DebugLog)
        //    Debug.Log($"Number of Nodes Created: {nodesCreated}");

        nodesCreated++;
    }

    private void DebugTerrainNodes()
    {
        if (DebugLogPassableNodes && DebugData && DebugLog)
        {
            Debug.Log("Passable Terrain:");
            foreach (var entry in PassableTerrain)
            {
                Debug.Log($"Y: {entry.Key}, X: {string.Join(",", entry.Value)}");
            }
        }

        if (DebugLogImpassableNodes && DebugData && DebugLog)
        {
            Debug.Log("Impassable Terrain:");
            foreach (var entry in ImpassableTerrain)
            {
                Debug.Log($"Y: {entry.Key}, X: {string.Join(",", entry.Value)}");
            }
        }
    }

    internal protected List<Vector2> FindShortestPath(Vector3 currentPos, Vector3 destinationPos)
    {
        List<Vector2> ShortestPathCollection = new List<Vector2>();

        /*
        Do calculations for determining the possible shortest path here.
        No idea what this will look like yet! =D
        */

        // Example for verifying path is valid
        foreach (var entry in PassableTerrain)
        {
            foreach (float x in entry.Value)
            {
                if (x >= 0 && x < 100) // Assuming a grid size of 100x100
                    ShortestPathCollection.Add(new Vector2(x, entry.Key));
            }
        }

        return ShortestPathCollection;
    }

    private void OnDrawGizmos()
    {
        if (DebugPassableGizmos && DebugData && DebugGizmos)
        {
            foreach (var entry in PassableTerrain)
            {
                Gizmos.color = Color.green;
                foreach (float x in entry.Value)
                {
                    Vector3 nodeCenter = new Vector3(x * nodeSize, terrain.SampleHeight(new Vector3(x * nodeSize, 0, entry.Key * nodeSize)) + yOffset, entry.Key * nodeSize);
                    Gizmos.DrawWireCube(nodeCenter, new Vector3(nodeSize, 0.1f, nodeSize));
                }
            }
        }

        if (DebugImpassableGizmos && DebugData && DebugGizmos)
        {
            foreach (var entry in ImpassableTerrain)
            {
                Gizmos.color = Color.red;
                foreach (float x in entry.Value)
                {
                    Vector3 nodeCenter = new Vector3(x * nodeSize, terrain.SampleHeight(new Vector3(x * nodeSize, 0, entry.Key * nodeSize)) + yOffset, entry.Key * nodeSize);
                    Gizmos.DrawWireCube(nodeCenter, new Vector3(nodeSize, 0.1f, nodeSize));
                }
            }
        }
    }
}
