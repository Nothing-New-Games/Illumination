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
    bool isJumping = false;
    bool isMoving = false;


    public override AnimationType Handle(Alive entity)
    {
        Player player = entity as Player;
        //Get the player engine
        PlayerEngine playerEngine = entity.Engine as PlayerEngine;
        Camera playerCamera = player.MainCamera;

        if (!entity.IsAlive)
            return AnimationType.Dead;

        // Calculate the sensitivity based on the magnitude of the input vector
        float inputMagnitude = new Vector2(Player.MouseX, Player.MouseY).magnitude;
        float sensitivity = player.CameraSensitivity * Mathf.Lerp(1, inputMagnitude, player.CameraSensitivity);

        playerCamera.transform.position += playerCamera.transform.up * Mathf.Clamp(Player.MouseY, -1, 1) * sensitivity
                                            +
                                          playerCamera.transform.right * Mathf.Clamp(Player.MouseX, -1, 1) * sensitivity;

        if (Input.GetAxis("EmotesMenu") == 1)
        {
            //Fire event to open emotes menu
        }
        else if (Input.GetAxis("EmotesMenu") == 0)
        {
            //Fire event to close emotes menu
        } //Perhaps a toggle for the event?

        isGrounded = entity.IsGrounded();
        isJumping = Input.GetAxis("Jump") != 0;
        isMoving = playerEngine.IsMoving();
        if (entity.IsPerformingAnimation(AnimationType.Falling) && isGrounded)
            return AnimationType.Touchdown;
        if (entity.IsPerformingAnimation(AnimationType.Touch))
            return AnimationType.PreventMovement;
        if (entity.IsPerformingAnimation(AnimationType.Loot))
            return AnimationType.PreventMovement;

        if (Input.GetButtonUp("Interact") && isGrounded)
        {
            if (entity.CurrentInteractTarget != null && !entity.IsPerformingAnimation(AnimationType.Touch))
            {
                entity.CurrentInteractTarget.Interact();
                return AnimationType.Touch;
            }
            else if (!entity.IsPerformingAnimation(AnimationType.Touch))
                Debug.Log("No interactable object nearby!");
            else Debug.Log("You are already interacting with something!");
        }

        //Forward and Backward
        Vector3 movementFactor = playerCamera.transform.forward * Mathf.RoundToInt(Input.GetAxis("Vertical"))
                                 +
                                 //Left and Right
                                 playerCamera.transform.right * Mathf.RoundToInt(Input.GetAxis("Horizontal"));
        //Remove the y value, so the character cannot walk upwards when the camera is pointed up.
        movementFactor.y = 0;


        if (isGrounded) //Determine if the player is grounded. Currently defaulting to true since we don't even have a jump yet.
        {
            //Determine which animations should be used when on the ground.
            if (false) //Create conditions for is pushing. No idea how to do this yet, so defaulting to false so it checks the other conditions.
            {

            }


            //Calculate how to handle movement
            /*Currently, diagonal is faster and I can fix that by adding more conditions to check if the player is
                moving diagonal, but I got too much other stuff to do LMAO*/
            if (Input.GetButton("Sprint") && playerEngine.IsMoving())
            {
                entity._CurrentMovementSpeedValue = entity.BaseMovementSpeed * entity.RunMovementMultiplier;
                isRunning = true;

            }
            else if (playerEngine.IsMoving())
            {
                entity._CurrentMovementSpeedValue = entity.BaseMovementSpeed;
                isRunning = false;
            }
            else isRunning = false;
        }
        else
        {
            movementFactor /= player.FallingMovementReduction;
        }

        //Do player engine stoof
        playerEngine.SetMovementFactor(movementFactor);
        playerEngine.UpdateVariables(entity);
        if (isGrounded)
            playerEngine.HandleMovement();
        else
        {
            isRunning = false;
            isCrouching = false;
            isPushing = false;
            isMoving = false;
        }

        //Return the correct animation.
        if (isJumping && isGrounded)
            return AnimationType.Jumping;
        if (!isGrounded)
            return AnimationType.Falling;
        if (isRunning && !isJumping)
            return AnimationType.Run;
        if (isMoving && !isJumping)
            return AnimationType.Walk;


        return AnimationType.Idle;
    }

    protected internal override bool SearchForTarget(Alive thisEntity, Alive DesiredTarget = null) => false;
}
