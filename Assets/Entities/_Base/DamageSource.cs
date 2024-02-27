using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageSource
{
    public GameObject Source {  get; private set; }
    public float Damage {  get; private set; }

    public DamageSource(GameObject source, float damage)
    {
        Source = source;
        Damage = damage;
    }
}
