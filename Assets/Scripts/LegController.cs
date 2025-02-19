using UnityEngine;

public class LegController : MonoBehaviour
{
    [Header("Character Components")]
    public Transform character;
    public Transform leftLeg;
    public Transform rightLeg;

    [Header("Motion Settings")]
    [SerializeField] private float positionScale = 1f;
    [SerializeField] private float smoothing = 10f;

    private Quaternion initialLeftRotation;
    private Quaternion initialRightRotation;
    private Vector3 initialPosition;
    
    private Quaternion targetLeftRotation;
    private Quaternion targetRightRotation;
    private Vector3 targetPosition;
    
    private bool hasNewRotationData = false;
    private bool hasNewPositionData = false;

    private void Start()
    {
        if (leftLeg) initialLeftRotation = leftLeg.rotation;
        if (rightLeg) initialRightRotation = rightLeg.rotation;
        if (character) initialPosition = character.position;
        
        targetPosition = initialPosition;
    }

    private void OnEnable()
    {
        UDPModel.OnDataReceived += HandleNewRotationData;
        UDPModel.OnPositionReceived += HandleNewPositionData;
    }

    private void OnDisable()
    {
        UDPModel.OnDataReceived -= HandleNewRotationData;
        UDPModel.OnPositionReceived -= HandleNewPositionData;
    }

    private void HandleNewRotationData(Quaternion leftRotation, Quaternion rightRotation)
    {
        if (leftLeg) targetLeftRotation = initialLeftRotation * leftRotation;
        if (rightLeg) targetRightRotation = initialRightRotation * rightRotation;
        hasNewRotationData = true;
    }

    private void HandleNewPositionData(Vector3 newPosition)
    {
        if (character)
        {
            targetPosition = initialPosition + (newPosition * positionScale);
            hasNewPositionData = true;
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

        if (hasNewPositionData && character)
        {
            character.position = Vector3.Lerp(
                character.position,
                targetPosition,
                Time.deltaTime * smoothing
            );
        }
    }
}