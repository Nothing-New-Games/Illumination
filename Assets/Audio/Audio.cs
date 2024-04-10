using NNG.CustomUnityInspector;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Audio : MonoBehaviour
{
    [ShowInInspector, ReadOnly, TabGroup("Debug Data")]
    private Dictionary<string, List<AudioClip>> _AudioClips = new();
    [ShowInInspector, ReadOnly, TabGroup("Debug Data"), LabelText("Currently Playing: ")]
    private string _CurrentlyPlayingString;

    private int _CurrentTime;
    public /*private*/ int _ClipLength = 100;


    [ShowInInspector, ReadOnly, TabGroup("Debug Data"), ShowIf("")]
    private string _CurrentPlayTime;
    [ShowInInspector, /*ReadOnly,*/ TabGroup("Debug Data"), DynamicSlider(0, "_ClipLength")]
    private int _PlaytimeSlider;


    protected internal virtual void UpdateDebug()
    {

    }

    protected internal void AddNewClipList(string listName, List<AudioClip> newList)
    {
        if (!_AudioClips.ContainsKey(listName))
        {
            _AudioClips.Add(listName, newList);
        }
        else throw new Exception("Playlist already exists!");
    }
    protected internal void RemoveNewClipList(string listName)
    {
        if (_AudioClips.ContainsKey(listName))
            _AudioClips.Remove(listName);
        else throw new Exception($"Unable to find playlist {listName}!");
    }
}
