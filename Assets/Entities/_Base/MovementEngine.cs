using UnityEngine;

namespace Assets.Entities.AI
{
    public class MovementEngine
    {
        private float MinDistanceToDest { get; set; }
        private Vector3 CurrentDestination { get; set; }
        private Vector3 CurrentPosition { get; set; }
        public bool IsMoving => Vector3.Distance(CurrentDestination, CurrentPosition) > MinDistanceToDest;

        protected internal virtual void UpdateVariables(Alive entity)
        {
            MinDistanceToDest = entity.MinDistanceToTarget;
            CurrentPosition = entity.transform.position;
        }

        protected internal virtual void MoveTo(Vector3 destination, Alive entity)
        {
            UpdateVariables(entity);
            CurrentDestination = destination;

            #region Error Handling
            if (entity.Controller == null)
            {
                Debug.LogError($"Error! No controller found on {entity.name}! This component is required for the default AI!");
                return;
            }
            #endregion

            //Are we stuck?
            //If we are, Check what direction to go


            //Check if running
            if (entity.CurrentTarget != null)
                entity._CurrentMovementSpeed = entity.BaseMovementSpeed * entity.RunMovementMultiplier;
            else entity._CurrentMovementSpeed = entity.BaseMovementSpeed;

            if (IsMoving)
            {
                /*Get the normalized direction of the destination from the entity. (Normalize because we
                don't want to make the movement faster when the destination is farther away).*/
                Vector3 movementFactor = Vector3.Normalize(destination - entity.transform.position);
                entity.transform.rotation = Quaternion.Euler(destination - entity.transform.position);

                //Move to destination.
                entity.Controller.SimpleMove(movementFactor * entity._CurrentMovementSpeed * Time.deltaTime);
            }
        }
    }
}