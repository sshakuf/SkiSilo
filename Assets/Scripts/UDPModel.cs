using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System;

public class UDPModel : MonoBehaviour
{
    [SerializeField] private int port = 5005;
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = true;
    private readonly Queue<Action> _mainThreadActions = new Queue<Action>();

    public static event Action<Quaternion, Quaternion> OnDataReceived;

    private void Start()
    {
        Application.runInBackground = true;
        InitializeUDP();
    }

    private void InitializeUDP()
    {
        try
        {
            // Allow binding to any available address
            udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            Debug.Log($"UDP listening on port {port}");
            
            // Print all available IP addresses for debugging
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
                string message = Encoding.UTF8.GetString(data);
                
                Debug.Log($"Received data from {remoteEndPoint.Address}: {message}");

                lock (_mainThreadActions)
                {
                    _mainThreadActions.Enqueue(() => ProcessData(message));
                }
            }
            catch (Exception e)
            {
                if (isRunning) // Only log if we're still supposed to be running
                {
                    Debug.LogError($"Error receiving data: {e}");
                }
            }
        }
    }

    private void ProcessData(string message)
    {
        try
        {
            string[] values = message.Split(',');
            if (values.Length == 6)
            {
                if (float.TryParse(values[0], out float leftX) &&
                    float.TryParse(values[1], out float leftY) &&
                    float.TryParse(values[2], out float leftZ) &&
                    float.TryParse(values[3], out float rightX) &&
                    float.TryParse(values[4], out float rightY) &&
                    float.TryParse(values[5], out float rightZ))
                {
                    Quaternion leftRotation = Quaternion.Euler(leftX, leftY, leftZ);
                    Quaternion rightRotation = Quaternion.Euler(rightX, rightY, rightZ);

                    OnDataReceived?.Invoke(leftRotation, rightRotation);
                }
                else
                {
                    Debug.LogError($"Failed to parse values from message: {message}");
                }
            }
            else
            {
                Debug.LogError($"Incorrect number of values in message. Expected 6, got {values.Length}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing data: {e}\nMessage: {message}");
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
            receiveThread.Join(1000); // Wait up to 1 second for the thread to finish
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