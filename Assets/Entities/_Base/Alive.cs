using Assets.EDO;
using Assets.Entities.AI;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Unity.VisualScripting;
using System;
using UnityEngine.UIElements;
using System.Linq;

namespace Assets.Entities
{
    [RequireComponent(typeof(Rigidbody))]
    public abstract class Alive : MonoBehaviour, ISender
    {
        #region Variables
        protected internal Rigidbody RB;

        protected internal Animator _Animator;
        /// <summary>
        /// If you are replacing the engine with a custom one, override the start method.
        /// </summary>
        protected internal MovementEngine Engine;
        /// <summary>
        /// If you are replacing the AI with a custom one, override the start method.
        /// </summary>
        protected internal AIHandler AI;

        private float meshHeight = 0f;
        public static List<Alive> LivingCreatures { get; } = new();
        public static void RegisterCreature(Alive creature)
        {
            if (LivingCreatures.Contains(creature))
                Debug.LogWarning($"Attempted to register {creature.name} while it is already in the list!");
            else LivingCreatures.Add(creature);
        }
        public static void Deregister(Alive creature)
        {
            if (LivingCreatures.Contains(creature))
                LivingCreatures.Remove(creature);
            else Debug.LogWarning($"Attempted to remove {creature.name} from the list of living creatures, but it does not exist!");
        }
        #endregion

        #region Inspector
        [ReadOnly, DisplayAsString]
        public AnimationType CurrentAnimation;
        [SerializeField]
        protected internal bool IsAlive = true;

        [TabGroup("Main", "Stats"), Button]
        protected internal void RegisterAlive()
        {
            RegisterCreature(this);
        }
        [TabGroup("Main", "Stats"), Button]
        protected internal void DeRegisterAlive()
        {
            Deregister(this);
        }

        [TabGroup("Main/Stats/StatsSubTabs", "Movement")]
        public bool PauseMovement;
        [TabGroup("Main/Stats/StatsSubTabs", "Movement")]
        public bool RunStuckChecks = true;
        [TabGroup("Main/Stats/StatsSubTabs", "Movement")]
        public float BaseMovementSpeed = 5f;
        [TabGroup("Main/Stats/StatsSubTabs", "Movement")]
        public float RunMovementMultiplier = 2f;
        [TabGroup("Main/Stats/StatsSubTabs", "Movement"), ShowIf("@IsPlayer == false")]
        public float MinDistanceToDestination = 1f;
        [TabGroup("Main/Stats/StatsSubTabs", "Movement"), ShowIf("@IsPlayer == false")]
        public Vector2 MaxWanderDistXY;
        [TabGroup("Main/Stats/StatsSubTabs", "Movement"), ShowIf("@IsPlayer == false")]
        public float MinIdleDuration = 3f;
        [TabGroup("Main/Stats/StatsSubTabs", "Movement"), ShowIf("@IsPlayer == false")]
        public float MaxIdleDuration = 15f;
        [TabGroup("Main/Stats/StatsSubTabs", "Movement"), ShowIf("@IsPlayer == false")]
        public float PositioningCorrectionDistance = 3f;
        protected internal float _CurrentMovementSpeedValue = 0f;


        [TabGroup("Main/Stats/StatsSubTabs", "Movement"), ShowIf("@IsPlayer == false")]
        public float StuckCheckInterval = 3f;

        #region
        private float LastMeasuredHearingWeight;
        private float LastMeasuredSightWeight;
        private void WeightAdjusted()
        {
            if (LastMeasuredHearingWeight != HearingWeight)
                SightWeight = 100 - HearingWeight;
            else if (LastMeasuredSightWeight != SightWeight)
                HearingWeight = 100 - SightWeight;

            LastMeasuredHearingWeight = HearingWeight;
            LastMeasuredSightWeight = SightWeight;
        }
        #endregion

