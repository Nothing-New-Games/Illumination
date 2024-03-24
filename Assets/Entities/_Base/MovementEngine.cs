using Unity.VisualScripting;
using UnityEditor;
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
            _CurrentStuckCheckInterval = _Entity.StuckCheckInterval;
            if (destination != default)
                CurrentDestination = destination;
        }




        Vector3 _PreviousPositionForStuckCheck;
        LayerMask _ObstacleLayer;
        private Vector3 _TempPosToGetUnstuck;
        private bool _PerformingAntiStuck;
        private float _CurrentStuckCheckInterval;
        protected internal int _StuckCheckCounter = 0;

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
                        _TempPosToGetUnstuck = 
                            _Entity.transform.position + 
                            (Quaternion.Euler(0, _Entity.StuckCorrectionDegrees / 2, 0) * 
                            (_Entity.transform.forward * 
                            _Entity.PositioningCorrectionDistance));
                    }
                }
            }
            else _PerformingAntiStuck = false;
            
            if (IsMoving() && !_PerformingAntiStuck)
            {
                MoveToDestination(CurrentDestination);
            }
            else if (IsMoving())
            {
                MoveToDestination(_TempPosToGetUnstuck);
            }
        }

        internal virtual bool CheckIfStuck()
        {
            if (_Entity.IsPerformingAnimations(AnimationType.Idle)/* || !_Entity.IsGrounded()*/)
                return false;

            float distanceMoved = Vector3.Distance(_PreviousPositionForStuckCheck, _Entity.transform.position);

            if (distanceMoved < _Entity.PositioningCorrectionDistance / 4 && LookingAtTarget)
            {
                _StuckCheckCounter++;
                Debug.LogWarning($"NPC has made {_StuckCheckCounter}/{_Entity.MaxStuckCounter} reports for being unable to move!");
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

        private Vector3 _DesiredEulerRotation;
        public bool LookingAtTarget =>
            Vector3.Angle(_Entity.transform.forward, _DesiredEulerRotation) <= _Entity.MaxTargetingDegrees / 2;
        private void MoveToDestination(Vector3 DesiredDestination)
        {
            Vector3 CurrentDestinationWithoutYPos = new Vector3(DesiredDestination.x, _Entity.transform.position.y, DesiredDestination.z);

            Ray ray = new Ray(_Entity.transform.position, CurrentDestinationWithoutYPos);
            RaycastHit hit;
            LayerMask mask = ~LayerMask.GetMask(LayerMask.LayerToName(_Entity.gameObject.layer), LayerMask.LayerToName(Player.PlayerInstance.gameObject.layer));

            if (Physics.Raycast(ray, out hit, Vector3.Distance(_Entity.transform.position, CurrentDestinationWithoutYPos), mask) && hit.transform.name != "Terrain")
            {
                if (_Entity.PrintStuckCorrectionLogs)
                    Debug.LogWarning($"{hit.transform.name} is in the way! Correcting position so as to not get stuck on a wall or some such!");
                
                CurrentDestinationWithoutYPos = hit.point - ((hit.point - _Entity.transform.position).normalized * _Entity.PositioningCorrectionDistance);
                CurrentDestination = CurrentDestinationWithoutYPos;
            }


            Vector3 movementFactor = Vector3.Normalize(CurrentDestinationWithoutYPos - _Entity.transform.position);
            movementFactor.y = 0;

            _DesiredEulerRotation = (CurrentDestinationWithoutYPos - _Entity.transform.position).normalized;
            _Entity.transform.rotation = Quaternion.RotateTowards
                (
                    _Entity.transform.rotation, 
                    Quaternion.LookRotation(_DesiredEulerRotation), 
                    _Entity.RotationSpeed * Time.deltaTime
                );
            //_Entity.transform.LookAt(CurrentDestinationWithoutYPos);


            //Move to destination.
            if(_Entity.IsGrounded() && LookingAtTarget)
                _Entity.RB.velocity = movementFactor * _Entity._CurrentMovementSpeedValue * Time.deltaTime;
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