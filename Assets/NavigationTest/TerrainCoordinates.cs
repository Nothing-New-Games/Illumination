#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using System;

public class TerrainCoordinates : MonoBehaviour
{
    #region Variable Fields
    private Terrain terrain;

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

    #region Timer Options
    [FoldoutGroup("Rescanning Options"), Tooltip("How often will rescans of the terrain be performed (in seconds)?")]
    public float TimeBetweenChecks = 10f;
    [FoldoutGroup("Rescanning Options"), Tooltip("New scanns will not be performed outside of runtime.")]
    public bool PerformRescans = true;
    private float _TimeElapsed = 0f;
    #endregion


    private TerrainCollider terrainCollider;

    Dictionary<float, List<float>> PassableTerrain = new Dictionary<float, List<float>>();
    Dictionary<float, List<float>> ImpassableTerrain = new Dictionary<float, List<float>>();

    int _TotalNodesCreated = 0;
    #endregion

#if UNITY_EDITOR
    [MenuItem("Tile Nodes/Generate Nodes for All Terrain")]
    public static void GenerateNodes()
    {
        //Get all the terrain objects in the scene
        TerrainCoordinates[] terrainCoordinates = FindObjectsOfType<TerrainCoordinates>();
        //Loop through them all to see if they need nodes generated.
        foreach (TerrainCoordinates terrainCoord in terrainCoordinates)
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
        TerrainCoordinates[] terrainCoordinates = FindObjectsOfType<TerrainCoordinates>();
        //Loop through them all
        foreach (TerrainCoordinates terrainCoord in terrainCoordinates)
        {
            //And clear their data.
            terrainCoord.ClearData();
        }
    }

    [MenuItem("Tile Nodes/Update All Nodes")]
    public static void UpdateTerrainNodes()
    {
        //Get all the terrain objects in the scene
        TerrainCoordinates[] terrainCoordinates = FindObjectsOfType<TerrainCoordinates>();
        //Loop through them all to see if they need nodes updated
        foreach (TerrainCoordinates terrainCoord in terrainCoordinates)
        {
            //Skip any terrain that does not have any nodes generated.
            if (terrainCoord._TotalNodesCreated == 0) continue;

            //Finally, update the nodes for the terrain =)
            terrainCoord.UpdateData();
        }
    }
#endif

    void Start()
    {
        GenerateTerrainNodes();
        DebugTerrainNodes();
    }

    private void FixedUpdate()
    {
        if (!PerformRescans)
        {
            TimeBetweenChecks = 0f;
            return;
        }

        if (_TimeElapsed < TimeBetweenChecks && PerformRescans)
            _TimeElapsed += Time.deltaTime;
        else if (PerformRescans)
        {
            if (DebugLog) Debug.Log("Performing rescan!");
            UpdateData();
            _TimeElapsed = 0;
        }
    }

    [Button("Generate Nodes"), EnableIf("@_TotalNodesCreated == 0")]
    private void GenerateTerrainNodes()
    {
        if (terrain == null)
            terrain = GetComponent<Terrain>();

        if (terrainCollider == null)
            terrainCollider = terrain.GetComponent<TerrainCollider>();

        // Get the bounds of the terrain collider
        Bounds terrainBounds = terrainCollider.bounds;

        if (DebugLog)
            Debug.Log($"Bounds: {terrainBounds}\nSize: {terrainBounds.size}\nMin: {terrainBounds.min}\nMax: {terrainBounds.max}");

        float totalNeededNodes = terrainBounds.size.x * terrainBounds.size.z;

        if (DebugLog && DebugLogNodeCount)
            Debug.Log($"Number of Nodes Required: {totalNeededNodes}");

        //I guess fuck all logic, who tf needs it

        // Loop through each node within the terrain bounds
        for (float x = terrainBounds.min.x - XZOffset; x < terrainBounds.max.x + XZOffset; x += 1)
        {
            for (float z = terrainBounds.min.z - XZOffset; z < terrainBounds.max.z + XZOffset; z += 1)
            {
                Vector2 nodePosition = new Vector2(x, z)/* + new Vector2(terrain.transform.position.x, terrain.transform.position.z)*/;

                // Check if the node is within the terrain bounds
                if (terrainBounds.Contains(new Vector3(nodePosition.x, 0, nodePosition.y)))
                {
                    // Check for colliders intersecting with the node
                    Collider[] colliders = Physics.OverlapBox(new Vector3(x, 0, z), new Vector3(XZOffset * 2, YOffset, XZOffset * 2))
                                       .Where(col => col != terrainCollider)
                                       .ToArray();
                    
                    if (colliders.Length == 0)
                    {
                        AddNewNode(nodePosition, ref PassableTerrain);
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
                        AddNewNode(nodePosition, ref PassableTerrain);
                        continue;
                    }
                    else
                    {
                        AddNewNode(nodePosition, ref ImpassableTerrain);
                        continue;
                    }
                }
            }
        }

        _TotalNodesCreated = PassableTerrain.Count + ImpassableTerrain.Count;


        if (DebugLog && DebugLogTerrainGenerationPassableCount && DebugLogNodeCount)
            Debug.Log($"Number of Passable Nodes Created: {PassableTerrain.Count}");

        if (DebugLog && DebugLogTerrainGenerationImpassableCount && DebugLogNodeCount)
            Debug.Log($"Number of Impassable Nodes Created: {ImpassableTerrain.Count}");

        if (DebugLog && DebugLogNodeCount)
            Debug.Log($"Total nodes created: {_TotalNodesCreated}");
    }

