using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Entities
{
    /// <summary>
    /// An interactable item, don't forget to register it with IInteractable.RegisterInteractable(this)!
    /// </summary>
    public interface IInteractable
    {
        public static List<IInteractable> Interactables { get; } = new();

        internal protected static void RegisterInteractable(IInteractable caller) => Interactables.Add(caller);
        public void Interact();

        public T GetComponent<T>()
        {
            if (this is MonoBehaviour monoBehaviour)
                return monoBehaviour.gameObject.GetComponent<T>();
            else throw new Exception("This interactable does not inherit from MonoBehavior!");
        }
    }
}