        [TabGroup("Main/Stats/StatsSubTabs", "Targeting"), SerializeField, ShowIf("@IsPlayer == false")]
        [Tooltip("The distance the creature has to be in order to interact with or attack things.")]
        protected internal float MaxAttackOrInteractDistance = 30f;
        [TabGroup("Main/Stats/StatsSubTabs", "Targeting"), SerializeField, ShowIf("@IsPlayer == false && IsBlind == false")]
        public float MaxSightDistanceToTarget = 3f;
        [TabGroup("Main/Stats/StatsSubTabs", "Targeting"), SerializeField, ShowIf("@IsPlayer == false && IsDeaf == false")]
        public float MaxHearingDistanceToTarget = 3f;
        [OnValueChanged("WeightAdjusted"), Range(0, 100), ShowIf("@IsBlind == false && IsPlayer == false"), TabGroup("Main/Stats/StatsSubTabs", "Targeting")]
        public float SightWeight = 50f;
        [OnValueChanged("WeightAdjusted"), Range(0, 100), ShowIf("@IsDeaf == false && IsPlayer == false"), TabGroup("Main/Stats/StatsSubTabs", "Targeting")]
        public float HearingWeight = 50f;
        [TabGroup("Main/Stats/StatsSubTabs", "Targeting"), Range(1, 360), SerializeField]
        [Tooltip("The creature's ability to see things around them."), ShowIf("@IsPlayer == false")]
        protected internal float MaxAngleDetection = 20;
        [TabGroup("Main/Stats/StatsSubTabs", "Targeting"), Range(1, 360)]
        public float MaxTargetingDegrees = 40f;
        [TabGroup("Main/Stats/StatsSubTabs", "Targeting"), ShowIf("@IsDeaf == true && IsPlayer == false")]
        public float MaxSpeedDetection = 10f;
        [TabGroup("Main/Stats/StatsSubTabs", "Targeting"), SerializeField, ShowIf("@IsPlayer == false"), Range(0, 100)]
        [Tooltip("The creatures base chance to detect the player. The higher it is, the more likely it is.")]
        protected internal float BaseDetectionChance = 100f;

        [SerializeField]
        [TabGroup("Main/Stats/StatsSubTabs", "Health"), Tooltip("The current health of the entity.")]
        protected internal float CurrentHealth = 1f;
        [SerializeField]
        [TabGroup("Main/Stats/StatsSubTabs", "Health"), Tooltip("The max health the entity will have.")]
        protected internal float MaxHealth = 1f;
        [SerializeField]
        [TabGroup("Main/Stats/StatsSubTabs", "Health"), Tooltip("Min/Max damage the entity can do with an attack.")]
        protected internal Vector2 Damage = new Vector2(1, 1);


        [TabGroup("Main/Stats/StatsSubTabs", "Targeting"), OnValueChanged("IsBlindToggle"), SerializeField]
        protected internal bool IsBlind = false;
        #region
        [TabGroup("Main/Stats/StatsSubTabs", "Targeting"), ReadOnly, DisplayAsString, HideLabel, ShowInInspector]
        public const string dumb = "and";
        #endregion
        [TabGroup("Main/Stats/StatsSubTabs", "Targeting"), OnValueChanged("IsDeafToggle"), SerializeField]
        protected internal bool IsDeaf = false;
        #region
        [TabGroup("Main/Stats/StatsSubTabs", "Targeting"), ReadOnly, DisplayAsString, HideLabel, ShowInInspector]
        public const string moreDumb = "and born to follow";
        #endregion
        #endregion

        #region Methods
        private float _PreviousHearingDistance;
        private float _PreviousSightDistance;
        private void IsDeafToggle()
        {
            if (IsDeaf)
            {
                _PreviousHearingDistance = MaxHearingDistanceToTarget;
                MaxHearingDistanceToTarget = 0f;
            }
            else
                MaxHearingDistanceToTarget = _PreviousHearingDistance;
        }
        public Alive ClosestLiving()
        {
            Alive closest = null;
            float lastRecordedDistance = 0f;
            foreach (var creature in Alive.LivingCreatures)
            {
                if (Vector3.Distance(transform.position, creature.transform.position) < lastRecordedDistance)
                {
                    lastRecordedDistance = Vector3.Distance(transform.position, closest.transform.position);
                    closest = creature;
                }
            }

            return closest;
        }
        private void IsBlindToggle()
        {
            if (IsBlind)
            {
                _PreviousSightDistance = MaxSightDistanceToTarget;
                MaxSightDistanceToTarget = 0f;
            }
            else
                MaxSightDistanceToTarget = _PreviousSightDistance;
        }

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
                return LayerMask.LayerToName(hit.transform.gameObject.layer) == "Ground";
                //return hit.transform.tag == "Walkable";
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

        protected internal void UpdateVelocityDebug()
        {
            Velocity = RB.velocity;
        }

        internal bool IsPerformingAnimation(AnimationType animation)
        {
            return _Animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.ToLower().Contains(animation.ToString().ToLower());
        }
        internal virtual void CreateAI() => AI = new();

