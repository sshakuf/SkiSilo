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
    public RotationData left;
    public RotationData right;
}

[Serializable]
public class RotationData
{
    public float pitch;
    public float yaw;
    public float roll;
    public float accX;
    public float accY;
    public float accZ;
}

public class UDPModel : MonoBehaviour
{
    [SerializeField] private int port = 5005;
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = true;
    private readonly Queue<Action> _mainThreadActions = new Queue<Action>();

    // Original event for both legs - kept exactly as is
    public static event Action<Quaternion, Vector3, Quaternion, Vector3> OnDataReceived; // Left (rotation, acceleration) | Right (rotation, acceleration)
    

    // Original method - kept exactly as is
     public static void OnDataReceivedInvoke(Quaternion leftRotation, Vector3 leftAcceleration, 
                                           Quaternion rightRotation, Vector3 rightAcceleration)
    {
        // Invoke the static event in UDPModel
        if (UDPModel.OnDataReceived != null)
        {
            OnDataReceived.Invoke(leftRotation, leftAcceleration, rightRotation, rightAcceleration);
            // Debug.Log("UDPModel.OnDataReceived event invoked from HTTP sensor data");
        }
        else
        {
            // Debug.LogWarning("UDPModel.OnDataReceived is null. Make sure UDPModel is initialized.");
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
            
            if (motionData.legs != null)
            {
                // Set default values for both legs
                Quaternion leftRotation = Quaternion.identity;
                Vector3 leftAcceleration = Vector3.zero;
                Quaternion rightRotation = Quaternion.identity;
                Vector3 rightAcceleration = Vector3.zero;
                
                // Check if left leg data exists and update if present
                if (motionData.legs.left != null)
                {
                    leftRotation = Quaternion.Euler(
                        motionData.legs.left.pitch,
                        motionData.legs.left.yaw,
                        motionData.legs.left.roll
                    );
                    leftAcceleration = new Vector3(
                        motionData.legs.left.accX,
                        motionData.legs.left.accY,
                        motionData.legs.left.accZ
                    );
                }
                
                // Check if right leg data exists and update if present
                if (motionData.legs.right != null)
                {
                    rightRotation = Quaternion.Euler(
                        motionData.legs.right.pitch,
                        motionData.legs.right.yaw,
                        motionData.legs.right.roll
                    );
                    rightAcceleration = new Vector3(
                        motionData.legs.right.accX,
                        motionData.legs.right.accY,
                        motionData.legs.right.accZ
                    );
                }
                
                // Only invoke the event if at least one leg's data is present
                if (motionData.legs.left != null || motionData.legs.right != null)
                {
                    OnDataReceived?.Invoke(leftRotation, leftAcceleration, rightRotation, rightAcceleration);
                }
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