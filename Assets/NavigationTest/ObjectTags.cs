using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectTags : MonoBehaviour
{
    public string[] Tags;

    public bool ContainsTag(string tag) => Tags.Contains(tag);
    public bool ContainsTags(string[] tags)
    {
        foreach (string tag in tags)
        {
            if (!Tags.Contains(tag))
                return false;
        }
        return true;
    }
}
