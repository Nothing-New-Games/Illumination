using Assets.Entities.AI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class PlayerEngine : MovementEngine
{
    Vector3 _MovementFactor { get; set; }


    protected internal override void MoveToDestination()
    {
        Player player = _Entity as Player;

        /*This will create a bug most likely when the player dies. Likely the player will float, but I'm not
                certain.*/

        if (_MovementFactor != Vector3.zero)
        {
            Quaternion newRotation = Quaternion.LookRotation(_MovementFactor);
            newRotation.x = 0;
            newRotation.z = 0;

            _Entity.transform.rotation = newRotation;
        }


        _Entity.Controller.SimpleMove(_MovementFactor * Time.deltaTime * _Entity._CurrentMovementSpeedValue);
        //Tested, it most definitely does XD we know how to fix it, but let's wait to see what time is left.
        //This bug should be created by the Player script as the update method returns when the player is dead.
        //Ez fix would be to call the engine move but with vector3.zero instead of movement factor.

        //Invoke an event for movement.
    }

    public override bool IsMoving() => Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;


    protected internal void SetMovementFactor(Vector3 movementFactor) => _MovementFactor = movementFactor;
}
