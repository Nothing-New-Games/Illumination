using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Music : Audio
{
    public List<AudioClip> Clips = new();

    private void Awake()
    {
        AddNewClipList(name, Clips);
    }
}
