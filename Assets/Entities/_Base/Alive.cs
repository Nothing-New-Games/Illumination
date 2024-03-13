using Assets.EDO;
using Assets.Entities.AI;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Unity.VisualScripting;
using System;

namespace Assets.Entities
{
    [RequireComponent(typeof(CharacterController))]
    public abstract class Alive : MonoBehaviour, ISender
    {
        public static List<Alive> LivingCreatures { get; } = new();
        [ShowInInspector, DisplayAsString, TabGroup("Main", "Debug Data")]
        protected internal List<Alive> NearbyCreatures { get; } = new();

        [ShowInInspector, DisplayAsString, TabGroup("Main", "Debug Data")]
        protected internal List<IInteractable> NearbyInteractables { get; } = new();

        protected internal virtual void UpdateNearbyCreaturesList()
        {
            NearbyCreatures.Clear();

            foreach (var creature in LivingCreatures)
            {
                if (creature == this) continue;

                if (Vector3.Distance(transform.position, creature.transform.position) <= MinDistanceToTarget)
                {
                    NearbyCreatures.Add(creature);
                }
            }
        }

        [Button]
        protected internal void RegisterAlive()
        {
            if (!LivingCreatures.Contains(this))
                LivingCreatures.Add(this);
            else Debug.LogWarning($"{name} is already registered as a living creature!");
        }

        protected internal virtual void UpdateNearbyInteractablesList()
        {
            NearbyInteractables.Clear();

            foreach (var interactable in IInteractable.Interactables)
            {
                Transform interactableTransform = null;
                try
                {
                    interactableTransform = interactable.GetComponent<Transform>();
                }
                catch(Exception ex)
                {
                    Debug.LogError($"Error when trying to get transform for {interactable}!\n{ex.Message}");
                }
                if (interactableTransform == null)
                {
                    Debug.LogWarning($"Warning! Could not find transform for {interactable}!\n" +
                        $"Skipping call!");
                    continue;
                }

                if (Vector3.Distance(transform.position, interactableTransform.position) <= MinDistanceToTarget)
                {
                    NearbyInteractables.Add(interactable);
                }
            }
        }

        internal protected virtual float DegreesFromForward(Transform TargetInQuestion) => 
            Vector3.Angle(transform.position + transform.forward, transform.position + TargetInQuestion.position - transform.forward);

        internal protected virtual IInteractable NearestInteractable()
        {
            foreach (var interactable in NearbyInteractables)
            {
                if (DegreesFromForward(interactable.GetComponent<Transform>()) <= MinTargettingDegrees)
                    return interactable;
            }

            return null;
        }

        #region Variables
        protected internal CharacterController Controller;

        protected internal Animator _Animator;
        [ReadOnly, DisplayAsString]
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
        [TabGroup("Main", "Movement"), ShowIf("@IsPlayer == false")]
        public float MinDistanceToDestination = 1f;
        [TabGroup("Main", "Movement"), ShowIf("@IsPlayer == false")]
        public Vector2 MaxWanderDistXY;
        [TabGroup("Main", "Movement"), ShowIf("@IsPlayer == false")]
        public float MinIdleDuration = 3f;
        [TabGroup("Main", "Movement"), ShowIf("@IsPlayer == false")]
        public float MaxIdleDuration = 15f;
        [TabGroup("Main", "Movement"), ShowIf("@IsPlayer == false")]
        public float PositioningCorrectionDistance = 3f;
        protected internal float _CurrentMovementSpeedValue = 0f;

        [SerializeField]
        [TabGroup("Main", "Targetting"), DisplayAsString]
        protected internal Alive CurrentLivingTarget/* { get; set; }*/;


        [SerializeField, TabGroup("Main", "Targetting"), DisplayAsString, LabelText("CurrentInteractTarget")]
        private string InspectorDisplayForCurrentInteractableTarget = "Null";
        private void UpdateInteractableTargetDisplay()
        {
            if (CurrentInteractTarget == null)
                InspectorDisplayForCurrentInteractableTarget = "Null";
            else
                InspectorDisplayForCurrentInteractableTarget = CurrentInteractTarget.GetComponent<Transform>().name;
        }
        [DisplayAsString, TabGroup("Main", "Debug Data"), SerializeField]
        protected internal bool HasInteractableTarget = false;

        protected internal IInteractable CurrentInteractTarget;

