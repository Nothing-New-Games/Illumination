using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class Node : MonoBehaviour
{
    [TabGroup("Main", "Debug"), ReadOnly, ShowInInlineEditors, DisplayAsString]
    private TerrainTiling parentTerrainHandler;
    [TabGroup("Main", "Debug"), ReadOnly, ShowInInlineEditors, DisplayAsString]
    private Terrain parentTerrain;

    private float _X_ => transform.position.x;
    private float _Y_ => parentTerrain.SampleHeight(new Vector3(_X_, 0, _Z_));
    private float _Z_ => transform.position.z;

    [TabGroup("Main", "Debug"), ShowInInspector, ReadOnly, DisplayAsString]
    public float X { get { return _X_; } }
    [TabGroup("Main", "Debug"), ShowInInspector, ReadOnly, DisplayAsString]
    public float Y { get { return _Y_; } }
    [TabGroup("Main", "Debug"), ShowInInspector, ReadOnly, DisplayAsString]
    public float Z { get { return _Z_; } }

    private bool _IsPassable_ = true;
    [TabGroup("Main", "Debug"), ShowInInspector, ReadOnly, DisplayAsString, GUIColor("GetColor")]
    public bool IsPassable { get { return _IsPassable_; } }
    private Color GetColor()
    {
        if (_IsPassable_) return Color.green; else return Color.red;
    }


    [TabGroup("Main", "Debug"), ShowInInspector, ReadOnly, DisplayAsString]
    public GameObject ContestedBy { get; private set; }

    private void Start()
    {
        name = $"({X}, {Z})";

        GetRequiredComponents();
        if (parentTerrainHandler == null)
        {
            Debug.LogError($"{name} is missing a terrain that it belongs to!");
            enabled = false;
            return;
        }
        CheckForColliders();
    }

    private void GetRequiredComponents()
    {
        parentTerrainHandler = GetComponentInParent<TerrainTiling>();
        parentTerrain = GetComponentInParent<Terrain>();
    }

    public void CheckForColliders()
    {
        GetRequiredComponents();
        Collider[] colliders = Physics.OverlapBox(transform.position, new Vector3(1, parentTerrainHandler.YOffset, 1) / 2)
            .Where(collider => collider != parentTerrainHandler.terrainCollider && collider.GetComponent<Node>() == null).ToArray();

        if (colliders.Length > 0)
        {
            ContestedBy = colliders.First().gameObject;
        }

        _IsPassable_ = colliders.Length == 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == parentTerrainHandler.terrainCollider || other.GetComponent<Node>()) return;

        ObjectTags tags = other.gameObject.GetComponent<ObjectTags>();
        if (tags != null)
        {
            _IsPassable_ = tags.ContainsTag("Passable");
            return;
        }

        ContestedBy = other.gameObject;
        _IsPassable_ = false;
    }
    private void OnTriggerExit(Collider other)
    {
        _IsPassable_ = true;
        ContestedBy = null;
        CheckForColliders();
    }



    private void OnDrawGizmos()
    {
        if (parentTerrainHandler == null || parentTerrain == null)
        {
            GetRequiredComponents();
        }

        if (parentTerrainHandler == null || parentTerrain == null) return;

        if (parentTerrainHandler.DebugPassableGizmos && parentTerrainHandler.DebugData && parentTerrainHandler.DebugGizmos)
        {
            if (_IsPassable_)
            {
                Gizmos.color = Color.green;
                //Vector3 nodeCenter = new Vector3(_X_, _Y_ + parentTerrainHandler.YOffset, _Z_);
                Gizmos.DrawWireCube(transform.position, transform.localScale);
            }
        }

        if (parentTerrainHandler.DebugImpassableGizmos && parentTerrainHandler.DebugData && parentTerrainHandler.DebugGizmos)
        {
            if (!_IsPassable_)
            {
                Gizmos.color = Color.red;
                //Vector3 nodeCenter = new Vector3(_X_, parentTerrainHandler.YOffset, _Z_);
                Gizmos.DrawWireCube(transform.position, transform.localScale);
            }
        }
    }
}


public class NodeCluster
{
    private Node TrueTile { get; set; }
    int NodeCollectionSize { get; set; }

    private List<Node> Nodes;

    public NodeCluster(Node trueTile, List<Node> nodes = null)
    {
        TrueTile = trueTile;
        if (nodes != null)
            Nodes = nodes;
        else Nodes = new();

        NodeCollectionSize = Nodes.Count;
    }

    // Method to add a single node to the multi-node
    public void AddNode(Node node) => Nodes.Add(node);

    // Method to add multiple nodes to the multi-node
    public void AddNodes(List<Node> nodeList) => Nodes.AddRange(nodeList);

    // Method to remove a single node from the multi-node
    public void RemoveNode(Node node) => Nodes.Remove(node);

    // Method to remove multiple nodes from the multi-node
    public void RemoveNodes(List<Node> nodeList) => nodeList.ForEach(node =>  Nodes.Remove(node));

    // Method to get all nodes in the multi-node
    public List<Node> GetAllNodes => Nodes;

    public Node GetTrueTile => TrueTile;

    // Method to perform any operation on the multi-node, such as checking passability
    public bool IsPassable() => Nodes.All(node => node.IsPassable);
}