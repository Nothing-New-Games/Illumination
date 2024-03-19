using Assets.Entities.AI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class PlayerEngine : MovementEngine
{
    Vector3 _MovementFactor;
    bool _HasJumped { get; set; }   

    protected internal override void HandleMovement()
    {
        Player player = _Entity as Player;

        if (_MovementFactor != Vector3.zero)
        {
            Quaternion newRotation = Quaternion.LookRotation(_MovementFactor);
            newRotation.x = 0;
            newRotation.z = 0;

            _Entity.transform.rotation = newRotation;
        }

        if (!_Entity.PauseMovement)
            _Entity.RB.velocity = (_MovementFactor.normalized * _Entity._CurrentMovementSpeedValue) * Time.deltaTime;

        if (Input.GetButtonDown("Jump") || Input.GetAxis("Jump") != 0) _Entity.RB.velocity += player.JumpVelocity;

        //Invoke an event for movement.
    }

    public override bool IsMoving() => Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;

    protected internal void SetMovementFactor(Vector3 movementFactor) => _MovementFactor = movementFactor;
}
