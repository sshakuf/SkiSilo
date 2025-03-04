using UnityEngine;

public class LegController : MonoBehaviour
{
    [Header("Leg Components")]
    public Transform leftLeg;
    public Transform rightLeg;

    [Header("Motion Settings")]
    [SerializeField] private float smoothing = 10f;
    [SerializeField] private float accelerationScale = 1f; // Scale for acceleration movement
    [SerializeField] private float gravityScale = 0.5f;    // Scale for gravity influence
    [SerializeField] private float returnSpeed = 5f;       // Speed to return to the base position
    [SerializeField] private bool useGravityData = true;   // Toggle to enable/disable gravity influence
    [SerializeField] private bool useGravityForAngle = true; // Toggle to use gravity for angle calculation

    private Quaternion initialLeftRotation;
    private Quaternion initialRightRotation;
    
    private Vector3 initialLeftPosition;
    private Vector3 initialRightPosition;
    
    private Quaternion targetLeftRotation;
    private Quaternion targetRightRotation;

    private Vector3 leftAcceleration;
    private Vector3 rightAcceleration;
    
    private Vector3 leftGravity;
    private Vector3 rightGravity;

    private bool hasNewRotationData = false;
    private bool hasNewAccelerationData = false;
    private bool hasNewGravityData = false;

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
        
        // Initialize gravity vectors
        leftGravity = Vector3.zero;
        rightGravity = Vector3.zero;
    }

    private void OnEnable()
    {
        // Subscribe to the single combined event
        UDPModel.OnDataReceived += HandleMotionData;
    }

    private void OnDisable()
    {
        // Unsubscribe from the event
        UDPModel.OnDataReceived -= HandleMotionData;
    }
    
    // Convert gravity values (-1 to 1) to angles (-180 to 180)
    private Quaternion GravityToRotation(Vector3 gravity)
    {
        // Calculate pitch and roll based on gravity
        // Gravity is inverted for intuitive rotation (negative gravity = positive angle)
        float pitchAngle = Mathf.Asin(-gravity.x) * Mathf.Rad2Deg; // Convert to degrees
        float rollAngle = Mathf.Asin(gravity.z) * Mathf.Rad2Deg;  // Convert to degrees
        
        // Use atan2 for more accurate angle when possible
        if (Mathf.Abs(gravity.y) > 0.001f)
        {
            pitchAngle = Mathf.Atan2(-gravity.x, Mathf.Abs(gravity.y)) * Mathf.Rad2Deg;
            rollAngle = Mathf.Atan2(gravity.z, Mathf.Abs(gravity.y)) * Mathf.Rad2Deg;
        }
        
        // Create a rotation from these angles (pitch = X-axis, roll = Z-axis)
        return Quaternion.Euler(pitchAngle, 0, rollAngle);
    }
    
    // Single handler for all motion data including gravity
    private void HandleMotionData(Quaternion leftRotation, Vector3 leftAcc, Vector3 leftGrav, 
                                 Quaternion rightRotation, Vector3 rightAcc, Vector3 rightGrav)
    {
        bool updatedRotation = false;
        bool updatedAcceleration = false;
        bool updatedGravity = false;
        
        if (leftLeg)
        {
            if (useGravityForAngle && leftGrav != Vector3.zero)
            {
                // Use gravity to determine rotation
                Quaternion gravityRotation = GravityToRotation(leftGrav);
                targetLeftRotation = initialLeftRotation * gravityRotation;
                updatedRotation = true;
                
                // Store gravity for position effects if needed
                leftGravity = leftGrav * gravityScale;
                updatedGravity = true;
            }
            else if (leftRotation != Quaternion.identity)
            {
                // Use explicit rotation from gyroscope data
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
            if (useGravityForAngle && rightGrav != Vector3.zero)
            {
                // Use gravity to determine rotation
                Quaternion gravityRotation = GravityToRotation(rightGrav);
                targetRightRotation = initialRightRotation * gravityRotation;
                updatedRotation = true;
                
                // Store gravity for position effects if needed
                rightGravity = rightGrav * gravityScale;
                updatedGravity = true;
            }
            else if (rightRotation != Quaternion.identity)
            {
                // Use explicit rotation from gyroscope data
                targetRightRotation = initialRightRotation * rightRotation;
                updatedRotation = true;
            }
            
            if (rightAcc != Vector3.zero)
            {
                rightAcceleration = rightAcc * accelerationScale;
                updatedAcceleration = true;
            }
        }

        // Reset acceleration if both are zero
        if (leftAcc == Vector3.zero && rightAcc == Vector3.zero)
        {
            leftAcceleration = Vector3.zero;
            rightAcceleration = Vector3.zero;
            updatedAcceleration = true;
        }

        // Update state flags
        if (updatedRotation)
        {
            hasNewRotationData = true;
        }
        if (updatedAcceleration)
        {
            hasNewAccelerationData = true;
        }
        if (updatedGravity)
        {
            hasNewGravityData = true;
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

        if (hasNewAccelerationData || hasNewGravityData)
        {
            ApplyMotionEffects();
        }

        ReturnToBasePosition();
    }

    private void ApplyMotionEffects()
    {
        if (leftLeg)
        {
            Vector3 combinedEffect = leftAcceleration;
            
            // Apply gravity effect if enabled and not using it for rotation
            if (useGravityData && !useGravityForAngle && hasNewGravityData)
            {
                combinedEffect += leftGravity;
            }
            
            leftLeg.position = initialLeftPosition + combinedEffect;
        }

        if (rightLeg)
        {
            Vector3 combinedEffect = rightAcceleration;
            
            // Apply gravity effect if enabled and not using it for rotation
            if (useGravityData && !useGravityForAngle && hasNewGravityData)
            {
                combinedEffect += rightGravity;
            }
            
            rightLeg.position = initialRightPosition + combinedEffect;
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