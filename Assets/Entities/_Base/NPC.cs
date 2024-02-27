using Assets.EDO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Entities
{
    public class NPC : Alive
    {
        CharacterController _Controller;
        public float BaseMovementSpeed = 5f;
        public float RunMovementMultiplier = 2f;

        private float _CurrentMovementSpeed = 0f;

        private Animator _Animator;
        public AnimationTrigger CurrentAnimation;
        

        public override void DealDamage(DamageSource source)
        {
            Debug.Log(source.Source.name + " has damaged " + name + " for " + source.Damage + "!");

            
            CurrentHealth -= source.Damage;
        }

        /// <summary>
        /// Play death animation
        /// </summary>
        public override void OnDeath()
        {
            //Set the current animation to dead.
            CurrentAnimation = AnimationTrigger.Dead;
            //Perform the animation
            _Animator.SetTrigger(CurrentAnimation.ToString());
        }

        // Start is called before the first frame update
        void Start()
        {
            _Controller = GetComponent<CharacterController>();
            _Animator = GetComponent<Animator>();
        }

        // Update is called once per frame
        void Update()
        {
            if (!IsAlive) return;
            else if (CurrentHealth <= 0) FireEvent(); //Handled by Alive to trigger death.

            //Check if running

            //Are we at our destination?
            //Are we stuck?
            //If we are, Check what direction to go
            Vector3 movementFactor = Vector3.zero;

            //Get the current animation.


            //Animation here:
            _Animator.SetTrigger(CurrentAnimation.ToString());

            //Move to destination.
            _Controller.SimpleMove(movementFactor * Time.deltaTime * _CurrentMovementSpeed);
        }
    }

    public enum AnimationTrigger
    {
        Walk, Run, Attack, Push, Jumping, Falling, Dead, Idle, IdleLong
    }
}