    [Button("Clear Nodes"), EnableIf("@_TotalNodesCreated != 0")]
    private void ClearData()
    {
        if (DebugLog)
            Debug.Log($"Clearing nodes for {name}...");

        PassableTerrain.Clear();
        ImpassableTerrain.Clear();
        _TotalNodesCreated = 0;

        if (DebugLog)
            Debug.Log($"{name} has been cleared!");
    }

    [Button("Update Nodes"), EnableIf("@_TotalNodesCreated != 0")]
    private void UpdateData()
    {
        if (DebugLog)
            Debug.Log($"Updating nodes for {name}...");

        try
        {
            Dictionary<float, List<float>> tempPassableNodes = new();
            Dictionary<float, List<float>> tempImpassableNodes = new();
            Vector2 nodePosition = new();

            //Loop through all z positions
            for (float z = 0; z < terrainCollider.bounds.max.z; z += XZOffset)
            {
                //Check if the z position is a key for passable
                if (PassableTerrain.ContainsKey(z))
                {
                    //Loop through each x coordinate
                    foreach (var x in PassableTerrain[z].ToList())
                    {
                        //If the x coordinate is contained as a value of z,
                        if (PassableTerrain[z].Contains(x))
                        {
                            //Get the node position
                            nodePosition = new(x, z);
                            DetermineNodeStatus(nodePosition, ref tempPassableNodes, ref tempImpassableNodes);

                            //Reset the node position.
                            nodePosition = Vector2.zero;
                        }
                    }
                }
                
                //Check if the z position is a key for impassable.
                if (ImpassableTerrain.ContainsKey(z))
                {
                    //Loop through each X coordinate
                    foreach (var x in ImpassableTerrain[z].ToList())
                    {
                        //If the x coordinate is contained as a value of z,
                        if (ImpassableTerrain[z].Contains(x))
                        {
                            //get the node position.
                            nodePosition = new(x, z);
                            DetermineNodeStatus(nodePosition, ref tempPassableNodes, ref tempImpassableNodes);

                            //Reset the node position.
                            nodePosition = Vector2.zero;
                        }
                    }
                }
            }


            #region Old
            ////Loop through all passable nodes to verify they are still valid.
            //foreach (var z in PassableTerrain.Keys.ToList())
            //{
            //    foreach (var x in PassableTerrain[z].ToList())
            //    {
            //        Vector3 pos = GetNodePosition(new Vector2(x, z));
            //        var intersectingObjects = 
            //            Physics.OverlapBox(pos, new Vector3(XZOffset * 2, YOffset, XZOffset * 2));
            //        if (intersectingObjects.Length > 0)
            //        {
            //            AddNewNode(new Vector2(x, z), ref ImpassableTerrain);
            //            PassableTerrain[z].Remove(x);


            //            if (DebugLog)
            //                Debug.Log($"Marking ({x}, {z}) as impassable!");
            //        }

            //        pos = new();
            //    }
            //}

            ////Loop through impassable nodes to verify they are still valid
            //foreach (var z in ImpassableTerrain.Keys.ToList())
            //{
            //    foreach (var x in ImpassableTerrain[z].ToList())
            //    {
            //        //We do not need to convert this to a node position, as we are getting the prior saved node position.
            //        Vector3 pos = GetNodePosition(new Vector2(x, z));
            //        //Find all freed up nodes that were previously impassable that are no longer obstructed.
            //        Collider[] colliders = Physics.OverlapBox(pos, new Vector3(XZOffset * 2, YOffset, XZOffset * 2))
            //            .Where(col => col != terrainCollider)
            //            .ToArray();

            //        // If no collider is found at the node position, mark it as passable
            //        if (colliders.Length == 0)
            //        {
            //            //Mark the node as passable =D
            //            AddNewNode(new Vector2(x, z), ref PassableTerrain);
            //            //We don't need to check if it exists, because we couldn't have gotten here if the data didn't already exist.
            //            ImpassableTerrain[z].Remove(x);

            //        }
            //    }
            //}
            #endregion

            PassableTerrain = tempPassableNodes;
            ImpassableTerrain = tempImpassableNodes;

            if (DebugLog)
                Debug.Log($"{name}'s nodes have been updated!");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Exception thrown: {ex.Message} Something tells me that wasn't supposed to happen.\n{ex.Data}");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="transform"></param>
    /// <returns>The position rounded if it is an existing node.</returns>
    /// <exception cref="Exception">Throws an exception if the position does not exist as a node</exception>
    private Vector3 GetNodePosition(Vector2 pos)
    {
        Vector3 truePos = new Vector3(pos.x, transform.position.y, pos.y);
        bool successful = false;

        if (PassableTerrain.ContainsKey(pos.y) || ImpassableTerrain.ContainsKey(pos.y))
        {
            successful = PassableTerrain[pos.y].Contains(truePos.z) || ImpassableTerrain[pos.y].Contains(truePos.z);
        }

        if (successful)
            return truePos;
        else throw new Exception("Coordinate was not found as a node!");
    }

    private void AddNewNode(Vector2 terrainPos, ref Dictionary<float, List<float>> dictionary)
    {
        if (!dictionary.ContainsKey(terrainPos.y))
            dictionary.Add(terrainPos.y, new List<float>());

        dictionary[terrainPos.y].Add(terrainPos.x);
    }

    /// <summary>
    /// Determines if the node is passable or impassable and adds it to the corresponding list.
    /// </summary>
    /// <param name="nodePosition"></param>
    /// <param name="passables"></param>
    /// <param name="impassables"></param>
    private void DetermineNodeStatus(Vector2 nodePosition, ref Dictionary<float, List<float>> passables, ref Dictionary<float, List<float>> impassables)
    {
        //Check if anything is intersecting at that position.
        var intersectingObjects =
            Physics.OverlapBox(new Vector3(nodePosition.x, 0, nodePosition.y), new Vector3(XZOffset * 2, YOffset, XZOffset * 2))
                                       .Where(col => col != terrainCollider)
                                       .ToArray();

        //If we found anything,
        if (intersectingObjects.Length > 0)
        {
            //Check if the object is tagged as passable.
            var isTagged = intersectingObjects.All(obj => obj.GetComponent<ObjectTags>());
            if (isTagged)
            {
                if (intersectingObjects.All(obj => obj.GetComponent<ObjectTags>().ContainsTag("Passable")))
                {
                    //If so, we add mark the node as passable.
                    AddNewNode(nodePosition, ref passables);

                    //Reset the node position.
                    nodePosition = Vector2.zero;
                    return;
                }
            }

            //If we reach this point, the object was not tagged as passable and thus the node is impassable.
            AddNewNode(nodePosition, ref impassables);
        }
        else if (intersectingObjects.Length == 0) //We did not find anything intersecting with the node
        {
            //So we can mark the node as passable.
            AddNewNode(nodePosition, ref passables);
        }
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
        List<Vector2> ShortestPathCollection = new();

        /*
        Do calculations for determining the possible shortest path here.
        No idea what this will look like yet! =D
        */

        //example for verifying path is valid
        foreach (var vector in ShortestPathCollection)
        {

            if (PassableTerrain[vector.y].Contains(vector.x)) continue;
            else if (ImpassableTerrain[vector.y].Contains(vector.x))
                //Impassible, do not walk there! Bad!
                throw new Exception($"Coordinate {vector} is invalid, as this is marked as impassible terrain!"); 

            throw new Exception("Coordinate was not stored in the grid!");
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
                    Vector3 nodeCenter = new Vector3(x, terrain.SampleHeight(new Vector3(x, 0, entry.Key)) + YOffset, entry.Key);
                    Gizmos.DrawWireCube(nodeCenter, new Vector3(1, YOffset, 1));
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
                    Vector3 nodeCenter = new Vector3(x, terrain.SampleHeight(new Vector3(x, 0, entry.Key)) + YOffset, entry.Key);
                    Gizmos.DrawWireCube(nodeCenter, new Vector3(1, YOffset, 1));
                }
            }
        }
    }
}