        [TabGroup("Main", "Targetting")]
        public float MinDistanceToTarget = 3f;
        [TabGroup("Main", "Targetting"), Range(1, 360), SerializeField]
        [Tooltip("The creature's ability to see things around them. 180 = complete 360 vision (Don't ask, that's how Unity works), 0 = Blind.")]
        protected internal float MaxAngleDetection = 20;
        [TabGroup("Main", "Targetting"), Range(1, 360)]
        public float MinTargettingDegrees = 40f;
        [TabGroup("Main", "Targetting"), SerializeField, ShowIf("@IsPlayer == false")]
        [Tooltip("The creature's ability to see things in front of them.")]
        protected internal float MaxDetectionDistance = 30f;
        [TabGroup("Main", "Targetting"), SerializeField, ShowIf("@IsPlayer == false")]
        [Tooltip("The creatures base chance to detect the player. The higher it is, the more likely it is.")]
        protected internal float BaseDetectionChance = 100f;

        [SerializeField]
        [TabGroup("Main", "Stats"), Tooltip("The current health of the entity.")]
        protected internal float CurrentHealth = 1f;
        [SerializeField]
        [TabGroup("Main", "Stats"), Tooltip("The max health the entity will have.")]
        protected internal float MaxHealth = 1f;
        [SerializeField]
        [TabGroup("Main", "Stats"), Tooltip("Min/Max damage the entity can do with an attack.")]
        protected internal Vector2 Damage = new Vector2(1, 1);

        private float meshHeight = 0f;


        [TabGroup("Main", "Debug Data"), ShowInInspector, DisplayAsString]
        internal bool IsPlayer =>
            typeof(Player) == this.GetType();

        #endregion


        protected internal bool AnimatorIsPlaying()
        {
            if (_Animator != null)
                return _Animator.GetCurrentAnimatorStateInfo(0).length >
                   _Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            
            return true;
        }
        protected internal bool IsGrounded()
        {
            Ray ray = new Ray(transform.position, -transform.up);
            RaycastHit hit;
            Physics.Raycast(ray, out hit, 0.3f + meshHeight /2);
            if (hit.transform != null)
                return hit.transform.tag == "Walkable";
            else return false;
        }
        protected internal void FindEntityHeight()
        {
            Collider collider = GetComponentInChildren<Collider>();
            if (collider == null)
            {
                Debug.LogWarning("Could not find mesh renderer! One last attempt to grab it!");
                collider = GetComponent<Collider>();
            }

            meshHeight = collider.bounds.size.y;
        }
        protected internal bool CloseToTarget()
        {
            if (CurrentLivingTarget != null)
                return Vector3.Distance(transform.position, CurrentLivingTarget.transform.position) <= MinDistanceToDestination;
            
            return false;
        }

        public abstract void DealDamage(DamageSource source);

        [TabGroup("Main", "Debug Data"), DisplayAsString]
        public Vector3 Velocity;
        private Vector3 LastPosition { get; set; } = Vector3.zero;
        protected internal void CurrentVelocity()
        {
            if (LastPosition == Vector3.zero) 
            {
                LastPosition = transform.position;
                Velocity = Vector3.zero;
            }

            Vector3 currentVelocity =  (transform.position - LastPosition) / Time.deltaTime;
            LastPosition = transform.position;

            Velocity = currentVelocity;
        }


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
            FindEntityHeight();
            LivingCreatures.Add(this);
        }

        internal virtual void CreateAI() => AI = new();

        internal virtual void CreateEngine() => Engine = new();

        internal virtual void Start()
        {
            if (!LivingCreatures.Contains(this))
                LivingCreatures.Add(this);
        }
        internal virtual void Update()
        {
            UpdateNearbyCreaturesList();
            UpdateNearbyInteractablesList();

            if (!IsAlive) return;
            else if (CurrentHealth <= 0) FireEvent(); //Handled by Alive to trigger death.
            Engine.UpdateVariables(this);
            UpdateInteractableTargetDisplay();
            HasInteractableTarget = CurrentInteractTarget != null;
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
            LivingCreatures.Remove(this);
        }
        internal virtual void OnDisable()
        {
            LivingCreatures.Remove(this);
        }
        internal virtual void OnEnable()
        {
            LivingCreatures.Add(this);
        }


        #region Debug
        #region Debug Variables
        [TabGroup("Main", "Debug")]
        public bool DebugData = false;

        [TabGroup("Main", "Debug"), ShowIf("@DebugData == true && IsPlayer == false")]
        public bool DrawDestinationGizmo = false;
        [ShowIf("@DebugData == true && DrawDestinationGizmo && IsPlayer == false"), TabGroup("Main", "Debug")]
        public float DestinationGizmoSize = 1f;
        [ShowIf("@DebugData == true && DrawDestinationGizmo == true && IsPlayer == false"), TabGroup("Main", "Debug")]
        public Color DestinationColor = Color.cyan;

        [ShowIf("@DebugData == true && DrawDestinationGizmo == true && IsPlayer == false"), TabGroup("Main", "Debug")]
        public bool DrawDestinationDistanceGizmo = false;
        [ShowIf("@DebugData == true && DrawDestinationDistanceGizmo == true && IsPlayer == false"), TabGroup("Main", "Debug")]
        public Color DestinationDistanceColor = Color.red;

