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

    internal override void CreateEngine()
    {
        Engine = new AuriphonEngine();
    }

    public override void OnDeath()
    {
        //These cannot take damage, so no death necessary.
    }
}
