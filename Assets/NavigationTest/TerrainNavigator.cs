using Assets.Entities;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [ShowInInspector, ReadOnly, FoldoutGroup("ReadOnly")]
    private Node TrueTile;
    [ShowInInspector, ReadOnly, FoldoutGroup("ReadOnly")]
    private Node DestinationTile;

    private float timeElapsed = 0;
    [FoldoutGroup("Customization")]
    public float TimerDuration = 0.1f;


    #region Debug Toggles
    [FoldoutGroup("Gizmos Toggles")]
    public bool DebugData = false;

    [FoldoutGroup("Gizmos Toggles"), ShowIf("@DebugData")]
    public bool DrawGizmos = false;
    [FoldoutGroup("Gizmos Toggles"), ShowIf("@DebugData && DrawGizmos")]
    public bool DrawShortestPath = true;
    [FoldoutGroup("Gizmos Toggles"), ShowIf("@DebugData && DrawGizmos")]
    public bool DrawNeighbors = true;
    [FoldoutGroup("Gizmos Toggles"), ShowIf("@DebugData && DrawGizmos")]
    public bool DrawDestinationTile = true;
    [FoldoutGroup("Gizmos Toggles"), ShowIf("@DebugData && DrawGizmos")]
    public bool DrawTrueTile = true;

    [FoldoutGroup("Printing"), ShowIf("@DebugData")]
    public bool PrintErrors = true;
    [FoldoutGroup("Printing"), ShowIf("@DebugData")]
    public bool PrintWarnings = true;
    [FoldoutGroup("Printing"), ShowIf("@DebugData")]
    public bool PrintMessages = true;
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

    #endregion

    private void Awake()
    {
        if (_AliveComponent == null) _AliveComponent = GetComponentInChildren<Alive>() ?? GetComponent<Alive>() ?? GetComponentInParent<Alive>();

        if (_AliveComponent == null)
        {

            if (PrintErrors && DebugData)
                Debug.LogError($"No component of type Alive found on {name}!");
            else Debug.LogWarning($"Critical error message hidden due to current toggles for {name}");
            gameObject.SetActive(false);
        }
    }

    private void FixedUpdate()
    {
        if (timeElapsed >= TimerDuration)
        {
            if (terrainHandler == null)
            {
                GetCurrentTerrainHandler();
            }

            timeElapsed = 0;
            TrueTile = terrainHandler.GetCurrentNode(transform.position);
            neighbors = terrainHandler.GetNeighbors(TrueTile);

            Vector3 targetPosition = _AliveComponent.CurrentLivingTarget?.transform.position ?? _AliveComponent.CurrentInteractTarget?.GetComponent<Transform>().position ?? _AliveComponent._DebugCurrentDestination;
            if (targetPosition !=  Vector3.zero)
                DestinationTile = terrainHandler.GetCurrentNode(targetPosition);
            else
            {
                if (PrintWarnings && DebugData)
                    Debug.LogWarning($"{name} currently has no target!");
                return;
            }

            if (TrueTile == null)
            {
                if (PrintWarnings && DebugData)
                    Debug.LogWarning("No current node found!");
                return;
            }
            shortestPath = terrainHandler.FindShortestPathAStar(TrueTile, DestinationTile);
        }

        timeElapsed += Time.deltaTime;
        
    }

    private void GetCurrentTerrainHandler()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit))
        {
            terrainHandler = hit.collider.gameObject.GetComponent<TerrainTiling>();
        }
    }


    private void OnDrawGizmos()
    {
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
                        Gizmos.DrawWireCube(neighbor.transform.position + new Vector3(0, terrainHandler.YOffset, 0), new Vector3(1, 0.1f, 1));
                    }
                }
                if (DrawDestinationTile && DestinationTile)
                {
                    Gizmos.color = DestinationTileGizmoColor;
                    Gizmos.DrawWireCube(DestinationTile.transform.position, DestinationTile.transform.localScale);
                }
                if (DrawTrueTile && TrueTile)
                {
                    Gizmos.color = TrueTileGizmoColor;
                    Gizmos.DrawWireCube(TrueTile.transform.position, TrueTile.transform.localScale);
                }
            }
        }
    }
}
