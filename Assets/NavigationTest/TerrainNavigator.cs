using Assets.Entities;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Alive))]
public class TerrainNavigator : MonoBehaviour
{
    [FoldoutGroup("ReadOnly"), ReadOnly, DisplayAsString, ShowInInspector]
    public bool IsOnTerrain => terrainHandler != null;
    TerrainTiling terrainHandler;

    private Alive _AliveComponent;


    [ShowInInspector, ReadOnly, FoldoutGroup("ReadOnly")]
    public List<Node> shortestPath { get; private set; }  = new();
    [ShowInInspector, ReadOnly, FoldoutGroup("ReadOnly")]
    private List<Node> neighbors = new();



    //[FoldoutGroup("ReadOnly"), LabelText("True Tile:"), ShowInInspector]
    private Node TrueTile;
    public Node GetTrueTile => TrueTile;
    [FoldoutGroup("ReadOnly"), ShowInInspector, PropertySpaceAttribute(0, 20)]
    public NodeCluster cluster;


    [ShowInInspector, ReadOnly, FoldoutGroup("ReadOnly")]
    private Node DestinationTile;

    private float timeElapsed = 0;
    [FoldoutGroup("Customization")]
    public float TimerDuration = 0.1f;


    #region Debug Toggles
    [FoldoutGroup("Debug")]
    public bool DebugData = false;

    [FoldoutGroup("Debug/Gizmos Toggles"), ShowIf("@DebugData"), Indent]
    public bool DrawGizmos = false;
    [FoldoutGroup("Debug/Gizmos Toggles"), ShowIf("@DebugData && DrawGizmos"), Indent(2)]
    public bool DrawShortestPath = true;
    [FoldoutGroup("Debug/Gizmos Toggles"), ShowIf("@DebugData && DrawGizmos"), Indent(2)]
    public bool DrawNeighbors = true;
    [FoldoutGroup("Debug/Gizmos Toggles"), ShowIf("@DebugData && DrawGizmos"), Indent(2)]
    public bool DrawDestinationTile = true;
    [FoldoutGroup("Debug/Gizmos Toggles"), ShowIf("@DebugData && DrawGizmos"), Indent(2)]
    public bool DrawTrueTile = true;
    [FoldoutGroup("Debug/Gizmos Toggles"), ShowIf("@DebugData && DrawGizmos"), Indent(2)]
    public bool DrawClusterTiles = true;

    [FoldoutGroup("Debug/Printing"), ShowIf("@DebugData"), Indent]
    public bool PrintMessages = true;
    [FoldoutGroup("Debug/Printing"), ShowIf("@DebugData"), Indent]
    public bool PrintErrors = true;
    [FoldoutGroup("Debug/Printing"), ShowIf("@DebugData"), Indent]
    public bool PrintWarnings = true;
    #endregion

    #region Gizmo Colors
    [ShowIf("@DrawGizmos && DrawShortestPath"), FoldoutGroup("Gizmo Colors")]
    public Color PathGizmoColor = Color.cyan;
    [ShowIf("@DrawGizmos && DrawNeighbors"), FoldoutGroup("Gizmo Colors")]
    public Color NeighborsGizmoColor = Color.blue;

    [ShowIf("@DrawGizmos && DrawTrueTile"), FoldoutGroup("Gizmo Colors")]
    public Color TrueTileGizmoColor = Color.cyan;
    [ShowIf("@DrawGizmos && DrawDestinationTile"), FoldoutGroup("Gizmo Colors")]
    public Color DestinationTileGizmoColor = Color.blue;
    [ShowIf("@DrawGizmos && DrawTrueTile"), FoldoutGroup("Gizmo Colors")]
    public Color ClusterNodeGizmoColor = Color.cyan;

    #endregion

    private void Awake()
    {
        UpdateAliveComponentRef();
    }

    private void FixedUpdate()
    {
        if (timeElapsed >= TimerDuration)
        {
            timeElapsed = 0;
            UpdateTileData();
        }

        timeElapsed += Time.deltaTime;
        
    }

