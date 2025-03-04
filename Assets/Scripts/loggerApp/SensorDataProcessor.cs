// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// /// <summary>
// /// Processes sensor data and integrates with the existing UDPModel system.
// /// Add this component to your GameObject to bridge the HTTP sensor data to the UDP event system.
// /// </summary>
// public class SensorDataProcessor : MonoBehaviour
// {
//     [Header("Debug")]
//     [SerializeField] private bool showDebugInfo = true;
//     [SerializeField] private Transform leftLegDebug;
//     [SerializeField] private Transform rightLegDebug;
    
//     private Quaternion leftRotation = Quaternion.identity;
//     private Vector3 leftAcceleration = Vector3.zero;
//     private Quaternion rightRotation = Quaternion.identity;
//     private Vector3 rightAcceleration = Vector3.zero;
    
//     private void OnEnable()
//     {
//         // Subscribe to the SensorDataEndpoint's motion data event
//         SensorDataEndpoint.OnMotionDataReceived += OnSensorMotionDataReceived;
//     }
    
//     private void OnDisable()
//     {
//         // Unsubscribe from events
//         SensorDataEndpoint.OnMotionDataReceived -= OnSensorMotionDataReceived;
//     }
    
//     private void Update()
//     {
//         // Update debug objects if enabled
//         if (showDebugInfo)
//         {
//             if (leftLegDebug != null)
//             {
//                 leftLegDebug.rotation = leftRotation;
//             }
            
//             if (rightLegDebug != null)
//             {
//                 rightLegDebug.rotation = rightRotation;
//             }
//         }
//     }
    
//     /// <summary>
//     /// Handler for motion data received from the sensor endpoint
//     /// </summary>
//     private void OnSensorMotionDataReceived(Quaternion leftRot, Vector3 leftAcc, Quaternion rightRot, Vector3 rightAcc)
//     {
//         // Store the data
//         leftRotation = leftRot;
//         leftAcceleration = leftAcc;
//         rightRotation = rightRot;
//         rightAcceleration = rightAcc;
        
//         // Create motion data and trigger the same event as UDPModel would
//         MotionData motionData = MotionDataConverter.CreateMotionData(leftRot, leftAcc, rightRot, rightAcc);
        
//         // Forward the data to the UDPModel event system
//         UDPModel.OnDataReceivedInvoke(leftRot, leftAcc, rightRot, rightAcc);
        
//         if (showDebugInfo)
//         {
//             Debug.Log($"Forwarded motion data: Left({leftRot.eulerAngles}, {leftAcc}), Right({rightRot.eulerAngles}, {rightAcc})");
//         }
//     }
// }