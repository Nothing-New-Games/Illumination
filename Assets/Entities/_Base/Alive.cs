using Assets.EDO;
using Assets.Entities.AI;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace Assets.Entities
{
    [RequireComponent(typeof(CharacterController))]
    public abstract class Alive : MonoBehaviour, ISender
    {
        #region Variables
        protected internal CharacterController Controller;

        protected internal Animator _Animator;
        [ReadOnly]
        public AnimationType CurrentAnimation;
        /// <summary>
        /// If you are replacing the engine with a custom one, override the start method.
        /// </summary>
        protected internal MovementEngine Engine;
        /// <summary>
        /// If you are replacing the AI with a custom one, override the start method.
        /// </summary>
        protected internal AIHandler AI;
        [SerializeField]
        protected internal bool IsAlive = true;

        [TabGroup("Main", "Movement")]
        public bool PauseMovement;
        [TabGroup("Main", "Movement")]
        public float BaseMovementSpeed = 5f;
        [TabGroup("Main", "Movement")]
        public float RunMovementMultiplier = 2f;
        [TabGroup("Main", "Movement")]
        public float MinDistanceToDestination = 1f;
        [TabGroup("Main", "Movement")]
        public float MinDistanceToTarget = 3f;
        [TabGroup("Main", "Movement")]
        public Vector2 MaxWanderDistXY;
        [TabGroup("Main", "Movement")]
        public float MinIdleDuration = 3f;
        [TabGroup("Main", "Movement")]
        public float MaxIdleDuration = 15f;
        [TabGroup("Main", "Movement")]
        public float PositioningCorrectionDistance = 3f;
        protected internal float _CurrentMovementSpeedValue = 0f;

        [SerializeField]
        [TabGroup("Main", "Movement")]
        protected internal Alive CurrentTarget/* { get; set; }*/;

        [SerializeField]
        [TabGroup("Main", "Stats"), Tooltip("The current health of the entity.")]
        protected internal float CurrentHealth = 1f;
        [SerializeField]
        [TabGroup("Main", "Stats"), Tooltip("The max health the entity will have.")]
        protected internal float MaxHealth = 1f;
        [SerializeField]
        [TabGroup("Main", "Stats"), Tooltip("Min/Max damage the entity can do with an attack.")]
        protected internal Vector2 Damage = new Vector2(1, 1);

        [TabGroup("Main", "Stats"), Range(1, 360), SerializeField]
        [Tooltip("The creature's ability to see things around them. 180 = complete 360 vision (Don't ask, that's how Unity works), 0 = Blind.")]
        protected internal float MaxAngleDetection = 20;
        [TabGroup("Main", "Stats"), SerializeField]
        [Tooltip("The creature's ability to see things in front of them.")]
        protected internal float MaxDetectionDistance = 30f;
        [TabGroup("Main", "Stats"), SerializeField]
        [Tooltip("The creatures base chance to detect the player. The higher it is, the more likely it is.")]
        protected internal float BaseDetectionChance = 100f;
        #endregion


        protected internal bool AnimatorIsPlaying()
        {
            if (_Animator != null)
                return _Animator.GetCurrentAnimatorStateInfo(0).length >
                   _Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            
            return true;
        }
        protected internal bool CloseToTarget()
        {
            if (CurrentTarget != null)
                return Vector3.Distance(transform.position, CurrentTarget.transform.position) <= MinDistanceToDestination;
            
            return false;
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
            _CurrentMovementSpeedValue = BaseMovementSpeed;
            CreateAI();
            CreateEngine();
            Engine.UpdateVariables(this, transform.position);
        }

        internal virtual void CreateAI() => AI = new();

        internal virtual void CreateEngine() => Engine = new();

        internal virtual void Start()
        {

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


            _Animator.ResetTrigger(CurrentAnimation.ToString());
            //Get the animation that is supposed to be playing.
            //AI method that returns enum of the current animation.
            //The AI will handle the movement calculations and return the proper animation.
            CurrentAnimation = AI.Handle(this);

            if (!_Animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.ToLower().Contains(CurrentAnimation.ToString().ToLower()))
            {
                //Debug.Log("Good news, everyone!");
                _Animator.SetTrigger(CurrentAnimation.ToString());
            }
        }
        internal virtual void FixedUpdate()
        {

        }
        internal virtual void OnDestroy()
        {

        }



        [TabGroup("Main", "Debug")]
        public bool DebugData = false;

        [TabGroup("Main", "Debug"), ShowIf("@DebugData == true")]
        public bool DrawDestinationGizmo = false;
        [ShowIf("@DebugData == true && DrawDestinationGizmo"), TabGroup("Main", "Debug")]
        public float DestinationGizmoSize = 1f;
        [ShowIf("@DebugData == true && DrawDestinationGizmo"), TabGroup("Main", "Debug")]
        public Color DestinationColor = Color.cyan;

        [ShowIf("@DebugData == true && DrawDestinationGizmo"), TabGroup("Main", "Debug")]
        public bool DrawDestinationDistanceGizmo = false;
        [ShowIf("@DebugData == true && DrawDestinationDistanceGizmo"), TabGroup("Main", "Debug")]
        public Color DestinationDistanceColor = Color.red;

        [ShowIf("@DebugData == true"), TabGroup("Main", "Debug")]
        public bool DrawSightAngles = false;
        [ShowIf("@DebugData == true && DrawSightAngles"), TabGroup("Main", "Debug")]
        public Vector3 AngleVisualOffset = new();
        [ShowIf("@DebugData == true && DrawSightAngles"), TabGroup("Main", "Debug")]
        public Color SightGizmoColor = Color.yellow;

        [ShowIf("@DebugData == true"), TabGroup("Main", "Debug")]
        public bool DrawDistanceToPlayerGizmo = false;
        [ShowIf("@DebugData == true && DrawDistanceToPlayerGizmo"), TabGroup("Main", "Debug")]
        public Color DistanceToPlayerColor = Color.blue;

        internal virtual void OnDrawGizmos()
        {
            if (DebugData)
            {
                if (Engine != null)
                {
                    //Destination
                    if (DrawDestinationGizmo)
                    {
                        Gizmos.color = DestinationColor;
                        Gizmos.DrawWireSphere(Engine.GetCurrentDest, DestinationGizmoSize);
                    }

                    //Distance to Destination
                    if (DrawDestinationDistanceGizmo)
                    {
                        Gizmos.color = DestinationDistanceColor;
                        Gizmos.DrawLine(Engine.GetCurrentDest, Engine.GetCurrentDest + Vector3.Normalize(transform.position - Engine.GetCurrentDest));
                    }
                }

                //Sight Angle
                if (DrawSightAngles)
                {
                    Gizmos.color = SightGizmoColor;
                    if (MaxAngleDetection < 360)
                    {
                        //Left
                        Gizmos.DrawLine(transform.position + AngleVisualOffset, AngleVisualOffset + transform.position + (Quaternion.Euler(0, MaxAngleDetection / 2, 0) * (Vector3.forward * MaxDetectionDistance)));
                        //Right
                        Gizmos.DrawLine(transform.position + AngleVisualOffset, (AngleVisualOffset + transform.position + (Quaternion.Euler(0, -MaxAngleDetection / 2, 0) * (Vector3.forward * MaxDetectionDistance))));
                    }
                    else
                    {
                        Gizmos.DrawWireSphere(transform.position + AngleVisualOffset, MaxDetectionDistance);
                    }
                }


                //Distance to Player
                if (DrawDistanceToPlayerGizmo && CurrentTarget != null)
                {
                    Gizmos.color = DistanceToPlayerColor;
                    Gizmos.DrawLine(CurrentTarget.transform.position, CurrentTarget.transform.position + Vector3.Normalize(transform.position - CurrentTarget.transform.position));
                }
            }
        }
        #endregion
    }
}