using Assets.EDO;
using UnityEngine;

namespace Assets.Entities
{
    public abstract class Alive : MonoBehaviour, ISender
    {
        /// <summary>
        /// The current health of the entity.
        /// </summary>
        [SerializeField]
        protected internal float CurrentHealth = 1f;
        /// <summary>
        /// The max health the entity will have.
        /// </summary>
        [SerializeField]
        protected internal float MaxHealth = 1f;
        /// <summary>
        /// Min/Max damage the entity can do with an attack.
        /// </summary>
        [SerializeField]
        protected internal Vector2 Damage = new Vector2(1, 1);
        [SerializeField]
        protected internal bool IsAlive = true;

        public abstract void DealDamage(DamageSource damage);



        #region Death Event
        public event ISender.EventConditionsMetHandler OnEventConditionsMet;

        /// <summary>
        /// Event for when the creature dies.
        /// </summary>
        public void FireEvent()
        {
            //Execute creature's death code.
            OnDeath();
            //Set alive to false.
            IsAlive = false;
            //Inform the next of kin.
            OnEventConditionsMet?.Invoke();
        }

        /// <summary>
        /// Everything that is supposed to happen on the creature's end should it die, including animations.
        /// </summary>
        public abstract void OnDeath();
        #endregion
    }
}