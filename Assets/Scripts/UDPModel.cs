using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System;

[Serializable]
public class MotionData
{
    public LegsData legs;
}

[Serializable]
public class LegsData
{
    public LegData left;
    public LegData right;
}

[Serializable]
public class LegData
{
    public float yaw;
    public float pitch;
    public float roll;
    public AccelerationData acc;
    public AccelerationData gravity;
}

[Serializable]
public class AccelerationData
{
    public float x;
    public float y;
    public float z;
}

public class UDPModel : MonoBehaviour
{
    [SerializeField] private int port = 5555; // Match the port in the Python code
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = true;
    private readonly Queue<Action> _mainThreadActions = new Queue<Action>();

    // Single combined event that includes all data (rotation, acceleration, gravity)
    public static event Action<Quaternion, Vector3, Vector3, Quaternion, Vector3, Vector3> OnDataReceived; 
    // Parameters: leftRotation, leftAcceleration, leftGravity, rightRotation, rightAcceleration, rightGravity

    // Legacy method signature kept for compatibility with external code
    public static void OnDataReceivedInvoke(Quaternion leftRotation, Vector3 leftAcceleration, 
                                           Quaternion rightRotation, Vector3 rightAcceleration)
    {
        // Call the new combined event with zero gravity vectors when invoked externally
        if (OnDataReceived != null)
        {
            OnDataReceived.Invoke(leftRotation, leftAcceleration, Vector3.zero, rightRotation, rightAcceleration, Vector3.zero);
        }
    }

    private void Start()
    {
        Application.runInBackground = true;
        InitializeUDP();
    }

    private void InitializeUDP()
    {
        try
        {
            udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            Debug.Log($"UDP listening on port {port}");
            
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    Debug.Log($"Available IP: {ip}");
                }
            }

            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize UDP: {e}");
        }
    }

    private void ReceiveData()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        
        while (isRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string json = Encoding.UTF8.GetString(data);
                
                Debug.Log($"Received from {remoteEndPoint.Address}: {json}");

                lock (_mainThreadActions)
                {
                    _mainThreadActions.Enqueue(() => ProcessJsonData(json));
                }
            }
            catch (Exception e)
            {
                if (isRunning)
                {
                    Debug.LogError($"Error receiving data: {e}");
                }
            }
        }
    }

    private void ProcessJsonData(string json)
    {
        try
        {
            MotionData motionData = JsonUtility.FromJson<MotionData>(json);
            
            // Set default values for both legs
            Quaternion leftRotation = Quaternion.identity;
            Vector3 leftAcceleration = Vector3.zero;
            Vector3 leftGravity = Vector3.zero;
            
            Quaternion rightRotation = Quaternion.identity;
            Vector3 rightAcceleration = Vector3.zero;
            Vector3 rightGravity = Vector3.zero;
            
            bool hasValidData = false;
            
            // Check if legs data exists
            if (motionData.legs != null)
            {
                // Process left leg data if available
                if (motionData.legs.left != null)
                {
                    leftRotation = Quaternion.Euler(
                        motionData.legs.left.pitch,
                        motionData.legs.left.yaw,
                        motionData.legs.left.roll
                    );
                    
                    if (motionData.legs.left.acc != null)
                    {
                        leftAcceleration = new Vector3(
                            motionData.legs.left.acc.x,
                            motionData.legs.left.acc.y,
                            motionData.legs.left.acc.z
                        );
                    }
                    
                    if (motionData.legs.left.gravity != null)
                    {
                        leftGravity = new Vector3(
                            motionData.legs.left.gravity.x,
                            motionData.legs.left.gravity.y,
                            motionData.legs.left.gravity.z
                        );
                    }
                    
                    hasValidData = true;
                }
                
                // Process right leg data if available
                if (motionData.legs.right != null)
                {
                    rightRotation = Quaternion.Euler(
                        motionData.legs.right.pitch,
                        motionData.legs.right.yaw,
                        motionData.legs.right.roll
                    );
                    
                    if (motionData.legs.right.acc != null)
                    {
                        rightAcceleration = new Vector3(
                            motionData.legs.right.acc.x,
                            motionData.legs.right.acc.y,
                            motionData.legs.right.acc.z
                        );
                    }
                    
                    if (motionData.legs.right.gravity != null)
                    {
                        rightGravity = new Vector3(
                            motionData.legs.right.gravity.x,
                            motionData.legs.right.gravity.y,
                            motionData.legs.right.gravity.z
                        );
                    }
                    
                    hasValidData = true;
                }
            }
            
            // Only invoke the event if at least one leg's data is present
            if (hasValidData && OnDataReceived != null)
            {
                OnDataReceived.Invoke(leftRotation, leftAcceleration, leftGravity, rightRotation, rightAcceleration, rightGravity);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing JSON data: {e}\nJSON: {json}");
        }
    }

    private void Update()
    {
        lock (_mainThreadActions)
        {
            while (_mainThreadActions.Count > 0)
            {
                _mainThreadActions.Dequeue()?.Invoke();
            }
        }
    }

    private void OnApplicationQuit()
    {
        isRunning = false;
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(1000);
            if (receiveThread.IsAlive)
            {
                receiveThread.Abort();
            }
        }
        if (udpClient != null)
        {
            udpClient.Close();
        }
    }

    private void OnDisable()
    {
        isRunning = false;
    }
}