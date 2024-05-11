using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Reflection;
using System;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph.Drawing.Inspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class GroundExtensions
{
    public static bool CheckForPassableTag(this GameObject obj)
    {
        ObjectTags tags = obj.GetComponent<ObjectTags>();
        if (tags != null)
        {
            return tags.ContainsTag("Passable");
        }
        return false;
    }

    public static float SampleGroundHeight(this GroundBase ground, Vector3 atPos)
    {
        float sampledY = 0f;

        Terrain terrain = ground.GetComponent<Terrain>();
        if (terrain != null)
        {
            sampledY =  terrain.SampleHeight(new Vector3(atPos.x, 0, atPos.z));

            if (sampledY == 0) sampledY += ground.transform.position.y;
        }
        else
        {
            int layerMask = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
            RaycastHit hit;

            if (Physics.Raycast(atPos, ground.transform.position - atPos, out hit, Mathf.Infinity, layerMask))
            {
                sampledY = hit.point.y;
            }
        }


        return sampledY;
    }
}


public class TileClusterDrawer : OdinAttributeProcessor<TileCluster>
{
    TileCluster cluster { get; set; }

    public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
    {
        cluster = property.TryGetTypedValueEntry<TileCluster>().SmartValue;


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
            foreach (var attribute in attributes)
            {
                if (attribute.GetType() != typeof(InfoBoxAttribute))
                    attributes.Add(attribute);
            }
        }
    }
}

[CustomEditor(typeof(Tile))]
public class TileDrawer : OdinEditor
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