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
    public PositionData position;
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
}

[Serializable]
public class PositionData
{
    public float x;
    public float y;
    public float z;
}

public class UDPModel : MonoBehaviour
{
    [SerializeField] private int port = 5005;
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = true;
    private readonly Queue<Action> _mainThreadActions = new Queue<Action>();

    public static event Action<Quaternion, Quaternion> OnDataReceived;
    public static event Action<Vector3> OnPositionReceived;

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
            
            // Process leg rotations
            if (motionData.legs != null)
            {
                Quaternion leftRotation = Quaternion.Euler(
                    motionData.legs.left.pitch,
                    motionData.legs.left.yaw,
                    motionData.legs.left.roll
                );

                Quaternion rightRotation = Quaternion.Euler(
                    motionData.legs.right.pitch,
                    motionData.legs.right.yaw,
                    motionData.legs.right.roll
                );

                OnDataReceived?.Invoke(leftRotation, rightRotation);
            }

            // Process position
            if (motionData.position != null)
            {
                Vector3 position = new Vector3(
                    motionData.position.x,
                    motionData.position.y,
                    motionData.position.z
                );

                OnPositionReceived?.Invoke(position);
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