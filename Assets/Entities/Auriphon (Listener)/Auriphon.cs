using UnityEngine;
using Assets.Entities;
using Assets.Entities.AI;

public class Auriphon : Alive
{
    public override void DealDamage(DamageSource source)
    {
        Debug.Log(source.Source.name + " has damaged " + name + " for " + source.Damage + "!");
        CurrentHealth -= source.Damage;
    }

    public override void OnDeath()
    {
        //Set the current animation to dead.
        CurrentAnimation = AnimationTrigger.Dead;
        //Perform the animation
        _Animator.SetTrigger(CurrentAnimation.ToString());
    }
}
