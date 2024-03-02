using Sirenix.Serialization;
using UnityEditorInternal;
using UnityEngine;

namespace Assets.Entities.AI
{
    #region Animation Enum
    public enum AnimationType
    {
        Idle, Walk, Run, Attack, Push, Jumping, Falling, Dead, IdleLong
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
            if (entity.PauseMovement)
                return AnimationType.Idle;

            if (entity._CurrentMovementSpeedValue == 0)
            {
                Debug.LogWarning("Dunno why this is happening, but I know how to work around it! =D");
                return AnimationType.Idle;
            }

            //Determine if the player is detected
            //If the player is detected, clear the idle timer and assign the player as a target.
            SearchForTarget(entity);

            #region Idle Animation
            //Idle = Current location
            if (!entity.Engine.IsMoving && _CurrentIdleTime < _ChosenIdleTime)
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

            //If the movement speed is faster than the base speed
            if (!IsWalking && entity.CurrentTarget != null)
            {
                entity.Engine.UpdateVariables(entity, entity.CurrentTarget.transform.position);
            }
            //If movement speed is the base speed
            else if (IsWalking && entity.Engine.IsCloseToDestination())
            {
                //Calculate the new destination
                entity.Engine.UpdateVariables(entity, GetNewDestination(entity));
            }
            #endregion

            #region Attack and Attack Animation
            //Check if we are close enough to the target to attack
            if (entity.CurrentTarget != null)
                if (Vector3.Distance(entity.CurrentTarget.transform.position, entity.transform.position) <= entity.MinDistanceToTarget)
                {
                    //Handle attacking the target.
                    //Call method from Entity.Attack?
                    entity.CurrentTarget.DealDamage(new DamageSource(entity.gameObject, Random.Range(entity.Damage.x, entity.Damage.y)));

                    //Return the attack animation.
                    return AnimationType.Attack;
                }
            #endregion


            //Move to chosen destination.
            entity.Engine.MoveToDestination();

            if (IsWalking)
                return AnimationType.Walk;
            else return AnimationType.Run;
        }

        private Vector3 GetNewDestination(Alive entity)
        {
            Vector3 NewDestination = new Vector3
                (
                    Random.Range(-entity.MaxWanderDistXY.x, entity.MaxWanderDistXY.x),
                    0,
                    Random.Range(-entity.MaxWanderDistXY.y, entity.MaxWanderDistXY.y)                 
                ) + entity.transform.position;

            return NewDestination;
        }

        /// <summary>
        /// Searches for the player and returns true if they have been detected.
        /// </summary>
        /// <param name="entity">The entity searching for the player.</param>
        protected internal virtual bool SearchForTarget(Alive entity)
        {
            //Define the chance of detecting the player. We will take away from this value the less likely the entity is to detect them.
            float DetectionChance = entity.BaseDetectionChance;
            float FailureChance = 0f;

            //Start by getting the distance to the player.
            float DistanceToPlayer = Vector3.Distance(entity.transform.position, Player.PlayerInstance.position);
            float AngleToPlayer = Vector3.Angle(entity.transform.forward, entity.transform.position - Player.PlayerInstance.position);

            if (DistanceToPlayer < entity.MaxDetectionDistance)
                FailureChance = DetectionChance;
            if (AngleToPlayer > entity.MaxAngleDetection)
                FailureChance += 30f;

            DetectionChance -= FailureChance;

            if (DetectionChance <= 0f)
                return false;

            float DetectionAttempt = Random.Range(0, DetectionChance);
            bool PlayerDetected = DetectionChance / entity.BaseDetectionChance >= DetectionAttempt;

            Debug.Log($"The current Manhattan distance from the player to {entity.name} is {DistanceToPlayer} at an angle of {AngleToPlayer}.\nWas the player found with a chance of {DetectionChance}? {PlayerDetected}");

            return PlayerDetected;
        }
    }
}