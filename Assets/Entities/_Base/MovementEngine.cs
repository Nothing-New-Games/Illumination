using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace Assets.Entities.AI
{
    public class MovementEngine
    {
        protected internal float MinDistanceToDest { get; set; }
        protected internal Vector3 CurrentDestination { get; set; }
        public Vector3 GetCurrentDest => CurrentDestination;

        protected internal Vector3 CurrentPosition { get; set; }
        protected internal Alive _Entity {  get; set; }
        public virtual bool IsMoving() => Vector3.Distance(CurrentDestination, CurrentPosition) > MinDistanceToDest;
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




        Vector3 _PreviousPositionForStuckCheck;
        LayerMask _ObstacleLayer;
        private Vector3 _TempPosToGetUnstuck;
        private Vector3 _DesiredRotationForUnstuckifying;
        private bool _PerformingAntiStuck;
        private float _CurrentStuckCheckInterval;
        protected internal virtual void HandleMovement()
        {
            #region Error Handling
            if (_Entity.RB == null)
            {
                Debug.LogError($"Error! No controller found on {_Entity.name}! This component is required for the default AI!");
                return;
            }
            #endregion

            //Update speed for if we need to run.
            UpdateMovementSpeed();


            //Are we stuck?
            if (_Entity.RunStuckChecks)
            {
                _Entity._StuckCheckTimer += Time.deltaTime;
                if (_Entity._StuckCheckTimer >= _CurrentStuckCheckInterval)
                {
                    _PerformingAntiStuck = CheckIfStuck();
                    _Entity._StuckCheckTimer = 0;

                    if (_PerformingAntiStuck)
                    {
                        _DesiredRotationForUnstuckifying = _Entity.transform.rotation * Quaternion.Euler(0f, 22.5f, 0).eulerAngles;
                        _TempPosToGetUnstuck = _DesiredRotationForUnstuckifying
                                               * (Vector3.Distance(_Entity.transform.position, CurrentPosition) * 2)
                                               + _Entity.transform.position;
                    }
                }
            }
            
            if (IsMoving() && !_PerformingAntiStuck)
            {
                MoveToDestination(CurrentDestination);
            }
            else
            {
                _Entity.transform.rotation = Quaternion.Lerp(_Entity.transform.rotation, 
                    new Quaternion(_DesiredRotationForUnstuckifying.x, _DesiredRotationForUnstuckifying.y, _DesiredRotationForUnstuckifying.z, 0), 
                    Time.deltaTime * 20f);

                MoveToDestination(_TempPosToGetUnstuck);
                _PerformingAntiStuck = CheckIfStuck();
            }
        }

        internal virtual bool CheckIfStuck()
        {
            if (_Entity.IsPerformingAnimation(AnimationType.Idle))
                return false;

            float distanceMoved = Vector3.Distance(_PreviousPositionForStuckCheck, _Entity.transform.position);

            if (distanceMoved < 0.1f) // Adjust threshold as needed
            {
                Debug.LogWarning("NPC is stuck due to lack of movement!");
                return true;
            }
            RaycastHit hit;

            if (Physics.Raycast(_Entity.transform.position, _Entity.transform.forward, out hit, _Entity.PositioningCorrectionDistance, _ObstacleLayer))
            {
                if (_Entity.PrintStuckCorrectionLogs)
                    Debug.LogWarning($"NPC is stuck due to continuous collision with {hit.transform.name}!");
                return true;
            }

            _PreviousPositionForStuckCheck = _Entity.transform.position;
            return false;
        }

        private void MoveToDestination(Vector3 DesiredDestination)
        {
            Vector3 CurrentDestinationWithoutYPos = new Vector3(DesiredDestination.x, _Entity.transform.position.y, DesiredDestination.z);

            Vector3 movementFactor = Vector3.Normalize(CurrentDestinationWithoutYPos - _Entity.transform.position);
            movementFactor.y = 0;

            _Entity.transform.LookAt(CurrentDestinationWithoutYPos);

            Ray ray = new Ray(_Entity.transform.position, _Entity.transform.forward);
            RaycastHit hit;
            LayerMask mask = ~LayerMask.GetMask(LayerMask.LayerToName(_Entity.gameObject.layer));

            if (Physics.Raycast(ray, out hit, Vector3.Distance(_Entity.transform.position, CurrentDestinationWithoutYPos), mask))
            {
                if (!hit.transform.name.ToLower().Equals("terrain"))
                {
                    if (_Entity.CurrentLivingTarget != null)
                    {
                        //We handle this slightly different because we want the creature to be close before attacking where as a wall, we want them to be a comfortable distance away.
                        CurrentDestinationWithoutYPos = new Vector3(hit.point.x - _Entity.MinDistanceToDestination, hit.point.y, hit.point.z - _Entity.MinDistanceToDestination);
                    }
                    else if (_Entity.PrintStuckCorrectionLogs)
                    {
                        Debug.LogWarning($"{hit.transform.name} is in the way! Correcting position so as to not get stuck on a wall or some such!");
                        CurrentDestinationWithoutYPos = new Vector3(hit.point.x - _Entity.PositioningCorrectionDistance, hit.point.y, hit.point.z - _Entity.PositioningCorrectionDistance);
                    }
                }
            }

            //Move to destination.
            _Entity.RB.velocity = (movementFactor * _Entity._CurrentMovementSpeedValue) * Time.deltaTime;
        }

        internal virtual void UpdateMovementSpeed()
        {
            if (_Entity.CurrentLivingTarget != null)
            {
                _ObstacleLayer = ~LayerMask.GetMask(LayerMask.LayerToName(_Entity.gameObject.layer));
                _Entity._CurrentMovementSpeedValue = _Entity.BaseMovementSpeed * _Entity.RunMovementMultiplier;
            }
            else _Entity._CurrentMovementSpeedValue = _Entity.BaseMovementSpeed;
        }
    }
}