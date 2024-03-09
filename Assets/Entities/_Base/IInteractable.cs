using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
