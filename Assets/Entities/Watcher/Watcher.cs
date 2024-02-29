using Assets.Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Watcher : Alive
{
    public override void DealDamage(DamageSource source)
    {
        Debug.Log(source.Source.name + " has damaged " + name + " for " + source.Damage + "!");
        CurrentHealth -= source.Damage;
    }

    public override void OnDeath()
    {
        //These cannot take damage, so no death necessary.
    }
}