        internal virtual void CreateEngine() => Engine = new();

        Vector3 _PreviousPositionForStuckCheck;
        LayerMask _ObstacleLayer;
        internal virtual bool CheckIfStuck()
        {
            if (IsPerformingAnimation(AnimationType.Idle))
                return false;

            Vector3 currentPosition = RB.position;
            float distanceMoved = Vector3.Distance(_PreviousPositionForStuckCheck, currentPosition);

            if (distanceMoved < 0.1f) // Adjust threshold as needed
            {
                AI.GetUnstuck(this);
                Debug.LogWarning("NPC is stuck due to lack of movement!");
                return true;
            }
            RaycastHit hit;

            if (Physics.Raycast(transform.position, transform.forward, out hit, PositioningCorrectionDistance, _ObstacleLayer))
            {
                if (PrintStuckCorrectionLogs)
                    Debug.LogWarning($"NPC is stuck due to continuous collision with {hit.transform.name}!");
                AI.GetUnstuck(this);
            }

            _PreviousPositionForStuckCheck = currentPosition;
            return false;
        }

        private void UpdateInteractableTargetDisplay()
        {
            if (CurrentInteractTarget == null)
                InspectorDisplayForCurrentInteractableTarget = "Null";
            else
                InspectorDisplayForCurrentInteractableTarget = CurrentInteractTarget.GetComponent<Transform>().name;
        }

        protected internal IInteractable CurrentInteractTarget;


        protected internal virtual void UpdateNearbyCreaturesList()
        {
            NearbyCreatures.Clear();

            foreach (var creature in LivingCreatures)
            {
                if (creature == this) continue;

                if (MaxSightDistanceToTarget > MaxHearingDistanceToTarget)
                {
                    if (Vector3.Distance(transform.position, creature.transform.position) <= MaxSightDistanceToTarget)
                        NearbyCreatures.Add(creature);
                }
                else if (MaxHearingDistanceToTarget > MaxSightDistanceToTarget)
                {
                    if (Vector3.Distance(transform.position, creature.transform.position) <= MaxHearingDistanceToTarget)
                        NearbyCreatures.Add(creature);
                }
                else if (DrawNearbyCreaturesGizmo) Debug.Log($"{name} IS DEAF AND BLIND AND DUMB AND BORN TO FOLLOW!");
            }
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
                catch (Exception ex)
                {
                    Debug.LogError($"Error when trying to get transform for {interactable}!\n{ex.Message}");
                }
                if (interactableTransform == null)
                {
                    Debug.LogWarning($"Warning! Could not find transform for {interactable}!\n" +
                        $"Skipping call!");
                    continue;
                }

                if (Vector3.Distance(transform.position, interactableTransform.position) <= MaxSightDistanceToTarget)
                {
                    NearbyInteractables.Add(interactable);
                }
            }
        }

