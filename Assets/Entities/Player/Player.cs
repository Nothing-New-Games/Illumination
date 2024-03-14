using Assets.Entities;
using UnityEngine;
using Sirenix.OdinInspector;
using System;

public class Player : Alive
{
    public static Transform PlayerInstance { get; private set; }
    [HideInInspector]
    public Camera MainCamera { get; private set; }

    [DisplayAsString, TabGroup("Main", "Debug Data"), ShowInInspector]
    public static float MouseY;
    [DisplayAsString, TabGroup("Main", "Debug Data"), ShowInInspector]
    public static float MouseX;

    [TabGroup("Main", "Movement")]
    public Vector3 JumpVelocity;
    [TabGroup("Main", "Movement")]
    public float FallingMovementReduction;


    [TabGroup("Main", "Player Settings")]
    public float CameraSensitivity = 0.3f;
    [TabGroup("Main", "Player Settings")]
    public bool InvertCameraX = false;
    [TabGroup("Main", "Player Settings")]
    public bool InvertCameraY = false;

    [OnValueChanged("SwitchGFX"), TabGroup("Main", "Debug")]
    [LabelText("$UseTestGFXString")]
    public bool UseTestGFX = false;
    private string UseTestGFXString => $"Use Test GFX ({UseTestGFX})";

    [TabGroup("Main", "Debug")]
    public GameObject[] TrueGFX;
    [TabGroup("Main", "Debug")]
    public GameObject[] TestGFX;

    [Button]
    private void ToggleGFX()
    {
        UseTestGFX = !UseTestGFX;
        SwitchGFX();
    }

    private void SwitchGFX()
    {
        if (UseTestGFX)
        {
            foreach (var item in TrueGFX)
            {
                item.SetActive(false);
            }
            foreach (var item in TestGFX)
            {
                item.SetActive(true);
            }
        }
        else
        {
            foreach (var item in TrueGFX)
            {
                item.SetActive(true);
            }
            foreach (var item in TestGFX)
            {
                item.SetActive(false);
            }
        }
    }

    internal override void Awake()
    {
        //Get the animator and controller
        RB = GetComponent<Rigidbody>();
        _Animator = GetComponentInChildren<Animator>();
        
        MainCamera = Camera.main;
        Cursor.lockState = CursorLockMode.Locked;

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
    internal override void FixedUpdate()
    {
        MouseY = -Input.GetAxis("Mouse Y");
        MouseX = -Input.GetAxis("Mouse X");
        if (InvertCameraX)
            MouseX *= -1;
        if (InvertCameraY)
            MouseY *= -1;

        base.FixedUpdate();
    }

    internal override void LateUpdate()
    {
        CurrentInteractTarget = NearestInteractable();
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
