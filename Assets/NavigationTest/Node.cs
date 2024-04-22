using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.ShaderGraph.Drawing.Inspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;


//[ShowInInspector, DisplayAsString, HideLabel]
public class Node : MonoBehaviour
{
    [TabGroup("Main", "Debug"), ReadOnly, ShowInInlineEditors, DisplayAsString]
    private TerrainTiling parentTerrainHandler;
    [TabGroup("Main", "Debug"), ReadOnly, ShowInInlineEditors, DisplayAsString]
    private Terrain parentTerrain;

    private float _X_ => Mathf.Round(transform.position.x);
    private float _Y_ => Mathf.Round(parentTerrain.SampleHeight(new Vector3(_X_, 0, _Z_)));
    private float _Z_ => Mathf.Round(transform.position.z);

    [TabGroup("Main", "Debug"), ShowInInspector, ReadOnly, DisplayAsString]
    public float X { get { return _X_; } }
    [TabGroup("Main", "Debug"), ShowInInspector, ReadOnly, DisplayAsString]
    public float Y { get { return _Y_; } }
    [TabGroup("Main", "Debug"), ShowInInspector, ReadOnly, DisplayAsString]
    public float Z { get { return _Z_; } }

    #region Geometry fields
    private float _TileArea_;
    [TabGroup("Main", "Debug"), ShowInInspector, ReadOnly, DisplayAsString]
    public float TileArea => _TileArea_;
    #endregion

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
        _TileArea_ = transform.localScale.x * transform.localScale.z;

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


    public override string ToString()
    {
        if (this == null)
            return "Node undefined";

        return $"({_X_}, {_Z_}) (Y = {_Y_})";
    }
}

public class NodeCluster
{
    private Node TrueTile { get; set; }
    int NodeCollectionSize { get; set; }
    float _Area_ { get; set; }

    [ShowInInspector, ReadOnly, DisplayAsString]
    public float Area => _Area_;

    private float _MaxWidth_;
    [ShowInInspector, ReadOnly, DisplayAsString]
    public float MaxWidth => _MaxWidth_;
    private float _MaxLength_;
    [ShowInInspector, ReadOnly, DisplayAsString]
    public float MaxLength => _MaxLength_;


    [ShowInInspector]
    private List<Node> ClusterMembers;

    public NodeCluster(Node trueTile, List<Node> nodes = null)
    {
        TrueTile = trueTile;

        if (nodes != null)
            ClusterMembers = nodes;
    }

    #region Adjustment stuff
    // Method to add a single node to the multi-node
    public void AddNode(Node node) => ClusterMembers.Add(node);

    // Method to add multiple nodes to the multi-node
    public void AddNodes(List<Node> nodeList) => ClusterMembers.AddRange(nodeList);

    // Method to remove a single node from the multi-node
    public void RemoveNode(Node node) => ClusterMembers.Remove(node);

    // Method to remove multiple nodes from the multi-node
    public void RemoveNodes(List<Node> nodeList) => nodeList.ForEach(node =>  ClusterMembers.Remove(node));
    #endregion


    public List<Node> GetAllNodes => ClusterMembers;

    [ShowInInspector]
    public Node GetTrueTile => TrueTile;

    public bool Contains(Node node) => ClusterMembers.Contains(node) || node == TrueTile;

    // Method to perform any operation on the multi-node, such as checking passability
    public bool IsPassable() => ClusterMembers.All(node => node.IsPassable);

    public override string ToString()
    {
        if (TrueTile != null && this.ToString() != "Null")
            return $"True Tile: {TrueTile.ToString()}, Area: {_Area_}";
        else return "Cluster undefined";
    }
}



#region Inspector Drawing
public class NodeClusterDrawer : OdinAttributeProcessor<NodeCluster>
{
    NodeCluster cluster { get; set; }

    public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
    {
        cluster = property.TryGetTypedValueEntry<NodeCluster>().SmartValue;


        if (cluster == null)
            attributes.Add(new InfoBoxAttribute("Cluster undefined"));
        //else attributes.Remove(attributes.First(attribute => attribute.GetType() == typeof(InfoBoxAttribute)));

        //attributes.Add(new InlinePropertyAttribute());
    }

    public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
    {
        attributes.Add(new BoxGroupAttribute("Cluster", false));

        if (cluster != null)
        {
            var parentAttributes = parentProperty.Attributes;
            foreach ( var attribute in attributes)
            {
                if (attribute.GetType() != typeof(InfoBoxAttribute))
                    attributes.Add(attribute);
            }
        }
    }
}

[CustomEditor(typeof(Node))]
public class NodeDrawer : OdinEditor
{
    public override void OnInspectorGUI()
    {
        var property = serializedObject.GetIterator();
        Node node = target.GetComponent<Node>();
        string title = "Node undefined";
        Label label;

        // Check if property is an object reference
        if (property.propertyType == SerializedPropertyType.ObjectReference)
        {
            // Check if the referenced object is a Node
            if (node)
            {
                title = node.ToString();

                // Draw button for the referenced Node
                if (GUILayout.Button(title))
                {
                    // Handle button click event
                    Selection.activeObject = node;
                    Debug.Log($"Node reference clicked for {node.ToString()}");
                    EditorUtility.SetDirty(target);
                    return;
                }
            }
            else
            {
                label = PropertyDrawerUtils.CreateLabel(title, 0, FontStyle.Normal);
                this.DrawPreview(label.contentRect);
                return;
            }
        }

        this.DrawDefaultInspector();

        EditorUtility.SetDirty(target);
    }
}
#endregion