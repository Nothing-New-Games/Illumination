using UnityEngine;
using Assets.EDO;
using Assets.Entities;
using Sirenix.OdinInspector;

public class Lever : MonoBehaviour, ISender, IInteractable
{
    private void Start() => IInteractable.RegisterInteractable(this);

    public event ISender.EventConditionsMetHandler OnEventConditionsMet;

    public void FireEvent() => OnEventConditionsMet?.Invoke();


    [Button]
    public void Interact() => FireEvent();
}