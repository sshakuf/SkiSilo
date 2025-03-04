// using System;
// using UnityEngine;

// /// <summary>
// /// Utility class for converting sensor data to Unity motion data.
// /// </summary>
// public static class MotionDataConverter
// {
//     /// <summary>
//     /// Extracts rotation and acceleration data from sensor events.
//     /// </summary>
//     /// <param name="sensorEvents">Array of sensor events to process</param>
//     /// <param name="leftRotation">Output left leg rotation</param>
//     /// <param name="leftAcceleration">Output left leg acceleration</param>
//     /// <param name="rightRotation">Output right leg rotation</param>
//     /// <param name="rightAcceleration">Output right leg acceleration</param>
//     /// <returns>True if data was successfully processed</returns>
//     public static bool ExtractMotionData(SensorDataEndpoint.SensorEvent[] sensorEvents, 
//                                        out Quaternion leftRotation, 
//                                        out Vector3 leftAcceleration, 
//                                        out Quaternion rightRotation, 
//                                        out Vector3 rightAcceleration)
//     {
//         // Initialize output values
//         leftRotation = Quaternion.identity;
//         leftAcceleration = Vector3.zero;
//         rightRotation = Quaternion.identity;
//         rightAcceleration = Vector3.zero;
        
//         // Values to track orientation and acceleration
//         Vector3 gyroValues = Vector3.zero;
//         Vector3 accelerometerValues = Vector3.zero;
//         Quaternion orientationQuat = Quaternion.identity;
//         float yaw = 0f, pitch = 0f, roll = 0f;
        
//         bool hasOrientation = false;
//         bool hasAcceleration = false;
        
//         // Process each sensor event
//         foreach (var sensorEvent in sensorEvents)
//         {
//             switch (sensorEvent.name)
//             {
//                 case "gyroscope":
//                 case "gyroscopeuncalibrated":
//                     // Process gyroscope data
//                     gyroValues.x = sensorEvent.values.x;
//                     gyroValues.y = sensorEvent.values.y;
//                     gyroValues.z = sensorEvent.values.z;
//                     break;
                    
//                 case "accelerometer":
//                 case "accelerometeruncalibrated":
//                     // Process accelerometer data
//                     accelerometerValues.x = sensorEvent.values.x;
//                     accelerometerValues.y = sensorEvent.values.y;
//                     accelerometerValues.z = sensorEvent.values.z;
//                     hasAcceleration = true;
//                     break;
                    
//                 case "orientation":
//                     // Process orientation data
//                     if (sensorEvent.values != null)
//                     {
//                         yaw = sensorEvent.values.yaw;
//                         pitch = sensorEvent.values.pitch;
//                         roll = sensorEvent.values.roll;
                        
//                         // If we have quaternion values, use them directly
//                         if (sensorEvent.values.qw != 0 || sensorEvent.values.qx != 0 || 
//                             sensorEvent.values.qy != 0 || sensorEvent.values.qz != 0)
//                         {
//                             orientationQuat = new Quaternion(
//                                 sensorEvent.values.qx,
//                                 sensorEvent.values.qy,
//                                 sensorEvent.values.qz,
//                                 sensorEvent.values.qw
//                             );
//                         }
//                         else
//                         {
//                             // Otherwise, create a quaternion from Euler angles
//                             orientationQuat = Quaternion.Euler(
//                                 pitch * Mathf.Rad2Deg,
//                                 yaw * Mathf.Rad2Deg,
//                                 roll * Mathf.Rad2Deg
//                             );
//                         }
                        
//                         hasOrientation = true;
//                     }
//                     break;
//             }
//         }
        
//         // If we have both orientation and acceleration data, we can return motion data
//         if (hasOrientation || hasAcceleration)
//         {
//             // For now, assign the same values to both legs
//             // In a real implementation, you'd need to identify which sensor belongs to which leg
//             leftRotation = orientationQuat;
//             leftAcceleration = accelerometerValues;
//             rightRotation = orientationQuat;
//             rightAcceleration = accelerometerValues;
            
//             return true;
//         }
        
//         return false;
//     }
    
//     /// <summary>
//     /// Creates a MotionData object from orientation and acceleration values.
//     /// </summary>
//     public static MotionData CreateMotionData(Quaternion leftRotation, Vector3 leftAcceleration,
//                                             Quaternion rightRotation, Vector3 rightAcceleration)
//     {
//         Vector3 leftEuler = leftRotation.eulerAngles;
//         Vector3 rightEuler = rightRotation.eulerAngles;
        
//         MotionData motionData = new MotionData
//         {
//             legs = new LegsData
//             {
//                 left = new RotationData
//                 {
//                     pitch = leftEuler.x,
//                     yaw = leftEuler.y,
//                     roll = leftEuler.z,
//                     accX = leftAcceleration.x,
//                     accY = leftAcceleration.y,
//                     accZ = leftAcceleration.z
//                 },
//                 right = new RotationData
//                 {
//                     pitch = rightEuler.x,
//                     yaw = rightEuler.y,
//                     roll = rightEuler.z,
//                     accX = rightAcceleration.x,
//                     accY = rightAcceleration.y,
//                     accZ = rightAcceleration.z
//                 }
//             }
//         };
        
//         return motionData;
//     }
// }