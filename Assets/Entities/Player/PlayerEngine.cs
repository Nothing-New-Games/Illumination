using Assets.Entities.AI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEngine : MovementEngine
{
    Vector3 _MovementFactor { get; set; }

    protected internal override void MoveToDestination()
    {
        /*This will create a bug most likely when the player dies. Likely the player will float, but I'm not
                certain.*/
        _Entity.Controller.SimpleMove(_MovementFactor * Time.deltaTime * _Entity._CurrentMovementSpeedValue);
        //Tested, it most definitely does XD we know how to fix it, but let's wait to see what time is left.

        //Invoke an event for movement.
    }

    public override bool IsMoving() => Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;


    protected internal void SetMovementFactor(Vector3 movementFactor) => _MovementFactor = movementFactor;
}
