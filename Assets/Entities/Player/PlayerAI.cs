using Assets.Entities;
using Assets.Entities.AI;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class PlayerAI : AIHandler
{
    bool isRunning = false;
    bool isCrouching = false;
    bool isPushing = false;
    bool isGrounded = true;


    public override AnimationType Handle(Alive entity)
    {
        Vector3 movementFactor = new(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));




        //Get the player engine
        PlayerEngine playerEngine = entity.Engine as PlayerEngine;
        isGrounded = entity.IsGrounded();


        if (isGrounded) //Determine if the player is grounded. Currently defaulting to true since we don't even have a jump yet.
        {
            //Determine which animations should be used when on the ground.
            if (false) //Create conditions for is pushing. No idea how to do this yet, so defaulting to false so it checks the other conditions.
            {

            }
            //else if (Input.GetKeyDown(KeyCode.LeftAlt) && !isCrouching)
            //{
            //    isCrouching = true;
            //}
            //else if (Input.GetKeyDown(KeyCode.LeftAlt) && isCrouching)
            //{
            //    isCrouching = false;
            //}


            //Calculate how to handle movement
            /*Currently, diagonal is faster and I can fix that by adding more conditions to check if the player is
                moving diagonal, but I got too much other stuff to do LMAO*/
            if (Input.GetKey(KeyCode.LeftShift) && playerEngine.IsMoving())
            {
                entity._CurrentMovementSpeedValue = entity.BaseMovementSpeed * entity.RunMovementMultiplier;
                isRunning = true;
                
            }
            else if (playerEngine.IsMoving())
            {
                entity._CurrentMovementSpeedValue = entity.BaseMovementSpeed;
                isRunning = false;
            }
        }

        //Do player engine stoof
        playerEngine.SetMovementFactor(movementFactor);
        playerEngine.UpdateVariables(entity);
        playerEngine.MoveToDestination();


        //Return the correct animation.
        if (!isGrounded)
            return AnimationType.Falling;
        if (isRunning)
            return AnimationType.Run;
        if (playerEngine.IsMoving())
            return AnimationType.Walk;


        return AnimationType.Idle;
    }

    protected internal override bool SearchForTarget(Alive entity) => false;
}
