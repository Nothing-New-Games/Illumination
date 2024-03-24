using Sirenix.Serialization;
using UnityEditorInternal;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace Assets.Entities.AI
{
    #region Animation Enum
    public enum AnimationType
    {
        /// <summary>
        /// This enum souly exists to say "Let us finish the current animation!"
        /// </summary>
        Default,
        PreventNewAnimation,
        Idle, Walk, Run, Attack, Push, Jumping, Falling, Dead, CrouchIdle, CrouchWalk, CrouchRun, Touch, Touchdown, Loot
    }
    #endregion
    public class AIHandler
    {
        private float _CurrentIdleTime = 0f;
        private float _ChosenIdleTime = 0f;

        /// <summary>
        /// Handles the AI and returns an enum of type AnimationType. This will be used to call the animation trigger for the model.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual AnimationType Handle(Alive entity)
        {
            //We're not running Engine.Handle for some reason when we should be. Need to investigate
            if (entity.PauseMovement/* || !entity.IsGrounded()*/)
                return AnimationType.Idle;

            if (entity._CurrentMovementSpeedValue == 0)
            {
                Debug.LogWarning("Dunno why this is happening, but I know how to work around it! =D");
                return AnimationType.Idle;
            }

            //Determine if the player is detected
            //If the player is detected, clear the idle timer and assign the player as a target.
            var closestLiving = entity.ClosestLiving();
            if (closestLiving != null)
                if (closestLiving.IsPlayer)
                    if (closestLiving.IsAlive)
                    {
                        if (SearchForTarget(entity, closestLiving))
                            entity.CurrentLivingTarget = Player.PlayerInstance.GetComponent<Player>();
                    }
                    else if (entity.CurrentLivingTarget == closestLiving)
                    {
                        entity.CurrentLivingTarget = null;
                    }


            #region Attack and Attack Animation
            //Check if we are close enough to the target to attack
            if (entity.CurrentLivingTarget != null)
                if (Vector3.Distance(entity.CurrentLivingTarget.transform.position, entity.transform.position) <= entity.MaxAttackOrInteractDistance)
                {
                    //Handle attacking the target.
                    if (!entity.IsPerformingAnimations(AnimationType.Attack) && entity.IsPerformingAnimations(AnimationType.Idle))
                    {
                        entity.CurrentLivingTarget.DealDamage(new DamageSource(entity.gameObject, Random.Range(entity.Damage.x, entity.Damage.y)));

                        //Return the attack animation.
                        return AnimationType.Attack;
                    }
                    else return AnimationType.Idle;
                }
            #endregion

            #region Idle Animation
            //Idle = Current location
            if (!entity.Engine.IsMoving() && _CurrentIdleTime < _ChosenIdleTime)
            {
                _CurrentIdleTime += Time.deltaTime;
                return AnimationType.Idle;
            }
            else if (_CurrentIdleTime >= _ChosenIdleTime)
            {
                _CurrentIdleTime = 0f;
                _ChosenIdleTime = Random.Range(entity.MinIdleDuration, entity.MaxIdleDuration);
            }
            #endregion

            #region Movement and Movement Animation
            bool IsWalking = entity._CurrentMovementSpeedValue == entity.BaseMovementSpeed;

            if (entity.Engine._StuckCheckCounter >= entity.MaxStuckCounter)
            {
                entity.Engine.UpdateVariables(entity, GetNewDestination(entity));
                Debug.LogWarning($"Engine has made the maximum allotted reports for being unable to move! Choosing a new destination!");
                entity.Engine._StuckCheckCounter = 0;
            }

            //If the movement speed is faster than the base speed
            if (!IsWalking && entity.CurrentLivingTarget != null)
            {
                entity.Engine.UpdateVariables(entity, entity.CurrentLivingTarget.transform.position);
            }
            //If movement speed is the base speed
            else if (IsWalking && entity.Engine.IsCloseToDestination())
            {
                //Calculate the new destination
                entity.Engine.UpdateVariables(entity, GetNewDestination(entity));
            }
            #endregion

            //Move to chosen destination.
            entity.Engine.HandleMovement();

            if (IsWalking && entity.Engine.LookingAtTarget)
                return AnimationType.Walk;
            else if (entity.Engine.LookingAtTarget) return AnimationType.Run;
            else return AnimationType.Idle;
        }

        private Vector3 GetNewDestination(Alive entity)
        {
            Vector3 newDestination;

            if (entity.FreeRoam)
                newDestination = new Vector3
                    (
                        Random.Range(-entity.MaxWanderDistXY.x, entity.MaxWanderDistXY.x),
                        0,
                        Random.Range(-entity.MaxWanderDistXY.y, entity.MaxWanderDistXY.y)
                    ) + entity.transform.position;
            else
            {
                newDestination = new Vector3
                    (
                        Random.Range(-entity.MaxWanderDistXY.x, entity.MaxWanderDistXY.x),
                        0,
                        Random.Range(-entity.MaxWanderDistXY.y, entity.MaxWanderDistXY.y)
                    ) + entity._SpawnLocation;
            }

            return newDestination;
        }

        
        /// <summary>
        /// Searches for the player and returns true if they have been detected.
        /// </summary>
        /// <param name="thisEntity">The entity searching for the player.</param>
        protected internal virtual bool SearchForTarget(Alive thisEntity, Alive DesiredTarget = null)
        {
            if (DesiredTarget == null)
                DesiredTarget = Player.PlayerInstance.GetComponent<Alive>();

            #region
            float sightDetectionChance;
            float hearingDetectionChance;
            float totalDetectionChance;
            #endregion

            float velocityFactor =
                Mathf.Clamp01(1f - (DesiredTarget.Velocity.magnitude / thisEntity.MaxSpeedDetection));
            float sightFactor =
                Mathf.Clamp01(1f -
                    (Vector3.Distance(thisEntity.transform.position, DesiredTarget.transform.position) / thisEntity.MaxSightDistanceToTarget));
            float hearingFactor
                = Mathf.Clamp01(1f -
                    (Vector3.Distance(thisEntity.transform.position, DesiredTarget.transform.position) / thisEntity.MaxHearingDistanceToTarget));
            float angleFactor
                = Mathf.Clamp01(1f -
                    Mathf.Abs(Vector3.Angle(thisEntity.transform.forward, DesiredTarget.transform.position)) / (thisEntity.MaxAngleDetection / 2f));

            sightDetectionChance = thisEntity.SightWeight * sightFactor;

            if (hearingFactor > 0)
                hearingFactor = Mathf.Clamp01(hearingFactor + velocityFactor);
            hearingDetectionChance = thisEntity.HearingWeight * hearingFactor;
            totalDetectionChance = thisEntity.BaseDetectionChance * sightDetectionChance + hearingDetectionChance;

            return Random.Range(0f, 100f) <= totalDetectionChance * angleFactor;
        }
    }
}