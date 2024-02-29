using Sirenix.Serialization;
using UnityEngine;

namespace Assets.Entities.AI
{
    #region Animation Enum
    public enum AnimationTrigger
    {
        Idle, Walk, Run, Attack, Push, Jumping, Falling, Dead, IdleLong
    }
    #endregion
    public class AIHandler
    {
        private float _CurrentIdleTime = 0f;
        private float _ChosenIdleDuration = 0f;

        public AnimationTrigger Handle(Alive entity)
        {
            //Idle = Current location
            if (!entity.Engine.IsMoving && _CurrentIdleTime < _ChosenIdleDuration)
            {
                _CurrentIdleTime += Time.deltaTime;
                return AnimationTrigger.Idle;
            }
            else if (_CurrentIdleTime >= _ChosenIdleDuration)
            {
                _CurrentIdleTime = 0f;
                _ChosenIdleDuration = Random.Range(entity.MinIdleDuration, entity.MaxIdleDuration);
            }

            Vector3 NewDestination = Vector3.zero;
            bool IsWalking = true;

            //If the movement speed is faster than the base speed
            if (entity._CurrentMovementSpeed > entity.BaseMovementSpeed)
            {
                NewDestination = entity.CurrentTarget.transform.position;
                IsWalking = false;
            }
            //If movement speed is the base speed
            else if (entity._CurrentMovementSpeed == entity.BaseMovementSpeed)
            {
                //Engine should be called here.
                //Takes arguments for Destination. 
                //First calculate the destination.
                NewDestination = GetNewDestination(entity); //Using this until we find out how we actually wanna calculate.
            }
            //Check if we are close enough to the target to attack
            if (entity.CurrentTarget != null)
                if (Vector3.Distance(entity.CurrentTarget.transform.position, entity.transform.position) <= entity.MinDistanceToTarget)
                {
                    //Handle attacking the target.
                    //Call method from Entity.Attack?

                    //Return the attack animation.
                    return AnimationTrigger.Attack;
                }


            //Move to target destination.
            entity.Engine.MoveTo(NewDestination, entity);

            if (IsWalking)
                return AnimationTrigger.Walk;
            else return AnimationTrigger.Run;
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
    }
}