        [ShowIf("@DebugData == true"), TabGroup("Main", "Debug")]
        public bool DrawSightAngles = false;
        [ShowIf("@DebugData == true && DrawSightAngles"), TabGroup("Main", "Debug")]
        public Vector3 AngleVisualOffset = new();
        [ShowIf("@DebugData == true && DrawSightAngles"), TabGroup("Main", "Debug")]
        public Color SightGizmoColor = Color.yellow;
        
        [ShowIf("@DebugData == true"), TabGroup("Main", "Debug")]
        public bool DrawInteractAngles = false;
        [ShowIf("@DebugData == true && DrawInteractAngles"), TabGroup("Main", "Debug")]
        public Vector3 InteractVisualOffset = new();
        [ShowIf("@DebugData == true && DrawInteractAngles"), TabGroup("Main", "Debug")]
        public Color InteractGizmoColor = Color.yellow;

        [ShowIf("@DebugData == true && IsPlayer == false"), TabGroup("Main", "Debug")]
        public bool DrawDistanceToPlayerGizmo = false;
        [ShowIf("@DebugData == true && DrawDistanceToPlayerGizmo == true && IsPlayer == false"), TabGroup("Main", "Debug")]
        public Color DistanceToPlayerColor = Color.blue;

        [ShowIf("@DebugData == true"), TabGroup("Main", "Debug")]
        public bool DrawNearbyCreaturesGizmo = false;
        [ShowIf("@DebugData == true && DrawNearbyCreaturesGizmo"), TabGroup("Main", "Debug")]
        public Color NearbyCreaturesColor = Color.red;

        [ShowIf("@DebugData == true"), TabGroup("Main", "Debug")]
        public bool DrawFeetPositionCalculation = false;
        [ShowIf("@DebugData == true"), TabGroup("Main", "Debug")]
        public float FeetPositionGizmoSize = 0.3f;
        [ShowIf("@DebugData == true && DrawFeetPositionCalculation"), TabGroup("Main", "Debug")]
        public Color FeetPositionColor = Color.white;
        #endregion


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
                        Gizmos.DrawLine(transform.position + AngleVisualOffset, AngleVisualOffset + transform.position + (Quaternion.Euler(0, MaxAngleDetection / 2, 0) * (transform.forward * MaxDetectionDistance)));
                        //Right
                        Gizmos.DrawLine(transform.position + AngleVisualOffset, (AngleVisualOffset + transform.position + (Quaternion.Euler(0, -MaxAngleDetection / 2, 0) * (transform.forward * MaxDetectionDistance))));
                    }
                    else
                    {
                        Gizmos.DrawWireSphere(transform.position + AngleVisualOffset, MaxDetectionDistance);
                    }
                }

                //Interact Angles
                if (DrawInteractAngles)
                {
                    Gizmos.color = InteractGizmoColor;
                    if (MinTargettingDegrees < 360)
                    {
                        //Left
                        Gizmos.DrawLine(transform.position + InteractVisualOffset, InteractVisualOffset + transform.position + (Quaternion.Euler(0, MinTargettingDegrees / 2, 0) * (transform.forward * MinDistanceToTarget)));
                        //Left
                        Gizmos.DrawLine(transform.position + InteractVisualOffset, InteractVisualOffset + transform.position + (Quaternion.Euler(0, -MinTargettingDegrees / 2, 0) * (transform.forward * MinDistanceToTarget)));
                    }
                    else
                    {
                        Gizmos.DrawWireSphere(transform.position + InteractVisualOffset, MinDistanceToTarget);
                    }
                }


                //Distance to Player
                if (DrawDistanceToPlayerGizmo && CurrentLivingTarget != null)
                {
                    Gizmos.color = DistanceToPlayerColor;
                    Gizmos.DrawLine(CurrentLivingTarget.transform.position, CurrentLivingTarget.transform.position + Vector3.Normalize(transform.position - CurrentLivingTarget.transform.position));
                }

                //Distance to Nearby Creatures
                if (DrawNearbyCreaturesGizmo && NearbyCreatures.Count > 0)
                {
                    Gizmos.color = NearbyCreaturesColor;
                    foreach(var creature in NearbyCreatures)
                    {
                        Gizmos.DrawLine(transform.position, creature.transform.position);
                    }
                }

                if (DrawFeetPositionCalculation)
                {
                    Gizmos.color = FeetPositionColor;
                    Gizmos.DrawWireSphere(new Vector3(transform.position.x, transform.position.y - (meshHeight /2), transform.position.z), FeetPositionGizmoSize);
                }
            }
        }
        #endregion
        #endregion
    }
}