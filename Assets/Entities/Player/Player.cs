using Assets.Entities;
using UnityEngine;

public class Player : Alive
{
    public static Transform PlayerInstance { get; private set; }


    internal override void Awake()
    {
        //Get the animator and controller
        Controller = GetComponent<CharacterController>();
        _Animator = GetComponentInChildren<Animator>();

        if (PlayerInstance == null)
            PlayerInstance = transform;
        else
        {
            Debug.LogWarning("Multiple players found in the scene! Disabling the second one to show up!");
            enabled = false;
        }


        AI = new PlayerAI();
        Engine = new PlayerEngine();
    }
    // Update is called once per frame
    internal override void Update()
    {
        if (!IsAlive) return;

        CurrentAnimation = AI.Handle(this);
    }

    

    internal override void LateUpdate()
    {
        
    }

    public override void DealDamage(DamageSource source)
    {
        Debug.Log(source.Source.name + " has damaged " + name + " for " + source.Damage + "!");
        CurrentHealth -= source.Damage;
    }

    public override void OnDeath()
    {
        //Play death animation
        //Game over screen should be handled elsewhere by a subscriber.
    }
}
