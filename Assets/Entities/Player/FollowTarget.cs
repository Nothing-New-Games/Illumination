using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using TMPro;

public class FollowTarget : MonoBehaviour
{
    //I'd like to add an offset to the follow script that is applied to the position of the camera with a toggle
    //to add the offset to where the follower is looking.
    //IE: so the forward position of the follower is pointed toward the offset despite looking "at" the target.

    public Transform Target;

    [TabGroup("Main", "Customization")]
    public bool LookAtTarget;
    [TabGroup("Main", "Customization")]
    public bool LockYPosWhenMoving;
    [TabGroup("Main", "Customization")]
    public float FollowDistance;
    [TabGroup("Main", "Customization")]
    public float TooFarDistance;
    [TabGroup("Main", "Customization")]
    public float TooFarDistanceSpeedMultiplier;
    [TabGroup("Main", "Customization")]
    public float FollowSpeed;
    [TabGroup("Main", "Customization")]
    public float RotationSpeed;


    [DisplayAsString, TabGroup("Debug Display")]
    public Vector3 DesiredPosition;
    [DisplayAsString, TabGroup("Debug Display")]
    public float CurrentDistanceToTarget;
    [DisplayAsString, TabGroup("Debug Display")]
    public Vector3 Velocity;

    private void Start()
    {
        if (Target == null)
        {
            Debug.LogError($"No target assigned! Disabling {name}'s follow script!");
            enabled = false;
        }
    }


    private void Update()
    {
        
    }
    private void LateUpdate()
    {
        if (LookAtTarget)
            transform.rotation = Quaternion.RotateTowards
                (
                    transform.rotation, 
                    Quaternion.LookRotation(Target.position - transform.position), 
                    RotationSpeed * Time.deltaTime
                );

        CurrentDistanceToTarget = Vector3.Distance(transform.position, Target.position);

        if (CurrentDistanceToTarget > FollowDistance && CurrentDistanceToTarget < TooFarDistance)
            Velocity = Vector3.Normalize(Target.position - transform.position) * CurrentDistanceToTarget;
        else if (CurrentDistanceToTarget < FollowDistance)
            Velocity = -Vector3.Normalize(Target.position - transform.position) * CurrentDistanceToTarget;
        else if (CurrentDistanceToTarget > TooFarDistance)
            Velocity = Vector3.Normalize(Target.position - transform.position) 
                * CurrentDistanceToTarget 
                * TooFarDistanceSpeedMultiplier;

        else Velocity = new();

        Vector3 newPos = MoveToDestination();

        if (LockYPosWhenMoving)
        {
            //This doesn't really do much but I think it helps a little bit.
            float yDifference = transform.position.y - Target.position.y;
            newPos.y += yDifference;
        }

        transform.position = MoveToDestination();
    }

    private Vector3 GetHardDistancePosition() =>
        Target.position + (Vector3.Normalize(transform.position - Target.position) * FollowDistance);

    private Vector3 MoveToDestination() => 
        Vector3.SmoothDamp
        (
            transform.position,
            GetHardDistancePosition(),
            ref Velocity,
            FollowSpeed * Time.deltaTime
        );


    [TabGroup("Main", "Debug")]
    public bool DrawGizmos;
    [TabGroup("Main", "Debug"), ShowIf("@DrawGizmos")]
    public bool DrawDirectionToTarget;
    [TabGroup("Main", "Debug"), ShowIf("@DrawGizmos && DrawDirectionToTarget")]
    public Color TargetDirectionColor = Color.black;
    [TabGroup("Main", "Debug"), ShowIf("@DrawGizmos")]
    public bool DrawDistanceSize;
    [TabGroup("Main", "Debug"), ShowIf("@DrawGizmos && DrawDistanceSize")]
    public Color DistanceColor = Color.black;


    [TabGroup("Main", "Debug"), ShowIf("@DrawGizmos")]
    public bool DrawHardDistancePosition;
    [TabGroup("Main", "Debug"), ShowIf("@DrawGizmos && DrawHardDistancePosition")]
    public float HardDistancePositionGizmoSize;
    [TabGroup("Main", "Debug"), ShowIf("@DrawGizmos && DrawHardDistancePosition")]
    public Color HardDistanceColor = Color.black;


    private void OnDrawGizmos()
    {
        if (DrawGizmos && Target != null)
        {
            if (DrawDirectionToTarget)
            {
                Gizmos.color = TargetDirectionColor;
                Gizmos.DrawLine(transform.position, transform.position + Vector3.Normalize(Target.position - transform.position) * Vector3.Distance(Target.position, transform.position));
            }
            if (DrawDistanceSize)
            {
                Gizmos.color = DistanceColor;
                Gizmos.DrawWireSphere(Target.position, FollowDistance);
            }
            if (DrawHardDistancePosition)
            {
                DesiredPosition = GetHardDistancePosition();
                Gizmos.color = HardDistanceColor;
                Gizmos.DrawWireSphere(DesiredPosition, HardDistancePositionGizmoSize);
            }
        }
    }
}