        internal protected virtual float DegreesFromForward(Transform TargetInQuestion)
        {
            Vector3 targetDirection = TargetInQuestion.position - transform.position;

            float angle = Vector3.Angle(targetDirection, transform.forward);

            return angle;
        }
        internal protected virtual IInteractable NearestInteractable()
        {
            float degreesFromTarget;
            foreach (var interactable in NearbyInteractables)
            {
                degreesFromTarget = DegreesFromForward(interactable.GetComponent<Transform>());

                if (degreesFromTarget <= MaxTargetingDegrees / 2)
                    return interactable;
            }

            return null;
        }
        #endregion

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
            RB = GetComponent<Rigidbody>();
            _Animator = GetComponent<Animator>();
            RegisterCreature(this);
            _CurrentMovementSpeedValue = BaseMovementSpeed;
            CreateAI();
            CreateEngine();
            Engine.UpdateVariables(this, transform.position);
            FindEntityHeight();
            _ObstacleLayer = ~LayerMask.GetMask(LayerMask.LayerToName(gameObject.layer));
        }
        internal virtual void Start()
        {

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
            UpdateVelocityDebug();
            _DebugCurrentDestination = Engine.GetCurrentDest;

            if (CurrentLivingTarget)
                TargetAngleDebug = DegreesFromForward(CurrentLivingTarget.GetComponent<Transform>()) * 2;
            else if (CurrentInteractTarget != null)
                TargetAngleDebug = DegreesFromForward(CurrentInteractTarget.GetComponent<Transform>()) * 2;
            else TargetAngleDebug = 0;

        }
        internal virtual void LateUpdate()
        {
            
        }
        internal virtual void FixedUpdate()
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

            try
            {
                if (!IsPerformingAnimation(CurrentAnimation))
                {
                    //Debug.Log("Good news, everyone!");
                    _Animator.SetTrigger(CurrentAnimation.ToString());
                }
            }
            catch { }


            if (RunStuckChecks)
            {
                _StuckCheckTimer += Time.deltaTime;
                if (_StuckCheckTimer >= StuckCheckInterval)
                {
                    IsStuck = CheckIfStuck();
                    _StuckCheckTimer = 0;
                }
            }
        }
        internal virtual void OnDestroy()
        {
            Deregister(this);
        }
        internal virtual void OnDisable()
        {
            Deregister(this);
        }
        internal virtual void OnEnable()
        {
            RegisterCreature(this);
        }
        #endregion

        #region Debug
        #region Debug Display
        [TabGroup("Main", "Debug"), FoldoutGroup("Main/Debug/Debug Data"), ShowInInspector, DisplayAsString]
        protected internal List<Alive> NearbyCreatures { get; } = new();

        [FoldoutGroup("Main/Debug/Debug Data"), ShowInInspector, DisplayAsString]
        protected internal List<IInteractable> NearbyInteractables { get; } = new();

        [SerializeField]
        [FoldoutGroup("Main/Debug/Debug Data"), DisplayAsString]
        protected internal Alive CurrentLivingTarget/* { get; set; }*/;

        [FoldoutGroup("Main/Debug/Debug Data"), SerializeField, DisplayAsString, LabelText("CurrentInteractTarget")]
        private string InspectorDisplayForCurrentInteractableTarget = "Null";


        [FoldoutGroup("Main/Debug/Debug Data"), DisplayAsString, SerializeField, ShowIf("@IsPlayer == true")]
        protected internal bool HasInteractableTarget = false;


        [FoldoutGroup("Main/Debug/Debug Data"), DisplayAsString]
        public Vector3 Velocity;
        [SerializeField, ShowIf("@IsPlayer == false")]
        [FoldoutGroup("Main/Debug/Debug Data"), DisplayAsString]
        protected internal Vector3 _DebugCurrentDestination;

        [FoldoutGroup("Main/Debug/Debug Data"), ReadOnly, Range(0, 360)]
        public float TargetAngleDebug;

        [FoldoutGroup("Main/Debug/Debug Data"), DisplayAsString]
        public bool IsStuck = false;
        [FoldoutGroup("Main/Debug/Debug Data"), DisplayAsString, SerializeField]
        private float _StuckCheckTimer = 0f;


        [FoldoutGroup("Main/Debug/Debug Data"), ShowInInspector, DisplayAsString]
        internal bool IsPlayer =>
            typeof(Player) == this.GetType();
        #endregion

        [ReadOnly, HideLabel, DisplayAsString, TabGroup("Main", "Debug")]
        public string SpaceTheFinalFrontier = " ";

        #region Debug Variables
        [BoxGroup("Main/Debug/Debug Toggles")]
        public bool DebugData = false;

        [Indent, BoxGroup("Main/Debug/Debug Toggles"), ShowIf("@DebugData == true && IsPlayer == false")]
        public bool DrawDestinationGizmo = false;
        [Indent(2), ShowIf("@DebugData == true && DrawDestinationGizmo && IsPlayer == false"), BoxGroup("Main/Debug/Debug Toggles")]
        public float DestinationGizmoSize = 1f;
        [Indent(2), ShowIf("@DebugData == true && DrawDestinationGizmo == true && IsPlayer == false"), BoxGroup("Main/Debug/Debug Toggles")]
        public Color DestinationColor = Color.cyan;

        [Indent, ShowIf("@DebugData == true && IsPlayer == false"), BoxGroup("Main/Debug/Debug Toggles")]
        public bool DrawDestinationDistanceGizmo = false;
        [Indent(2), ShowIf("@DebugData == true && DrawDestinationDistanceGizmo == true && IsPlayer == false"), BoxGroup("Main/Debug/Debug Toggles")]
        public Color DestinationDistanceColor = Color.red;

        [Indent, ShowIf("@DebugData == true && IsPlayer == false"), BoxGroup("Main/Debug/Debug Toggles")]
        public bool DrawSightAngles = false;
        [Indent(2), ShowIf("@DebugData == true && DrawSightAngles"), BoxGroup("Main/Debug/Debug Toggles")]
        public Vector3 AngleVisualOffset = new();
        [Indent(2), ShowIf("@DebugData == true && DrawSightAngles"), BoxGroup("Main/Debug/Debug Toggles")]
        public Color SightGizmoColor = Color.yellow;
        
        [Indent, ShowIf("@DebugData == true"), BoxGroup("Main/Debug/Debug Toggles")]
        public bool DrawInteractAngles = false;
        [Indent(2), ShowIf("@DebugData == true && DrawInteractAngles"), BoxGroup("Main/Debug/Debug Toggles")]
        public Vector3 InteractVisualOffset = new();
        [Indent(2), ShowIf("@DebugData == true && DrawInteractAngles"), BoxGroup("Main/Debug/Debug Toggles")]
        public Color InteractGizmoColor = Color.yellow;

        [Indent, ShowIf("@DebugData == true && IsPlayer == false"), BoxGroup("Main/Debug/Debug Toggles")]
        public bool DrawDistanceToPlayerGizmo = false;
        [Indent(2), ShowIf("@DebugData == true && DrawDistanceToPlayerGizmo == true && IsPlayer == false"), BoxGroup("Main/Debug/Debug Toggles")]
        public Color DistanceToPlayerColor = Color.blue;

        [Indent, ShowIf("@DebugData == true"), BoxGroup("Main/Debug/Debug Toggles")]
        public bool DrawNearbyCreaturesGizmo = false;
        [Indent(2), ShowIf("@DebugData == true && DrawNearbyCreaturesGizmo == true"), BoxGroup("Main/Debug/Debug Toggles")]
        public Color NearbyCreaturesColor = Color.red;

        [Indent, ShowIf("@DebugData == true"), BoxGroup("Main/Debug/Debug Toggles")]
        public bool DrawFeetPositionCalculation = false;
        [Indent(2), ShowIf("@DebugData == true && DrawFeetPositionCalculation == true"), BoxGroup("Main/Debug/Debug Toggles")]
        public float FeetPositionGizmoSize = 0.3f;
        [Indent(2), ShowIf("@DebugData == true && DrawFeetPositionCalculation == true"), BoxGroup("Main/Debug/Debug Toggles")]
        public Color FeetPositionColor = Color.white;

        [Indent, ShowIf("@DebugData == true && IsPlayer == false"), BoxGroup("Main/Debug/Debug Toggles")]
        public bool PrintStuckCorrectionLogs = false;
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
                        Gizmos.DrawLine(transform.position + AngleVisualOffset, AngleVisualOffset + transform.position + (Quaternion.Euler(0, MaxAngleDetection / 2, 0) * (transform.forward * MaxSightDistanceToTarget)));
                        //Right
                        Gizmos.DrawLine(transform.position + AngleVisualOffset, (AngleVisualOffset + transform.position + (Quaternion.Euler(0, -MaxAngleDetection / 2, 0) * (transform.forward * MaxSightDistanceToTarget))));
                    }
                    else
                    {
                        Gizmos.DrawWireSphere(transform.position + AngleVisualOffset, MaxHearingDistanceToTarget);
                    }
                }

                //Interact Angles
                if (DrawInteractAngles)
                {
                    Gizmos.color = InteractGizmoColor;
                    if (MaxTargetingDegrees < 360)
                    {
                        //Left
                        Gizmos.DrawLine(transform.position + InteractVisualOffset, InteractVisualOffset + transform.position + (Quaternion.Euler(0, MaxTargetingDegrees / 2, 0) * (transform.forward * MaxAttackOrInteractDistance)));
                        //Left
                        Gizmos.DrawLine(transform.position + InteractVisualOffset, InteractVisualOffset + transform.position + (Quaternion.Euler(0, -MaxTargetingDegrees / 2, 0) * (transform.forward * MaxAttackOrInteractDistance)));
                    }
                    else
                    {
                        Gizmos.DrawWireSphere(transform.position + InteractVisualOffset, MaxAttackOrInteractDistance);
                    }
                }


                //Distance to Player
                if (DrawDistanceToPlayerGizmo && Player.PlayerInstance != null)
                {
                    Gizmos.color = DistanceToPlayerColor;
                    Gizmos.DrawLine(transform.position, Player.PlayerInstance.position);
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
    }
}