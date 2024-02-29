using Assets.Entities;
using UnityEngine;

public class Player : Alive
{
    internal override void Awake()
    {
        //Get the animator and controller
        Controller = GetComponent<CharacterController>();

    }
    // Update is called once per frame
    internal override void Update()
    {
        if (!IsAlive) return;

        Vector3 movementFactor = new(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        /*Currently, diaginal is faster and I can fix that by adding more conditions to check if the player is
        moving diaginally, but I got too much other stuff to do LMAO*/
        if (Input.GetKey(KeyCode.LeftShift))
            _CurrentMovementSpeed = BaseMovementSpeed * RunMovementMultiplier;
        else _CurrentMovementSpeed = BaseMovementSpeed;

        /*This will create a bug most likely when the player dies. Likely the player will float, but I'm not
        certain.*/
        Controller.SimpleMove(movementFactor * Time.deltaTime * _CurrentMovementSpeed);
        //Tested, it most definitely does XD we know how to fix it, but let's wait to see what time is left.
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