    [Button]
    private void UpdateTileData()
    {
        if (terrainHandler != null || GetCurrentTerrainHandler())
        {
            if (_AliveComponent == null)
                UpdateAliveComponentRef();

            if (DebugData && PrintMessages)
                Debug.Log($"Terrain pulled: {terrainHandler}");

            //if (cluster != null && (cluster.GetAllNodes == null || cluster.GetAllNodes.Count == 0 || cluster.GetAllNodes.Any(node => node.IsDestroyed())))
            //{
            //    cluster = null;
            //}

            //if (neighbors.Count == 0 || neighbors.Any(node => node.IsDestroyed()))
            //{
            //    neighbors = new();
            //}

            //if (TrueTile != null && TrueTile.IsDestroyed())
            //{
            //    TrueTile = null;
            //}

            TrueTile = terrainHandler.GetCurrentNode(transform.position);
            neighbors = terrainHandler.GetNeighbors(TrueTile);
            cluster = terrainHandler.CreateNodeClusterFromAgent(this);


            Vector3 targetPosition = _AliveComponent.CurrentLivingTarget?.transform.position ?? _AliveComponent.CurrentInteractTarget?.GetComponent<Transform>().position ?? _AliveComponent._DebugCurrentDestination;
            if (targetPosition != Vector3.zero)
                DestinationTile = terrainHandler.GetCurrentNode(targetPosition);
            else
            {
                if (PrintWarnings && DebugData)
                    Debug.LogWarning($"{name} currently has no target!");
                return;
            }

            if (TrueTile == null && DestinationTile != null)
            {
                if (PrintWarnings && DebugData)
                    Debug.LogWarning("No current node found!");
                return;
            }
            shortestPath = terrainHandler.FindShortestPathAStar(TrueTile, DestinationTile);
        }
        else if (PrintErrors && DebugData) Debug.LogError("Unable to update tiles, Terrain not found!");
        else Debug.LogWarning("Error message suppressed!");
    }

    private void UpdateAliveComponentRef()
    {
        if (_AliveComponent == null) _AliveComponent = GetComponentInChildren<Alive>() ?? GetComponent<Alive>() ?? GetComponentInParent<Alive>();

        if (_AliveComponent == null)
        {
            if (PrintErrors && DebugData)
                Debug.LogError($"No component of type Alive found on {name}!");
            else Debug.LogWarning($"Critical error message hidden due to current toggles for {name}");
            gameObject.SetActive(false);
        }

        else if (PrintMessages && DebugData)
            Debug.Log("Alive component successfully found.");
    }

    private bool GetCurrentTerrainHandler()
    {
        RaycastHit hit;
        // Exclude the layer of the object this script is attached to
        int layerMask = ~(1 << gameObject.layer | 1 << LayerMask.NameToLayer("Ignore Raycast"));

        if (Physics.Raycast(transform.position + Vector3.up * 10, Vector3.down, out hit, Mathf.Infinity, layerMask))
        {
            if (PrintMessages && DebugData)
                Debug.Log($"Hit: {hit.collider.name}");

            terrainHandler = hit.collider.gameObject.GetComponent<TerrainTiling>();
        }
        else if (PrintWarnings && DebugData) Debug.LogWarning($"Attempted to find terrain, but hit {hit.collider.name}!");

        return terrainHandler != null;
    }


    private void OnDrawGizmos()
    {
        //Visual test for raycast to find terrain
        //Gizmos.color = Color.green;
        //Gizmos.DrawLine(transform.position + Vector3.up * 10, new Vector3(transform.position.x, transform.position.y - 1000, transform.position.z));

        if (DebugData)
        {
            if (DrawGizmos)
            {
                if (shortestPath.Count != 0 && DrawShortestPath)
                {
                    Gizmos.color = PathGizmoColor;
                    for (int i = 0; i < shortestPath.Count; i++)
                    {
                        if (i == 0)
                            Gizmos.DrawLine(shortestPath[0].transform.position, shortestPath[i].transform.position);
                        else
                            Gizmos.DrawLine(shortestPath[i - 1].transform.position, shortestPath[i].transform.position);
                    }
                }
                if (neighbors.Count != 0 && DrawNeighbors)
                {
                    Gizmos.color = NeighborsGizmoColor;
                    foreach (var neighbor in neighbors)
                    {
                        if (neighbor != null)
                            Gizmos.DrawCube(neighbor.transform.position + new Vector3(0, terrainHandler.YOffset, 0), new Vector3(1, 0.1f, 1));
                    }
                }
                if (DrawDestinationTile && DestinationTile)
                {
                    Gizmos.color = DestinationTileGizmoColor;
                    Gizmos.DrawCube(DestinationTile.transform.position, DestinationTile.transform.localScale);
                }
                if (DrawTrueTile && TrueTile)
                {
                    Gizmos.color = TrueTileGizmoColor;
                    Gizmos.DrawCube(TrueTile.transform.position, TrueTile.transform.localScale);
                }
                //Draw cluster tiles
                if (DrawClusterTiles && cluster != null)
                {
                    Gizmos.color = ClusterNodeGizmoColor;
                    foreach (Node tile in cluster.GetAllNodes)
                        Gizmos.DrawCube(tile.transform.position, tile.transform.localScale);
                }
            }
        }
    }
}
