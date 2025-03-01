using UnityEngine;

public class LegController : MonoBehaviour
{
    [Header("Leg Components")]
    public Transform leftLeg;
    public Transform rightLeg;

    [Header("Motion Settings")]
    [SerializeField] private float smoothing = 10f;
    [SerializeField] private float accelerationScale = 1f; // Scale for acceleration movement
    [SerializeField] private float returnSpeed = 5f; // Speed to return to the base position

    private Quaternion initialLeftRotation;
    private Quaternion initialRightRotation;
    
    private Vector3 initialLeftPosition;
    private Vector3 initialRightPosition;
    
    private Quaternion targetLeftRotation;
    private Quaternion targetRightRotation;

    private Vector3 leftAcceleration;
    private Vector3 rightAcceleration;

    private bool hasNewRotationData = false;
    private bool hasNewAccelerationData = false;

    private void Start()
    {
        if (leftLeg) 
        {
            initialLeftRotation = leftLeg.rotation;
            initialLeftPosition = leftLeg.position;
        }
        if (rightLeg) 
        {
            initialRightRotation = rightLeg.rotation;
            initialRightPosition = rightLeg.position;
        }
    }

    private void OnEnable()
    {
        UDPModel.OnDataReceived += HandleNewMotionData;
    }

    private void OnDisable()
    {
        UDPModel.OnDataReceived -= HandleNewMotionData;
    }

    private void HandleNewMotionData(Quaternion leftRotation, Vector3 leftAcc, Quaternion rightRotation, Vector3 rightAcc)
    {
        bool updatedRotation = false;
        bool updatedAcceleration = false;
        
        if (leftLeg)
        {
            if (leftRotation != Quaternion.identity)
            {
                targetLeftRotation = initialLeftRotation * leftRotation;
                updatedRotation = true;
            }
            if (leftAcc != Vector3.zero)
            {
                leftAcceleration = leftAcc * accelerationScale;
                updatedAcceleration = true;
            }
        }

        if (rightLeg)
        {
            if (rightRotation != Quaternion.identity)
            {
                targetRightRotation = initialRightRotation * rightRotation;
                updatedRotation = true;
            }
            if (rightAcc != Vector3.zero)
            {
                rightAcceleration = rightAcc * accelerationScale;
                updatedAcceleration = true;
            }
        }

        // If both legs are present and both rotations are default, force an update.
        if (leftRotation == Quaternion.identity && rightRotation == Quaternion.identity)
        {
            targetLeftRotation = initialLeftRotation;
            targetRightRotation = initialRightRotation;
            updatedRotation = true;
        }

        if (leftAcc == Vector3.zero && rightAcc == Vector3.zero)
        {
            leftAcceleration = Vector3.zero;
            rightAcceleration = Vector3.zero;
            updatedAcceleration = true;
        }

        if (updatedRotation)
        {
            hasNewRotationData = true;
        }
        if (updatedAcceleration)
        {
            hasNewAccelerationData = true;
        }
    }
    private void LateUpdate()
    {
        if (hasNewRotationData)
        {
            if (leftLeg)
            {
                leftLeg.rotation = Quaternion.Lerp(
                    leftLeg.rotation,
                    targetLeftRotation,
                    Time.deltaTime * smoothing
                );
            }

            if (rightLeg)
            {
                rightLeg.rotation = Quaternion.Lerp(
                    rightLeg.rotation,
                    targetRightRotation,
                    Time.deltaTime * smoothing
                );
            }
        }

        if (hasNewAccelerationData)
        {
            ApplyAccelerationEffects();
        }

        ReturnToBasePosition();
    }

    private void ApplyAccelerationEffects()
    {
        if (leftLeg)
        {
            leftLeg.position = initialLeftPosition + leftAcceleration;
        }

        if (rightLeg)
        {
            rightLeg.position = initialRightPosition + rightAcceleration;
        }
    }

    private void ReturnToBasePosition()
    {
        if (leftLeg)
        {
            leftLeg.position = Vector3.Lerp(
                leftLeg.position,
                initialLeftPosition,
                Time.deltaTime * returnSpeed
            );
        }

        if (rightLeg)
        {
            rightLeg.position = Vector3.Lerp(
                rightLeg.position,
                initialRightPosition,
                Time.deltaTime * returnSpeed
            );
        }
    }
}