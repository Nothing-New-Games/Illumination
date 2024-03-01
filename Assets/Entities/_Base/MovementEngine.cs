using Unity.VisualScripting;
using UnityEngine;

namespace Assets.Entities.AI
{
    public class MovementEngine
    {
        protected internal float MinDistanceToDest { get; set; }
        protected internal Vector3 CurrentDestination { get; set; }
        public Vector3 GetCurrentDest => CurrentDestination;

        protected internal Vector3 CurrentPosition { get; set; }
        protected internal Alive _Entity {  get; set; }
        public bool IsMoving => Vector3.Distance(CurrentDestination, CurrentPosition) > MinDistanceToDest;
        public bool IsCloseToDestination()
        {
            if (_Entity != null)
                return Vector3.Distance(_Entity.transform.position, CurrentDestination) <= MinDistanceToDest;

            return true;
        }

        protected internal virtual void UpdateVariables(Alive entity, Vector3 destination = default)
        {
            MinDistanceToDest = entity.MinDistanceToDestination;
            CurrentPosition = entity.transform.position;
            _Entity = entity;
            if (destination != default)
                CurrentDestination = destination;
        }

        protected internal virtual void MoveToDestination()
        {
            #region Error Handling
            if (_Entity.Controller == null)
            {
                Debug.LogError($"Error! No controller found on {_Entity.name}! This component is required for the default AI!");
                return;
            }
            #endregion

            //Are we stuck?
            //If we are, Check what direction to go


            //Update speed for if we need to run.
            UpdateMovementSpeed();

            if (IsMoving)
            {
                /*Get the normalized direction of the destination from the entity. (Normalize because we
                don't want to make the movement faster when the destination is farther away).*/
                Vector3 temp = Vector3.Normalize(CurrentDestination - _Entity.transform.position);
                Vector3 movementFactor = new Vector3(temp.x, _Entity.transform.position.y, temp.z);
                _Entity.transform.LookAt(CurrentDestination);

                Ray ray = new Ray(_Entity.transform.position, _Entity.transform.forward * Vector3.Distance(_Entity.transform.position, CurrentDestination));
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    if (_Entity.CurrentTarget != null)
                    {
                        //We handle this slightly different because we want the creature to be close before attacking where as a wall, we want them to be a comfortable distance away.
                        CurrentDestination = new Vector3(hit.point.x - _Entity.MinDistanceToDestination, hit.point.y, hit.point.z - _Entity.MinDistanceToDestination);
                    }
                    else
                    {
                        Debug.LogWarning("There is an object in the way! Correcting position so as to not get stuck on a wall or some such!");
                        CurrentDestination = new Vector3(hit.point.x - _Entity.PositioningCorrectionDistance, hit.point.y, hit.point.z - _Entity.PositioningCorrectionDistance);
                    }
                }

                //Move to destination.
                _Entity.Controller.SimpleMove(movementFactor * _Entity._CurrentMovementSpeedValue * Time.deltaTime);
            }
        }

        internal virtual void UpdateMovementSpeed()
        {
            if (_Entity.CurrentTarget != null)
                _Entity._CurrentMovementSpeedValue = _Entity.BaseMovementSpeed * _Entity.RunMovementMultiplier;
            else _Entity._CurrentMovementSpeedValue = _Entity.BaseMovementSpeed;
        }
    }
}