using Assets.Entities.AI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AuriphonEngine : MovementEngine
{
    private float _StartingAnimationSpeed = 0f;

    internal override void UpdateMovementSpeed()
    {
        if (_StartingAnimationSpeed == 0)
            _StartingAnimationSpeed = _Entity._Animator.speed;

        base.UpdateMovementSpeed();

        if (_Entity.CurrentTarget != null)
            _Entity._Animator.speed = _Entity.RunMovementMultiplier * _StartingAnimationSpeed;
        else _Entity._Animator.speed = _StartingAnimationSpeed;
    }
}
