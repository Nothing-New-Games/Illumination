using Assets.EDO;
using Assets.Entities.AI;
using UnityEngine;

namespace Assets.Entities
{
    [RequireComponent(typeof(CharacterController))]
    public abstract class Alive : MonoBehaviour, ISender
    {
        public CharacterController Controller;
        public float BaseMovementSpeed = 5f;
        public float RunMovementMultiplier = 2f;
        public float MinDistanceToTarget = 1f;
        public Vector2 MaxWanderDistXY;
        public float MinIdleDuration = 3f;
        public float MaxIdleDuration = 15f;
        protected internal float _CurrentMovementSpeed = 0f;

        protected internal Animator _Animator;
        public AnimationTrigger CurrentAnimation;

        [SerializeField]
        protected internal Alive CurrentTarget;
        /// <summary>
        /// If you are replacing the engine with a custom one, override the start method.
        /// </summary>
        protected internal MovementEngine Engine;
        /// <summary>
        /// If you are replacing the AI with a custom one, override the start method.
        /// </summary>
        protected internal AIHandler AI;

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


        protected internal bool AnimatorIsPlaying()
        {
            if (_Animator != null)
                return _Animator.GetCurrentAnimatorStateInfo(0).length >
                   _Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            else return true;
        }


        public abstract void DealDamage(DamageSource source);

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

        #region Unity Methods
        internal virtual void Awake()
        {
            Controller = GetComponent<CharacterController>();
            _Animator = GetComponent<Animator>();
        }
        internal virtual void Start()
        {
            AI = new();
            Engine = new();
        }
        internal virtual void Update()
        {
            if (!IsAlive) return;
            else if (CurrentHealth <= 0) FireEvent(); //Handled by Alive to trigger death.
            Engine.UpdateVariables(this);
        }
        internal virtual void LateUpdate()
        {
            //If the animation is still playing, wait for it to finish.
            if (AnimatorIsPlaying())
            {
                //We don't want to change the animation, but keep the AI going.
                AI.Handle(this);
            }


            //Get the animation that is supposed to be playing.
            //AI method that returns enum of the current animation.
            //The AI will handle the movement calculations and return the proper animation.
            CurrentAnimation = AI.Handle(this);
            _Animator.SetTrigger(CurrentAnimation.ToString());
        }
        internal virtual void FixedUpdate()
        {

        }
        internal virtual void OnDestroy()
        {

        }
        #endregion
    }
}